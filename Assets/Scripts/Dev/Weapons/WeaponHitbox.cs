using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
    [SerializeField] private string enemyTag = "Enemy";

    private PlayerStats         _owner;
    private WeaponSO            _weapon;
    private bool                _active;
    private readonly HashSet<Collider> _hitThisSwing = new();

    private Collider _col;

    private void Awake()
    {
        _col           = GetComponent<Collider>();
        _col.isTrigger = true;
        _col.enabled   = false;
    }

    public void Setup(PlayerStats owner, WeaponSO weapon)
    {
        _owner  = owner;
        _weapon = weapon;
    }

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

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[WeaponHitbox] OnTriggerEnter: {other.gameObject.name} | active={_active} | tag={other.tag}");

        if (!_active) return;
        if (!other.CompareTag(enemyTag))
        {
            Debug.Log($"[WeaponHitbox] Tag mismatch: got '{other.tag}', need '{enemyTag}'");
            return;
        }
        if (_hitThisSwing.Contains(other)) return;

        var target = other.GetComponent<IDamageable>()
                  ?? other.GetComponentInParent<IDamageable>();
        if (target == null)
        {
            Debug.Log($"[WeaponHitbox] No IDamageable on {other.gameObject.name}");
            return;
        }

        _hitThisSwing.Add(other);

        // Calculate and apply damage
        float rawDmg = _owner != null
            ? _owner.RawDamage(_weapon)
            : (_weapon != null ? _weapon.damage : 5f);

        Debug.Log($"[WeaponHitbox] Dealing {rawDmg} damage to {other.gameObject.name}");
        target.TakeDamage(rawDmg);

        // Calculate and apply stagger
        float staggerDmg = _owner != null
            ? _owner.CalculateStagger(_weapon)
            : (_weapon != null ? _weapon.baseStagger : 25f);

        Debug.Log($"[WeaponHitbox] Applying {staggerDmg} stagger to {other.gameObject.name}");
        target.TakeStagger(staggerDmg);
    }
}