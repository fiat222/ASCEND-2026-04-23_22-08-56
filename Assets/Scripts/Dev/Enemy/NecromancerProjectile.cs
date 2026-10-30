using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NecromancerProjectile : MonoBehaviour
{
    public float damage   = 20f;
    public float speed    = 10f;
    public float lifetime = 6f;

    private Rigidbody _rb;

    private void Awake() => _rb = GetComponent<Rigidbody>();

    public void Launch(Vector3 direction)
    {
        _rb.velocity = direction.normalized * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Player"))
            col.gameObject.GetComponent<IDamageable>()?.TakeDamage(damage);
        Destroy(gameObject);
    }
}
