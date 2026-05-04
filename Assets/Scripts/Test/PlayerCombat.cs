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
    public Transform throwPoint;
    public float spearForce = 25f;
    [SerializeField] private Transform handStringAnchor;

    [Header("Aim")]
    [SerializeField] private float aimDistance = 100f;
    [SerializeField] private LayerMask aimMask = ~0;

    [Header("Animation Params - General")]
    [SerializeField] private string attack1HParam    = "Attack1H";
    [SerializeField] private string attack2HParam    = "Attack2H";
    [SerializeField] private string attackMagicParam = "AttackMagic";
    [SerializeField] private string attackShieldParam = "AttackShield";
    [SerializeField] private string attackSpearParam  = "AttackSpear";
    [SerializeField] private string isInActionParam = "IsInAction";
    [SerializeField] private int attackAnimatorLayer = 1;
    
    [Header("Idle States")]
    [SerializeField] private string idle1HParam      = "Is1HIdle";
    [SerializeField] private string idle2HParam      = "Is2HIdle";
    [SerializeField] private string idleSpearParam   = "IsSpearIdle";
    [SerializeField] private string idleBowParam     = "IsBowIdle";

    [Header("Magic Animation Params")]
    [SerializeField] private string skillStaffParam = "Skill_Staff";
    [SerializeField] private string skillWandParam  = "Skill_Wand";
    [SerializeField] private string isWandHoldParam = "IsWandHold";

    [Header("Bow Animation Params")]
    [SerializeField] private string attackBowParam = "Attack_Bow";
    [SerializeField] private string holdBowParam = "Hold_Bow";

    [Header("Spear Throw Params")]
    [SerializeField] private string holdSpearParam   = "Hold_Spear";
    [SerializeField] private string throwSpearParam  = "Throw_Spear";

    private Animator _anim;
    private GameObject _equippedWeapon;
    private WeaponSO _equippedSO;
    private bool _isAttacking;
    private DrakkarTrail _weaponTrail;

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

        switch (_equippedSO.weaponType)
        {
            case WeaponType.Bow:
                HandleBowInput();
                break;
            case WeaponType.Spear:
                HandleSpearInput();
                break;
            case WeaponType.Staff:
                HandleStaffInput();
                break;
            case WeaponType.Wand:
                HandleWandInput();
                break;
            default:
                if (Input.GetMouseButtonDown(0))
                    StartCoroutine(AttackRoutine());
                break;
        }
    }

    // ---------------- ACTION LOCK ----------------
    private void SetInAction(bool value)
    {
        _anim.SetBool(isInActionParam, value);
    }

    // ---------------- CORE ----------------

    private void ClearAllIdle()
    {
        _anim.SetBool(idle1HParam, false);
        _anim.SetBool(idle2HParam, false);
        _anim.SetBool(idleSpearParam, false);
        _anim.SetBool(idleBowParam, false);
    }

    private void RestoreIdle()
    {
        if (_anim == null) return;

        // reset ก่อน
        _anim.SetBool(idle1HParam, false);
        _anim.SetBool(idle2HParam, false);
        _anim.SetBool(idleSpearParam, false);
        _anim.SetBool(idleBowParam, false);

        // ถ้าไม่มีอาวุธ → ปล่อยทุก idle = false (ไป Idle_NoWeapon)
        if (_equippedSO == null) return;

        // เปิด idle ตามอาวุธ
        _anim.SetBool(idle1HParam, _equippedSO.weaponType is WeaponType.OneHand or WeaponType.Staff or WeaponType.Shield or WeaponType.Wand);
        _anim.SetBool(idle2HParam, _equippedSO.weaponType == WeaponType.TwoHand);
        _anim.SetBool(idleSpearParam, _equippedSO.weaponType == WeaponType.Spear);
        _anim.SetBool(idleBowParam, _equippedSO.weaponType == WeaponType.Bow);
        }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        SetInAction(true);      
        ClearAllIdle();        

        _anim.SetTrigger(GetAttackParam());

        // Wait for transition to start or state to change from "Empty"
        yield return new WaitUntil(() => _anim.IsInTransition(attackAnimatorLayer) || !_anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer).IsName("Empty"));

        AnimatorStateInfo state;
        if (_anim.IsInTransition(attackAnimatorLayer))
            state = _anim.GetNextAnimatorStateInfo(attackAnimatorLayer);
        else
            state = _anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer);

        float clipLength = state.length > 0.1f ? state.length : 1f;

        _weaponTrail?.Begin();
        yield return new WaitForSeconds(clipLength);
        _weaponTrail?.End();

        _isAttacking = false;

        SetInAction(false);     
        RestoreIdle();         
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

    // ---------------- MAGIC ----------------

    private void HandleStaffInput()
    {
        if (Input.GetMouseButtonDown(1))
            StartCoroutine(StaffSkillRoutine());

        if (Input.GetMouseButtonDown(0))
            StartCoroutine(AttackRoutine());
    }

    private IEnumerator StaffSkillRoutine()
    {
        _isAttacking = true;
        SetInAction(true);
        ClearAllIdle();

        _anim.SetTrigger(skillStaffParam);

        // รอจนกว่า animator จะเข้า state "Staff_Skill" จริงๆ
        yield return null;
        while (!_anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer).IsName("Staff_Skill"))
            yield return null;

        // อ่าน clip length แล้วรอให้เล่นจบ
        AnimatorStateInfo state = _anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer);
        yield return new WaitForSeconds(state.length);

        _isAttacking = false;
        SetInAction(false);
        RestoreIdle();
    }

    private void HandleWandInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            SetInAction(true);
            ClearAllIdle();
            UpdateWeaponGrip(true);

            _anim.SetTrigger(skillWandParam);
            _anim.SetBool(isWandHoldParam, true);
        }

        if (Input.GetMouseButtonUp(1))
        {
            _anim.SetBool(isWandHoldParam, false);
            UpdateWeaponGrip(false);

            SetInAction(false);
            RestoreIdle();
        }

        if (Input.GetMouseButtonDown(0))
            StartCoroutine(AttackRoutine());
    }

    // ---------------- BOW ----------------

    private void HandleBowInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SetInAction(true);
            ClearAllIdle();
            UpdateWeaponGrip(true);

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
        yield return new WaitForSeconds(0.2f);

        _anim.SetBool(attackBowParam, false);
        UpdateWeaponGrip(false);

        SetInAction(false); 
        RestoreIdle();
    }

    // ---------------- SPEAR ----------------

    private bool _isHoldingSpear = false;

    private void HandleSpearInput()
    {
        // RMB hold → enter hold/aim state
        if (Input.GetMouseButtonDown(1) && !_isHoldingSpear)
        {
            _isHoldingSpear = true;
            SetInAction(true);
            ClearAllIdle();
            UpdateWeaponGrip(true);
            _anim.SetBool(holdSpearParam, true);
        }

        // RMB release → cancel hold, no throw
        if (Input.GetMouseButtonUp(1) && _isHoldingSpear)
        {
            _isHoldingSpear = false;
            _anim.SetBool(holdSpearParam, false);
            UpdateWeaponGrip(false);
            SetInAction(false);
            RestoreIdle();
        }

        // LMB while holding → throw
        if (Input.GetMouseButtonDown(0) && _isHoldingSpear)
        {
            _isHoldingSpear = false;
            _anim.SetBool(holdSpearParam, false);
            _anim.SetTrigger(throwSpearParam);
            StartCoroutine(FinishSpearThrowRoutine());
        }
        // LMB without hold → normal melee
        else if (Input.GetMouseButtonDown(0) && !_isHoldingSpear)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator FinishSpearThrowRoutine()
    {
        _isAttacking = true;

        // Wait for throw anim to start
        yield return new WaitUntil(() => _anim.IsInTransition(attackAnimatorLayer) ||
                                         !_anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer).IsName("Empty"));

        AnimatorStateInfo state = _anim.IsInTransition(attackAnimatorLayer)
            ? _anim.GetNextAnimatorStateInfo(attackAnimatorLayer)
            : _anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer);

        float clipLength = state.length > 0.1f ? state.length : 0.8f;

        yield return new WaitForSeconds(clipLength);

        _isAttacking = false;
        UpdateWeaponGrip(false);
        SetInAction(false);
        RestoreIdle();
    }

    private void UpdateWeaponGrip(bool isHolding)
    {
        if (_equippedWeapon == null || _equippedSO == null) return;

        if (isHolding)
        {
            _equippedWeapon.transform.localPosition = _equippedSO.holdPositionOffset;
            _equippedWeapon.transform.localEulerAngles = _equippedSO.holdRotationOffset;
        }
        else
        {
            _equippedWeapon.transform.localPosition = _equippedSO.gripPositionOffset;
            _equippedWeapon.transform.localEulerAngles = _equippedSO.gripRotationOffset;
        }
    }

    // ---------------- AIM ----------------

    private Vector3 GetCrosshairTarget()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimMask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return ray.origin + ray.direction * aimDistance;
    }

    // ---------------- EVENTS ----------------

    public void ExecuteShoot()
    {
        if (arrowPrefab == null || shotPoint == null) return;

        Vector3 target = GetCrosshairTarget();
        Vector3 dir    = (target - shotPoint.position).normalized;

        GameObject arrow = Instantiate(arrowPrefab, shotPoint.position, Quaternion.LookRotation(dir));
        if (arrow.TryGetComponent<ProjectilePrefab>(out var proj))
            proj.Launch(dir, arrowForce);
    }

    public void ExecuteSpearThrow()
    {
        if (_equippedSO == null || throwPoint == null) return;

        GameObject prefabToThrow = _equippedSO.throwPrefab != null ? _equippedSO.throwPrefab : _equippedSO.prefab;

        Vector3 target = GetCrosshairTarget();
        Vector3 dir    = (target - throwPoint.position).normalized;

        GameObject spear = Instantiate(prefabToThrow, throwPoint.position, Quaternion.LookRotation(dir));
        if (spear.TryGetComponent<ProjectilePrefab>(out var proj))
            proj.Launch(dir, spearForce);

        StartCoroutine(HideWeaponInHand());
    }

    private IEnumerator HideWeaponInHand()
    {
        _equippedWeapon.SetActive(false);
        yield return new WaitForSeconds(0.6f);
        _equippedWeapon.SetActive(true);
    }

    // ---------------- EQUIP ----------------

    public void EquipWeapon(WeaponSO so)
    {
        InterruptUpperOnly();

        if (_equippedWeapon != null)
            Destroy(_equippedWeapon);

        _equippedSO = so;
        _isAttacking = false;
        _isHoldingSpear = false;

        UpdateWeaponGrip(false);

        ClearAllIdle(); // reset ก่อน

        if (so != null)
        {
            _anim.SetBool(idle1HParam, so.weaponType is WeaponType.OneHand or WeaponType.Staff or WeaponType.Shield or WeaponType.Wand);
            _anim.SetBool(idle2HParam, so.weaponType == WeaponType.TwoHand);
            _anim.SetBool(idleSpearParam, so.weaponType == WeaponType.Spear);
            _anim.SetBool(idleBowParam, so.weaponType == WeaponType.Bow);
        }

        if (so == null || so.prefab == null) return;

        Transform slot = (so.useOffHand && offHandSlot != null) ? offHandSlot : weaponSlot;

        _equippedWeapon = Instantiate(so.prefab, slot);
        UpdateWeaponGrip(false);

        // Disable physics on hand weapon — prefab may have Rigidbody for throw version
        var equippedRb = _equippedWeapon.GetComponent<Rigidbody>();
        if (equippedRb != null) equippedRb.isKinematic = true;

        _weaponTrail = _equippedWeapon.GetComponentInChildren<DrakkarTrail>();

        if (so.weaponType == WeaponType.Bow)
        {
            var bowString = _equippedWeapon.GetComponent<BowStringController>();
            if (bowString != null)
                bowString.handStringPoint = handStringAnchor;

            Transform foundPoint = _equippedWeapon.transform.Find("ShotPoint");
            if (foundPoint != null)
                shotPoint = foundPoint;
        }
    }

    //------------------ Interupt ------------------
    private void InterruptUpperOnly()
    {
        StopAllCoroutines();
        _isAttacking = false;
        _isHoldingSpear = false;

        _anim.SetBool(isWandHoldParam, false);
        _anim.SetBool(holdBowParam, false);
        _anim.SetBool(holdSpearParam, false);

        SetInAction(false); // ปลดล็อก Idle

        _anim.Play("Empty", attackAnimatorLayer, 0f);
        _anim.Update(0f);

        _weaponTrail?.End();

        RestoreIdle(); // กลับ Idle ของอาวุธใหม่
    }
    
}