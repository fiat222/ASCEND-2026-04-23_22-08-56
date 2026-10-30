# ASCEND — Agent Instructions

## Session Rules (ALWAYS ACTIVE)
1. **Read context.md first** — always read `context.md` at session start for full project state
2. **Write progress** — after every meaningful change, append to Progress Log in `context.md`
3. **Plan before building** — for major features, outline the plan and confirm before implementing

## context.md
- Path: `context.md` (project root, gitignored — local only, never committed)
- Format for progress entries:
  ```
  ### YYYY-MM-DD (Tool: OpenCode)
  - [x] What was done — file/system affected
  ```
- Always include the tool name so Claude Code and OpenCode entries are distinguishable

## Project Overview
- **Game:** ASCEND — survival roguelite (Muck-inspired), Unity 2022.3 LTS
- **Networking:** PurrNet (UDP, host-authority P2P). Host = server. 2–10 players or solo.
- **Platform:** PC/Windows primary. Demo itch.io → Steam.
- **Branch model:** `main` = stable, `fiat_branch` = active dev

## Key Script Locations
```
Assets/Scripts/
  Core/Interfaces/    IDamageable.cs
  Data/               WeaponSO.cs, StatConfig.cs, StatCurveCalculator.cs
  Dev/
    Player/           PlayerMovement.cs, PlayerCombat.cs, PlayerStats.cs
    Enemy/            EnemyAI.cs, EnemyStats.cs
    Weapons/          WeaponHitbox.cs, EnemyHitbox.cs, BowStringController.cs, ProjectilePrefab.cs
    Weapons/Magic/    StaffSpell.cs, StaffPillar.cs, WandLaser.cs, MagicBallDamage.cs
  Network/
    Lobby/            NetworkLobby.cs, PlayerSpawner.cs
    Player/           NetworkPlayerMovement.cs, NetworkPlayerCombat.cs, NetworkPlayerStats.cs, NetworkInventory.cs
    Enemy/            NetworkEnemyStats.cs
  Inventory/          Inventory.cs, ItemSO.cs, ItemDatabase.cs, WorldItem.cs, InventoryUI.cs, InventorySlotUI.cs
  UI/HUD/             PlayerHUD.cs, PlayerStatPanel.cs
```

## Combat Phase Status
| Phase | Status | Content |
|-------|--------|---------|
| 1 | ✅ Done | Stats, hitbox, damage formula, enemy HP |
| 2 | ✅ Done | Hit react, death anim+sink, HP bar HUD, TAB stat panel |
| 3 | ✅ Done | 1H parry (0.3s), hold-block, 2H/Shield block stamina drain, guard break |
| 4 | ⚠️ Code done | Network HP sync + damage over wire — Unity Editor setup pending |
| 5 | ⏳ Pending | Enemy attack anim sync, stamina/mana sync, projectile network sync, respawn |

## Networking Patterns
- **Damage:** host-auth. `TakeDamage()` checks `isServer` → direct on host, `[ServerRpc(requireOwnership:false)]` from clients
- **HP sync:** `[ObserversRpc]` broadcasts HP to all clients after host applies damage
- **Component order:** NetworkEnemyStats/NetworkPlayerStats ABOVE local stats in Inspector (found first by hitbox)
- **EnemyAI:** host-only guard `if(!isOffline && !isServer) return`
- **No double-damage:** projectile ObserversRpc uses `visualOnly=true` on non-owners
- PurrNet: `NetworkManager.main.isServer/.isOffline/.isHost`; `StartHost()` = server+client

## Animator Parameters (Player)
- Base Layer: `MoveX` (float), `MoveY` (float), `IsRun` (bool), `Jump` (trigger), `IsGround` (bool)
- Upper Layer: `Attack1H`, `Attack2H`, `AttackMagic`, `AttackShield`, `AttackSpear` (triggers)
- Idle bools: `idle1HParam`, `idle2HParam`, `idleMagicParam`, `idleShieldParam`, `idleSpearParam`

## Enemy Animator Pattern (Skeleton — reuse for all enemies)
- Single layer, 2D BlendTree (MoveX + MoveY), Any-State transitions for attacks/damage/death
- Standard params: `MoveX`, `MoveY`, `Attack_Slash01`, `Attack_Slash02`, `Attack_Stab`, `GetDamaged`, `Die`
- Clips live at: `Assets/Import Asset/SazenGames/Skeleton/Art/Animations/`

## Current TODOs (see context.md for full list)
### Phase 4 Unity Editor Setup
- [ ] Network Charactor prefab: add NetworkPlayerStats (ABOVE PlayerStats) + CinemachineVirtualCamera child
- [ ] Enemy prefab: add NetworkIdentity + NetworkTransform + NetworkEnemyStats (ABOVE EnemyStats)
- [ ] Scene enemies: add NetworkIdentity
### Inventory
- [ ] Create ItemDatabase asset, WorldItem prefab, wire to NetworkInventory + WorldItem scripts
- [ ] Add InventoryUI Canvas to scene
### Other
- [ ] Magic projectile: destroy on hit + Impact prefab
- [ ] Phase 5: enemy attack anim sync, stamina/mana sync, respawn
- [ ] Steam transport
