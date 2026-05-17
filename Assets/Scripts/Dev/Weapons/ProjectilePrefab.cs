using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ProjectilePrefab : MonoBehaviour
{
    [Header("Settings")]
    public float lifetime = 5f;
    public int   damage   = 10;

    [Header("Rotation Fix")]
    [Tooltip("90 if model points UP (Y-axis). 0 if model points FORWARD (Z-axis).")]
    public float rotationOffsetX = 90f;

    private Rigidbody  _rb;
    private bool       _stuck;
    private PlayerStats _owner;
    private WeaponSO    _weapon;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity      = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.interpolation   = RigidbodyInterpolation.Interpolate;
    }

    public void Setup(PlayerStats owner, WeaponSO weapon) { _owner = owner; _weapon = weapon; }

    public void Launch(Vector3 direction, float speed)
    {
        _rb.velocity = direction.normalized * speed;
        Destroy(gameObject, lifetime);
    }

    public void LaunchStraight(Vector3 direction, float speed)
    {
        _rb.useGravity = false;
        _rb.velocity   = direction.normalized * speed;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (_stuck || _rb.velocity.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.LookRotation(_rb.velocity)
                           * Quaternion.Euler(rotationOffsetX, 0f, 0f);
    }

    private void OnCollisionEnter(Collision col)
    {
        if (_stuck) return;
        if (col.gameObject.CompareTag("Player")) return;

        if (col.gameObject.CompareTag("Enemy"))
        {
            var target = col.gameObject.GetComponent<IDamageable>()
                      ?? col.gameObject.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                float dmg = _owner != null ? _owner.RawDamage(_weapon)
                          : (_weapon != null ? _weapon.damage : damage);
                target.TakeDamage(dmg);
            }
            Destroy(gameObject);
            return;
        }

        _stuck = true;
        _rb.velocity    = Vector3.zero;
        _rb.isKinematic = true;
        transform.SetParent(col.transform);
        Destroy(gameObject, 10f);
    }
}
