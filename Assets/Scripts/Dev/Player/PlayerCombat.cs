using System.Collections;
using UnityEngine;
using Drakkar.GameUtils;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combat Stats")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Weapon Slots & UI")]
    [SerializeField] private Transform        weaponSlot;
    [SerializeField] private Transform        offHandSlot;
    [SerializeField] private HotbarController hotbar;

    [Header("Projectile Points")]
    public Transform magicPoint;
    public Transform shotPoint;
    public Transform throwPoint;
    [SerializeField] private Transform handStringAnchor;

    [Header("Staff Spell")]
    private StaffSpell staffSpell;

    [Header("Wand Laser")]
    private WandLaser wandLaser;

    [Header("Aim")]
    [SerializeField] private float     aimDistance = 100f;
    [SerializeField] private LayerMask aimMask     = ~0;

    [Header("Animation Params - General")]
    [SerializeField] private string attack1HParam     = "Attack1H";
    [SerializeField] private string attack2HParam     = "Attack2H";
    [SerializeField] private string attackMagicParam  = "AttackMagic";
    [SerializeField] private string attackShieldParam = "AttackShield";
    [SerializeField] private string attackSpearParam  = "AttackSpear";
    [SerializeField] private string isInActionParam   = "IsInAction";
    [SerializeField] private int    attackAnimatorLayer = 1;

    [Header("Idle States")]
    [SerializeField] private string idle1HParam    = "Is1HIdle";
    [SerializeField] private string idle2HParam    = "Is2HIdle";
    [SerializeField] private string idleSpearParam = "IsSpearIdle";
    [SerializeField] private string idleBowParam   = "IsBowIdle";
    [SerializeField] private string idleShieldParam = "IsShieldIdle";

    [Header("Parry")]
    [SerializeField] private float  parryWindow1H    = 0.3f;
    [SerializeField] private string guardBreakParam  = "GuardBreak";

    [Header("1H Block Animation Params")]
    [SerializeField] private string is1HBlockParam = "Is1HBlock";

    [Header("2H Block Animation Params")]
    [SerializeField] private string is2HBlockParam       = "Is2HBlock";
    [SerializeField] private string attack2HCounterParam  = "Attack2HCounter";  // Upper layer (1)
    [SerializeField] private string counter2HBaseParam   = "Counter2HBase";    // Base layer (0)
    [SerializeField] private string counterStateName     = "2H_Counter";
    [SerializeField] private float  counterWindow2H      = 0.5f;

    [Header("Shield Animation Params")]
    [SerializeField] private string isShieldBlockParam = "IsShieldBlock";

    [Header("Shield+Sword Params")]
    [SerializeField] private string isShieldSwordBlockParam = "IsShieldSwordBlock"; // RMB กัน
    [SerializeField] private string shieldSwordParryParam   = "ShieldSwordParry";   // LMB ขณะกัน

    [Header("Magic Animation Params")]
    [SerializeField] private string skillStaffParam  = "Skill_Staff";
    [SerializeField] private string skillWandParam   = "Skill_Wand";
    [SerializeField] private string isWandHoldParam  = "IsWandHold";

    [Header("Bow Animation Params")]
    [SerializeField] private string attackBowParam = "Attack_Bow";
    [SerializeField] private string holdBowParam   = "Hold_Bow";

    [Header("Spear Throw Params")]
    [SerializeField] private string holdSpearParam  = "Hold_Spear";
    [SerializeField] private string throwSpearParam = "Throw_Spear";

    // ─── Private State ────────────────────────────────────────
    private Animator    _anim;

    // Main hand
    private GameObject   _equippedWeapon;
    private WeaponSO     _equippedSO;
    private DrakkarTrail _weaponTrail;
    private WeaponHitbox _hitbox;

    // Off hand
    private GameObject  _equippedOffHand;
    private WeaponSO    _offHandSO;

    // Combat flags
    private bool _isAttacking;
    private bool _isStaffCharging;
    private bool _isWandHolding;
    private bool _is1HBlocking;
    private bool _is2HBlocking;
    private bool _isShieldBlocking;         // โล่เป็น main hand (WeaponType.Shield)
    private bool _isShieldSwordBlocking;    // โล่เป็น off-hand + ดาบ 1H
    private bool _isHoldingSpear;

    // 1H Parry
    private Coroutine _parry1HCoroutine;

    // 2H Counter
    private bool      _canCounter2H;
    private Coroutine _counter2HWindowCoroutine;

    // ─── Helpers ──────────────────────────────────────────────
    /// <summary>ถือโล่เป็น off-hand อยู่ไหม</summary>
    private bool HasShieldOffHand => _offHandSO != null
                                  && _offHandSO.weaponType == WeaponType.Shield;

    // ─── Unity ────────────────────────────────────────────────
    private void Start()
    {
        _anim = GetComponentInChildren<Animator>();

        if (hotbar != null)
        {
            hotbar.OnSlotChanged    += (_) => EquipWeapon(hotbar.SelectedWeapon);
            hotbar.OnOffHandChanged += EquipOffHand;
            EquipWeapon(hotbar.SelectedWeapon);
        }

        if (playerStats != null) playerStats.OnGuardBreak += HandleGuardBreak;
    }

    private void OnDestroy()
    {
        if (playerStats != null) playerStats.OnGuardBreak -= HandleGuardBreak;
    }

    private void Update()
    {
        if (_equippedSO == null) return;

        // Staff charging release
        if (_isStaffCharging && Input.GetMouseButtonUp(1))
        {
            _isStaffCharging = false;
            StartCoroutine(StaffSkillRoutine());
            return;
        }

        // ─── Block tracking (runs even while attacking) ────────
        if (HasShieldOffHand && _equippedSO.weaponType == WeaponType.OneHand)
            TrackShieldSwordBlock();         // ดาบ + โล่ off-hand
        else if (_equippedSO.weaponType == WeaponType.OneHand)
            Track1HParry();
        else if (_equippedSO.weaponType == WeaponType.TwoHand)
            Track2HBlock();
        else if (_equippedSO.weaponType == WeaponType.Shield)
            TrackShieldBlock();

        if (_isAttacking) return;

        // ─── Action input ──────────────────────────────────────
        switch (_equippedSO.weaponType)
        {
            case WeaponType.Bow:    HandleBowInput();    break;
            case WeaponType.Spear:  HandleSpearInput();  break;
            case WeaponType.Staff:  HandleStaffInput();  break;
            case WeaponType.Wand:   HandleWandInput();   break;
            case WeaponType.Shield: HandleShieldInput(); break;
            case WeaponType.Torch:  Handle1HInput();     break;  // Torch — ฟันแบบ 1H ธรรมดา
            case WeaponType.OneHand:
                if (HasShieldOffHand) HandleShieldSwordInput();
                else                  Handle1HInput();
                break;
            case WeaponType.TwoHand: Handle2HInput();   break;
            default:
                if (Input.GetMouseButtonDown(0))
                    StartCoroutine(AttackRoutine());
                break;
        }
    }

    // ─── ACTION LOCK ──────────────────────────────────────────
    private void SetInAction(bool value) => _anim.SetBool(isInActionParam, value);

    // ─── IDLE ─────────────────────────────────────────────────
    private void ClearAllIdle()
    {
        _anim.SetBool(idle1HParam,    false);
        _anim.SetBool(idle2HParam,    false);
        _anim.SetBool(idleSpearParam, false);
        _anim.SetBool(idleBowParam,   false);
        _anim.SetBool(idleShieldParam,false);
    }

    private void RestoreIdle()
    {
        if (_anim == null) return;

        ClearAllIdle();
        if (_equippedSO == null) return;

        _anim.SetBool(idle1HParam,     _equippedSO.weaponType is WeaponType.OneHand
                                                              or WeaponType.Torch
                                                              or WeaponType.Staff
                                                              or WeaponType.Wand);
        _anim.SetBool(idle2HParam,     _equippedSO.weaponType == WeaponType.TwoHand);
        _anim.SetBool(idleSpearParam,  _equippedSO.weaponType == WeaponType.Spear);
        _anim.SetBool(idleBowParam,    _equippedSO.weaponType == WeaponType.Bow);
        _anim.SetBool(idleShieldParam, _equippedSO.weaponType == WeaponType.Shield);
    }

    // ─── ATTACK ROUTINE ───────────────────────────────────────
    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        SetInAction(true);
        ClearAllIdle();
        playerStats?.DrainStamina(_equippedSO?.staminaCost ?? 10f);

        _anim.SetTrigger(GetAttackParam());

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

    private string GetAttackParam()
    {
        if (_equippedSO == null) return attack1HParam;

        return _equippedSO.AttackType switch
        {
            AttackType.TwoHand => attack2HParam,
            AttackType.Magic   => attackMagicParam,
            AttackType.Shield  => attackShieldParam,
            AttackType.Spear   => attackSpearParam,
            AttackType.Torch   => attack1HParam,   // Torch ใช้ animation เดียวกับ 1H
            _                  => attack1HParam
        };
    }

    // ─── MAGIC ────────────────────────────────────────────────
    private void HandleStaffInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _isStaffCharging = true;
            SetInAction(true);
            ClearAllIdle();
            staffSpell?.BeginCast();
        }

        if (Input.GetMouseButtonDown(0) && !_isStaffCharging)
            StartCoroutine(AttackRoutine());
    }

    private IEnumerator StaffSkillRoutine()
    {
        _isAttacking = true;
        playerStats?.DrainStamina(_equippedSO?.staminaCost ?? 10f);
        _anim.SetTrigger(skillStaffParam);

        yield return null;
        while (!_anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer).IsName("Staff_Skill"))
            yield return null;

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
            _isWandHolding = true;
            SetInAction(true);
            ClearAllIdle();
            UpdateWeaponGrip(true);
            _anim.SetTrigger(skillWandParam);
            _anim.SetBool(isWandHoldParam, true);
        }

        if (Input.GetMouseButtonUp(1))
        {
            _isWandHolding = false;
            _anim.SetBool(isWandHoldParam, false);
            UpdateWeaponGrip(false);
            wandLaser?.StopFire();
            SetInAction(false);
            RestoreIdle();
        }

        if (Input.GetMouseButtonDown(0))
            StartCoroutine(AttackRoutine());
    }

    // ─── 1H PARRY (ไม่มีโล่ — แตะ RMB = parry window สั้น) ──────────────────
    private void Track1HParry()
    {
        if (Input.GetMouseButtonDown(1) && _parry1HCoroutine == null && !_isAttacking)
            _parry1HCoroutine = StartCoroutine(Parry1HRoutine());

        if (Input.GetMouseButtonUp(1) && _is1HBlocking)
        {
            if (_parry1HCoroutine != null) { StopCoroutine(_parry1HCoroutine); _parry1HCoroutine = null; }
            _is1HBlocking = false;
            playerStats.IsParrying = false;
            _anim.SetBool(is1HBlockParam, false);
            if (!_isAttacking) { SetInAction(false); RestoreIdle(); }
        }
    }

    private IEnumerator Parry1HRoutine()
    {
        _is1HBlocking = true;
        SetInAction(true);
        ClearAllIdle();
        _anim.SetBool(is1HBlockParam, true);
        playerStats.IsParrying = true;

        yield return new WaitForSeconds(parryWindow1H);

        // Parry window expired — switch to hold-block (lower guard than 2H, no base stability)
        playerStats.IsParrying = false;
        playerStats.IsBlocking = true;
        _parry1HCoroutine = null;
        // _is1HBlocking + Is1HBlock stay true — RMB release clears everything
    }

    private void Handle1HInput()
    {
        if (Input.GetMouseButtonDown(0) && !_is1HBlocking)
            StartCoroutine(AttackRoutine());
    }

    // ─── SHIELD + SWORD (โล่เป็น off-hand) ───────────────────

    /// <summary>
    /// RMB → กันด้วยโล่
    /// LMB ขณะกัน → Parry
    /// LMB ปกติ → ฟัน
    /// </summary>
    private void TrackShieldSwordBlock()
    {
        // RMB down → เริ่มกัน
        if (Input.GetMouseButtonDown(1) && !_isShieldSwordBlocking)
        {
            _isShieldSwordBlocking = true;
            playerStats.IsBlocking = true;
            Debug.Log($"[Block] Shield+Sword block START — HasShieldOffHand={HasShieldOffHand} offHandSO={_offHandSO?.weaponName} mainSO={_equippedSO?.weaponName}");
            if (!_isAttacking)
            {
                SetInAction(true);
                ClearAllIdle();
                _anim.SetBool(isShieldSwordBlockParam, true);
            }
        }

        // RMB up → เลิกกัน
        if (Input.GetMouseButtonUp(1) && _isShieldSwordBlocking)
        {
            _isShieldSwordBlocking = false;
            playerStats.IsBlocking = false;
            Debug.Log("[Block] Shield+Sword block END");
            _anim.SetBool(isShieldSwordBlockParam, false);
            if (!_isAttacking) { SetInAction(false); RestoreIdle(); }
        }
    }

    private void HandleShieldSwordInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (_isShieldSwordBlocking)
            {
                // LMB ขณะกด RMB ค้าง → Parry
                StartCoroutine(ParryRoutine());
            }
            else
            {
                // LMB ปกติ → ฟัน
                StartCoroutine(AttackRoutine());
            }
        }
    }

    private IEnumerator ParryRoutine()
    {
        _isAttacking = true;
        playerStats?.DrainStamina(_equippedSO?.staminaCost ?? 10f);

        // ปิด block ชั่วคราวระหว่าง parry anim
        _anim.SetBool(isShieldSwordBlockParam, false);
        _anim.SetTrigger(shieldSwordParryParam);

        yield return new WaitUntil(() =>
            _anim.IsInTransition(attackAnimatorLayer) ||
            !_anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer).IsName("Empty"));

        AnimatorStateInfo state = _anim.IsInTransition(attackAnimatorLayer)
            ? _anim.GetNextAnimatorStateInfo(attackAnimatorLayer)
            : _anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer);

        float clipLength = state.length > 0.1f ? state.length : 0.5f;
        yield return new WaitForSeconds(clipLength);

        _isAttacking = false;

        // กลับสู่ blocking ถ้ายังกด RMB อยู่
        if (_isShieldSwordBlocking)
            _anim.SetBool(isShieldSwordBlockParam, true);
        else
        {
            SetInAction(false);
            RestoreIdle();
        }
    }

    // ─── GUARD BREAK ─────────────────────────────────────────
    private void HandleGuardBreak()
    {
        _is1HBlocking          = false;
        _is2HBlocking          = false;
        _isShieldBlocking      = false;
        _isShieldSwordBlocking = false;
        playerStats.IsBlocking = false;
        playerStats.IsParrying = false;
        _anim.SetBool(is1HBlockParam,          false);
        _anim.SetBool(is2HBlockParam,          false);
        _anim.SetBool(isShieldBlockParam,      false);
        _anim.SetBool(isShieldSwordBlockParam, false);
        StartCoroutine(GuardBreakRoutine());
    }

    private IEnumerator GuardBreakRoutine()
    {
        _isAttacking = true;
        SetInAction(true);
        ClearAllIdle();
        _anim.SetTrigger(guardBreakParam);

        yield return new WaitUntil(() =>
            _anim.IsInTransition(attackAnimatorLayer) ||
            !_anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer).IsName("Empty"));

        AnimatorStateInfo state = _anim.IsInTransition(attackAnimatorLayer)
            ? _anim.GetNextAnimatorStateInfo(attackAnimatorLayer)
            : _anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer);

        float clipLength = state.length > 0.1f ? state.length : 0.8f;
        yield return new WaitForSeconds(clipLength);

        _isAttacking = false;
        SetInAction(false);
        RestoreIdle();
    }

    // ─── 2H BLOCK ─────────────────────────────────────────────
    private void Track2HBlock()
    {
        if (Input.GetMouseButtonDown(1) && !_is2HBlocking)
        {
            _is2HBlocking = true;
            playerStats.IsBlocking = true;
            if (!_isAttacking) { SetInAction(true); ClearAllIdle(); _anim.SetBool(is2HBlockParam, true); }
        }

        if (Input.GetMouseButtonUp(1) && _is2HBlocking)
        {
            _is2HBlocking = false;
            playerStats.IsBlocking = false;
            _anim.SetBool(is2HBlockParam, false);

            // open counter window on block release
            _canCounter2H = true;
            if (_counter2HWindowCoroutine != null) StopCoroutine(_counter2HWindowCoroutine);
            _counter2HWindowCoroutine = StartCoroutine(Counter2HWindowRoutine());

            if (!_isAttacking) { SetInAction(false); RestoreIdle(); }
        }
    }

    private IEnumerator Counter2HWindowRoutine()
    {
        yield return new WaitForSeconds(counterWindow2H);
        _canCounter2H = false;
        _counter2HWindowCoroutine = null;
    }

    private void Handle2HInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (_canCounter2H)
            StartCoroutine(Counter2HRoutine());
        else if (!_is2HBlocking)
            StartCoroutine(AttackRoutine());
    }

    private IEnumerator Counter2HRoutine()
    {
        _canCounter2H = false;
        if (_counter2HWindowCoroutine != null) { StopCoroutine(_counter2HWindowCoroutine); _counter2HWindowCoroutine = null; }
        playerStats?.DrainStamina(_equippedSO?.staminaCost ?? 10f);

        _isAttacking = true;
        SetInAction(true);
        ClearAllIdle();
        _anim.SetBool(is2HBlockParam, false);
        _anim.SetTrigger(counter2HBaseParam);   // Layer 0
        _anim.SetTrigger(attack2HCounterParam); // Layer 1

        // Poll Base Layer (0) — drives full-body clip length for 360 flip
        yield return new WaitUntil(() =>
            _anim.IsInTransition(0) ||
            _anim.GetCurrentAnimatorStateInfo(0).IsName(counterStateName));

        AnimatorStateInfo state = _anim.IsInTransition(0)
            ? _anim.GetNextAnimatorStateInfo(0)
            : _anim.GetCurrentAnimatorStateInfo(0);

        float clipLength = state.length > 0.1f ? state.length : 1f;

        _weaponTrail?.Begin();
        yield return new WaitForSeconds(clipLength);
        _weaponTrail?.End();

        _isAttacking = false;
        _is2HBlocking = false;
        SetInAction(false);
        RestoreIdle();
    }

    // ─── SHIELD MAIN HAND ─────────────────────────────────────
    private void TrackShieldBlock()
    {
        if (Input.GetMouseButtonDown(1) && !_isShieldBlocking)
        {
            _isShieldBlocking = true;
            playerStats.IsBlocking = true;
            if (!_isAttacking) { SetInAction(true); ClearAllIdle(); _anim.SetBool(isShieldBlockParam, true); }
        }

        if (Input.GetMouseButtonUp(1) && _isShieldBlocking)
        {
            _isShieldBlocking = false;
            playerStats.IsBlocking = false;
            _anim.SetBool(isShieldBlockParam, false);
            if (!_isAttacking) { SetInAction(false); RestoreIdle(); }
        }
    }

    private void HandleShieldInput()
    {
        if (Input.GetMouseButtonDown(0) && !_isShieldBlocking)
            StartCoroutine(AttackRoutine());
    }

    // ─── BOW ──────────────────────────────────────────────────
    private void HandleBowInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            playerStats?.DrainStamina(_equippedSO?.staminaCost ?? 10f);
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

    // ─── SPEAR ────────────────────────────────────────────────
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
            StartCoroutine(FinishSpearThrowRoutine());
        }
        else if (Input.GetMouseButtonDown(0) && !_isHoldingSpear)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator FinishSpearThrowRoutine()
    {
        _isAttacking = true;
        playerStats?.DrainStamina(_equippedSO?.staminaCost ?? 10f);

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

    // ─── GRIP ─────────────────────────────────────────────────
    private void UpdateWeaponGrip(bool isHolding)
    {
        if (_equippedWeapon == null || _equippedSO == null) return;

        _equippedWeapon.transform.localPosition    = isHolding
            ? _equippedSO.holdPositionOffset : _equippedSO.gripPositionOffset;
        _equippedWeapon.transform.localEulerAngles = isHolding
            ? _equippedSO.holdRotationOffset : _equippedSO.gripRotationOffset;
    }

    // ─── AIM ──────────────────────────────────────────────────
    private Vector3 GetCrosshairTarget()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimMask, QueryTriggerInteraction.Ignore)
            ? hit.point
            : ray.origin + ray.direction * aimDistance;
    }

    // ─── HITBOX (Animation Events) ────────────────────────────
    public void OnHitboxOpen()  => _hitbox?.EnableHitbox();
    public void OnHitboxClose() => _hitbox?.DisableHitbox();

    // ─── EVENTS (Animation) ───────────────────────────────────
    public void ExecuteShoot()
    {
        if (_equippedSO?.projectilePrefab == null || shotPoint == null) return;
        Vector3 dir = (GetCrosshairTarget() - shotPoint.position).normalized;
        GameObject arrow = Instantiate(_equippedSO.projectilePrefab, shotPoint.position, Quaternion.LookRotation(dir));
        if (arrow.TryGetComponent<ProjectilePrefab>(out var proj))
        {
            proj.Setup(playerStats, _equippedSO);
            proj.Launch(dir, _equippedSO.projectileForce);
        }
    }

    public void ExecuteMagicBall()
    {
        if (_equippedSO?.projectilePrefab == null || magicPoint == null) return;
        if (playerStats != null && !playerStats.DrainMana(_equippedSO.manaCost)) return;

        Vector3 dir = (GetCrosshairTarget() - magicPoint.position).normalized;
        GameObject ball = Instantiate(_equippedSO.projectilePrefab, magicPoint.position, Quaternion.LookRotation(dir));
        if (ball.TryGetComponent<MagicBallDamage>(out var dmg))
        {
            dmg.speed    = _equippedSO.projectileSpeed;
            dmg.maxRange = _equippedSO.projectileRange;
            dmg.Setup(playerStats, _equippedSO);
        }
    }

    public void OnStaffImpact()  => staffSpell?.Cast();

    public void OnWandFireStart()
    {
        if (_isWandHolding) wandLaser?.StartFire();
    }

    public void ExecuteSpearThrow()
    {
        if (_equippedSO == null || throwPoint == null) return;
        GameObject prefabToThrow = _equippedSO.projectilePrefab != null ? _equippedSO.projectilePrefab : _equippedSO.prefab;
        Vector3 dir = (GetCrosshairTarget() - throwPoint.position).normalized;
        GameObject spear = Instantiate(prefabToThrow, throwPoint.position, Quaternion.LookRotation(dir));
        if (spear.TryGetComponent<ProjectilePrefab>(out var proj))
        {
            proj.Setup(playerStats, _equippedSO);
            proj.Launch(dir, _equippedSO.projectileForce);
        }
        StartCoroutine(HideWeaponInHand());
    }

    private IEnumerator HideWeaponInHand()
    {
        _equippedWeapon.SetActive(false);
        yield return new WaitForSeconds(0.6f);
        _equippedWeapon.SetActive(true);
    }

    // ─── EQUIP MAIN HAND ──────────────────────────────────────
    public void EquipWeapon(WeaponSO so)
    {
        InterruptUpperOnly();

        if (_equippedWeapon != null) Destroy(_equippedWeapon);
        _hitbox = null;

        _equippedSO  = so;
        _isAttacking = false;
        _isHoldingSpear = _is1HBlocking = _is2HBlocking = _isShieldBlocking = false;
        _isShieldSwordBlocking = false;

        playerStats?.SetGuardBase(so?.baseGuardStability ?? 0f);
        if (playerStats != null) { playerStats.IsBlocking = false; playerStats.IsParrying = false; }

        UpdateWeaponGrip(false);
        ClearAllIdle();

        if (so != null)
        {
            _anim.SetBool(idle1HParam,     so.weaponType is WeaponType.OneHand or WeaponType.Torch or WeaponType.Staff or WeaponType.Wand);
            _anim.SetBool(idle2HParam,     so.weaponType == WeaponType.TwoHand);
            _anim.SetBool(idleSpearParam,  so.weaponType == WeaponType.Spear);
            _anim.SetBool(idleBowParam,    so.weaponType == WeaponType.Bow);
            _anim.SetBool(idleShieldParam, so.weaponType == WeaponType.Shield);

            if (so.weaponType == WeaponType.OneHand)
            {
                // Re-show off-hand prefab if hotbar still has one assigned
                if (hotbar?.OffHandWeapon != null && _equippedOffHand == null)
                    EquipOffHand(hotbar.OffHandWeapon);
            }
            else
            {
                // Hide prefab only — keep hotbar off-hand slot intact
                HideOffHandPrefab();
            }
        }

        if (so == null || so.prefab == null) return;

        Transform slot = (so.useOffHand && offHandSlot != null) ? offHandSlot : weaponSlot;
        _equippedWeapon = Instantiate(so.prefab, slot);
        UpdateWeaponGrip(false);

        var rb = _equippedWeapon.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        _weaponTrail = _equippedWeapon.GetComponentInChildren<DrakkarTrail>();

        _hitbox = _equippedWeapon.GetComponentInChildren<WeaponHitbox>();
        _hitbox?.Setup(playerStats, so);

        if (so.weaponType == WeaponType.Bow)
        {
            var bowString = _equippedWeapon.GetComponent<BowStringController>();
            if (bowString != null) { bowString.handStringPoint = handStringAnchor; bowString.playerAnim = _anim; }
            var foundPoint = _equippedWeapon.transform.Find("ShotPoint");
            if (foundPoint != null) shotPoint = foundPoint;
        }

        if (so.weaponType is WeaponType.Staff or WeaponType.Wand)
        {
            var foundPoint = _equippedWeapon.transform.Find("MagicPoint");
            if (foundPoint != null) magicPoint = foundPoint;
        }

        staffSpell = so.weaponType == WeaponType.Staff
            ? _equippedWeapon.GetComponentInChildren<StaffSpell>() : null;

        wandLaser = so.weaponType == WeaponType.Wand
            ? _equippedWeapon.GetComponentInChildren<WandLaser>() : null;
    }

    // ─── EQUIP OFF HAND ───────────────────────────────────────
    private void EquipOffHand(WeaponSO so)
    {
        if (_equippedOffHand != null)
        {
            Destroy(_equippedOffHand);
            _equippedOffHand = null;
        }

        _offHandSO             = so;
        _isShieldSwordBlocking = false;
        _anim.SetBool(isShieldSwordBlockParam, false);

        // Same SO moved to off-hand — hide main-hand prefab, switch to 1H idle
        if (so != null && so == _equippedSO && _equippedWeapon != null)
        {
            Destroy(_equippedWeapon);
            _equippedWeapon = null;
            ClearAllIdle();
            _anim.SetBool(idle1HParam, true);
        }

        if (so == null || so.prefab == null) return;
        if (offHandSlot == null)
        {
            Debug.LogWarning("[PlayerCombat] offHandSlot ไม่ได้ assign ใน Inspector");
            return;
        }

        _equippedOffHand = Instantiate(so.prefab, offHandSlot);
        _equippedOffHand.transform.localPosition    = so.offHandPositionOffset;
        _equippedOffHand.transform.localEulerAngles = so.offHandRotationOffset;

        var rb = _equippedOffHand.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    private void HideOffHandPrefab()
    {
        if (_equippedOffHand != null)
        {
            Destroy(_equippedOffHand);
            _equippedOffHand = null;
        }
        _offHandSO             = null;
        _isShieldSwordBlocking = false;
        _anim.SetBool(isShieldSwordBlockParam, false);
    }

    // ─── INTERRUPT ────────────────────────────────────────────
    private void InterruptUpperOnly()
    {
        StopAllCoroutines();
        _isAttacking = _isHoldingSpear = _isStaffCharging = false;
        _is1HBlocking = _is2HBlocking = _isShieldBlocking = false;
        _isShieldSwordBlocking = _isWandHolding = false;
        _canCounter2H = false;
        _counter2HWindowCoroutine = null;
        _parry1HCoroutine = null;

        if (playerStats != null) { playerStats.IsParrying = false; playerStats.IsBlocking = false; }

        staffSpell?.CancelCast();
        wandLaser?.StopFire();

        _anim.SetBool(isWandHoldParam,           false);
        _anim.SetBool(holdBowParam,              false);
        _anim.SetBool(holdSpearParam,            false);
        _anim.SetBool(is1HBlockParam,            false);
        _anim.SetBool(is2HBlockParam,            false);
        _anim.SetBool(isShieldBlockParam,        false);
        _anim.SetBool(isShieldSwordBlockParam,   false);

        SetInAction(false);
        _anim.Play("Empty", attackAnimatorLayer, 0f);
        _anim.Update(0f);

        _weaponTrail?.End();
        RestoreIdle();
    }
}