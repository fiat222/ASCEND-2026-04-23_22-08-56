using System.Collections;
using UnityEngine;
using PurrNet;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyStats))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] protected float   chaseRange         = 10f;
    [SerializeField] protected Vector3 attackCenterOffset = new Vector3(0f, 1f, 0f);

    [Header("Movement")]
    [SerializeField] protected float walkSpeed     = 2f;
    [SerializeField] protected float runSpeed      = 4f;
    [SerializeField] protected float rotationSpeed = 180f;

    [Header("Spawn")]
    [SerializeField] private bool   useReviveSpawn = false;
    [SerializeField] private string spawnParam     = "Spawn";
    [SerializeField] private string reviveParam    = "Revive";

    [Header("Aggro")]
    [SerializeField] private string screamParam = "Scream";

    [Header("Animation Params")]
    [SerializeField] protected string moveXParam    = "MoveX";
    [SerializeField] protected string moveYParam    = "MoveY";
    [SerializeField] private   string hitReactParam = "GetDamaged";
    [SerializeField] private   string deathParam    = "Die";
    [SerializeField] private   float  sinkDuration  = 1.5f;
    [SerializeField] private   float  sinkDepth     = 2f;

    protected Animator              _anim;
    protected EnemyStats            _stats;
    protected Transform             _target;
    private   SkinnedMeshRenderer[] _meshes;
    private   bool                  _sinking;

    protected enum State { Dormant, Spawning, Aggro, Idle, Chase, Dead }
    protected State _state = State.Dormant;

    public bool IsDead => _state == State.Dead;

    protected virtual void Awake()
    {
        _anim   = GetComponent<Animator>();
        _stats  = GetComponent<EnemyStats>();
        _meshes = GetComponentsInChildren<SkinnedMeshRenderer>();
        SetVisible(false);
    }

    protected virtual void Start()
    {
        _stats.OnDied        += OnDied;
        _stats.OnHit         += OnHitHandler;
        _stats.OnInterrupted += OnInterruptedHandler;
        _stats.OnStunned     += OnStunnedHandler;
    }

    protected virtual void OnDestroy()
    {
        if (_stats == null) return;
        _stats.OnDied        -= OnDied;
        _stats.OnHit         -= OnHitHandler;
        _stats.OnInterrupted -= OnInterruptedHandler;
        _stats.OnStunned     -= OnStunnedHandler;
    }

    public void TriggerSpawn(bool useRevive = false)
    {
        useReviveSpawn = useRevive;
        FindNearestPlayer();
        SetVisible(true);
        StartCoroutine(SpawnRoutine());
    }

    public void ReviveEnemy()
    {
        StopAllCoroutines();
        _sinking = false;
        _state   = State.Dormant;
        _stats.Revive();
        TriggerSpawn(true);
    }

    public void OnDeathAnimEnd()
    {
        if (_sinking) return;
        _sinking = true;
        StartCoroutine(SinkAndDestroy());
    }

    private void Update()
    {
        if (_state == State.Dead || _state == State.Spawning || _state == State.Aggro) return;

        if (NetworkManager.main != null && !NetworkManager.main.isOffline && !NetworkManager.main.isServer)
            return;

        if (_stats.IsInterrupted || _stats.IsStunned)
        {
            SetMovement(0f, 0f);
            return;
        }

        if (_target == null)
        {
            FindNearestPlayer();
            if (_target == null) return;
        }

        switch (_state)
        {
            case State.Dormant:
                SetMovement(0f, 0f);
                break;
            case State.Idle:
                SetMovement(0f, 0f);
                float dist = Vector3.Distance(transform.position + attackCenterOffset, _target.position);
                if (dist <= chaseRange) StartCoroutine(AggroRoutine());
                break;
            case State.Chase:
                UpdateChase();
                break;
        }
    }

    protected abstract void UpdateChase();

    // ── Spawn / Aggro ──────────────────────────────────────────────────────────

    private IEnumerator SpawnRoutine()
    {
        _state = State.Spawning;
        _anim.SetTrigger(useReviveSpawn ? reviveParam : spawnParam);
        yield return new WaitUntil(() => _anim.IsInTransition(0));
        float len = _anim.GetNextAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(len > 0.1f ? len : 2f);
        StartCoroutine(AggroRoutine());
    }

    private IEnumerator AggroRoutine()
    {
        _state = State.Aggro;
        _anim.SetTrigger(screamParam);
        yield return new WaitUntil(() => _anim.IsInTransition(0));
        float len = _anim.GetNextAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(len > 0.1f ? len : 2f);
        _state = State.Chase;
    }

    // ── Movement ───────────────────────────────────────────────────────────────

    protected void RotateToward(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    protected float TurnAngle()
    {
        Vector3 toTarget = (_target.position - transform.position).normalized;
        toTarget.y = 0f;
        float angle = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);
        return Mathf.Clamp(angle / 90f, -1f, 1f);
    }

    protected void SetMovement(float x, float y)
    {
        _anim.SetFloat(moveXParam, x, 0.1f, Time.deltaTime);
        _anim.SetFloat(moveYParam, y, 0.1f, Time.deltaTime);
    }

    // ── Death ──────────────────────────────────────────────────────────────────

    protected virtual void OnDied()
    {
        _state = State.Dead;
        StopAllCoroutines();
        _anim.SetTrigger(deathParam);
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

    // ── Hit Reaction ───────────────────────────────────────────────────────────

    protected virtual void OnHitHandler()
    {
        if (!_stats.IsAlive) return;
        if (_state == State.Dormant || _state == State.Spawning || _state == State.Aggro) return;
        _anim.ResetTrigger(hitReactParam);
        _anim.SetTrigger(hitReactParam);
    }

    protected virtual void OnInterruptedHandler()
    {
        if (!_stats.IsAlive) return;
        if (_state == State.Dormant || _state == State.Spawning || _state == State.Aggro) return;
        _anim.ResetTrigger(hitReactParam);
        _anim.SetTrigger(hitReactParam);
    }

    protected virtual void OnStunnedHandler()
    {
        if (!_stats.IsAlive) return;
        if (_state == State.Dormant || _state == State.Spawning || _state == State.Aggro) return;
        _anim.ResetTrigger(hitReactParam);
        _anim.SetTrigger(hitReactParam);
    }

    // ── Utility ────────────────────────────────────────────────────────────────

    protected void SetVisible(bool visible)
    {
        foreach (var m in _meshes) m.enabled = visible;
    }

    protected void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float best = float.MaxValue;
        _target    = null;
        foreach (var p in players)
        {
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < best) { best = d; _target = p.transform; }
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
