using UnityEngine;

public enum WeaponType { OneHand, TwoHand, Staff, Shield, Spear, Bow, Wand, Torch }
public enum AttackType { OneHand, TwoHand, Magic, Shield, Spear, Bow, Torch }

[CreateAssetMenu(fileName = "NewWeapon", menuName = "ASCEND/Weapon")]
public class WeaponSO : ScriptableObject
{
    public string     weaponName;
    public WeaponType weaponType;
    public int        damage;
    public GameObject prefab;
    public GameObject throwPrefab;
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
}