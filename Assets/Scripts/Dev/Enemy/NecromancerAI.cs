using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NecromancerAI : EnemyBase
{
    [Header("Flee")]
    [SerializeField] private float fleeRange = 4f;

    [Header("Magic Attack")]
    [SerializeField] private float      castRange        = 12f;
    [SerializeField] private float      castCooldown     = 3f;
    [SerializeField] private float      castDamage       = 20f;
    [SerializeField] private Transform  castPoint;
    [SerializeField] private GameObject magicProjectilePrefab;
    [SerializeField] private string     attackMagicParam = "Attack_Magic";

    [Header("Revive")]
    [SerializeField] private float           reviveRange    = 15f;
    [SerializeField] private float           reviveCooldown = 8f;
    [SerializeField] private List<EnemyBase> ownedSkeletons;

    private float _castTimer;
    private float _reviveTimer;
    private bool  _isCasting;
    private bool  _isReviving;

    protected override void UpdateChase()
    {
        if (_isCasting || _isReviving)
        {
            SetMovement(0f, 0f);
            return;
        }

        _castTimer   -= Time.deltaTime;
        _reviveTimer -= Time.deltaTime;

        float dist = Vector3.Distance(transform.position + attackCenterOffset, _target.position);

        if (dist < fleeRange)
        {
            RotateToward(_target.position);
            transform.position -= transform.forward * walkSpeed * Time.deltaTime;
            SetMovement(0f, -0.5f);
            return;
        }

        RotateToward(_target.position);

        if (_reviveTimer <= 0f)
        {
            EnemyBase dead = GetRevivableTarget();
            if (dead != null)
            {
                StartCoroutine(ReviveRoutine(dead));
                return;
            }
        }

        if (dist <= castRange && _castTimer <= 0f)
        {
            StartCoroutine(CastRoutine());
            return;
        }

        if (dist > castRange)
        {
            transform.position += transform.forward * walkSpeed * Time.deltaTime;
            SetMovement(0f, 0.5f);
        }
        else
        {
            SetMovement(0f, 0f);
        }
    }

    private EnemyBase GetRevivableTarget()
    {
        if (ownedSkeletons == null) return null;
        foreach (var s in ownedSkeletons)
        {
            if (s != null && s.IsDead &&
                Vector3.Distance(transform.position, s.transform.position) <= reviveRange)
                return s;
        }
        return null;
    }

    private IEnumerator CastRoutine()
    {
        _isCasting = true;
        SetMovement(0f, 0f);
        _anim.SetTrigger(attackMagicParam);
        yield return new WaitUntil(() => _anim.IsInTransition(0));
        float len = _anim.GetNextAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(len > 0.1f ? len : 1.5f);
        _castTimer = castCooldown;
        _isCasting = false;
    }

    // Called by animation event at cast frame in Attack_Magic clip
    public void OnCastProjectile()
    {
        if (_target == null || magicProjectilePrefab == null || castPoint == null) return;
        Vector3 dir = (_target.position + Vector3.up * 0.5f) - castPoint.position;
        var go   = Instantiate(magicProjectilePrefab, castPoint.position, Quaternion.LookRotation(dir));
        var proj = go.GetComponent<NecromancerProjectile>();
        if (proj != null) { proj.damage = castDamage; proj.Launch(dir); }
    }

    private IEnumerator ReviveRoutine(EnemyBase skeleton)
    {
        _isReviving = true;

        while (skeleton != null && skeleton.IsDead)
        {
            float dist = Vector3.Distance(transform.position, skeleton.transform.position);
            if (dist <= 2f) break;

            if (!_stats.IsInterrupted && !_stats.IsStunned)
            {
                RotateToward(skeleton.transform.position);
                transform.position += transform.forward * walkSpeed * Time.deltaTime;
                SetMovement(0f, 0.5f);
            }
            else
            {
                SetMovement(0f, 0f);
            }
            yield return null;
        }

        if (skeleton != null && skeleton.IsDead)
        {
            SetMovement(0f, 0f);
            skeleton.ReviveEnemy();
        }

        _reviveTimer = reviveCooldown;
        _isReviving  = false;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, castRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fleeRange);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, reviveRange);
    }
}
