# Status Effect System Design

**Document Version**: 1.1
**Created**: 2026-05-23
**Last Updated**: 2026-05-23
**Project**: ASCEND - Survival Permadeath RPG

---

## 1. Overview

Status effects are applied via weapon hits. Each effect has its own independent bar that fills on hit. When a bar reaches its threshold, the effect activates for a fixed duration, then deactivates and the bar resets to 0 — allowing the effect to be applied again.

**Three Status Effects:**
| Effect | Duration | Trigger Condition |
|--------|----------|-------------------|
| Bleeding | 20 sec | Bar fills to threshold |
| Poison | 60 sec | Bar fills to threshold |
| Freezing | 90 sec | Bar fills to threshold |

**Key Rule**: Bar only fills when the status effect is **inactive**. Once triggered, the bar locks at 0 for the entire duration. No refilling while active.

---

## 2. Status Effect Details

### 2.1 Bleeding

| Property | Value |
|----------|-------|
| **Duration** | 20 seconds |
| **Buildup Threshold** | 100 (configurable per weapon) |
| **Effect** | While active, each incoming hit deals normal damage + (3% of target's MaxHP) as bonus damage |
| **Behavior** | No passive tick. Only modifies damage on hits received |

**Example:**
- Enemy MaxHP = 1000
- Enemy is bleeding (20 seconds)
- Enemy gets hit 3 times while bleeding
- Each hit takes normal damage + 30 bonus damage (3% × 1000)

---

### 2.2 Poison

| Property | Value |
|----------|-------|
| **Duration** | 60 seconds |
| **Buildup Threshold** | 80 (configurable per weapon) |
| **Effect** | While active, ALL healing is completely blocked (items, spells, any source) |
| **Behavior** | Affects `Heal()` method — if poisoned, heal does nothing |

**Blocked Healing Sources:**
- Healing items (potions)
- Healing spells
- Passive HP regen (if any)

---

### 2.3 Freezing

| Property | Value |
|----------|-------|
| **Duration** | 90 seconds |
| **Buildup Threshold** | 90 (configurable per weapon) |
| **Effect A** | Stamina recovery × 0.25 (75% reduction) |
| **Effect B** | Incoming stagger damage × 1.5 (50% more stagger damage taken from hits) |
| **Player Note** | Player has no stagger bar, so Effect B does not apply to player |

**Effect B applies to:**
- Enemy stagger bars (Interrupt/Stun) — when frozen, incoming stagger damage is multiplied by 1.5
- Player — no self-stagger system exists, so Effect B is skipped for player

---

## 3. Bar System & Stacking

### 3.1 Bar Filling

Each weapon has static status damage values:
```
WeaponSO:
  bleedingDmg = 20    (flat value added to bar per hit)
  poisonDmg = 10
  freezeDmg = 15
```

On hit, bar accumulates:
```
currentAccumulation += weaponStatusDmg × (1 - target.StatusResistance)
```

**StatusResistance formula** (from derived stats):
```
StatusResistance = (ARCCurve × 70) + (VITCurve × 30)
```
- 30% StatusResistance → weapon's 20 bleeding dmg adds 14 to bar

### 3.2 Bar Behavior

**Cycle:**
```
[INACTIVE] Bar fills on hit
    ↓
Bar reaches threshold
    ↓
[ACTIVE] Effect triggers, bar LOCKS at 0
    ↓
Duration ends
    ↓
[INACTIVE] Bar resets to 0, can fill again
```

### 3.3 Stacking Rule

If a status effect is applied while already active:
- Duration resets to full duration
- Bar resets to 0
- No double-stacking of damage or intensity

---

## 4. Components

### 4.1 `StatusEffectSO` (ScriptableObject)

Defines static data per status type.

```csharp
[CreateAssetMenu(fileName = "Bleeding", menuName = "ASCEND/StatusEffect/Bleeding")]
public class StatusEffectSO : ScriptableObject
{
    public string effectName;
    public float buildupThreshold = 100f;
    public float duration = 20f;

    [Header("Bleeding")]
    public float bonusDamagePercentMaxHP = 3f;  // % of target MaxHP added per hit

    [Header("Poison")]
    public bool blocksHealing = true;

    [Header("Freezing")]
    public float staminaRecoveryMultiplier = 0.25f;   // 75% reduction
    public float selfStaggerDamageMultiplier = 1.5f; // 50% more stagger taken
}
```

### 4.2 `StatusEffectHandler` (Component)

Attached to same GameObject as `PlayerStats` or `EnemyStats`. Manages all 3 status bars independently.

**Per-status runtime fields:**
```csharp
[Serializable]
public class StatusBar
{
    public float currentAccumulation;
    public float currentThreshold;
    public bool isActive;
    public float timeRemaining;
}
```

**Main Methods:**
```csharp
public class StatusEffectHandler : MonoBehaviour
{
    public StatusBar bleeding = new StatusBar();
    public StatusBar poison = new StatusBar();
    public StatusBar freezing = new StatusBar();

    // Events use StatusEffectType (not CoreStatType)
    public event Action<StatusEffectType> OnStatusTriggered;
    public event Action<StatusEffectType> OnStatusEnded;

    // Called by WeaponHitbox on hit
    public void ApplyStatusHit(WeaponSO weapon, float statusResistancePercent);

    // Internal
    private void TriggerStatus(StatusBar bar, StatusEffectSO data, StatusEffectType effectType);
    private void EndStatus(StatusBar bar, StatusEffectType effectType);
    private void Update(); // ticks duration countdown
}
```

**TriggerStatus() Logic:**
```
1. bar.isActive = true
2. bar.timeRemaining = effectSO.duration
3. bar.currentAccumulation = 0  (bar shows 0 while active)
4. Fire event OnStatusTriggered(effectType)
```

**ApplyStatusHit() Logic:**
```
On every hit:
  if (!bleeding.isActive)
    bleeding.currentAccumulation += weapon.bleedingDmg × (1 - statusResistance)
    if (bleeding.currentAccumulation >= bleeding.currentThreshold)
      TriggerStatus(bleeding, BleedingSO)

  if (!poison.isActive)
    ...

  if (!freezing.isActive)
    ...
```

**Effect Application (in entity TakeDamage/Heal/Stagger methods):**
```
PlayerStats.TakeDamage():
  if (statusEffectHandler.bleeding.isActive)
    finalDamage += target.MaxHP × 0.03

PlayerStats.Heal():
  if (statusEffectHandler.poison.isActive)
    return 0  // blocked

EnemyStats.TakeStagger():
  if (statusEffectHandler.freezing.isActive)
    staggerDamage × 1.5
```

### 4.3 Weapon Changes (`WeaponSO`)

```csharp
[Header("Status Effects")]
public float bleedingDmg = 0f;
public float poisonDmg = 0f;
public float freezeDmg = 0f;
```

**Example weapons:**
```
Steel Sword:
  bleedingDmg = 20
  poisonDmg = 0
  freezeDmg = 0

Poison Dagger:
  bleedingDmg = 5
  poisonDmg = 25
  freezeDmg = 0

Frost Axe:
  bleedingDmg = 15
  poisonDmg = 0
  freezeDmg = 20

War Hammer:
  bleedingDmg = 10
  poisonDmg = 0
  freezeDmg = 5
```

### 4.4 `WeaponHitbox` Changes

```csharp
private void OnTriggerEnter(Collider other)
{
    // ... existing damage/stagger logic ...

    // Apply status effects (use collider's GameObject directly)
    var handler = other.GetComponent<StatusEffectHandler>();
    if (handler != null)
    {
        float statusResistance = GetTargetStatusResistance(other.gameObject);
        handler.ApplyStatusHit(_weapon, statusResistance);
    }
}

private float GetTargetStatusResistance(GameObject targetGo)
{
    if (targetGo.TryGetComponent<PlayerStats>(out var ps))
        return ps.StatusResistance;
    if (targetGo.TryGetComponent<EnemyStats>(out var es))
        return es.StatusResistance;
    return 0f;
}
```

---

## 5. Interface Considerations

### 5.1 Player

Player HUD shows 3 status icons (Bleeding/Poison/Freezing) when active, each with a countdown ring showing time remaining. The bar itself is not shown in HUD — only the active effect icons.

### 5.2 Enemy

No HUD bar for enemy status. Visual VFX (color tint, particle effect) indicates active status. HP bar already exists.

### 5.3 Status Resistance

Both Player and Enemy have StatusResistance derived stat that reduces status damage added to bars per hit.

---

## 6. Interaction With Existing Systems

| System | Interaction |
|--------|-------------|
| `IDamageable.TakeDamage()` | Bleeding adds bonus damage if bleeding is active |
| `IDamageable.TakeStagger()` | Freezing multiplies stagger damage if freezing is active |
| `PlayerStats.Heal()` | Poison blocks healing if poisoned |
| `PlayerStats.Stamina Recovery` | Freezing reduces stamina recovery rate |
| `EnemyStats` stagger bars | Freezing increases stagger damage taken |
| `WeaponHitbox` | Calls `ApplyStatusHit()` on hit |

---

## 7. Default Values Summary

| Status | Threshold | Duration | Key Effect |
|--------|-----------|----------|------------|
| Bleeding | 100 | 20 sec | +3% MaxHP per hit received |
| Poison | 80 | 60 sec | Blocks all healing |
| Freezing | 90 | 90 sec | -75% stamina recovery, +50% stagger taken |

---

**Status**: Design approved — implemented (2026-05-23)

---

## Appendix: Enum Architecture

Status effects use a **separate enum** from core stats to maintain clean separation:

```
CoreStatType { VIT, END, AGI, STR, DEX, ARC }         ← stats only
StatusEffectType { Bleeding, Poison, Freezing }       ← status effects only
```

**Files:**
- `CoreStatType.cs` — pure stats enum
- `StatusEffectType.cs` — separate enum for status effect type identification

This ensures status effects are never confused with character stats in systems like `IStatable`, `CentralizedStatSystem.RecalculateDerived()`, and event callbacks.