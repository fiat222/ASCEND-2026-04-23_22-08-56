using UnityEngine;

public enum WeaponType { OneHand, TwoHand, Staff }

public enum AttackType { OneHand, TwoHand, Magic }

[CreateAssetMenu(fileName = "NewWeapon", menuName = "ASCEND/Weapon")]
public class WeaponSO : ScriptableObject
{
    public string weaponName;
    public WeaponType weaponType;
    public int damage;
    public GameObject prefab;

    public AttackType AttackType => weaponType switch
    {
        WeaponType.OneHand => AttackType.OneHand,
        WeaponType.TwoHand => AttackType.TwoHand,
        WeaponType.Staff   => AttackType.Magic,
        _                  => AttackType.OneHand
    };

    public bool IsTwoHanded => weaponType == WeaponType.TwoHand;
}
