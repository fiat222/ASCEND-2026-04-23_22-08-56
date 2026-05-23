using UnityEngine;

public enum WeaponType { OneHand, TwoHand, Staff, Shield, Spear, Bow, Wand, Torch }
public enum AttackType { OneHand, TwoHand, Magic, Shield, Spear, Bow, Torch, Slash, Blunt, Pierce, Fire, Ice }

public enum ScalingGrade { None, E, D, C, B, A, S }

[CreateAssetMenu(fileName = "NewWeapon", menuName = "ASCEND/Weapon")]
public class WeaponSO : ScriptableObject
{
    public string     weaponName;
    public WeaponType weaponType;
    public int        damage;
    public GameObject prefab;
    public Sprite     icon;

    [Header("Grip")]
    public Vector3 gripPositionOffset;
    public Vector3 gripRotationOffset;

    [Header("Hold Mode Grip")]
    public Vector3 holdPositionOffset;
    public Vector3 holdRotationOffset;
    public bool    useOffHand;

    [Header("Off-Hand Grip")]
    public Vector3 offHandPositionOffset;
    public Vector3 offHandRotationOffset;

    [Header("Guard")]
    public float baseGuardStability = 0f;

    [Header("Stamina")]
    public float staminaCost = 10f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float      projectileSpeed = 15f;
    public float      projectileRange = 50f;
    public float      projectileForce = 1500f;
    public float      manaCost        = 20f;

    [Header("Damage Scaling")]
    public ScalingGrade strScaling;
    public ScalingGrade dexScaling;
    public ScalingGrade arcScaling;

    public float StrScale => GradeToValue(strScaling);
    public float DexScale => GradeToValue(dexScaling);
    public float ArcScale => GradeToValue(arcScaling);

    [Header("Stagger")]
    public float baseStagger = 25f;
    public ScalingGrade staggerScaling = ScalingGrade.C;  // which stat scales stagger

    public float StaggerScale => GradeToValue(staggerScaling);

    [Header("Status Effects")]
    public float bleedingDmg = 0f;
    public float poisonDmg = 0f;
    public float freezeDmg = 0f;

    private static float GradeToValue(ScalingGrade grade) => grade switch
    {
        ScalingGrade.S    => 1.00f,
        ScalingGrade.A    => 0.80f,
        ScalingGrade.B    => 0.65f,
        ScalingGrade.C    => 0.50f,
        ScalingGrade.D    => 0.35f,
        ScalingGrade.E    => 0.20f,
        _                 => 0f
    };

    public AttackType AttackType => weaponType switch
    {
        WeaponType.OneHand => AttackType.OneHand,
        WeaponType.TwoHand => AttackType.TwoHand,
        WeaponType.Staff   => AttackType.Magic,
        WeaponType.Wand    => AttackType.Magic,
        WeaponType.Shield  => AttackType.Shield,
        WeaponType.Spear   => AttackType.Spear,
        WeaponType.Bow     => AttackType.Bow,
        WeaponType.Torch   => AttackType.Torch,
        _                  => AttackType.OneHand
    };

    public bool IsTwoHanded => weaponType == WeaponType.TwoHand || weaponType == WeaponType.Bow;

    /// <summary>
    /// อาวุธที่ใส่มือซ้ายได้ — Shield และ Torch เท่านั้น
    /// </summary>
    public bool CanEquipOffHand => weaponType == WeaponType.Shield
                                || weaponType == WeaponType.Torch;

    /// <summary>
    /// Get stagger multiplier based on attack type.
    /// Slash = 0.5, Blunt = 1.0, Pierce = 0.7, Magic = 0.3, Fire/Ice = 0.4, others = 1.0
    /// </summary>
    public float GetStaggerMultiplier() => AttackType switch
    {
        AttackType.Slash  => 0.5f,
        AttackType.Blunt   => 1.0f,
        AttackType.Pierce  => 0.7f,
        AttackType.Magic   => 0.3f,
        AttackType.Fire    => 0.4f,
        AttackType.Ice     => 0.4f,
        _                  => 1.0f
    };
}