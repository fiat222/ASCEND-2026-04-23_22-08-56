using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MagicBallDamage : MonoBehaviour
{
    [Header("Impact")]
    [SerializeField] private GameObject impactPrefab;

    [Header("Movement")]
    public float speed    = 15f;
    public float maxRange = 50f;

    private PlayerStats _owner;
    private WeaponSO    _weapon;
    private bool        _hit;
    private Vector3     _startPos;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Start()
    {
        _startPos = transform.position;
    }

    public void Setup(PlayerStats owner, WeaponSO weapon)
    {
        _owner  = owner;
        _weapon = weapon;
    }

    private void Update()
    {
        if (_hit) return;
        transform.position += transform.forward * speed * Time.deltaTime;
        if (Vector3.Distance(_startPos, transform.position) >= maxRange)
            SpawnImpact();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hit) return;
        if (other.CompareTag("Player")) return;

        if (other.CompareTag("Enemy"))
        {
            var target = other.GetComponent<IDamageable>()
                      ?? other.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                float dmg = _owner != null ? _owner.RawDamage(_weapon)
                          : (_weapon != null ? _weapon.damage : 10f);
                target.TakeDamage(dmg);
            }
        }

        SpawnImpact();
    }

    private void SpawnImpact()
    {
        if (_hit) return;
        _hit = true;

        if (impactPrefab != null)
            Instantiate(impactPrefab, transform.position, Quaternion.identity);

        var ps = GetComponentInChildren<ParticleSystem>();
        ps?.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        Destroy(gameObject);
    }
}
