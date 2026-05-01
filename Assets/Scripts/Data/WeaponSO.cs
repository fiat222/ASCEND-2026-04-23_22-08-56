using UnityEngine;

// เพิ่ม Bow ใน WeaponType
public enum WeaponType { OneHand, TwoHand, Staff, Shield, Spear, Bow }

// เพิ่ม Bow ใน AttackType
public enum AttackType { OneHand, TwoHand, Magic, Shield, Spear, Bow }

[CreateAssetMenu(fileName = "NewWeapon", menuName = "ASCEND/Weapon")]
public class WeaponSO : ScriptableObject
{
    public string weaponName;
    public WeaponType weaponType;
    public int damage;
    public GameObject prefab;
    public Sprite icon;

    [Header("Grip")]
    public Vector3 gripPositionOffset;
    public Vector3 gripRotationOffset;
    public bool useOffHand;

    // ปรับการ Switch ให้รองรับ Bow
    public AttackType AttackType => weaponType switch
    {
        WeaponType.OneHand => AttackType.OneHand,
        WeaponType.TwoHand => AttackType.TwoHand,
        WeaponType.Staff   => AttackType.Magic,
        WeaponType.Shield  => AttackType.Shield,
        WeaponType.Spear   => AttackType.Spear,
        WeaponType.Bow     => AttackType.Bow,
        _                  => AttackType.OneHand
    };

    public bool IsTwoHanded => weaponType == WeaponType.TwoHand || weaponType == WeaponType.Bow;
}