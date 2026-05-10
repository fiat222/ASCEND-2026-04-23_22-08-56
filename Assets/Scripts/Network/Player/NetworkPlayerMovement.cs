using UnityEngine;
using Cinemachine;
using PurrNet;

[RequireComponent(typeof(CharacterController))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed     = 5f;
    [SerializeField] private float runSpeed      = 8f;
    [SerializeField] private float jumpHeight    = 2f;
    [SerializeField] private float gravity       = -20f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed     = 15f;
    [SerializeField] private float dashDuration  = 0.2f;
    [SerializeField] private float dashCooldown  = 1f;

    [Header("References")]
    [SerializeField] private Transform playerVisual;
    [SerializeField] private Transform cameraTarget;

    [Header("Animator Params")]
    [SerializeField] private string paramMoveX   = "MoveX";
    [SerializeField] private string paramMoveY   = "MoveY";
    [SerializeField] private string paramIsRun   = "IsRun";
    [SerializeField] private string paramJump    = "Jump";
    [SerializeField] private string paramIsGround = "IsGround";

    [Header("Cinemachine Axis")]
    public AxisState xAxis;
    public AxisState yAxis;

    [Header("Spine Aim")]
    [SerializeField] private Transform spineBone;

    private CharacterController _cc;
    private Animator            _anim;
    private float               _verticalVel;
    private bool                _isDashing;
    private float               _dashTimer;
    private float               _dashCooldownTimer;
    private Vector3             _dashDir;
    private bool                _isRunning;

    protected override void OnSpawned()
    {
        _cc   = GetComponent<CharacterController>();
        _anim = playerVisual.GetComponentInChildren<Animator>();

        if (!isOwner) return;

        if (string.IsNullOrEmpty(xAxis.m_InputAxisName)) xAxis.m_InputAxisName = "Mouse X";
        if (string.IsNullOrEmpty(yAxis.m_InputAxisName)) yAxis.m_InputAxisName = "Mouse Y";

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void Update()
    {
        if (!isOwner) return;

        xAxis.Update(Time.deltaTime);
        yAxis.Update(Time.deltaTime);
        HandleMovement();
    }

    private void LateUpdate()
    {
        if (!isOwner || cameraTarget == null) return;

        transform.rotation             = Quaternion.Euler(0f, xAxis.Value, 0f);
        cameraTarget.localEulerAngles  = new Vector3(yAxis.Value, 0f, 0f);

        if (spineBone != null)
            spineBone.localRotation = Quaternion.Euler(yAxis.Value, 0f, 0f);
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.LeftShift)) _isRunning = !_isRunning;

        Vector3 moveDir = transform.forward * v + transform.right * h;
        if (moveDir.sqrMagnitude > 0.01f) moveDir.Normalize();

        if (_dashCooldownTimer > 0f) _dashCooldownTimer -= Time.deltaTime;

        if (!_isDashing && _dashCooldownTimer <= 0f && Input.GetKeyDown(KeyCode.V))
        {
            _isDashing  = true;
            _dashTimer  = dashDuration;
            _dashDir    = moveDir.sqrMagnitude > 0.01f ? moveDir : transform.forward;
        }

        if (_isDashing)
        {
            _cc.Move(_dashDir * dashSpeed * Time.deltaTime);
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f)
            {
                _isDashing         = false;
                _dashCooldownTimer = dashCooldown;
            }
            return;
        }

        bool isGrounded = _cc.isGrounded;
        _anim.SetBool(paramIsGround, isGrounded);

        if (isGrounded)
        {
            if (_verticalVel < 0f) _verticalVel = -2f;

            if (Input.GetButtonDown("Jump"))
            {
                _verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _anim.SetTrigger(paramJump);
                SyncJumpServerRpc();
            }
        }

        _verticalVel += gravity * Time.deltaTime;

        float   speed    = _isRunning ? runSpeed : walkSpeed;
        Vector3 velocity = moveDir * speed;
        velocity.y = _verticalVel;

        _cc.Move(velocity * Time.deltaTime);

        if (_anim != null)
        {
            Vector3 localMove = transform.InverseTransformDirection(moveDir);
            float   animX     = localMove.x;
            float   animY     = localMove.z;
            bool    animRun   = _isRunning && moveDir.sqrMagnitude > 0.01f;

            _anim.SetFloat(paramMoveX, animX, 0.1f, Time.deltaTime);
            _anim.SetFloat(paramMoveY, animY, 0.1f, Time.deltaTime);
            _anim.SetBool(paramIsRun, animRun);

            SyncMoveAnimServerRpc(animX, animY, animRun);
        }
    }

    [ServerRpc]
    private void SyncMoveAnimServerRpc(float x, float y, bool isRun)
        => SyncMoveAnimObserversRpc(x, y, isRun);

    [ObserversRpc(excludeOwner: true)]
    private void SyncMoveAnimObserversRpc(float x, float y, bool isRun)
    {
        if (_anim == null) return;
        _anim.SetFloat(paramMoveX, x);
        _anim.SetFloat(paramMoveY, y);
        _anim.SetBool(paramIsRun, isRun);
    }

    [ServerRpc]
    private void SyncJumpServerRpc() => SyncJumpObserversRpc();

    [ObserversRpc(excludeOwner: true)]
    private void SyncJumpObserversRpc() => _anim?.SetTrigger(paramJump);
}
