using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private float inputSmoothing = 15f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Animation Parameters")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string isRunParam = "IsRun";

    private CharacterController _cc;
    private Animator _anim;
    private Camera _cam;
    private float _verticalVelocity;
    private Vector3 _smoothDir;
    private bool _isRunning;

    private void Start()
    {
        _cc = GetComponent<CharacterController>();
        _anim = GetComponentInChildren<Animator>();
        _cam = Camera.main;
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // ระบบวิ่ง (Shift)
        if (Input.GetKeyDown(KeyCode.LeftShift)) _isRunning = !_isRunning;

        Vector3 moveDir = Vector3.zero;
        if (_cam != null)
        {
            Vector3 camForward = Vector3.Scale(_cam.transform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = Vector3.Scale(_cam.transform.right, new Vector3(1, 0, 1)).normalized;
            moveDir = camForward * v + camRight * h;
        }

        if (moveDir.magnitude > 1f) moveDir.Normalize();

        // Gravity
        if (_cc.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
        else _verticalVelocity += gravity * Time.deltaTime;

        float speed = _isRunning ? runSpeed : moveSpeed;
        Vector3 velocity = moveDir * speed + Vector3.up * _verticalVelocity;
        _cc.Move(velocity * Time.deltaTime);

        // Smoothing Rotation
        _smoothDir = Vector3.Lerp(_smoothDir, moveDir, inputSmoothing * Time.deltaTime);
        if (_smoothDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_smoothDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // ส่งค่าไป Animator
        Vector3 localMove = transform.InverseTransformDirection(moveDir);
        _anim?.SetFloat(moveXParam, localMove.x);
        _anim?.SetFloat(moveYParam, localMove.z);
        _anim?.SetBool(isRunParam, _isRunning && moveDir.sqrMagnitude > 0.01f);
    }
}