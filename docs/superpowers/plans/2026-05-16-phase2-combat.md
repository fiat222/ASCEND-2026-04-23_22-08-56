# Phase 2 Combat Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add enemy hit reaction + death animation, player HP bar HUD, and TAB stat panel to the local dev combat system.

**Architecture:** Event-driven — `EnemyStats` fires C# events on hit/interrupt/stun/death; `EnemyAI` subscribes and drives animator triggers. Player UI scripts subscribe to `PlayerStats` events. No network code touched.

**Tech Stack:** Unity C#, TextMeshPro, UnityEngine.UI (Slider), Unity Animator triggers.

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `Assets/Scripts/Dev/Enemy/EnemyStats.cs` | Modify | Add `OnHit`, `OnInterrupted`, `OnStunned` events |
| `Assets/Scripts/Dev/Enemy/EnemyAI.cs` | Modify | Hit react + death anim + sink coroutine |
| `Assets/Scripts/Dev/Player/PlayerStats.cs` | Modify | Add `OnHpChanged` event + 5 derived stat properties |
| `Assets/Scripts/UI/HUD/PlayerHUD.cs` | Create | HP slider HUD — subscribes to `OnHpChanged` |
| `Assets/Scripts/UI/HUD/PlayerStatPanel.cs` | Create | TAB stat panel — reads all PlayerStats properties |

---

## Task 1: EnemyStats — Add hit/interrupt/stun events

**Files:**
- Modify: `Assets/Scripts/Dev/Enemy/EnemyStats.cs`

- [ ] **Step 1: Add three new events below `OnDied`**

Open `Assets/Scripts/Dev/Enemy/EnemyStats.cs`. Find line:
```csharp
public event Action OnDied;
```
Add after it:
```csharp
public event Action OnHit;
public event Action OnInterrupted;
public event Action OnStunned;
```

- [ ] **Step 2: Fire `OnHit` inside `TakeDamage`**

Find `TakeDamage`. Current code:
```csharp
public void TakeDamage(float rawDamage)
{
    if (!IsAlive) return;

    float finalDmg = rawDamage * (100f / (100f + def)) * _activeDamageBonus;
    currentHp = Mathf.Max(0f, currentHp - finalDmg);

    AccumulateStagger(rawDamage);
    RefreshSlider();

    if (currentHp <= 0f)
        OnDied?.Invoke();
}
```
Replace with:
```csharp
public void TakeDamage(float rawDamage)
{
    if (!IsAlive) return;

    float finalDmg = rawDamage * (100f / (100f + def)) * _activeDamageBonus;
    currentHp = Mathf.Max(0f, currentHp - finalDmg);

    AccumulateStagger(rawDamage);
    RefreshSlider();
    OnHit?.Invoke();

    if (currentHp <= 0f)
        OnDied?.Invoke();
}
```

Note: `OnHit` fires after `currentHp` is already updated. If the blow is lethal, `currentHp == 0` → `IsAlive == false` when `EnemyAI` checks it in the handler (used in Task 2 to skip the reaction on death blow).

- [ ] **Step 3: Fire `OnInterrupted` at start of `InterruptRoutine`**

Find `InterruptRoutine`. Current:
```csharp
private IEnumerator InterruptRoutine()
{
    _isInterrupted = true;
    yield return new WaitForSeconds(interruptDuration);
    _isInterrupted = false;
}
```
Replace with:
```csharp
private IEnumerator InterruptRoutine()
{
    _isInterrupted = true;
    OnInterrupted?.Invoke();
    yield return new WaitForSeconds(interruptDuration);
    _isInterrupted = false;
}
```

- [ ] **Step 4: Fire `OnStunned` at start of `StunRoutine`**

Find `StunRoutine`. Current:
```csharp
private IEnumerator StunRoutine()
{
    _isStunned       = true;
    _activeDamageBonus = stunDamageBonus;

    yield return new WaitForSeconds(stunDuration);
    ...
}
```
Replace with:
```csharp
private IEnumerator StunRoutine()
{
    _isStunned         = true;
    _activeDamageBonus = stunDamageBonus;
    OnStunned?.Invoke();

    yield return new WaitForSeconds(stunDuration);
    ...
}
```

- [ ] **Step 5: Commit**

```bash
rtk git add Assets/Scripts/Dev/Enemy/EnemyStats.cs
rtk git commit -m "feat: add OnHit/OnInterrupted/OnStunned events to EnemyStats"
```

---

## Task 2: EnemyAI — Hit reaction

**Files:**
- Modify: `Assets/Scripts/Dev/Enemy/EnemyAI.cs`

- [ ] **Step 1: Add `hitReactParam` serialized field**

In `EnemyAI`, find `[Header("Animation Params")]` block. Add one field at the end:
```csharp
[SerializeField] private string hitReactParam    = "GetDamaged";
```
Result:
```csharp
[Header("Animation Params")]
[SerializeField] private string moveXParam         = "MoveX";
[SerializeField] private string moveYParam         = "MoveY";
[SerializeField] private string attackStabParam    = "Attack_Stab";
[SerializeField] private string attackSlash01Param = "Attack_Slash01";
[SerializeField] private string attackSlash02Param = "Attack_Slash02";
[SerializeField] private string hitReactParam      = "GetDamaged";
```

- [ ] **Step 2: Subscribe to hit events in `Start()`**

Find `Start()`:
```csharp
private void Start()
{
    _anim   = GetComponent<Animator>();
    _stats  = GetComponent<EnemyStats>();
    _hitbox = GetComponentInChildren<EnemyHitbox>();
    _hitbox?.Setup(attackDamage);
    _stats.OnDied += OnDied;
    FindNearestPlayer();
}
```
Replace with:
```csharp
private void Start()
{
    _anim   = GetComponent<Animator>();
    _stats  = GetComponent<EnemyStats>();
    _hitbox = GetComponentInChildren<EnemyHitbox>();
    _hitbox?.Setup(attackDamage);
    _stats.OnDied        += OnDied;
    _stats.OnHit         += OnHitHandler;
    _stats.OnInterrupted += OnInterruptedHandler;
    _stats.OnStunned     += OnStunnedHandler;
    FindNearestPlayer();
}
```

- [ ] **Step 3: Add the three handler methods**

Add after the `FindNearestPlayer()` method (bottom of file, before closing `}`):
```csharp
// ── Hit Reaction ───────────────────────────────────────────────────────────

private void OnHitHandler()
{
    if (!_stats.IsAlive) return;
    if (_state == State.Attack) return;
    _anim.SetTrigger(hitReactParam);
}

private void OnInterruptedHandler() => _anim.SetTrigger(hitReactParam);
private void OnStunnedHandler()     => _anim.SetTrigger(hitReactParam);
```

- [ ] **Step 4: Commit**

```bash
rtk git add Assets/Scripts/Dev/Enemy/EnemyAI.cs
rtk git commit -m "feat: enemy hit reaction anim via EnemyStats events"
```

- [ ] **Step 5: Unity Editor — animator setup (manual)**

In the enemy's Animator Controller:
1. Add `GetDamaged` **Trigger** parameter
2. Add state `GetDamaged` on Base Layer (assign your hit-react clip)
3. Any State → `GetDamaged`: condition = trigger `GetDamaged`, uncheck "Can Transition To Self"
4. `GetDamaged` → exit: Exit Time = 1.0, transition duration = 0.1s, no conditions
5. Play in Editor, hit enemy with player weapon → hit react anim should fire

---

## Task 3: EnemyAI — Death anim + sink

**Files:**
- Modify: `Assets/Scripts/Dev/Enemy/EnemyAI.cs`

- [ ] **Step 1: Add death/sink serialized fields**

In `EnemyAI`, find `[Header("Animation Params")]` block. Add two more fields:
```csharp
[SerializeField] private string deathParam   = "Die";
[SerializeField] private float  sinkDuration = 1.5f;
[SerializeField] private float  sinkDepth    = 2f;
```
Result of the full Animation Params block:
```csharp
[Header("Animation Params")]
[SerializeField] private string moveXParam         = "MoveX";
[SerializeField] private string moveYParam         = "MoveY";
[SerializeField] private string attackStabParam    = "Attack_Stab";
[SerializeField] private string attackSlash01Param = "Attack_Slash01";
[SerializeField] private string attackSlash02Param = "Attack_Slash02";
[SerializeField] private string hitReactParam      = "GetDamaged";
[SerializeField] private string deathParam         = "Die";
[SerializeField] private float  sinkDuration       = 1.5f;
[SerializeField] private float  sinkDepth          = 2f;
```

- [ ] **Step 2: Replace `OnDied()` — remove immediate Destroy, trigger anim**

Find the `OnDied` method:
```csharp
private void OnDied()
{
    StopAllCoroutines();
    _hitbox?.DisableHitbox();
    Destroy(gameObject);
}
```
Replace with:
```csharp
private void OnDied()
{
    _state = State.Dead;
    StopAllCoroutines();
    _hitbox?.DisableHitbox();
    _anim.SetTrigger(deathParam);
}
```

- [ ] **Step 3: Add `OnDeathAnimEnd` and `SinkAndDestroy` coroutine**

Add after the `OnDied` method (before `FindNearestPlayer`):
```csharp
// ── Death ──────────────────────────────────────────────────────────────────

public void OnDeathAnimEnd()
{
    StartCoroutine(SinkAndDestroy());
}

private IEnumerator SinkAndDestroy()
{
    float elapsed = 0f;
    float speed   = sinkDepth / sinkDuration;
    while (elapsed < sinkDuration)
    {
        transform.position -= Vector3.up * speed * Time.deltaTime;
        elapsed            += Time.deltaTime;
        yield return null;
    }
    Destroy(gameObject);
}
```

- [ ] **Step 4: Commit**

```bash
rtk git add Assets/Scripts/Dev/Enemy/EnemyAI.cs
rtk git commit -m "feat: enemy death anim + sink-and-destroy coroutine"
```

- [ ] **Step 5: Unity Editor — animator + anim event setup (manual)**

In the enemy's Animator Controller:
1. Add `Die` **Trigger** parameter
2. Add state `Death` on Base Layer (assign your death clip)
3. Any State → `Death`: condition = trigger `Die`, uncheck "Can Transition To Self"
4. `Death` state: no exit transition (object gets destroyed)

In the death animation clip:
1. Open the clip in Animation window
2. At the last keyframe, add Animation Event
3. Function name: `OnDeathAnimEnd` (must match exactly — calls `EnemyAI.OnDeathAnimEnd()`)

Test: kill enemy in Play mode → death anim plays → enemy sinks → disappears.

---

## Task 4: PlayerStats — `OnHpChanged` event + derived properties

**Files:**
- Modify: `Assets/Scripts/Dev/Player/PlayerStats.cs`

- [ ] **Step 1: Add `OnHpChanged` event**

Find:
```csharp
public event Action OnDied;
```
Add after:
```csharp
public event Action<float, float> OnHpChanged;
```

- [ ] **Step 2: Fire `OnHpChanged` in `TakeDamage` and `Heal`**

Find `TakeDamage`:
```csharp
public void TakeDamage(float finalDmg)
{
    if (!IsAlive) return;

    currentHp = Mathf.Max(0f, currentHp - finalDmg);

    if (currentHp <= 0f)
        OnDied?.Invoke();
}
```
Replace with:
```csharp
public void TakeDamage(float finalDmg)
{
    if (!IsAlive) return;

    currentHp = Mathf.Max(0f, currentHp - finalDmg);
    OnHpChanged?.Invoke(currentHp, MaxHP);

    if (currentHp <= 0f)
        OnDied?.Invoke();
}
```

Find `Heal`:
```csharp
public void Heal(float amount)
    => currentHp = Mathf.Min(MaxHP, currentHp + amount);
```
Replace with:
```csharp
public void Heal(float amount)
{
    currentHp = Mathf.Min(MaxHP, currentHp + amount);
    OnHpChanged?.Invoke(currentHp, MaxHP);
}
```

- [ ] **Step 3: Add five derived stat properties**

Find the `MaxMana` property line:
```csharp
public float MaxMana    => 50f  + StatCurveCalculator.Curve(ARC) * 500f;
```
Add after it:
```csharp
public float StaminaRecovery => 10f  + StatCurveCalculator.Curve(AGI) * 15f;
public float EquipLoad       => 50f  + StatCurveCalculator.Curve(END) * 200f;
public float CritRate        => 5f   + StatCurveCalculator.Curve(DEX) * 30f;
public float CritDamage      => 1.5f + StatCurveCalculator.Curve(STR) * 0.5f;
public float MovementSpeed   => 100f + StatCurveCalculator.Curve(AGI) * 10f;
```

- [ ] **Step 4: Commit**

```bash
rtk git add Assets/Scripts/Dev/Player/PlayerStats.cs
rtk git commit -m "feat: add OnHpChanged event and derived stat properties to PlayerStats"
```

---

## Task 5: PlayerHUD — HP bar

**Files:**
- Create: `Assets/Scripts/UI/HUD/PlayerHUD.cs`

- [ ] **Step 1: Create the folder if it doesn't exist**

In Windows Explorer or Unity: create `Assets/Scripts/UI/HUD/` folder.
Or via terminal (Unity will auto-generate .meta on reimport):
```bash
mkdir -p "Assets/Scripts/UI/HUD"
```

- [ ] **Step 2: Create `PlayerHUD.cs`**

Create `Assets/Scripts/UI/HUD/PlayerHUD.cs` with content:
```csharp
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider      hpSlider;

    private void Start()
    {
        hpSlider.minValue = 0f;
        hpSlider.maxValue = 1f;
        playerStats.OnHpChanged += RefreshHp;
        RefreshHp(playerStats.CurrentHp, playerStats.MaxHP);
    }

    private void OnDestroy() => playerStats.OnHpChanged -= RefreshHp;

    private void RefreshHp(float current, float max) => hpSlider.value = current / max;
}
```

- [ ] **Step 3: Commit**

```bash
rtk git add Assets/Scripts/UI/HUD/PlayerHUD.cs
rtk git commit -m "feat: PlayerHUD screen-space HP bar"
```

- [ ] **Step 4: Unity Editor setup (manual)**

1. On the scene Canvas (Screen Space — Overlay):
   - Add a `Slider` UI element, anchor bottom-left
   - Disable the Slider's Handle (drag handle rect → scale to 0 or delete) if you want a static bar
2. Add `PlayerHUD` component to the Canvas or a HUD child GameObject
3. Assign `PlayerStats` (drag from Player root) and `Slider` (drag the slider) in Inspector
4. Play → take damage → bar should decrease

---

## Task 6: PlayerStatPanel — TAB stat panel

**Files:**
- Create: `Assets/Scripts/UI/HUD/PlayerStatPanel.cs`

- [ ] **Step 1: Create `PlayerStatPanel.cs`**

Create `Assets/Scripts/UI/HUD/PlayerStatPanel.cs` with content:
```csharp
using UnityEngine;
using TMPro;

public class PlayerStatPanel : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private GameObject  panel;

    [Header("Core Stats")]
    [SerializeField] private TMP_Text vitText;
    [SerializeField] private TMP_Text endText;
    [SerializeField] private TMP_Text agiText;
    [SerializeField] private TMP_Text strText;
    [SerializeField] private TMP_Text dexText;
    [SerializeField] private TMP_Text arcText;

    [Header("Derived Stats")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text stamRecText;
    [SerializeField] private TMP_Text equipLoadText;
    [SerializeField] private TMP_Text critRateText;
    [SerializeField] private TMP_Text critDmgText;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private TMP_Text moveSpeedText;

    private void Awake() => panel.SetActive(false);

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            TogglePanel();
    }

    private void TogglePanel()
    {
        bool next = !panel.activeSelf;
        panel.SetActive(next);
        if (next) Refresh();
    }

    private void Refresh()
    {
        vitText.text = playerStats.VIT.ToString();
        endText.text = playerStats.END.ToString();
        agiText.text = playerStats.AGI.ToString();
        strText.text = playerStats.STR.ToString();
        dexText.text = playerStats.DEX.ToString();
        arcText.text = playerStats.ARC.ToString();

        hpText.text        = Mathf.RoundToInt(playerStats.MaxHP).ToString();
        staminaText.text   = Mathf.RoundToInt(playerStats.MaxStamina).ToString();
        stamRecText.text   = playerStats.StaminaRecovery.ToString("F1");
        equipLoadText.text = Mathf.RoundToInt(playerStats.EquipLoad).ToString();
        critRateText.text  = playerStats.CritRate.ToString("F1") + "%";
        critDmgText.text   = playerStats.CritDamage.ToString("F2") + "x";
        manaText.text      = Mathf.RoundToInt(playerStats.MaxMana).ToString();
        moveSpeedText.text = playerStats.MovementSpeed.ToString("F1");
    }
}
```

- [ ] **Step 2: Commit**

```bash
rtk git add Assets/Scripts/UI/HUD/PlayerStatPanel.cs
rtk git commit -m "feat: PlayerStatPanel TAB stat screen"
```

- [ ] **Step 3: Unity Editor setup (manual)**

On the scene Canvas (same Screen Space Overlay):
1. Create a Panel GameObject (`panel`)
2. Inside `panel`, create two child sections (Left / Right) using a `HorizontalLayoutGroup`
3. Left section — 6 rows, each with a Label (e.g. "VIT") and a value TMP_Text
4. Right section — 8 rows, each with a Label and a value TMP_Text
5. Add `PlayerStatPanel` component to the Canvas or a HUD child
6. Assign all fields in Inspector: `playerStats`, `panel`, all 14 TMP_Text references
7. Play → press TAB → panel appears with correct values

---

## Summary of Commits

```
feat: add OnHit/OnInterrupted/OnStunned events to EnemyStats
feat: enemy hit reaction anim via EnemyStats events
feat: enemy death anim + sink-and-destroy coroutine
feat: add OnHpChanged event and derived stat properties to PlayerStats
feat: PlayerHUD screen-space HP bar
feat: PlayerStatPanel TAB stat screen
```

## Manual Unity Editor Checklist (after all code tasks)

- [ ] Enemy animator: `GetDamaged` trigger + state + Any State transition
- [ ] Enemy animator: `Die` trigger + `Death` state + Any State transition
- [ ] Death clip: `OnDeathAnimEnd` animation event at last frame
- [ ] Canvas: HP Slider bottom-left, `PlayerHUD` component assigned
- [ ] Canvas: Stat Panel with 14 TMP_Text fields, `PlayerStatPanel` component assigned
