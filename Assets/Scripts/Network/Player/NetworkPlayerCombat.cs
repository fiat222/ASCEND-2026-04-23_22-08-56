using System.Collections;
using UnityEngine;
using PurrNet;
using Drakkar.GameUtils;
using MaykerStudio.Demo;

public class NetworkPlayerCombat : NetworkBehaviour
{
    [Header("Weapon Slots & UI")]
    [SerializeField] private Transform       weaponSlot;
    [SerializeField] private Transform       offHandSlot;
    [SerializeField] private HotbarController hotbar;

    [Header("Magic Ball")]
    public GameObject magicBallPrefab;
    public Transform  magicPoint;
    public float      magicBallSpeed    = 15f;
    public float      magicBallDistance = 50f;

    [Header("Staff Spell")]
    private StaffSpell _staffSpell;

    [Header("Wand Laser")]
    private WandLaser _wandLaser;

    [Header("Bow & Spear")]
    public GameObject arrowPrefab;
    public Transform  shotPoint;
    public float      arrowForce = 1500f;
    public Transform  throwPoint;
    public float      spearForce = 25f;
    [SerializeField] private Transform handStringAnchor;

    [Header("Aim")]
    [SerializeField] private float     aimDistance = 100f;
    [SerializeField] private LayerMask aimMask     = ~0;

    [Header("Animation Params — General")]
    [SerializeField] private string attack1HParam     = "Attack1H";
    [SerializeField] private string attack2HParam     = "Attack2H";
    [SerializeField] private string attackMagicParam  = "AttackMagic";
    [SerializeField] private string attackShieldParam = "AttackShield";
    [SerializeField] private string attackSpearParam  = "AttackSpear";
    [SerializeField] private string isInActionParam   = "IsInAction";
    [SerializeField] private int    attackAnimatorLayer = 1;

    [Header("Idle States")]
    [SerializeField] private string idle1HParam     = "Is1HIdle";
    [SerializeField] private string idle2HParam     = "Is2HIdle";
    [SerializeField] private string idleSpearParam  = "IsSpearIdle";
    [SerializeField] private string idleBowParam    = "IsBowIdle";
    [SerializeField] private string idleShieldParam = "IsShieldIdle";

    [Header("Magic Params")]
    [SerializeField] private string skillStaffParam = "Skill_Staff";
    [SerializeField] private string skillWandParam  = "Skill_Wand";
    [SerializeField] private string isWandHoldParam = "IsWandHold";

    [Header("Bow Params")]
    [SerializeField] private string attackBowParam = "Attack_Bow";
    [SerializeField] private string holdBowParam   = "Hold_Bow";

    [Header("Spear Params")]
    [SerializeField] private string holdSpearParam  = "Hold_Spear";
    [SerializeField] private string throwSpearParam = "Throw_Spear";

    private Animator      _anim;
    private GameObject    _equippedWeapon;
    private WeaponSO      _equippedSO;
    private bool          _isAttacking;
    private bool          _isStaffCharging;
    private bool          _isHoldingSpear;
    private DrakkarTrail  _weaponTrail;

    protected override void OnSpawned()
    {
        _anim = GetComponentInChildren<Animator>();

        if (!isOwner) return;

        if (hotbar != null)
        {
            hotbar.OnSlotChanged += _ => EquipWeapon(hotbar.SelectedWeapon);
            EquipWeapon(hotbar.SelectedWeapon);
        }
    }

    private void Update()
    {
        if (!isOwner || _equippedSO == null) return;

        if (_isStaffCharging && Input.GetMouseButtonUp(1))
        {
            _isStaffCharging = false;
            StartCoroutine(StaffSkillRoutine());
            return;
        }

        if (_isAttacking) return;

        switch (_equippedSO.weaponType)
        {
            case WeaponType.Bow:   HandleBowInput();   break;
            case WeaponType.Spear: HandleSpearInput(); break;
            case WeaponType.Staff: HandleStaffInput(); break;
            case WeaponType.Wand:  HandleWandInput();  break;
            default:
                if (Input.GetMouseButtonDown(0))
                    StartCoroutine(AttackRoutine());
                break;
        }
    }

    // ── Action lock ────────────────────────────────────────────────────────────

    private void SetInAction(bool value) => _anim.SetBool(isInActionParam, value);

    // ── Idle state helpers ─────────────────────────────────────────────────────

    private void ClearAllIdle()
    {
        _anim.SetBool(idle1HParam,     false);
        _anim.SetBool(idle2HParam,     false);
        _anim.SetBool(idleSpearParam,  false);
        _anim.SetBool(idleBowParam,    false);
        _anim.SetBool(idleShieldParam, false);
    }

    private void RestoreIdle()
    {
        if (_anim == null) return;

        ClearAllIdle();

        if (_equippedSO == null) return;

        _anim.SetBool(idle1HParam,     _equippedSO.weaponType is WeaponType.OneHand or WeaponType.Staff or WeaponType.Wand);
        _anim.SetBool(idle2HParam,     _equippedSO.weaponType == WeaponType.TwoHand);
        _anim.SetBool(idleSpearParam,  _equippedSO.weaponType == WeaponType.Spear);
        _anim.SetBool(idleBowParam,    _equippedSO.weaponType == WeaponType.Bow);
        _anim.SetBool(idleShieldParam, _equippedSO.weaponType == WeaponType.Shield);
    }

    // ── Attack routing ─────────────────────────────────────────────────────────

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

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        SetInAction(true);
        ClearAllIdle();

        string trigger = GetAttackParam();
        _anim.SetTrigger(trigger);
        RequestAttackServerRpc(trigger);

        yield return new WaitUntil(() =>
            _anim.IsInTransition(attackAnimatorLayer) ||
            !_anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer).IsName("Empty"));

        AnimatorStateInfo state = _anim.IsInTransition(attackAnimatorLayer)
            ? _anim.GetNextAnimatorStateInfo(attackAnimatorLayer)
            : _anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer);

        float clipLength = state.length > 0.1f ? state.length : 1f;

        _weaponTrail?.Begin();
        yield return new WaitForSeconds(clipLength);
        _weaponTrail?.End();

        _isAttacking = false;
        SetInAction(false);
        RestoreIdle();
    }

    [ServerRpc]
    private void RequestAttackServerRpc(string trigger) => PlayAttackObserversRpc(trigger);

    [ObserversRpc(excludeOwner: true)]
    private void PlayAttackObserversRpc(string trigger) => _anim?.SetTrigger(trigger);

    // ── Staff ──────────────────────────────────────────────────────────────────

    private void HandleStaffInput()
    {
        if (Input.GetMouseButtonDown(1) && !_isStaffCharging)
        {
            _isStaffCharging = true;
            SetInAction(true);
            ClearAllIdle();
            _staffSpell?.BeginCast();
        }

        if (Input.GetMouseButtonDown(0) && !_isStaffCharging)
            StartCoroutine(AttackRoutine());
    }

    private IEnumerator StaffSkillRoutine()
    {
        _isAttacking = true;

        _anim.SetTrigger(skillStaffParam);
        RequestAttackServerRpc(skillStaffParam);

        yield return null;
        while (!_anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer).IsName("Staff_Skill"))
            yield return null;

        float len = _anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer).length;
        yield return new WaitForSeconds(len);

        _isAttacking = false;
        SetInAction(false);
        RestoreIdle();
    }

    // ── Wand ───────────────────────────────────────────────────────────────────

    private void HandleWandInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            SetInAction(true);
            ClearAllIdle();
            UpdateWeaponGrip(true);
            _anim.SetTrigger(skillWandParam);
            _anim.SetBool(isWandHoldParam, true);
            _wandLaser?.StartFire();
        }

        if (Input.GetMouseButtonUp(1))
        {
            _anim.SetBool(isWandHoldParam, false);
            UpdateWeaponGrip(false);
            _wandLaser?.StopFire();
            SetInAction(false);
            RestoreIdle();
        }

        if (Input.GetMouseButtonDown(0))
            StartCoroutine(AttackRoutine());
    }

    // ── Bow ────────────────────────────────────────────────────────────────────

    private void HandleBowInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SetInAction(true);
            ClearAllIdle();
            UpdateWeaponGrip(true);
            _anim.SetBool(attackBowParam, true);
            _anim.SetBool(holdBowParam,   true);
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

    // ── Spear ──────────────────────────────────────────────────────────────────

    private void HandleSpearInput()
    {
        if (Input.GetMouseButtonDown(1) && !_isHoldingSpear)
        {
            _isHoldingSpear = true;
            SetInAction(true);
            ClearAllIdle();
            UpdateWeaponGrip(true);
            _anim.SetBool(holdSpearParam, true);
        }

        if (Input.GetMouseButtonUp(1) && _isHoldingSpear)
        {
            _isHoldingSpear = false;
            _anim.SetBool(holdSpearParam, false);
            UpdateWeaponGrip(false);
            SetInAction(false);
            RestoreIdle();
        }

        if (Input.GetMouseButtonDown(0) && _isHoldingSpear)
        {
            _isHoldingSpear = false;
            _anim.SetBool(holdSpearParam, false);
            _anim.SetTrigger(throwSpearParam);
            RequestAttackServerRpc(throwSpearParam);
            StartCoroutine(FinishSpearThrowRoutine());
        }
        else if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator FinishSpearThrowRoutine()
    {
        _isAttacking = true;

        yield return new WaitUntil(() =>
            _anim.IsInTransition(attackAnimatorLayer) ||
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

    // ── Grip switch ────────────────────────────────────────────────────────────

    private void UpdateWeaponGrip(bool isHolding)
    {
        if (_equippedWeapon == null || _equippedSO == null) return;

        _equippedWeapon.transform.localPosition    = isHolding ? _equippedSO.holdPositionOffset : _equippedSO.gripPositionOffset;
        _equippedWeapon.transform.localEulerAngles = isHolding ? _equippedSO.holdRotationOffset : _equippedSO.gripRotationOffset;
    }

    // ── Aim ────────────────────────────────────────────────────────────────────

    private Vector3 GetCrosshairTarget()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimMask, QueryTriggerInteraction.Ignore)
            ? hit.point
            : ray.origin + ray.direction * aimDistance;
    }

    // ── Animation Events ───────────────────────────────────────────────────────

    public void ExecuteShoot()
    {
        if (arrowPrefab == null || shotPoint == null) return;

        Vector3 dir   = (GetCrosshairTarget() - shotPoint.position).normalized;
        GameObject arrow = Instantiate(arrowPrefab, shotPoint.position, Quaternion.LookRotation(dir));
        if (arrow.TryGetComponent<ProjectilePrefab>(out var proj)) proj.Launch(dir, arrowForce);
    }

    public void ExecuteMagicBall()
    {
        if (magicBallPrefab == null || magicPoint == null) return;

        Vector3 dir  = (GetCrosshairTarget() - magicPoint.position).normalized;
        GameObject ball = Instantiate(magicBallPrefab, magicPoint.position, Quaternion.LookRotation(dir));
        if (ball.TryGetComponent<Projectile>(out var proj))
        {
            proj.speed    = magicBallSpeed;
            proj.distance = magicBallDistance;
            proj.Fire();
        }
    }

    public void OnStaffImpact() => _staffSpell?.Cast();

    public void ExecuteSpearThrow()
    {
        if (_equippedSO == null || throwPoint == null) return;

        GameObject prefabToThrow = _equippedSO.throwPrefab != null ? _equippedSO.throwPrefab : _equippedSO.prefab;
        Vector3    dir           = (GetCrosshairTarget() - throwPoint.position).normalized;

        GameObject spear = Instantiate(prefabToThrow, throwPoint.position, Quaternion.LookRotation(dir));
        if (spear.TryGetComponent<ProjectilePrefab>(out var proj)) proj.Launch(dir, spearForce);

        StartCoroutine(HideWeaponInHand());
    }

    private IEnumerator HideWeaponInHand()
    {
        _equippedWeapon.SetActive(false);
        yield return new WaitForSeconds(0.6f);
        _equippedWeapon.SetActive(true);
    }

    // ── Equip ──────────────────────────────────────────────────────────────────

    public void EquipWeapon(WeaponSO so)
    {
        InterruptUpperBody();

        if (_equippedWeapon != null) Destroy(_equippedWeapon);

        _equippedSO     = so;
        _isAttacking    = false;
        _isHoldingSpear = false;

        ClearAllIdle();

        if (so != null)
        {
            _anim.SetBool(idle1HParam,     so.weaponType is WeaponType.OneHand or WeaponType.Staff or WeaponType.Wand);
            _anim.SetBool(idle2HParam,     so.weaponType == WeaponType.TwoHand);
            _anim.SetBool(idleSpearParam,  so.weaponType == WeaponType.Spear);
            _anim.SetBool(idleBowParam,    so.weaponType == WeaponType.Bow);
            _anim.SetBool(idleShieldParam, so.weaponType == WeaponType.Shield);
        }

        if (so == null || so.prefab == null) return;

        Transform slot = (so.useOffHand && offHandSlot != null) ? offHandSlot : weaponSlot;
        _equippedWeapon = Instantiate(so.prefab, slot);
        UpdateWeaponGrip(false);

        var rb = _equippedWeapon.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        _weaponTrail = _equippedWeapon.GetComponentInChildren<DrakkarTrail>();

        if (so.weaponType == WeaponType.Bow)
        {
            var bowStr = _equippedWeapon.GetComponent<BowStringController>();
            if (bowStr != null)
            {
                bowStr.handStringPoint = handStringAnchor;
                bowStr.playerAnim      = _anim;
            }

            Transform sp = _equippedWeapon.transform.Find("ShotPoint");
            if (sp != null) shotPoint = sp;
        }

        if (so.weaponType is WeaponType.Staff or WeaponType.Wand)
        {
            Transform mp = _equippedWeapon.transform.Find("MagicPoint");
            if (mp != null) magicPoint = mp;
        }

        _staffSpell = so.weaponType == WeaponType.Staff
            ? _equippedWeapon.GetComponentInChildren<StaffSpell>()
            : null;

        _wandLaser = so.weaponType == WeaponType.Wand
            ? _equippedWeapon.GetComponentInChildren<WandLaser>()
            : null;
    }

    // ── Interrupt ──────────────────────────────────────────────────────────────

    private void InterruptUpperBody()
    {
        StopAllCoroutines();
        _isAttacking     = false;
        _isHoldingSpear  = false;
        _isStaffCharging = false;

        _staffSpell?.CancelCast();
        _wandLaser?.StopFire();

        _anim.SetBool(isWandHoldParam, false);
        _anim.SetBool(holdBowParam,    false);
        _anim.SetBool(holdSpearParam,  false);

        SetInAction(false);
        _anim.Play("Empty", attackAnimatorLayer, 0f);
        _anim.Update(0f);

        _weaponTrail?.End();
        RestoreIdle();
    }
}
