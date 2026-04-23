using System.Collections;
using UnityEngine;
using PurrNet;

[RequireComponent(typeof(CharacterController))]
public class CharactorMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Animation Params")]
    [SerializeField] private string isMoveParam = "IsMove";
    [SerializeField] private string attackParam = "Attack";
    [SerializeField] private int attackAnimatorLayer = 1;

    private CharacterController _cc;
    private Animator _anim;
    private Camera _cam;
    private float _verticalVelocity;

    protected override void OnSpawned()
    {
        _cc = GetComponent<CharacterController>();
        _anim = GetComponentInChildren<Animator>(); // searches children too

        if (isOwner)
            _cam = Camera.main;
    }

    private void Update()
    {
        if (!isOwner) return;
        HandleMovement();
        HandleAttackInput();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

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

        Vector3 velocity = moveDir * moveSpeed + Vector3.up * _verticalVelocity;
        _cc.Move(velocity * Time.deltaTime);

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        bool moving = moveDir.magnitude > 0.01f;
        _anim?.SetBool(isMoveParam, moving);

        if (moving)
            SyncMoveAnimServerRpc(true);
        else
            SyncMoveAnimServerRpc(false);
    }

    [ServerRpc]
    private void SyncMoveAnimServerRpc(bool moving)
    {
        SyncMoveAnimObserversRpc(moving);
    }

    [ObserversRpc(excludeOwner: true)]
    private void SyncMoveAnimObserversRpc(bool moving)
    {
        _anim?.SetBool(isMoveParam, moving);
    }

    private bool _isAttacking;

    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0) && !_isAttacking)
            RequestAttackServerRpc();
    }

    [ServerRpc]
    private void RequestAttackServerRpc()
    {
        PlayAttackObserversRpc();
    }

    [ObserversRpc]
    private void PlayAttackObserversRpc()
    {
        if (_anim != null)
            StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _anim.SetBool(attackParam, true);

        yield return null;
        yield return null; // wait 2 frames for transition

        AnimatorStateInfo state = _anim.GetCurrentAnimatorStateInfo(attackAnimatorLayer);
        float clipLength = state.length > 0.1f ? state.length : 1f; // fallback 1s
        yield return new WaitForSeconds(clipLength);

        _anim.SetBool(attackParam, false);
        _isAttacking = false;
    }
}
