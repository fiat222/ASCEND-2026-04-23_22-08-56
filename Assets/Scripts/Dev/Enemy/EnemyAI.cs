using System.Collections;
using UnityEngine;

public class EnemyAI : EnemyBase
{
    [Header("Attack")]
    [SerializeField] private float attackRange    = 2.5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackDamage   = 25f;

    [Header("Type")]
    [SerializeField] private bool isRange = false;

    [Header("Throw Attack")]
    [SerializeField] private string     attackThrowParam      = "Attack_Throw";
    [SerializeField] private float      throwRange            = 8f;
    [SerializeField] private float      throwCooldown         = 4f;
    [SerializeField] private float      throwDamage           = 20f;
    [SerializeField] private float      throwSpeed            = 12f;
    [SerializeField] private Transform  throwPoint;
    [SerializeField] private GameObject throwProjectilePrefab;

    [Header("Animation Params")]
    [SerializeField] private string attackStabParam    = "Attack_Stab";
    [SerializeField] private string attackSlash01Param = "Attack_Slash01";
    [SerializeField] private string attackSlash02Param = "Attack_Slash02";

    private EnemyHitbox _hitbox;
    private bool        _isAttacking;
    private bool        _inAttackState;
    private float       _attackTimer;
    private float       _throwTimer;

    protected override void Awake()
    {
        base.Awake();
        _hitbox = GetComponentInChildren<EnemyHitbox>();
        _hitbox?.Setup(attackDamage);
    }

    protected override void UpdateChase()
    {
        if (_inAttackState)
        {
            SetMovement(0f, 0f);
            if (!_isAttacking) StartCoroutine(AttackRoutine());
            return;
        }

        _attackTimer -= Time.deltaTime;
        _throwTimer  -= Time.deltaTime;

        float dist = Vector3.Distance(transform.position + attackCenterOffset, _target.position);

        if (isRange)
        {
            RotateToward(_target.position);
            if (dist < attackRange)
            {
                transform.position -= transform.forward * walkSpeed * Time.deltaTime;
                SetMovement(0f, -0.5f);
            }
            else if (dist <= throwRange && _throwTimer <= 0f)
            {
                _inAttackState = true;
            }
            else if (dist > throwRange)
            {
                transform.position += transform.forward * walkSpeed * Time.deltaTime;
                SetMovement(0f, 0.5f);
            }
            return;
        }

        // Melee
        if (dist > chaseRange) { _state = State.Idle; return; }

        if (dist <= attackRange && _attackTimer <= 0f)
        {
            _inAttackState = true;
            return;
        }

        RotateToward(_target.position);
        float speed = dist > attackRange * 2f ? runSpeed : walkSpeed;
        float moveY = dist > attackRange * 2f ? 1f       : 0.5f;
        transform.position += transform.forward * speed * Time.deltaTime;
        SetMovement(TurnAngle(), moveY);
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        RotateToward(_target.position);

        if (isRange)
        {
            _anim.SetTrigger(attackThrowParam);
            yield return new WaitUntil(() => _anim.IsInTransition(0));
            float len = _anim.GetNextAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(len > 0.1f ? len : 1.5f);
            _throwTimer = throwCooldown;
        }
        else
        {
            string[] attacks = { attackStabParam, attackSlash01Param, attackSlash02Param };
            _anim.SetTrigger(attacks[Random.Range(0, attacks.Length)]);
            yield return new WaitUntil(() => _anim.IsInTransition(0));
            AnimatorStateInfo info = _anim.GetNextAnimatorStateInfo(0);
            float len = info.length > 0.1f ? info.length : 1f;
            yield return new WaitForSeconds(len * 0.3f);
            _hitbox?.EnableHitbox();
            yield return new WaitForSeconds(len * 0.3f);
            _hitbox?.DisableHitbox();
            yield return new WaitForSeconds(len * 0.4f);
            _attackTimer = attackCooldown;
        }

        _isAttacking   = false;
        _inAttackState = false;
    }

    // Called by animation event on throw clip
    public void OnThrowProjectile()
    {
        if (_target == null || throwProjectilePrefab == null || throwPoint == null) return;
        Vector3 dir = (_target.position + Vector3.up) - throwPoint.position;
        var go   = Instantiate(throwProjectilePrefab, throwPoint.position, Quaternion.LookRotation(dir));
        var proj = go.GetComponent<EnemyProjectile>();
        if (proj != null) { proj.damage = throwDamage; proj.speed = throwSpeed; proj.Launch(dir); }
    }

    protected override void OnDied()
    {
        base.OnDied();
        _hitbox?.DisableHitbox();
    }

    protected override void OnHitHandler()
    {
        if (_inAttackState) return;
        base.OnHitHandler();
    }

    protected override void OnInterruptedHandler()
    {
        if (_inAttackState) return;
        base.OnInterruptedHandler();
    }

    protected override void OnStunnedHandler()
    {
        if (_inAttackState) return;
        base.OnStunnedHandler();
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Vector3 center = transform.position + attackCenterOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, throwRange);
    }
}
