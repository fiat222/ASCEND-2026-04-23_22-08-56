using System.Collections;
using UnityEngine;
using Drakkar.GameUtils;

[RequireComponent(typeof(CharacterController))]
public class PlayerTest : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float inputSmoothing = 15f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Animation Params")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string isRunParam = "IsRun";
    [SerializeField] private string idle1HParam      = "Is1HIdle";
    [SerializeField] private string idle2HParam      = "Is2HIdle";
    [SerializeField] private string idleSpearParam   = "IsSpearIdle";
    [SerializeField] private string attack1HParam    = "Attack1H";
    [SerializeField] private string attack2HParam    = "Attack2H";
    [SerializeField] private string attackMagicParam = "AttackMagic";
    [SerializeField] private string attackShieldParam = "AttackShield";
    [SerializeField] private string attackSpearParam  = "AttackSpear";
    [SerializeField] private int attackAnimatorLayer = 1;

    [Header("Bow Animation Params")]
    [SerializeField] private string attackBowParam = "Attack_Bow"; // Boolean
    [SerializeField] private string holdBowParam = "Hold_Bow";     // Boolean
    [SerializeField] private string idleBowParam = "IsBowIdle";    // Boolean

    [Header("Bow String")]
    [SerializeField] private Transform handStringAnchor; // ลากจุด HandStringPoint ในกระดูกมือมาใส่ที่นี่

    [Header("Weapon")]
    [SerializeField] private Transform weaponSlot;
    [SerializeField] private Transform offHandSlot;
    [SerializeField] private HotbarController hotbar;

    [Header("Trail")]
    [SerializeField] private DrakkarTrail weaponTrail;


    private CharacterController _cc;
    private Animator _anim;
    private Camera _cam;
    private float _verticalVelocity;
    private bool _isAttacking;
    private bool _isRunning;
    private Vector3 _smoothDir;
    private GameObject _equippedWeapon;
    private WeaponSO _equippedSO;

    private void Start()
    {
        _cc  = GetComponent<CharacterController>();
        _anim = GetComponentInChildren<Animator>();
        _cam = Camera.main;

        if (hotbar != null)
        {
            hotbar.OnSlotChanged += OnHotbarSlotChanged;
            EquipWeapon(hotbar.SelectedWeapon);
        }
    }

    private void OnDestroy()
    {
        if (hotbar != null)
            hotbar.OnSlotChanged -= OnHotbarSlotChanged;
    }

    private void OnHotbarSlotChanged(int _) => EquipWeapon(hotbar.SelectedWeapon);

    private void Update()
    {
        HandleMovement();
        HandleAttackInput();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            _isRunning = !_isRunning;

        Vector3 moveDir = Vector3.zero;

        if (_cam != null)
        {
            Vector3 camForward = _cam.transform.forward;
            Vector3 camRight   = _cam.transform.right;
            camForward.y = 0f;
            camRight.y   = 0f;
            camForward.Normalize();
            camRight.Normalize();
            moveDir = camForward * v + camRight * h;
        }
        else
        {
            moveDir = new Vector3(h, 0f, v);
        }

        if (moveDir.magnitude > 1f)
            moveDir.Normalize();

        if (_cc.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        float speed = _isRunning ? runSpeed : moveSpeed;
        Vector3 velocity = moveDir * speed + Vector3.up * _verticalVelocity;
        _cc.Move(velocity * Time.deltaTime);

        _smoothDir = Vector3.Lerp(_smoothDir, moveDir, inputSmoothing * Time.deltaTime);

        if (_smoothDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_smoothDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        Vector3 localMove = transform.InverseTransformDirection(moveDir);
        _anim?.SetFloat(moveXParam, localMove.x);
        _anim?.SetFloat(moveYParam, localMove.z);
        _anim?.SetBool(isRunParam, _isRunning && moveDir.sqrMagnitude > 0.01f);
    }

    private void HandleAttackInput()
    {
        if (_equippedSO == null) return;

        // แยก Logic ธนูออกจากอาวุธประชิด
        if (_equippedSO.weaponType == WeaponType.Bow)
        {
            HandleBowInput();
        }
        else
        {
            if (Input.GetMouseButtonDown(0) && !_isAttacking)
                StartCoroutine(AttackRoutine());
        }
    }

    private void HandleBowInput()
    {
        // 1. กดค้าง (Hold)
        if (Input.GetMouseButtonDown(0))
        {
            _anim.SetBool(attackBowParam, true);
            _anim.SetBool(holdBowParam, true);
        }

        // 2. ปล่อย (Release)
        if (Input.GetMouseButtonUp(0))
        {
            _anim.SetBool(holdBowParam, false);
            // ปล่อยให้แอนิเมชันเล่นช่วง Release แล้วค่อยปิด Attack_Bow
            StartCoroutine(FinishBowAttackRoutine());
        }
    }

    private IEnumerator FinishBowAttackRoutine()
    {
        // รอให้ Animation Release เล่นจนจบ (ปรับเวลาได้ตามความเหมาะสม)
        yield return new WaitForSeconds(0.2f); 
        _anim.SetBool(attackBowParam, false);
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _anim.SetTrigger(GetAttackParam());

        yield return null;
        yield return null;

        AnimatorStateInfo state = _anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer);
        float clipLength = state.length > 0.1f ? state.length : 1f;

        weaponTrail?.Begin();
        yield return new WaitForSeconds(clipLength);
        weaponTrail?.End();

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

    private void EquipWeapon(WeaponSO so)
    {
        if (_equippedWeapon != null)
        {
            Destroy(_equippedWeapon);
            _equippedWeapon = null;
        }

        _equippedSO = so;
        WeaponType? type = so?.weaponType;

        // Update Idle States
        _anim?.SetBool(idle1HParam,    type is WeaponType.OneHand or WeaponType.Staff or WeaponType.Shield);
        _anim?.SetBool(idle2HParam,    type == WeaponType.TwoHand);
        _anim?.SetBool(idleSpearParam, type == WeaponType.Spear);
        _anim?.SetBool(idleBowParam,   type == WeaponType.Bow);

        // Safety Reset
        _anim?.SetBool(attackBowParam, false);
        _anim?.SetBool(holdBowParam, false);
        _isAttacking = false;
        
        weaponTrail = null;

        if (so == null || so.prefab == null) return;
        Transform slot = (so.useOffHand && offHandSlot != null) ? offHandSlot : weaponSlot;
        if (slot == null) return;
        _equippedWeapon = Instantiate(so.prefab, slot);

        ///// Check ว่าใช้ธนูไหม /////
        if (so.weaponType == WeaponType.Bow)
        {
            var bowString = _equippedWeapon.GetComponent<BowStringController>();
            if (bowString != null)
            {
                bowString.handStringPoint = handStringAnchor; 
            }
        }

        _equippedWeapon.transform.localPosition = so.gripPositionOffset;
        _equippedWeapon.transform.localEulerAngles = so.gripRotationOffset;
        weaponTrail = _equippedWeapon.GetComponentInChildren<DrakkarTrail>();
    }

    ///////////////////// BOW /////////////////////

}