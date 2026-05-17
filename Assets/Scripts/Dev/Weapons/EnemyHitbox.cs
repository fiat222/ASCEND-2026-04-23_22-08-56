using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyHitbox : MonoBehaviour
{
    private float                    _damage;
    private bool                     _active;
    private readonly HashSet<Collider> _hitThisSwing = new();

    private Collider _col;

    private void Awake()
    {
        _col           = GetComponent<Collider>();
        _col.isTrigger = true;
        _col.enabled   = false;
    }

    public void Setup(float damage) => _damage = damage;

    public void EnableHitbox()
    {
        _hitThisSwing.Clear();
        _active      = true;
        _col.enabled = true;
    }

    public void DisableHitbox()
    {
        _active      = false;
        _col.enabled = false;
        _hitThisSwing.Clear();
    }

    private void OnTriggerEnter(Collider other) => TryHit(other);
    private void OnTriggerStay(Collider other)  => TryHit(other);

    private void TryHit(Collider other)
    {
        if (!_active) return;
        if (!other.CompareTag("Player")) return;
        if (_hitThisSwing.Contains(other)) return;
        if (!other.TryGetComponent<IDamageable>(out var target)) return;

        _hitThisSwing.Add(other);
        target.TakeDamage(_damage);
    }
}
