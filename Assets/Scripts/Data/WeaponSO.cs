using UnityEngine;

public enum WeaponType { OneHand, TwoHand, Staff, Shield, Spear }

public enum AttackType { OneHand, TwoHand, Magic, Shield, Spear }

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

    public AttackType AttackType => weaponType switch
    {
        WeaponType.OneHand => AttackType.OneHand,
        WeaponType.TwoHand => AttackType.TwoHand,
        WeaponType.Staff   => AttackType.Magic,
        WeaponType.Shield  => AttackType.Shield,
        WeaponType.Spear   => AttackType.Spear,
        _                  => AttackType.OneHand
    };

    public bool IsTwoHanded => weaponType == WeaponType.TwoHand;
}
