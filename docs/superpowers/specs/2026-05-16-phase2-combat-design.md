# Phase 2 Combat — Design Spec
**Date:** 2026-05-16  
**Branch:** feat/mageskill  
**Status:** Approved, ready for implementation

---

## Scope

Four features, all local dev (`Dev/` scripts only — no network):

1. Enemy Hit Reaction anim
2. Enemy Death anim + sink + Destroy
3. Player HP bar (screen-space HUD)
4. Player Stat Panel (TAB)

Out of scope: Enemy FOV (kept as distance-only).

---

## 1. Enemy Hit Reaction

### Approach
Event-driven. `EnemyStats` fires events → `EnemyAI` sets animator trigger.

### EnemyStats changes
- Add `public event Action OnHit` — fires on every `TakeDamage` call (before death check)
- Add `public event Action OnInterrupted` — fires when interrupt threshold crossed (inside `InterruptRoutine` start)
- Add `public event Action OnStunned` — fires when stun threshold crossed (inside `StunRoutine` start)

### EnemyAI changes
- Subscribe to all three events in `Start()`
- `OnHit` handler: if `_state != State.Attack` → `_anim.SetTrigger(hitReactParam)`
- `OnInterrupted` / `OnStunned` handlers: always `_anim.SetTrigger(hitReactParam)` (overrides attack)
- New serialized field: `[SerializeField] private string hitReactParam = "GetDamaged"`

### Animator (manual Unity Editor setup)
- Add `GetDamaged` trigger parameter
- Add `GetDamaged` state on Base Layer
- Any State → `GetDamaged`: condition trigger `GetDamaged`
- `GetDamaged` → exit: Exit Time = 1.0, transition duration 0.1s
- No Exit Time on entry transition

---

## 2. Enemy Death Anim + Sink

### Approach
Animation event on last frame of Death clip calls `OnDeathAnimEnd()` on `EnemyAI`. That starts `SinkAndDestroy()` coroutine.

### EnemyAI changes
- `OnDied()` callback (replaces `Destroy(gameObject)`):
  1. `_state = State.Dead`
  2. `StopAllCoroutines()`
  3. `_hitbox?.DisableHitbox()`
  4. `_anim.SetTrigger(deathParam)`
  5. (animation event fires `OnDeathAnimEnd()` at clip end)

- `public void OnDeathAnimEnd()`:
  - Starts `SinkAndDestroy()` coroutine

- `IEnumerator SinkAndDestroy()`:
  - Move `transform.position` downward each frame: `transform.position -= Vector3.up * (sinkDepth / sinkDuration) * Time.deltaTime`
  - Elapsed timer → when `>= sinkDuration` → `Destroy(gameObject)`

- New serialized fields:
  ```csharp
  [SerializeField] private string deathParam   = "Die";
  [SerializeField] private float  sinkDuration = 1.5f;
  [SerializeField] private float  sinkDepth    = 2f;
  ```

### Animator (manual Unity Editor setup)
- Add `Die` trigger parameter
- Add `Death` state on Base Layer
- Any State → `Death`: condition trigger `Die`
- `Death` → (no exit transition) — `Destroy` handles cleanup
- Add animation event `OnDeathAnimEnd` at last frame of Death clip

---

## 3. Player HP Bar (Screen-Space HUD)

### PlayerStats changes
- Add `public event Action<float, float> OnHpChanged` (current, max)
- Fire in `TakeDamage()` and `Heal()` after updating `currentHp`

### New script: `Assets/Scripts/UI/HUD/PlayerHUD.cs`
```
PlayerHUD : MonoBehaviour
  [SerializeField] PlayerStats playerStats
  [SerializeField] Slider      hpSlider

  Start()       → subscribe OnHpChanged, init slider
  OnDestroy()   → unsubscribe
  OnHpChanged(current, max) → hpSlider.value = current / max
```

- `hpSlider.minValue = 0`, `hpSlider.maxValue = 1` (normalized)
- Init: set slider value from `playerStats.CurrentHp / playerStats.MaxHP`

### Unity Editor setup
- Screen Space — Overlay Canvas
- Slider component bottom-left corner
- `PlayerHUD` component on Canvas or child GameObject
- Assign `PlayerStats` and `Slider` in Inspector

---

## 4. Player Stat Panel (TAB)

### PlayerStats additions (new readonly properties)
```csharp
public float StaminaRecovery => 10f  + StatCurveCalculator.Curve(AGI) * 15f;
public float EquipLoad       => 50f  + StatCurveCalculator.Curve(END) * 200f;
public float CritRate        => 5f   + StatCurveCalculator.Curve(DEX) * 30f;   // %
public float CritDamage      => 1.5f + StatCurveCalculator.Curve(STR) * 0.5f;  // multiplier
public float MovementSpeed   => 100f + StatCurveCalculator.Curve(AGI) * 10f;
```

### New script: `Assets/Scripts/UI/HUD/PlayerStatPanel.cs`
```
PlayerStatPanel : MonoBehaviour
  [SerializeField] PlayerStats playerStats
  [SerializeField] GameObject  panel
  // Core stat labels (TMP_Text)
  vitText, endText, agiText, strText, dexText, arcText
  // Derived stat labels (TMP_Text)
  hpText, staminaText, stamRecText, equipLoadText,
  critRateText, critDmgText, manaText, moveSpeedText

  Update()  → if Input.GetKeyDown(KeyCode.Tab) → TogglePanel()
  TogglePanel() → panel.SetActive(!panel.activeSelf); if active → Refresh()
  Refresh() → set all TMP_Text strings from playerStats properties
```

### Panel layout (two-column)
```
┌── Core Stats ──┬── Derived ──────────┐
│ VIT  [val]     │ HP         [val]    │
│ END  [val]     │ Stamina    [val]    │
│ AGI  [val]     │ Stam.Rec   [val]/s  │
│ STR  [val]     │ Equip Load [val]    │
│ DEX  [val]     │ Crit Rate  [val]%   │
│ ARC  [val]     │ Crit Dmg   [val]x   │
│                │ Mana       [val]    │
│                │ Move Speed [val]    │
└────────────────┴────────────────────┘
```

### Unity Editor setup
- Screen Space — Overlay Canvas (same Canvas as HP bar)
- Panel GameObject toggled active/inactive
- All TMP_Text fields assigned in Inspector
- Panel starts inactive (`SetActive(false)` in Awake)

---

## Files Changed / Created

| File | Change |
|------|--------|
| `Dev/Enemy/EnemyStats.cs` | Add `OnHit`, `OnInterrupted`, `OnStunned` events |
| `Dev/Enemy/EnemyAI.cs` | Hit react + death anim + sink logic |
| `Dev/Player/PlayerStats.cs` | Add `OnHpChanged` event + 5 new derived properties |
| `UI/HUD/PlayerHUD.cs` | NEW — HP bar controller |
| `UI/HUD/PlayerStatPanel.cs` | NEW — TAB stat panel |

## Unity Editor Work (manual)
- Enemy animator: add `GetDamaged` + `Die` triggers, states, transitions
- Death clip: add `OnDeathAnimEnd` animation event at last frame
- Canvas: build HP slider + stat panel UI, assign references in Inspector
