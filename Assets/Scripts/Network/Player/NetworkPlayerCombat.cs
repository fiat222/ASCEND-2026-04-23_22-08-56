using System.Collections;
using UnityEngine;
using PurrNet;
using Drakkar.GameUtils;

public class NetworkPlayerCombat : NetworkBehaviour
{
    [Header("Weapon Slots & UI")]
    [SerializeField] private Transform       weaponSlot;
    [SerializeField] private Transform       offHandSlot;
    [SerializeField] private HotbarController hotbar;

    [Header("Projectile Points")]
    public Transform magicPoint;
    public Transform shotPoint;
    public Transform throwPoint;
    [SerializeField] private Transform handStringAnchor;

    [Header("Aim")]
    [SerializeField] private float     aimDistance = 100f;
    [SerializeField] private LayerMask aimMask     = ~0;

    [Header("Animation Params — General")]
    [SerializeField] private string attack1HParam      = "Attack1H";
    [SerializeField] private string attack2HParam      = "Attack2H";
    [SerializeField] private string attackMagicParam   = "AttackMagic";
    [SerializeField] private string attackShieldParam  = "AttackShield";
    [SerializeField] private string attackSpearParam   = "AttackSpear";
    [SerializeField] private string isInActionParam    = "IsInAction";
    [SerializeField] private int    attackAnimatorLayer = 1;

    [Header("Idle States")]
    [SerializeField] private string idle1HParam     = "Is1HIdle";
    [SerializeField] private string idle2HParam     = "Is2HIdle";
    [SerializeField] private string idleSpearParam  = "IsSpearIdle";
    [SerializeField] private string idleBowParam    = "IsBowIdle";
    [SerializeField] private string idleShieldParam = "IsShieldIdle";

    [Header("Parry")]
    [SerializeField] private float  parryWindow1H   = 0.3f;
    [SerializeField] private string guardBreakParam = "GuardBreak";

    [Header("1H Block")]
    [SerializeField] private string is1HBlockParam = "Is1HBlock";

    [Header("2H Block")]
    [SerializeField] private string is2HBlockParam        = "Is2HBlock";
    [SerializeField] private string attack2HCounterParam  = "Attack2HCounter";
    [SerializeField] private string counter2HBaseParam    = "Counter2HBase";
    [SerializeField] private string counterStateName      = "2H_Counter";
    [SerializeField] private float  counterWindow2H       = 0.5f;

    [Header("Shield")]
    [SerializeField] private string isShieldBlockParam = "IsShieldBlock";

    [Header("Shield+Sword")]
    [SerializeField] private string isShieldSwordBlockParam = "IsShieldSwordBlock";
    [SerializeField] private string shieldSwordParryParam   = "ShieldSwordParry";

    [Header("Magic")]
    [SerializeField] private string skillStaffParam = "Skill_Staff";
    [SerializeField] private string skillWandParam  = "Skill_Wand";
    [SerializeField] private string isWandHoldParam = "IsWandHold";

    [Header("Bow")]
    [SerializeField] private string attackBowParam = "Attack_Bow";
    [SerializeField] private string holdBowParam   = "Hold_Bow";

    [Header("Spear")]
    [SerializeField] private string holdSpearParam  = "Hold_Spear";
    [SerializeField] private string throwSpearParam = "Throw_Spear";

    // ── Private state ──────────────────────────────────────────────────────────
    private Animator     _anim;
    private PlayerStats  _playerStats;

    private GameObject   _equippedWeapon;
    private WeaponSO     _equippedSO;
    private DrakkarTrail _weaponTrail;
    private WeaponHitbox _hitbox;

    private GameObject   _equippedOffHand;
    private WeaponSO     _offHandSO;

    private bool _isAttacking;
    private bool _isStaffCharging;
    private bool _isWandHolding;
    private bool _is1HBlocking;
    private bool _is2HBlocking;
    private bool _isShieldBlocking;
    private bool _isShieldSwordBlocking;
    private bool _isHoldingSpear;

    private Coroutine _parry1HCoroutine;
    private bool      _canCounter2H;
    private Coroutine _counter2HWindowCoroutine;

    private bool HasShieldOffHand => _offHandSO != null && _offHandSO.weaponType == WeaponType.Shield;

    // ── PurrNet lifecycle ──────────────────────────────────────────────────────

    protected override void OnSpawned()
    {
        _anim        = GetComponentInChildren<Animator>();
        _playerStats = GetComponent<PlayerStats>();

        if (_playerStats != null) _playerStats.OnGuardBreak += HandleGuardBreak;
    }

    protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
    {
        if (asServer || !isOwner) return;

        if (hotbar == null) hotbar = FindObjectOfType<HotbarController>();

        if (hotbar != null)
        {
            hotbar.OnSlotChanged    += _ => EquipWeapon(hotbar.SelectedWeapon);
            hotbar.OnOffHandChanged += EquipOffHand;
            EquipWeapon(hotbar.SelectedWeapon);
        }
    }

    private void OnDestroy()
    {
        if (_playerStats != null) _playerStats.OnGuardBreak -= HandleGuardBreak;
    }

    // ── Update ─────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!isOwner || _equippedSO == null) return;

        if (_isStaffCharging && Input.GetMouseButtonUp(1))
        {
            _isStaffCharging = false;
            StartCoroutine(StaffSkillRoutine());
            return;
        }

        if (HasShieldOffHand && _equippedSO.weaponType == WeaponType.OneHand)
            TrackShieldSwordBlock();
        else if (_equippedSO.weaponType == WeaponType.OneHand)
            Track1HParry();
        else if (_equippedSO.weaponType == WeaponType.TwoHand)
            Track2HBlock();
        else if (_equippedSO.weaponType == WeaponType.Shield)
            TrackShieldBlock();

        if (_isAttacking) return;

        switch (_equippedSO.weaponType)
        {
            case WeaponType.Bow:    HandleBowInput();    break;
            case WeaponType.Spear:  HandleSpearInput();  break;
            case WeaponType.Staff:  HandleStaffInput();  break;
            case WeaponType.Wand:   HandleWandInput();   break;
            case WeaponType.Shield: HandleShieldInput(); break;
            case WeaponType.Torch:  Handle1HInput();     break;
            case WeaponType.OneHand:
                if (HasShieldOffHand) HandleShieldSwordInput();
                else                  Handle1HInput();
                break;
            case WeaponType.TwoHand: Handle2HInput(); break;
            default:
                if (Input.GetMouseButtonDown(0)) StartCoroutine(AttackRoutine());
                break;
        }
    }

    // ── Action lock ────────────────────────────────────────────────────────────

    private void SetInAction(bool value) => _anim.SetBool(isInActionParam, value);

    // ── Idle ───────────────────────────────────────────────────────────────────

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

        _anim.SetBool(idle1HParam,     _equippedSO.weaponType is WeaponType.OneHand or WeaponType.Torch or WeaponType.Staff or WeaponType.Wand);
        _anim.SetBool(idle2HParam,     _equippedSO.weaponType == WeaponType.TwoHand);
        _anim.SetBool(idleSpearParam,  _equippedSO.weaponType == WeaponType.Spear);
        _anim.SetBool(idleBowParam,    _equippedSO.weaponType == WeaponType.Bow);
        _anim.SetBool(idleShieldParam, _equippedSO.weaponType == WeaponType.Shield);
    }

    // ── Attack ─────────────────────────────────────────────────────────────────

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        SetInAction(true);
        ClearAllIdle();
        _playerStats?.DrainStamina(_equippedSO?.staminaCost ?? 10f);

        string trigger = GetAttackParam();
        _anim.SetTrigger(trigger);
        SyncTriggerRpc(trigger);

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
            AttackType.Torch   => attack1HParam,
            _                  => attack1HParam
        };
    }

    // ── 1H Parry ───────────────────────────────────────────────────────────────

    private void Track1HParry()
    {
        if (Input.GetMouseButtonDown(1) && _parry1HCoroutine == null && !_isAttacking)
            _parry1HCoroutine = StartCoroutine(Parry1HRoutine());

        if (Input.GetMouseButtonUp(1) && _is1HBlocking)
        {
            if (_parry1HCoroutine != null) { StopCoroutine(_parry1HCoroutine); _parry1HCoroutine = null; }
            _is1HBlocking = false;
            _playerStats.IsParrying = false;
            _anim.SetBool(is1HBlockParam, false);
            SyncBoolRpc(is1HBlockParam, false);
            if (!_isAttacking) { SetInAction(false); RestoreIdle(); }
        }
    }

    private IEnumerator Parry1HRoutine()
    {
        _is1HBlocking = true;
        SetInAction(true);
        ClearAllIdle();
        _anim.SetBool(is1HBlockParam, true);
        SyncBoolRpc(is1HBlockParam, true);
        _playerStats.IsParrying = true;

        yield return new WaitForSeconds(parryWindow1H);

        _playerStats.IsParrying = false;
        _playerStats.IsBlocking = true;
        _parry1HCoroutine = null;
    }

    private void Handle1HInput()
    {
        if (Input.GetMouseButtonDown(0) && !_is1HBlocking)
            StartCoroutine(AttackRoutine());
    }

    // ── Shield+Sword ───────────────────────────────────────────────────────────

    private void TrackShieldSwordBlock()
    {
        if (Input.GetMouseButtonDown(1) && !_isShieldSwordBlocking)
        {
            _isShieldSwordBlocking = true;
            _playerStats.IsBlocking = true;
            if (!_isAttacking)
            {
                SetInAction(true);
                ClearAllIdle();
                _anim.SetBool(isShieldSwordBlockParam, true);
                SyncBoolRpc(isShieldSwordBlockParam, true);
            }
        }

        if (Input.GetMouseButtonUp(1) && _isShieldSwordBlocking)
        {
            _isShieldSwordBlocking = false;
            _playerStats.IsBlocking = false;
            _anim.SetBool(isShieldSwordBlockParam, false);
            SyncBoolRpc(isShieldSwordBlockParam, false);
            if (!_isAttacking) { SetInAction(false); RestoreIdle(); }
        }
    }

    private void HandleShieldSwordInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (_isShieldSwordBlocking) StartCoroutine(ParryRoutine());
            else                        StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator ParryRoutine()
    {
        _isAttacking = true;
        _playerStats?.DrainStamina(_equippedSO?.staminaCost ?? 10f);

        _anim.SetBool(isShieldSwordBlockParam, false);
        SyncBoolRpc(isShieldSwordBlockParam, false);
        _anim.SetTrigger(shieldSwordParryParam);
        SyncTriggerRpc(shieldSwordParryParam);

        yield return new WaitUntil(() =>
            _anim.IsInTransition(attackAnimatorLayer) ||
            !_anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer).IsName("Empty"));

        AnimatorStateInfo state = _anim.IsInTransition(attackAnimatorLayer)
            ? _anim.GetNextAnimatorStateInfo(attackAnimatorLayer)
            : _anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer);

        float clipLength = state.length > 0.1f ? state.length : 0.5f;
        yield return new WaitForSeconds(clipLength);

        _isAttacking = false;

        if (_isShieldSwordBlocking)
        {
            _anim.SetBool(isShieldSwordBlockParam, true);
            SyncBoolRpc(isShieldSwordBlockParam, true);
        }
        else { SetInAction(false); RestoreIdle(); }
    }

    // ── Guard break ────────────────────────────────────────────────────────────

    private void HandleGuardBreak()
    {
        _is1HBlocking = _is2HBlocking = _isShieldBlocking = _isShieldSwordBlocking = false;
        _playerStats.IsBlocking = false;
        _playerStats.IsParrying = false;
        _anim.SetBool(is1HBlockParam,          false);
        _anim.SetBool(is2HBlockParam,          false);
        _anim.SetBool(isShieldBlockParam,      false);
        _anim.SetBool(isShieldSwordBlockParam, false);
        SyncBoolRpc(is1HBlockParam,          false);
        SyncBoolRpc(is2HBlockParam,          false);
        SyncBoolRpc(isShieldBlockParam,      false);
        SyncBoolRpc(isShieldSwordBlockParam, false);
        StartCoroutine(GuardBreakRoutine());
    }

    private IEnumerator GuardBreakRoutine()
    {
        _isAttacking = true;
        SetInAction(true);
        ClearAllIdle();
        _anim.SetTrigger(guardBreakParam);
        SyncTriggerRpc(guardBreakParam);

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

    // ── 2H Block ───────────────────────────────────────────────────────────────

    private void Track2HBlock()
    {
        if (Input.GetMouseButtonDown(1) && !_is2HBlocking)
        {
            _is2HBlocking = true;
            _playerStats.IsBlocking = true;
            if (!_isAttacking)
            {
                SetInAction(true); ClearAllIdle();
                _anim.SetBool(is2HBlockParam, true);
                SyncBoolRpc(is2HBlockParam, true);
            }
        }

        if (Input.GetMouseButtonUp(1) && _is2HBlocking)
        {
            _is2HBlocking = false;
            _playerStats.IsBlocking = false;
            _anim.SetBool(is2HBlockParam, false);
            SyncBoolRpc(is2HBlockParam, false);

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
        if (_canCounter2H)          StartCoroutine(Counter2HRoutine());
        else if (!_is2HBlocking)    StartCoroutine(AttackRoutine());
    }

    private IEnumerator Counter2HRoutine()
    {
        _canCounter2H = false;
        if (_counter2HWindowCoroutine != null) { StopCoroutine(_counter2HWindowCoroutine); _counter2HWindowCoroutine = null; }
        _playerStats?.DrainStamina(_equippedSO?.staminaCost ?? 10f);

        _isAttacking = true;
        SetInAction(true);
        ClearAllIdle();
        _anim.SetBool(is2HBlockParam, false);
        SyncBoolRpc(is2HBlockParam, false);
        _anim.SetTrigger(counter2HBaseParam);
        _anim.SetTrigger(attack2HCounterParam);
        SyncTriggerRpc(counter2HBaseParam);
        SyncTriggerRpc(attack2HCounterParam);

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

        _isAttacking  = false;
        _is2HBlocking = false;
        SetInAction(false);
        RestoreIdle();
    }

    // ── Shield main hand ───────────────────────────────────────────────────────

    private void TrackShieldBlock()
    {
        if (Input.GetMouseButtonDown(1) && !_isShieldBlocking)
        {
            _isShieldBlocking = true;
            _playerStats.IsBlocking = true;
            if (!_isAttacking)
            {
                SetInAction(true); ClearAllIdle();
                _anim.SetBool(isShieldBlockParam, true);
                SyncBoolRpc(isShieldBlockParam, true);
            }
        }

        if (Input.GetMouseButtonUp(1) && _isShieldBlocking)
        {
            _isShieldBlocking = false;
            _playerStats.IsBlocking = false;
            _anim.SetBool(isShieldBlockParam, false);
            SyncBoolRpc(isShieldBlockParam, false);
            if (!_isAttacking) { SetInAction(false); RestoreIdle(); }
        }
    }

    private void HandleShieldInput()
    {
        if (Input.GetMouseButtonDown(0) && !_isShieldBlocking)
            StartCoroutine(AttackRoutine());
    }

    // ── Staff ──────────────────────────────────────────────────────────────────

    private void HandleStaffInput()
    {
        if (Input.GetMouseButtonDown(1) && !_isStaffCharging)
        {
            _isStaffCharging = true;
            SetInAction(true);
            ClearAllIdle();
            GetComponent<StaffSpell>()?.BeginCast();
        }

        if (Input.GetMouseButtonDown(0) && !_isStaffCharging)
            StartCoroutine(AttackRoutine());
    }

    private IEnumerator StaffSkillRoutine()
    {
        _isAttacking = true;
        _playerStats?.DrainStamina(_equippedSO?.staminaCost ?? 10f);
        _anim.SetTrigger(skillStaffParam);
        SyncTriggerRpc(skillStaffParam);

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
            _isWandHolding = true;
            SetInAction(true);
            ClearAllIdle();
            UpdateWeaponGrip(true);
            _anim.SetTrigger(skillWandParam);
            _anim.SetBool(isWandHoldParam, true);
            SyncTriggerRpc(skillWandParam);
            SyncBoolRpc(isWandHoldParam, true);
        }

        if (Input.GetMouseButtonUp(1))
        {
            _isWandHolding = false;
            _anim.SetBool(isWandHoldParam, false);
            SyncBoolRpc(isWandHoldParam, false);
            UpdateWeaponGrip(false);
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
            _playerStats?.DrainStamina(_equippedSO?.staminaCost ?? 10f);
            SetInAction(true);
            ClearAllIdle();
            UpdateWeaponGrip(true);
            _anim.SetBool(attackBowParam, true);
            _anim.SetBool(holdBowParam,   true);
            SyncBoolRpc(attackBowParam, true);
            SyncBoolRpc(holdBowParam,   true);
        }

        if (Input.GetMouseButtonUp(0))
        {
            _anim.SetBool(holdBowParam, false);
            SyncBoolRpc(holdBowParam, false);
            StartCoroutine(FinishBowAttackRoutine());
        }
    }

    private IEnumerator FinishBowAttackRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        _anim.SetBool(attackBowParam, false);
        SyncBoolRpc(attackBowParam, false);
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
            SyncBoolRpc(holdSpearParam, true);
        }

        if (Input.GetMouseButtonUp(1) && _isHoldingSpear)
        {
            _isHoldingSpear = false;
            _anim.SetBool(holdSpearParam, false);
            SyncBoolRpc(holdSpearParam, false);
            UpdateWeaponGrip(false);
            SetInAction(false);
            RestoreIdle();
        }

        if (Input.GetMouseButtonDown(0) && _isHoldingSpear)
        {
            _isHoldingSpear = false;
            _anim.SetBool(holdSpearParam, false);
            SyncBoolRpc(holdSpearParam, false);
            _anim.SetTrigger(throwSpearParam);
            SyncTriggerRpc(throwSpearParam);
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
        _playerStats?.DrainStamina(_equippedSO?.staminaCost ?? 10f);

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

    // ── Grip ───────────────────────────────────────────────────────────────────

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

    // ── Hitbox (Animation Events) ──────────────────────────────────────────────

    public void OnHitboxOpen()
    {
        if (!isOwner) return;
        _hitbox?.EnableHitbox();
    }

    public void OnHitboxClose()
    {
        if (!isOwner) return;
        _hitbox?.DisableHitbox();
    }

    // ── Other Animation Events ─────────────────────────────────────────────────

    public void ExecuteShoot()
    {
        if (!isOwner || _equippedSO?.projectilePrefab == null || shotPoint == null) return;
        Vector3 dir = (GetCrosshairTarget() - shotPoint.position).normalized;
        GameObject arrow = Instantiate(_equippedSO.projectilePrefab, shotPoint.position, Quaternion.LookRotation(dir));
        if (arrow.TryGetComponent<ProjectilePrefab>(out var proj))
        {
            proj.Setup(_playerStats, _equippedSO);
            proj.Launch(dir, _equippedSO.projectileForce);
        }
    }

    public void ExecuteMagicBall()
    {
        if (!isOwner || _equippedSO?.projectilePrefab == null || magicPoint == null) return;
        if (_playerStats != null && !_playerStats.DrainMana(_equippedSO.manaCost)) return;

        Vector3 dir = (GetCrosshairTarget() - magicPoint.position).normalized;
        GameObject ball = Instantiate(_equippedSO.projectilePrefab, magicPoint.position, Quaternion.LookRotation(dir));
        if (ball.TryGetComponent<MagicBallDamage>(out var dmg))
        {
            dmg.speed    = _equippedSO.projectileSpeed;
            dmg.maxRange = _equippedSO.projectileRange;
            dmg.Setup(_playerStats, _equippedSO);
        }
    }

    public void OnStaffImpact() { if (isOwner) GetComponentInChildren<StaffSpell>()?.Cast(); }

    public void OnWandFireStart() { if (isOwner && _isWandHolding) GetComponentInChildren<WandLaser>()?.StartFire(); }

    public void ExecuteSpearThrow()
    {
        if (!isOwner || _equippedSO == null || throwPoint == null) return;
        GameObject prefabToThrow = _equippedSO.projectilePrefab != null ? _equippedSO.projectilePrefab : _equippedSO.prefab;
        Vector3 dir = (GetCrosshairTarget() - throwPoint.position).normalized;
        GameObject spear = Instantiate(prefabToThrow, throwPoint.position, Quaternion.LookRotation(dir));
        if (spear.TryGetComponent<ProjectilePrefab>(out var proj))
        {
            proj.Setup(_playerStats, _equippedSO);
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

    // ── Equip main hand ────────────────────────────────────────────────────────

    public void EquipWeapon(WeaponSO so)
    {
        InterruptUpperOnly();

        if (_equippedWeapon != null) Destroy(_equippedWeapon);
        _hitbox = null;

        _equippedSO     = so;
        _isAttacking    = false;
        _isHoldingSpear = _is1HBlocking = _is2HBlocking = _isShieldBlocking = false;
        _isShieldSwordBlocking = false;

        _playerStats?.SetGuardBase(so?.baseGuardStability ?? 0f);
        if (_playerStats != null) { _playerStats.IsBlocking = false; _playerStats.IsParrying = false; }

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
                if (hotbar?.OffHandWeapon != null && _equippedOffHand == null)
                    EquipOffHand(hotbar.OffHandWeapon);
            }
            else
            {
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
        _hitbox      = _equippedWeapon.GetComponentInChildren<WeaponHitbox>();
        _hitbox?.Setup(_playerStats, so);

        if (so.weaponType == WeaponType.Bow)
        {
            var bowStr = _equippedWeapon.GetComponent<BowStringController>();
            if (bowStr != null) { bowStr.handStringPoint = handStringAnchor; bowStr.playerAnim = _anim; }
            var sp = _equippedWeapon.transform.Find("ShotPoint");
            if (sp != null) shotPoint = sp;
        }

        if (so.weaponType is WeaponType.Staff or WeaponType.Wand)
        {
            var mp = _equippedWeapon.transform.Find("MagicPoint");
            if (mp != null) magicPoint = mp;
        }
    }

    // ── Equip off hand ─────────────────────────────────────────────────────────

    private void EquipOffHand(WeaponSO so)
    {
        if (_equippedOffHand != null) { Destroy(_equippedOffHand); _equippedOffHand = null; }

        _offHandSO             = so;
        _isShieldSwordBlocking = false;
        _anim.SetBool(isShieldSwordBlockParam, false);

        if (so != null && so == _equippedSO && _equippedWeapon != null)
        {
            Destroy(_equippedWeapon);
            _equippedWeapon = null;
            ClearAllIdle();
            _anim.SetBool(idle1HParam, true);
        }

        if (so == null || so.prefab == null || offHandSlot == null) return;

        _equippedOffHand = Instantiate(so.prefab, offHandSlot);
        _equippedOffHand.transform.localPosition    = so.offHandPositionOffset;
        _equippedOffHand.transform.localEulerAngles = so.offHandRotationOffset;

        var rb = _equippedOffHand.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    private void HideOffHandPrefab()
    {
        if (_equippedOffHand != null) { Destroy(_equippedOffHand); _equippedOffHand = null; }
        _offHandSO             = null;
        _isShieldSwordBlocking = false;
        _anim.SetBool(isShieldSwordBlockParam, false);
    }

    // ── Interrupt ──────────────────────────────────────────────────────────────

    private void InterruptUpperOnly()
    {
        StopAllCoroutines();
        _isAttacking = _isHoldingSpear = _isStaffCharging = _isWandHolding = false;
        _is1HBlocking = _is2HBlocking = _isShieldBlocking = _isShieldSwordBlocking = false;
        _canCounter2H = false;
        _counter2HWindowCoroutine = null;
        _parry1HCoroutine = null;

        if (_playerStats != null) { _playerStats.IsParrying = false; _playerStats.IsBlocking = false; }

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

    // ── RPCs ───────────────────────────────────────────────────────────────────

    [ServerRpc]
    private void SyncTriggerRpc(string param) => SyncTriggerObserversRpc(param);

    [ObserversRpc(excludeOwner: true)]
    private void SyncTriggerObserversRpc(string param) => _anim?.SetTrigger(param);

    [ServerRpc]
    private void SyncBoolRpc(string param, bool val) => SyncBoolObserversRpc(param, val);

    [ObserversRpc(excludeOwner: true)]
    private void SyncBoolObserversRpc(string param, bool val) => _anim?.SetBool(param, val);
}
