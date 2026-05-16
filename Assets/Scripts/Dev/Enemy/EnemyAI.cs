using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float chaseRange  = 10f;
    [SerializeField] private float attackRange = 2f;

    [Header("Movement")]
    [SerializeField] private float walkSpeed     = 2f;
    [SerializeField] private float runSpeed      = 4f;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackDamage   = 25f;

    [Header("Animation Params")]
    [SerializeField] private string moveXParam         = "MoveX";
    [SerializeField] private string moveYParam         = "MoveY";
    [SerializeField] private string attackStabParam    = "Attack_Stab";
    [SerializeField] private string attackSlash01Param = "Attack_Slash01";
    [SerializeField] private string attackSlash02Param = "Attack_Slash02";
    [SerializeField] private string hitReactParam      = "GetDamaged";
    [SerializeField] private string deathParam   = "Die";
    [SerializeField] private float  sinkDuration = 1.5f;
    [SerializeField] private float  sinkDepth    = 2f;

    private Animator     _anim;
    private EnemyStats   _stats;
    private EnemyHitbox  _hitbox;
    private Transform    _target;
    private bool         _isAttacking;
    private float        _attackTimer;

    private enum State { Idle, Chase, Attack, Dead }
    private State _state = State.Idle;

    private void Start()
    {
        _anim   = GetComponent<Animator>();
        _stats  = GetComponent<EnemyStats>();
        _hitbox = GetComponentInChildren<EnemyHitbox>();
        _hitbox?.Setup(attackDamage);
        _stats.OnDied        += OnDied;
        _stats.OnHit         += OnHitHandler;
        _stats.OnInterrupted += OnInterruptedHandler;
        _stats.OnStunned     += OnStunnedHandler;
        FindNearestPlayer();
    }

    private void OnDestroy()
    {
        if (_stats == null) return;
        _stats.OnDied        -= OnDied;
        _stats.OnHit         -= OnHitHandler;
        _stats.OnInterrupted -= OnInterruptedHandler;
        _stats.OnStunned     -= OnStunnedHandler;
    }

    private void Update()
    {
        if (_state == State.Dead) return;

        // Interrupted: freeze movement
        if (_stats.IsInterrupted)
        {
            SetMovement(0f, 0f);
            return;
        }

        if (_stats.IsStunned)
        {
            SetMovement(0f, 0f);
            return;
        }

        if (_target == null)
        {
            FindNearestPlayer();
            if (_target == null) return;
        }

        float dist = Vector3.Distance(transform.position, _target.position);
        _attackTimer -= Time.deltaTime;

        switch (_state)
        {
            case State.Idle:
                SetMovement(0f, 0f);
                if (dist <= chaseRange) _state = State.Chase;
                break;

            case State.Chase:
                if (dist > chaseRange)                             { _state = State.Idle;   break; }
                if (dist <= attackRange && _attackTimer <= 0f)     { _state = State.Attack; break; }

                RotateToward(_target.position);
                float speed = dist > attackRange * 2f ? runSpeed : walkSpeed;
                float moveY = dist > attackRange * 2f ? 1f       : 0.5f;
                transform.position += transform.forward * speed * Time.deltaTime;
                SetMovement(TurnAngle(), moveY);
                break;

            case State.Attack:
                SetMovement(0f, 0f);
                if (!_isAttacking)
                {
                    if (dist > attackRange) { _state = State.Chase; break; }
                    StartCoroutine(AttackRoutine());
                }
                break;
        }
    }

    // ── Movement ───────────────────────────────────────────────────────────────

    private void RotateToward(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    private float TurnAngle()
    {
        Vector3 toPlayer = (_target.position - transform.position).normalized;
        toPlayer.y = 0f;
        float angle = Vector3.SignedAngle(transform.forward, toPlayer, Vector3.up);
        return Mathf.Clamp(angle / 90f, -1f, 1f);
    }

    private void SetMovement(float x, float y)
    {
        _anim.SetFloat(moveXParam, x, 0.1f, Time.deltaTime);
        _anim.SetFloat(moveYParam, y, 0.1f, Time.deltaTime);
    }

    // ── Attack ─────────────────────────────────────────────────────────────────

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        RotateToward(_target.position);

        string[] attacks = { attackStabParam, attackSlash01Param, attackSlash02Param };
        _anim.SetTrigger(attacks[Random.Range(0, attacks.Length)]);

        yield return new WaitForSeconds(0.15f);
        AnimatorStateInfo info = _anim.GetCurrentAnimatorStateInfo(0);
        float clipLength = info.length > 0.1f ? info.length : 1f;

        // Open hitbox at 30% — close at 60%
        yield return new WaitForSeconds(clipLength * 0.3f - 0.15f);
        _hitbox?.EnableHitbox();
        yield return new WaitForSeconds(clipLength * 0.3f);
        _hitbox?.DisableHitbox();
        yield return new WaitForSeconds(clipLength * 0.4f);

        _attackTimer = attackCooldown;
        _isAttacking = false;
        _state = State.Chase;
    }

    // ── Death ──────────────────────────────────────────────────────────────────

    private void OnDied()
    {
        _state = State.Dead;
        StopAllCoroutines();
        _hitbox?.DisableHitbox();
        _anim.SetTrigger(deathParam);
    }

    public void OnDeathAnimEnd()
    {
        StartCoroutine(SinkAndDestroy());
    }

    private IEnumerator SinkAndDestroy()
    {
        float elapsed = 0f;
        float speed   = sinkDepth / sinkDuration;
        while (elapsed < sinkDuration)
        {
            transform.position -= Vector3.up * speed * Time.deltaTime;
            elapsed            += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    // ── Utility ────────────────────────────────────────────────────────────────

    private void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float best = float.MaxValue;
        _target = null;

        foreach (var p in players)
        {
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < best) { best = d; _target = p.transform; }
        }
    }

    // ── Hit Reaction ───────────────────────────────────────────────────────────

    private void OnHitHandler()
    {
        if (!_stats.IsAlive) return;
        if (_state == State.Attack) return;
        _anim.ResetTrigger(hitReactParam);
        _anim.SetTrigger(hitReactParam);
    }

    private void OnInterruptedHandler() { if (_stats.IsAlive) { _anim.ResetTrigger(hitReactParam); _anim.SetTrigger(hitReactParam); } }
    private void OnStunnedHandler()     { if (_stats.IsAlive) { _anim.ResetTrigger(hitReactParam); _anim.SetTrigger(hitReactParam); } }
}
