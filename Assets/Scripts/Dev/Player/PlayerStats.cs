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

    public event Action OnDied;
    public event Action<float, float> OnHpChanged;

    // ── Derived stats ──────────────────────────────────────────────────────────

    public float MaxHP      => 500f + StatCurveCalculator.Curve(VIT) * 1000f;
    public float MaxStamina => 100f + StatCurveCalculator.Curve(END) * 300f;
    public float MaxMana    => 50f  + StatCurveCalculator.Curve(ARC) * 500f;

    public float StaminaRecovery => 10f  + StatCurveCalculator.Curve(AGI) * 15f;
    public float EquipLoad       => 50f  + StatCurveCalculator.Curve(END) * 200f;
    public float CritRate        => 5f   + StatCurveCalculator.Curve(DEX) * 30f;
    public float CritDamage      => 1.5f + StatCurveCalculator.Curve(STR) * 0.5f;
    public float MovementSpeed   => 100f + StatCurveCalculator.Curve(AGI) * 10f;

    public float CurrentHp  => currentHp;
    public bool  IsAlive    => currentHp > 0f;

    private void Awake() => currentHp = MaxHP;

    // ── Damage output ──────────────────────────────────────────────────────────

    /// Raw damage before enemy DEF reduction.
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
}
