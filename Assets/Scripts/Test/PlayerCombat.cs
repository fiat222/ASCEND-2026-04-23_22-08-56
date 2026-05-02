using System.Collections;
using UnityEngine;
using Drakkar.GameUtils;

public class PlayerCombat : MonoBehaviour
{
    [Header("Weapon Slots & UI")]
    [SerializeField] private Transform weaponSlot;
    [SerializeField] private Transform offHandSlot;
    [SerializeField] private HotbarController hotbar;

    [Header("Bow & Spear Settings")]
    public GameObject arrowPrefab;
    public Transform shotPoint; 
    public float arrowForce = 1500f;
    public Transform throwPoint; // จุดที่หอกจะถูกปล่อยออกจากมือ
    public float spearForce = 25f;
    [SerializeField] private Transform handStringAnchor; 

    [Header("Animation Params - General")]
    [SerializeField] private string attack1HParam    = "Attack1H";
    [SerializeField] private string attack2HParam    = "Attack2H";
    [SerializeField] private string attackMagicParam = "AttackMagic";
    [SerializeField] private string attackShieldParam = "AttackShield";
    [SerializeField] private string attackSpearParam  = "AttackSpear";
    [SerializeField] private int attackAnimatorLayer = 1;
    
    [Header("Idle States")]
    [SerializeField] private string idle1HParam      = "Is1HIdle";
    [SerializeField] private string idle2HParam      = "Is2HIdle";
    [SerializeField] private string idleSpearParam   = "IsSpearIdle";
    [SerializeField] private string idleBowParam     = "IsBowIdle";

    [Header("Bow Animation Params")]
    [SerializeField] private string attackBowParam = "Attack_Bow";
    [SerializeField] private string holdBowParam = "Hold_Bow";

    [Header("Spear Throw Params")]
    [SerializeField] private string holdSpearParam   = "Hold_Spear";   // Boolean ใน Animator
    [SerializeField] private string throwSpearParam  = "Throw_Spear";  // Trigger ใน Animator

    private Animator _anim;
    private GameObject _equippedWeapon;
    private WeaponSO _equippedSO;
    private bool _isAttacking;
    private DrakkarTrail _weaponTrail; // ใช้ DrakkarTrail ตามต้นฉบับ

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
        if (_equippedSO == null || _isAttacking) return;

        // แยก Logic ตามประเภทอาวุธ
        if (_equippedSO.weaponType == WeaponType.Bow)
        {
            HandleBowInput();
        }
        else if (_equippedSO.weaponType == WeaponType.Spear)
        {
            HandleSpearInput();
        }
        else
        {
            // อาวุธประชิดอื่นๆ
            if (Input.GetMouseButtonDown(0)) StartCoroutine(AttackRoutine());
        }
    }

    // --- 1. ระบบโจมตีประชิด (ใช้ Logic เดิมของคุณเป๊ะๆ) ---
    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _anim.SetTrigger(GetAttackParam());

        yield return null;
        yield return null;

        AnimatorStateInfo state = _anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer);
        float clipLength = state.length > 0.1f ? state.length : 1f;

        _weaponTrail?.Begin(); // ใช้ DrakkarTrail
        yield return new WaitForSeconds(clipLength);
        _weaponTrail?.End();

        _isAttacking = false;
    }

    private string GetAttackParam()
    {
        if (_equippedSO == null) return attack1HParam;
        return _equippedSO.AttackType switch
        {
            AttackType.TwoHand => attack2HParam,
            AttackType.Magic   => attackMagicParam,
            AttackType.Shield  => attackShieldParam,
            AttackType.Spear   => attackSpearParam,
            _                  => attack1HParam
        };
    }

    // --- 2. ระบบธนู (Bow) ---
    private void HandleBowInput()
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

    private IEnumerator FinishBowAttackRoutine()
    {
        _isAttacking = true;
        yield return new WaitForSeconds(0.2f); 
        _anim.SetBool(attackBowParam, false);
        _isAttacking = false;
    }

    // --- 3. ระบบหอก (Spear: แทงได้ + ปาได้) ---
    private void HandleSpearInput()
    {
        // คลิกขวาค้างเพื่อเล็ง (Hold)
        if (Input.GetMouseButtonDown(1)) _anim.SetBool(holdSpearParam, true);
        if (Input.GetMouseButtonUp(1)) _anim.SetBool(holdSpearParam, false);

        if (_anim.GetBool(holdSpearParam))
        {
            // ถ้าเล็งอยู่แล้วคลิกซ้าย -> ให้ปา
            if (Input.GetMouseButtonDown(0)) _anim.SetTrigger(throwSpearParam);
        }
        else
        {
            // ถ้าไม่ได้เล็งแล้วคลิกซ้าย -> ให้แทง (ใช้ Routine เดิม)
            if (Input.GetMouseButtonDown(0)) StartCoroutine(AttackRoutine());
        }
    }

    // --- Animation Events (ต้องไปตั้งใน Animator) ---
    public void ExecuteShoot() // ฟังก์ชันยิงที่คุณส่งมา
    {
        if (arrowPrefab == null || shotPoint == null) return;
        GameObject arrow = Instantiate(arrowPrefab, shotPoint.position, shotPoint.rotation);
        if (arrow.TryGetComponent<Rigidbody>(out Rigidbody rb))
            rb.AddForce(shotPoint.forward * arrowForce);
    }

    public void ExecuteSpearThrow() // สำหรับปาหอก
    {
        if (_equippedWeapon == null || throwPoint == null) return;
        
        // สร้างหอกออกมาปา (ใช้ Prefab จาก SO)
        GameObject spear = Instantiate(_equippedSO.prefab, throwPoint.position, throwPoint.rotation);
        if (spear.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.isKinematic = false; 
            rb.AddForce(throwPoint.forward * spearForce, ForceMode.Impulse);
        }

        StartCoroutine(HideWeaponInHand());
    }

    private IEnumerator HideWeaponInHand()
    {
        _equippedWeapon.SetActive(false);
        yield return new WaitForSeconds(0.6f); 
        _equippedWeapon.SetActive(true);
    }

    // --- ระบบสวมใส่ (อิงตาม PlayerTest เดิม) ---
    public void EquipWeapon(WeaponSO so)
    {
        if (_equippedWeapon != null) Destroy(_equippedWeapon);

        _equippedSO = so;
        _isAttacking = false;

        if (so == null || so.prefab == null) return;

        // อัปเดตท่าทาง Idle
        _anim?.SetBool(idle1HParam,    so.weaponType is WeaponType.OneHand or WeaponType.Staff or WeaponType.Shield);
        _anim?.SetBool(idle2HParam,    so.weaponType == WeaponType.TwoHand);
        _anim?.SetBool(idleSpearParam, so.weaponType == WeaponType.Spear);
        _anim?.SetBool(idleBowParam,   so.weaponType == WeaponType.Bow);

        Transform slot = (so.useOffHand && offHandSlot != null) ? offHandSlot : weaponSlot;
        _equippedWeapon = Instantiate(so.prefab, slot);
        _equippedWeapon.transform.localPosition = so.gripPositionOffset;
        _equippedWeapon.transform.localEulerAngles = so.gripRotationOffset;

        // ค้นหา DrakkarTrail
        _weaponTrail = _equippedWeapon.GetComponentInChildren<DrakkarTrail>();

        // Setup พิเศษสำหรับธนู
        if (so.weaponType == WeaponType.Bow)
        {
            var bowString = _equippedWeapon.GetComponent<BowStringController>();
            if (bowString != null) bowString.handStringPoint = handStringAnchor; 

            Transform foundPoint = _equippedWeapon.transform.Find("ShotPoint");
    
        if (foundPoint != null) 
        {
            shotPoint = foundPoint; // ถ้าเจอ ก็เอามาใช้เป็นจุดยิงเลย
        }
        }
    }
}