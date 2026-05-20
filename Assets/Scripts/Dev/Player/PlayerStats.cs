using System;
using ASCEND.Core;
using ASCEND.Data;
using ASCEND.Systems;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable, IStatable
{
    [Header("Core Stats (1-99)")]
    [Range(1, 99)] public int VIT = 10;
    [Range(1, 99)] public int END = 10;
    [Range(1, 99)] public int AGI = 10;
    [Range(1, 99)] public int STR = 10;
    [Range(1, 99)] public int DEX = 10;
    [Range(1, 99)] public int ARC = 10;

    [Header("Stat Config")]
    [SerializeField] private StatConfigSO statConfig;

    [Header("Runtime")]
    [SerializeField] private float currentHp;

    [Header("Runtime — Stamina")]
    [SerializeField] private float currentStamina;

    [Header("Runtime — Mana")]
    [SerializeField] private float currentMana;

    private float _guardStabilityBase;

    // ── Per-stat progress ──────────────────────────────────────────────────────
    private float _vITProgress, _eNDProgress, _aGIProgress, _sTRProgress, _dEXProgress, _aRCProgress;

    // ── Per-stat thresholds ────────────────────────────────────────────────────
    private float _vITThreshold, _eNDThreshold, _aGIThreshold, _sTRThreshold, _dEXThreshold, _aRCThreshold;

    // ── Block / Parry flags (set by PlayerCombat) ─────────────────────────────
    public bool IsBlocking { get; set; }
    public bool IsParrying { get; set; }

    // ── Events ─────────────────────────────────────────────────────────────────
    public event Action OnDied;
    public event Action OnParried;
    public event Action OnGuardBreak;
    public event Action<float, float> OnHpChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action<float, float> OnManaChanged;

    // Per-stat level up events (IStatable implementation)
    public event Action<CoreStatType, int> OnStatLevelUp;
    public event Action<CoreStatType, float, float> OnProgressChanged;

    // ── Derived stats (via CentralizedStatSystem) ─────────────────────────────

    // All derived stats now ask CentralizedStatSystem for the calculated value
    public float MaxHP       => CentralizedStatSystem.CalculateMaxHP(VIT);
    public float MaxStamina  => CentralizedStatSystem.CalculateMaxStamina(END);
    public float MaxMana     => CentralizedStatSystem.CalculateMaxMana(ARC);

    public float StaminaRecovery => CentralizedStatSystem.CalculateStaminaRecovery(AGI);
    public float EquipLoad       => CentralizedStatSystem.CalculateEquipLoad(END);
    public float CritRate        => CentralizedStatSystem.CalculateCritRate(DEX);
    public float CritDamage      => CentralizedStatSystem.CalculateCritDamage(STR);
    public float MovementSpeed    => CentralizedStatSystem.CalculateMovementSpeed(AGI);
    public float GuardStability  => CentralizedStatSystem.CalculateGuardStability(_guardStabilityBase, STR);

    public float ManaRecovery        => CentralizedStatSystem.CalculateManaRecovery(ARC);
    public float StatusResistance    => CentralizedStatSystem.CalculateStatusResistance(ARC, VIT);

    // Crit rate as percentage (for crit roll)
    public float CritRatePercent => CentralizedStatSystem.CalculateCritRatePercent(DEX);

    public float CurrentHp      => currentHp;
    public float CurrentStamina => currentStamina;
    public float CurrentMana    => currentMana;
    public bool  IsAlive        => currentHp > 0f;

    private void Awake()
    {
        // Load base stats from StatConfigSO if assigned
        if (statConfig != null)
        {
            VIT = statConfig.GetBaseStat(CoreStatType.VIT);
            END = statConfig.GetBaseStat(CoreStatType.END);
            AGI = statConfig.GetBaseStat(CoreStatType.AGI);
            STR = statConfig.GetBaseStat(CoreStatType.STR);
            DEX = statConfig.GetBaseStat(CoreStatType.DEX);
            ARC = statConfig.GetBaseStat(CoreStatType.ARC);
            Debug.Log($"[PlayerStats] Loaded base stats from StatConfigSO: {statConfig.className}");
        }

        currentHp      = MaxHP;
        currentStamina = MaxStamina;
        currentMana    = MaxMana;
        OnStaminaChanged?.Invoke(currentStamina, MaxStamina);
        OnManaChanged?.Invoke(currentMana, MaxMana);

        // Initialize thresholds for all stats
        RecalculateThreshold(CoreStatType.VIT);
        RecalculateThreshold(CoreStatType.END);
        RecalculateThreshold(CoreStatType.AGI);
        RecalculateThreshold(CoreStatType.STR);
        RecalculateThreshold(CoreStatType.DEX);
        RecalculateThreshold(CoreStatType.ARC);

        // Register with CentralizedStatSystem
        if (CentralizedStatSystem.Instance != null)
        {
            CentralizedStatSystem.Instance.Register(this);
            Debug.Log("[PlayerStats] Registered with CentralizedStatSystem.");
        }
        else
        {
            Debug.LogWarning("[PlayerStats] CentralizedStatSystem not found! Progress system will not function.");
        }

        Debug.Log($"[PlayerStats] VIT={VIT} STR={STR} Thresholds: VIT={_vITThreshold:F1} STR={_sTRThreshold:F1}");
    }

    private void OnDestroy()
    {
        if (CentralizedStatSystem.Instance != null)
        {
            CentralizedStatSystem.Instance.Unregister(this);
        }
    }

    private void Update()
    {
        if (!IsBlocking && currentStamina < MaxStamina)
        {
            currentStamina = Mathf.Min(MaxStamina, currentStamina + StaminaRecovery * Time.deltaTime);
            OnStaminaChanged?.Invoke(currentStamina, MaxStamina);
        }

        if (currentMana < MaxMana)
        {
            currentMana = Mathf.Min(MaxMana, currentMana + ManaRecovery * Time.deltaTime);
            OnManaChanged?.Invoke(currentMana, MaxMana);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IStatable Implementation
    // ═══════════════════════════════════════════════════════════════════════════

    public int GetStat(CoreStatType stat)
    {
        return stat switch
        {
            CoreStatType.VIT => VIT,
            CoreStatType.END => END,
            CoreStatType.AGI => AGI,
            CoreStatType.STR => STR,
            CoreStatType.DEX => DEX,
            CoreStatType.ARC => ARC,
            _ => 10
        };
    }

    public float GetProgress(CoreStatType stat)
    {
        return stat switch
        {
            CoreStatType.VIT => _vITProgress,
            CoreStatType.END => _eNDProgress,
            CoreStatType.AGI => _aGIProgress,
            CoreStatType.STR => _sTRProgress,
            CoreStatType.DEX => _dEXProgress,
            CoreStatType.ARC => _aRCProgress,
            _ => 0f
        };
    }

    public float GetThreshold(CoreStatType stat)
    {
        return stat switch
        {
            CoreStatType.VIT => _vITThreshold,
            CoreStatType.END => _eNDThreshold,
            CoreStatType.AGI => _aGIThreshold,
            CoreStatType.STR => _sTRThreshold,
            CoreStatType.DEX => _dEXThreshold,
            CoreStatType.ARC => _aRCThreshold,
            _ => 200f
        };
    }

    public void GainProgress(CoreStatType stat, float amount)
    {
        GainStatProgress(stat, amount);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Progress System
    // ═══════════════════════════════════════════════════════════════════════════

    private void SetProgress(CoreStatType stat, float value)
    {
        switch (stat)
        {
            case CoreStatType.VIT: _vITProgress = value; break;
            case CoreStatType.END: _eNDProgress = value; break;
            case CoreStatType.AGI: _aGIProgress = value; break;
            case CoreStatType.STR: _sTRProgress = value; break;
            case CoreStatType.DEX: _dEXProgress = value; break;
            case CoreStatType.ARC: _aRCProgress = value; break;
        }
    }

    /// <summary>
    /// Adds progress to a stat. Triggers level up if threshold is reached.
    /// </summary>
    public void GainStatProgress(CoreStatType stat, float amount)
    {
        float currentProgress = GetProgress(stat);
        float threshold = GetThreshold(stat);
        float newProgress = currentProgress + amount;

        SetProgress(stat, newProgress);
        Debug.Log($"[PlayerStats] GainProgress {stat}: +{amount:F1} → {currentProgress:F1}/{threshold:F1}");

        // Fire progress changed event
        OnProgressChanged?.Invoke(stat, newProgress, threshold);
        if (CentralizedStatSystem.Instance != null)
        {
            CentralizedStatSystem.Instance.NotifyProgressChanged(this, stat, newProgress, threshold);
        }

        // Check for level up
        if (newProgress >= threshold)
        {
            Debug.Log($"[PlayerStats] Progress threshold reached for {stat}! Triggering level up.");
            LevelUpStat(stat);
        }
    }

    /// <summary>
    /// Levels up a stat: increment stat, reset progress, recalc threshold, fire events.
    /// Stat value is clamped to min/max from StatConfigSO.
    /// </summary>
    public void LevelUpStat(CoreStatType stat)
    {
        int statValue = GetStat(stat);
        int newLevel = statValue + 1;

        // Clamp to min/max range
        newLevel = CentralizedStatSystem.ClampStat(newLevel, statConfig, stat);

        switch (stat)
        {
            case CoreStatType.VIT: VIT = newLevel; break;
            case CoreStatType.END: END = newLevel; break;
            case CoreStatType.AGI: AGI = newLevel; break;
            case CoreStatType.STR: STR = newLevel; break;
            case CoreStatType.DEX: DEX = newLevel; break;
            case CoreStatType.ARC: ARC = newLevel; break;
        }

        // Reset progress
        SetProgress(stat, 0f);

        // Recalculate threshold
        RecalculateThreshold(stat);

        Debug.Log($"[PlayerStats] LEVEL UP! {stat}: {statValue} → {newLevel} (new threshold: {GetThreshold(stat):F1})");

        // Fire events
        OnStatLevelUp?.Invoke(stat, newLevel);
        if (CentralizedStatSystem.Instance != null)
        {
            CentralizedStatSystem.Instance.NotifyStatLevelUp(this, stat, newLevel);
        }
    }

    /// <summary>
    /// Recalculates the progress threshold for a stat using the formula:
    /// threshold = progressBase + Curve(statValue, progressK) × progressBonus
    /// </summary>
    public void RecalculateThreshold(CoreStatType stat)
    {
        int statValue = GetStat(stat);

        float progressBase, progressBonus, progressK;

        if (statConfig != null)
        {
            (progressBase, progressBonus, progressK) = statConfig.GetProgressConfig(stat);
        }
        else
        {
            // Default values if no config
            (progressBase, progressBonus, progressK) = (50f, 150f, 0.05f);
        }

        float threshold = CentralizedStatSystem.CalculateThreshold(statValue, progressBase, progressBonus, progressK);

        switch (stat)
        {
            case CoreStatType.VIT: _vITThreshold = threshold; break;
            case CoreStatType.END: _eNDThreshold = threshold; break;
            case CoreStatType.AGI: _aGIThreshold = threshold; break;
            case CoreStatType.STR: _sTRThreshold = threshold; break;
            case CoreStatType.DEX: _dEXThreshold = threshold; break;
            case CoreStatType.ARC: _aRCThreshold = threshold; break;
        }

        Debug.Log($"[PlayerStats] RecalculateThreshold {stat}({statValue}): threshold = {threshold:F1}");
    }

    // ── Guard base (set by PlayerCombat on weapon equip) ──────────────────────
    public void SetGuardBase(float value) => _guardStabilityBase = value;

    // ── Mana drain (returns true if had enough mana) ──────────────────────────
    public bool DrainMana(float amount)
    {
        bool hadEnough = currentMana >= amount;
        currentMana = Mathf.Max(0f, currentMana - amount);
        OnManaChanged?.Invoke(currentMana, MaxMana);
        return hadEnough;
    }

    // ── Stamina drain (returns true if had enough stamina) ─────────────────────
    public bool DrainStamina(float amount)
    {
        bool hadEnough = currentStamina >= amount;
        currentStamina = Mathf.Max(0f, currentStamina - amount);
        OnStaminaChanged?.Invoke(currentStamina, MaxStamina);
        return hadEnough;
    }

    // ── Damage output ──────────────────────────────────────────────────────────

    public float RawDamage(WeaponSO weapon)
    {
        if (weapon == null) return 5f;

        float strCurve = StatCurveCalculator.Curve(STR);
        float dexCurve = StatCurveCalculator.Curve(DEX);
        float arcCurve = StatCurveCalculator.Curve(ARC);

        float scaling = 1f
            + strCurve * weapon.StrScale
            + dexCurve * weapon.DexScale
            + arcCurve * weapon.ArcScale;

        return weapon.damage * scaling;
    }

    /// <summary>
    /// Calculates raw stagger damage from weapon.
    /// Formula: baseStagger × (1 + scaleCurve × scaleValue) × attackTypeMultiplier
    /// </summary>
    public float CalculateStagger(WeaponSO weapon)
    {
        if (weapon == null) return 25f;

        // Determine which stat scales stagger
        int scaleStat = STR;  // default
        float scaleValue = weapon.StaggerScale;

        // Calculate stagger with scaling
        float scaleCurve = StatCurveCalculator.Curve(scaleStat);
        float rawStagger = weapon.baseStagger * (1f + scaleCurve * scaleValue);

        // Apply attack type multiplier
        float finalStagger = rawStagger * weapon.GetStaggerMultiplier();

        Debug.Log($"[PlayerStats] CalculateStagger: base={weapon.baseStagger}, scale={scaleCurve:F3}, value={scaleValue}, mult={weapon.GetStaggerMultiplier()}, final={finalStagger:F1}");

        return finalStagger;
    }

    // ── Incoming damage ────────────────────────────────────────────────────────

    public void TakeDamage(float finalDmg)
    {
        if (!IsAlive) return;

        Debug.Log($"[TakeDamage] dmg={finalDmg:F1} IsParrying={IsParrying} IsBlocking={IsBlocking} stamina={currentStamina:F1}/{MaxStamina:F1} guardStability={GuardStability:F1}");

        if (IsParrying)
        {
            Debug.Log("[TakeDamage] → PARRIED");
            OnParried?.Invoke();
            return;
        }

        if (IsBlocking)
        {
            float guardCost = finalDmg * (100f / (100f + GuardStability));
            Debug.Log($"[TakeDamage] → BLOCKED guardCost={guardCost:F1} staminaAfter={currentStamina - guardCost:F1}");
            if (currentStamina > 0f)
            {
                currentStamina = Mathf.Max(0f, currentStamina - guardCost);
                OnStaminaChanged?.Invoke(currentStamina, MaxStamina);
                if (currentStamina <= 0f) { Debug.Log("[TakeDamage] → GUARD BREAK"); OnGuardBreak?.Invoke(); }
                return;
            }
            else
            {
                Debug.Log("[TakeDamage] → GUARD BREAK (stamina already 0)");
                OnGuardBreak?.Invoke();
                // fall through — guard broken, take full damage
            }
        }

        currentHp = Mathf.Max(0f, currentHp - finalDmg);
        OnHpChanged?.Invoke(currentHp, MaxHP);

        if (currentHp <= 0f)
            OnDied?.Invoke();
    }

    // ── IDamageable ──────────────────────────────────────────────────────────

    public void TakeStagger(float staggerDmg)
    {
        // Player doesn't have stagger system yet, just log
        Debug.Log($"[PlayerStats] TakeStagger: {staggerDmg:F1} (player stagger not implemented)");
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(MaxHP, currentHp + amount);
        OnHpChanged?.Invoke(currentHp, MaxHP);
    }

    // Called on non-server clients by NetworkPlayerStats to sync host's result
    public void SyncHp(float hp)
    {
        currentHp = hp;
        OnHpChanged?.Invoke(currentHp, MaxHP);
        if (currentHp <= 0f) OnDied?.Invoke();
    }

    // ── Debug helpers ─────────────────────────────────────────────────────────

    [ContextMenu("Debug: Print All Stats")]
    public void DebugPrintStats()
    {
        Debug.Log($"=== PlayerStats Debug ===");
        Debug.Log($"VIT={VIT} (progress: {_vITProgress:F1}/{_vITThreshold:F1})");
        Debug.Log($"END={END} (progress: {_eNDProgress:F1}/{_eNDThreshold:F1})");
        Debug.Log($"AGI={AGI} (progress: {_aGIProgress:F1}/{_aGIThreshold:F1})");
        Debug.Log($"STR={STR} (progress: {_sTRProgress:F1}/{_sTRThreshold:F1})");
        Debug.Log($"DEX={DEX} (progress: {_dEXProgress:F1}/{_dEXThreshold:F1})");
        Debug.Log($"ARC={ARC} (progress: {_aRCProgress:F1}/{_aRCThreshold:F1})");
        Debug.Log($"MaxHP={MaxHP:F0} MaxStamina={MaxStamina:F0} MaxMana={MaxMana:F0}");
        Debug.Log($"CritRate={CritRate:F2} CritDamage={CritDamage:F2}x CritRatePercent={CritRatePercent:F1}%");
        Debug.Log($"StatusResistance={StatusResistance:F2}");
    }

    [ContextMenu("Debug: Give 100 STR Progress")]
    public void DebugGiveStrProgress()
    {
        GainStatProgress(CoreStatType.STR, 100f);
    }

    [ContextMenu("Debug: Give 500 All Progress")]
    public void DebugGiveAllProgress()
    {
        GainStatProgress(CoreStatType.VIT, 500f);
        GainStatProgress(CoreStatType.END, 500f);
        GainStatProgress(CoreStatType.AGI, 500f);
        GainStatProgress(CoreStatType.STR, 500f);
        GainStatProgress(CoreStatType.DEX, 500f);
        GainStatProgress(CoreStatType.ARC, 500f);
    }
}