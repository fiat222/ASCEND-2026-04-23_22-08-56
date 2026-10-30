# ASCEND — Network Architecture Design
**Date:** 2026-05-24  
**Scope:** 1–10 player PvE, host-client (PurrNet), Unity 2022.3 LTS  
**Status:** Approved

---

## Goal

Replace scattered RPCs across 4+ NetworkBehaviours with a clean, domain-based network architecture. All network traffic auditable by domain. Gameplay modules contain zero RPCs. Host validates all combat.

---

## Architecture Overview

```
GameScene (scene objects)
├── CombatNetworkService      ← animation triggers, hit reporting, damage sync
├── InventoryNetworkService   ← pickup, drop, equip, hotbar
├── WorldNetworkService       ← WorldItem spawn/despawn, interactables
└── SpawnNetworkService       ← player spawn, respawn, death

Player Prefab (spawned per player)
├── PlayerAgent               ← thin NetworkBehaviour, registry, wiring (~60 lines)
├── CombatModule              ← pure MonoBehaviour, input + local anim, no RPCs
├── InventoryModule           ← pure MonoBehaviour, item/hotbar logic, no RPCs
├── StatsModule               ← pure MonoBehaviour, HP/stamina/mana, no RPCs
└── NetworkTransform          ← PurrNet built-in, movement sync
```

---

## Core Rules

1. **No RPCs on gameplay modules.** CombatModule, InventoryModule, StatsModule are pure MonoBehaviours.
2. **No custom actorId passed from client.** Server infers caller from `RpcInfo.sender` (PurrNet built-in).
3. **Enum/byte opcodes only.** No string params in RPCs — smaller packets, type-safe.
4. **Local prediction for animation.** Owner plays animation immediately; network sync follows.
5. **NetworkTransform for movement.** Do not reimplement position sync.
6. **Server validates hit timing and range** before applying damage.
7. **`requireOwnership: false`** only on services — players call service RPCs they don't own.
8. **Frequent stats (HP/stamina) use SyncVar-style** via PurrNet NetworkVariables where supported, or targeted RPC on change only.

---

## Enum Payloads

```csharp
public enum AnimTrigger : byte
{
    Attack1H, Attack2H, AttackMagic, AttackSpear,
    StaffSkill, WandSkill, GuardBreak,
    Counter2HBase, Counter2H, ShieldSwordParry, SpearThrow
}

public enum AnimBool : byte
{
    Is1HBlock, Is2HBlock, IsShieldBlock, IsShieldSwordBlock,
    IsWandHold, HoldBow, AttackBow, HoldSpear, IsInAction,
    Is1HIdle, Is2HIdle, IsSpearIdle, IsBowIdle, IsShieldIdle
}

public enum EquipSlot : byte { MainHand, OffHand }
```

---

## PlayerAgent

**File:** `Assets/Scripts/Network/Player/PlayerAgent.cs`  
**Extends:** `NetworkBehaviour`

Responsibilities:
- Register/unregister in `static Dictionary<PlayerID, PlayerAgent> Registry`
- Init all gameplay modules on spawn
- Expose animation API: `PlayTrigger(AnimTrigger)`, `SetBool(AnimBool, bool)`, `EquipWeapon(WeaponSO, EquipSlot)`
- Hold references to CombatModule, InventoryModule, StatsModule

```csharp
protected override void OnSpawned()
{
    _anim = GetComponentInChildren<Animator>();
    if (owner.HasValue) Registry[owner.Value] = this;
    _combat.Init(this);
    _inventory.Init(this);
    _stats.Init(this);
}

protected override void OnDespawned()
{
    if (owner.HasValue) Registry.Remove(owner.Value);
}
```

Does NOT contain any `[ServerRpc]` or `[ObserversRpc]`.

---

## CombatNetworkService

**File:** `Assets/Scripts/Network/Services/CombatNetworkService.cs`  
**Extends:** `NetworkBehaviour` (scene-placed singleton)

### RPCs

```
SendTrigger(AnimTrigger)        [ServerRpc(requireOwnership:false)]
  → BroadcastTrigger(NetworkID actor, AnimTrigger)  [ObserversRpc]

SendBool(AnimBool, bool)        [ServerRpc(requireOwnership:false)]
  → BroadcastBool(NetworkID actor, AnimBool, bool)  [ObserversRpc]

ReportHit(NetworkID target, float rawDmg, WeaponStats stats)  [ServerRpc(requireOwnership:false)]
  → host validates range + timing
  → applies damage to target IDamageable
  → BroadcastHpSync(NetworkID target, float newHp)  [ObserversRpc]

SyncEquip(int dbIndex, EquipSlot slot)  [ServerRpc(requireOwnership:false)]
  → BroadcastEquip(NetworkID actor, int dbIndex, EquipSlot)  [ObserversRpc]
```

### Hit validation (server-side)
- Check target is alive
- Check attacker's last attack timestamp (debounce — prevent spam)
- Check distance ≤ weapon range + tolerance (0.5m buffer for latency)
- If valid: apply damage, broadcast HP sync

### Local prediction
Owner CombatModule plays animation locally before server confirms. If server rejects (impossible for PvE host-client — host is trusted), animation plays anyway. No rollback needed for PvE.

---

## InventoryNetworkService

**File:** `Assets/Scripts/Network/Services/InventoryNetworkService.cs`  
**Extends:** `NetworkBehaviour` (scene-placed singleton)

### RPCs

```
RequestPickup(NetworkID worldItemId)    [ServerRpc(requireOwnership:false)]
  → validates proximity (≤ pickupRadius)
  → despawns WorldItem
  → GrantItem(PlayerID, int dbIndex, int qty)  [TargetRpc]

RequestDrop(int hotbarSlot)             [ServerRpc(requireOwnership:false)]
  → removes from owner inventory (via PlayerAgent lookup)
  → SpawnWorldItemRpc(int dbIndex, int qty, Vector3 pos)  [ServerRpc on WorldNetworkService]

RequestSelectSlot(byte slot)            [ServerRpc(requireOwnership:false)]
  → updates selected slot
  → SyncEquip via CombatNetworkService

GrantItem (TargetRpc) → owner only, updates local InventoryModule
```

---

## WorldNetworkService

**File:** `Assets/Scripts/Network/Services/WorldNetworkService.cs`  
**Extends:** `NetworkBehaviour` (scene-placed singleton)

### RPCs

```
SpawnWorldItem(int dbIndex, int qty, Vector3 pos)  [ServerRpc(requireOwnership:false)]
  → server instantiates WorldItem prefab
  → NetworkManager.Spawn(go)

DespawnWorldItem(NetworkID id)  [ServerRpc(requireOwnership:false)]
  → server validates, NetworkManager.Despawn
```

---

## SpawnNetworkService

**File:** `Assets/Scripts/Network/Services/SpawnNetworkService.cs`  
**Extends:** `NetworkBehaviour` (scene-placed singleton)

### RPCs

```
BroadcastDeath(NetworkID victim)      [ObserversRpc] — all clients play death
RequestRespawn()                       [ServerRpc(requireOwnership:false)]
  → host picks spawn point
  → TeleportRpc(PlayerID, Vector3)   [TargetRpc] — owner repositions
  → BroadcastRespawn(NetworkID)      [ObserversRpc] — all clients show respawn
```

---

## Module Contracts

All modules extend `PlayerModule`:

```csharp
public abstract class PlayerModule : MonoBehaviour
{
    protected PlayerAgent Agent { get; private set; }
    protected bool IsOwner => Agent.isOwner;

    public virtual void Init(PlayerAgent agent)
    {
        Agent = agent;
        if (agent.isOwner) OnOwnerInit();
        else               OnObserverInit();
    }

    protected virtual void OnOwnerInit()   {}
    protected virtual void OnObserverInit() {}
}
```

**CombatModule** — reads input (owner), calls `CombatNetworkService.Instance.SendTrigger()`, plays local animation for prediction.  
**InventoryModule** — manages local inventory data, calls `InventoryNetworkService.Instance.Request*()`.  
**StatsModule** — subscribes to HP/stamina changes from service broadcasts, updates HUD.

---

## Migration Plan (current → target)

| Current | Becomes |
|---|---|
| `NetworkPlayerCombat` | `CombatModule` (pure) + `CombatNetworkService` (RPCs) |
| `NetworkInventory` | `InventoryModule` (pure) + `InventoryNetworkService` (RPCs) |
| `NetworkPlayerStats` | `StatsModule` (pure) + HP sync via `CombatNetworkService` |
| `NetworkPlayerMovement` | `MovementModule` (pure) + `NetworkTransform` (built-in) |
| `WorldItem` (RPCs) | `WorldNetworkService` owns spawn/despawn RPCs |

Migration order:
1. `PlayerAgent` + `PlayerModule` base — foundation
2. `CombatNetworkService` — most visible, unblocks attack sync fixes
3. `InventoryNetworkService` + `WorldNetworkService`
4. `SpawnNetworkService` + `StatsModule`
5. Remove old NetworkBehaviour scripts

---

## File Summary

| File | Type | New/Modify |
|---|---|---|
| `Network/Player/PlayerAgent.cs` | NetworkBehaviour | NEW |
| `Network/Player/PlayerModule.cs` | MonoBehaviour base | NEW |
| `Network/Services/CombatNetworkService.cs` | NetworkBehaviour | NEW |
| `Network/Services/InventoryNetworkService.cs` | NetworkBehaviour | NEW |
| `Network/Services/WorldNetworkService.cs` | NetworkBehaviour | NEW |
| `Network/Services/SpawnNetworkService.cs` | NetworkBehaviour | NEW |
| `Network/Enums/AnimTrigger.cs` | Enum | NEW |
| `Network/Enums/AnimBool.cs` | Enum | NEW |
| `Network/Enums/EquipSlot.cs` | Enum | NEW |
| `Dev/Player/CombatModule.cs` | MonoBehaviour | NEW (from NetworkPlayerCombat) |
| `Dev/Player/InventoryModule.cs` | MonoBehaviour | NEW (from NetworkInventory) |
| `Dev/Player/StatsModule.cs` | MonoBehaviour | NEW (from NetworkPlayerStats) |
| `Dev/Player/MovementModule.cs` | MonoBehaviour | NEW (from NetworkPlayerMovement) |
| `Network/Player/NetworkPlayerCombat.cs` | — | REMOVE (after migration) |
| `Network/Player/NetworkInventory.cs` | — | REMOVE (after migration) |
| `Network/Player/NetworkPlayerStats.cs` | — | REMOVE (after migration) |
| `Network/Player/NetworkPlayerMovement.cs` | — | REMOVE (after migration) |

---

## Out of Scope

- Anti-cheat beyond server hit validation (not needed for PvE host-client)
- Interest management / spatial partitioning (not needed for 10 players)
- Dedicated server support (host-client only)
- Client-side rollback/reconciliation (PvE prediction is fire-and-forget)
