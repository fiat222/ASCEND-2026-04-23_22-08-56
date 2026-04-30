using System.Collections;
using UnityEngine;
using Drakkar.GameUtils;

public class PlayerCombat : MonoBehaviour
{
    [Header("Bow Setup")]
    public GameObject arrowPrefab;
    public Transform shotPoint; // จุดที่ลูกธนูจะเกิด
    public float launchForce = 1500f;
    [SerializeField] private Transform handStringAnchor;

    [Header("Slots & Hotbar")]
    [SerializeField] private Transform weaponSlot;
    [SerializeField] private Transform offHandSlot;
    [SerializeField] private HotbarController hotbar;

    [Header("Animation Params")]
    [SerializeField] private string attackBowParam = "Attack_Bow";
    [SerializeField] private string holdBowParam = "Hold_Bow";
    [SerializeField] private string idleBowParam = "IsBowIdle";
    // เพิ่ม Param อื่นๆ ตามไฟล์เดิม
    [SerializeField] private string idle1HParam = "Is1HIdle";
    [SerializeField] private string idle2HParam = "Is2HIdle";

    private Animator _anim;
    private GameObject _equippedWeapon;
    private WeaponSO _equippedSO;
    private bool _isAttacking;

    private void Start()
    {
        _anim = GetComponentInChildren<Animator>();
        if (hotbar != null)
        {
            hotbar.OnSlotChanged += (id) => EquipWeapon(hotbar.SelectedWeapon);
            EquipWeapon(hotbar.SelectedWeapon);
        }
    }

    private void Update()
    {
        if (_equippedSO == null) return;
        HandleAttackInput();
    }

    private void HandleAttackInput()
    {
        if (_equippedSO.weaponType == WeaponType.Bow)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _anim.SetBool(attackBowParam, true);
                _anim.SetBool(holdBowParam, true);
            }
            if (Input.GetMouseButtonUp(0))
            {
                _anim.SetBool(holdBowParam, false);
                StartCoroutine(FinishBowAttackRoutine());
            }
        }
        // สามารถเพิ่ม Melee Attack Routine ต่อจากนี้ได้
    }

    private IEnumerator FinishBowAttackRoutine()
    {
        yield return new WaitForSeconds(0.2f); 
        _anim.SetBool(attackBowParam, false);
    }

    // ฟังก์ชันสำหรับ Animation Event 
    public void ExecuteShoot()
    {
        if (arrowPrefab == null || shotPoint == null) return;
        GameObject arrow = Instantiate(arrowPrefab, shotPoint.position, shotPoint.rotation);
        if (arrow.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.AddForce(shotPoint.forward * launchForce);
        }
    }

    public void EquipWeapon(WeaponSO so)
    {
        if (_equippedWeapon != null) Destroy(_equippedWeapon);
        _equippedSO = so;

        // Update Animator States เหมือนเดิมเป๊ะ
        WeaponType? type = so?.weaponType;
        _anim?.SetBool(idle1HParam, type is WeaponType.OneHand or WeaponType.Staff or WeaponType.Shield);
        _anim?.SetBool(idle2HParam, type == WeaponType.TwoHand);
        _anim?.SetBool(idleBowParam, type == WeaponType.Bow);

        if (so == null || so.prefab == null) return;

        Transform slot = (so.useOffHand && offHandSlot != null) ? offHandSlot : weaponSlot;
        _equippedWeapon = Instantiate(so.prefab, slot);

        // เชื่อมโยงสายธนู (Line Renderer)
        if (so.weaponType == WeaponType.Bow)
        {
            var bowString = _equippedWeapon.GetComponent<BowStringController>();
            if (bowString != null) bowString.handStringPoint = handStringAnchor;
        }
        Transform foundShotPoint = _equippedWeapon.transform.Find("ShotPoint");
        if (foundShotPoint != null)
        {
            shotPoint = foundShotPoint;
        }
        _equippedWeapon.transform.localPosition = so.gripPositionOffset;
        _equippedWeapon.transform.localEulerAngles = so.gripRotationOffset;
    }
}