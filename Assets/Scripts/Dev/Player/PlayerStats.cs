using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Core Stats (1-99)")]
    [Range(1, 99)] public int VIT = 10;
    [Range(1, 99)] public int END = 10;
    [Range(1, 99)] public int AGI = 10;
    [Range(1, 99)] public int STR = 10;
    [Range(1, 99)] public int DEX = 10;
    [Range(1, 99)] public int ARC = 10;

    [Header("Runtime")]
    [SerializeField] private float currentHp;

    [Header("Runtime — Stamina")]
    [SerializeField] private float currentStamina;

    [Header("Runtime — Mana")]
    [SerializeField] private float currentMana;

    private float _guardStabilityBase;

    // ── Block / Parry flags (set by PlayerCombat) ─────────────────────────────
    public bool IsBlocking { get; set; }
    public bool IsParrying { get; set; }

    // ── Events ─────────────────────────────────────────────────────────────────
    public event Action              OnDied;
    public event Action              OnParried;
    public event Action              OnGuardBreak;
    public event Action<float, float> OnHpChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action<float, float> OnManaChanged;

    // ── Derived stats ──────────────────────────────────────────────────────────

    public float MaxHP      => 500f + StatCurveCalculator.Curve(VIT) * 1000f;
    public float MaxStamina => 100f + StatCurveCalculator.Curve(END) * 300f;
    public float MaxMana    => 50f  + StatCurveCalculator.Curve(ARC) * 500f;

    public float StaminaRecovery => 10f  + StatCurveCalculator.Curve(AGI) * 15f;
    public float EquipLoad       => 50f  + StatCurveCalculator.Curve(END) * 200f;
    public float CritRate        => 5f   + StatCurveCalculator.Curve(DEX) * 30f;
    public float CritDamage      => 1.5f + StatCurveCalculator.Curve(STR) * 0.5f;
    public float MovementSpeed   => 100f + StatCurveCalculator.Curve(AGI) * 10f;
    public float GuardStability  => _guardStabilityBase + StatCurveCalculator.Curve(STR) * 50f;

    public float ManaRecovery   => 5f + StatCurveCalculator.Curve(ARC) * 10f;

    public float CurrentHp      => currentHp;
    public float CurrentStamina => currentStamina;
    public float CurrentMana    => currentMana;
    public bool  IsAlive        => currentHp > 0f;

    private void Awake()
    {
        currentHp      = MaxHP;
        currentStamina = MaxStamina;
        currentMana    = MaxMana;
        OnStaminaChanged?.Invoke(currentStamina, MaxStamina);
        OnManaChanged?.Invoke(currentMana, MaxMana);
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
}
