// StaffPillar.cs
using UnityEngine;

public class StaffPillar : MonoBehaviour
{
    [Header("Movement")]
    public float spawnHeight = 10f;
    public float moveSpeed = 12f;

    [Header("Impact")]
    public GameObject impactRoot;

    private Vector3 _groundWorldPos;
    private Vector3 _groundNormal;
    private int _damage;
    private LayerMask _damageMask;
    private float _aoeRadius;
    private bool _arrived;

    public void Init(int damage, LayerMask damageMask, float aoeRadius, Vector3 groundNormal)
    {
        _damage = damage;
        _damageMask = damageMask;
        _aoeRadius = aoeRadius;
        _groundNormal = groundNormal;
    }

    private void Start()
    {
        _groundWorldPos = transform.position;
        transform.position = _groundWorldPos + Vector3.up * spawnHeight;
        transform.rotation = Quaternion.identity;

        if (impactRoot != null)
            impactRoot.SetActive(false);
    }

    private void Update()
    {
        if (_arrived) return;

        transform.position = Vector3.MoveTowards(
            transform.position, _groundWorldPos, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _groundWorldPos) < 0.05f)
        {
            transform.position = _groundWorldPos;
            _arrived = true;
            OnReachGround();
        }
    }

    // StaffPillar.cs - แก้ OnReachGround
    private void OnReachGround()
    {
        if (impactRoot != null)
        {
            impactRoot.SetActive(true);
            impactRoot.transform.position = _groundWorldPos + _groundNormal * 0.02f;
            impactRoot.transform.rotation = Quaternion.FromToRotation(Vector3.up, _groundNormal);

            // หาร 2 เพื่อให้พอดีกับ aoeRadius
            float s = (_aoeRadius * 2f) / 10f / 2f;  // <-- หาร 2
            impactRoot.transform.localScale = new Vector3(s, 1f, s);

            foreach (var ps in impactRoot.GetComponentsInChildren<ParticleSystem>())
                ps.Play();
        }

        Collider[] hits = Physics.OverlapSphere(_groundWorldPos, _aoeRadius, _damageMask);
        foreach (var col in hits)
        {
            // col.GetComponent<IDamageable>()?.TakeDamage(_damage);
        }

        Destroy(gameObject, 2f);
    }
}