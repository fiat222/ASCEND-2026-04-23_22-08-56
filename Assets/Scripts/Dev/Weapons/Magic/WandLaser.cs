using UnityEngine;

public class WandLaser : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public float maxRange = 30f;
    public LayerMask hitMask;

    [Header("VFX")]
    public GameObject muzzleVFXPrefab;
    public GameObject impactVFXPrefab;

    [Header("Beam 3D")]
    public Material coreMaterial;   // สว่าง บาง
    public Material glowMaterial;   // โปร่งใส กว้าง
    public float coreWidth = 0.03f;
    public float glowWidth = 0.12f;

    [Header("Damage")]
    public float damagePerSecond = 20f;

    private GameObject _muzzleInstance;
    private GameObject _impactInstance;
    private GameObject _coreCylinder;
    private GameObject _glowCylinder;
    private bool _firing;

    private void Awake()
    {
        // ปิดตอนเริ่ม — cylinder ยังไม่ spawn จึงไม่ต้องทำอะไร
    }

    public void StartFire()
    {
        _firing = true;
        Transform origin = firePoint != null ? firePoint : transform;

        // Spawn cylinder beam
        if (_coreCylinder == null)
            _coreCylinder = CreateCylinder("CoreBeam", coreMaterial);
        if (_glowCylinder == null)
            _glowCylinder = CreateCylinder("GlowBeam", glowMaterial);

        // Spawn muzzle VFX
        if (muzzleVFXPrefab != null && _muzzleInstance == null)
        {
            _muzzleInstance = Instantiate(muzzleVFXPrefab, origin.position, origin.rotation, origin);
            foreach (var ps in _muzzleInstance.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.loop = true;
                main.stopAction = ParticleSystemStopAction.None;
            }
        }

        // Spawn impact VFX
        if (impactVFXPrefab != null && _impactInstance == null)
        {
            _impactInstance = Instantiate(impactVFXPrefab, origin.position, Quaternion.identity);
            foreach (var ps in _impactInstance.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.loop = true;
                main.stopAction = ParticleSystemStopAction.None;
            }
        }
    }

    public void StopFire()
    {
        _firing = false;

        if (_coreCylinder != null) { Destroy(_coreCylinder); _coreCylinder = null; }
        if (_glowCylinder != null) { Destroy(_glowCylinder); _glowCylinder = null; }
        if (_muzzleInstance != null) { Destroy(_muzzleInstance); _muzzleInstance = null; }
        if (_impactInstance != null) { Destroy(_impactInstance); _impactInstance = null; }
    }

    private void LateUpdate()
    {
        if (!_firing) return;

        Transform origin = firePoint != null ? firePoint : transform;
        Vector3 start = origin.position;
        Vector3 aimTarget = GetAimTarget(start);
        Vector3 dir = (aimTarget - start).normalized;

        Vector3 endPoint = Physics.Raycast(start, dir, out RaycastHit hit, maxRange, hitMask)
            ? hit.point
            : start + dir * maxRange;

        // อัปเดต cylinder beam
        UpdateCylinder(_coreCylinder, start, endPoint, coreWidth);
        UpdateCylinder(_glowCylinder, start, endPoint, glowWidth);

        // impact ที่ปลาย beam
        if (_impactInstance != null)
        {
            _impactInstance.transform.position = endPoint;
            _impactInstance.transform.rotation = Quaternion.LookRotation(-dir);
            foreach (var ps in _impactInstance.GetComponentsInChildren<ParticleSystem>())
                if (!ps.isPlaying) ps.Play();
        }

        // damage
        if (Physics.Raycast(start, dir, out RaycastHit dmgHit, maxRange, hitMask))
            dmgHit.collider.GetComponent<IDamageable>()?.TakeDamage(damagePerSecond * Time.deltaTime);
    }

    private GameObject CreateCylinder(string beamName, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = beamName;
        Destroy(go.GetComponent<Collider>());
        if (mat != null)
            go.GetComponent<Renderer>().material = mat;
        return go;
    }

    private void UpdateCylinder(GameObject cylinder, Vector3 start, Vector3 end, float width)
    {
        if (cylinder == null) return;

        Vector3 mid = (start + end) / 2f;
        float length = Vector3.Distance(start, end);

        cylinder.transform.position = mid;
        cylinder.transform.up = (end - start).normalized;
        cylinder.transform.localScale = new Vector3(width, length / 2f, width);
    }

    private Vector3 GetAimTarget(Vector3 fromPosition)
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, hitMask))
            return hit.point;
        return ray.origin + ray.direction * maxRange;
    }

    private void OnDestroy()
    {
        if (_coreCylinder != null) Destroy(_coreCylinder);
        if (_glowCylinder != null) Destroy(_glowCylinder);
        if (_muzzleInstance != null) Destroy(_muzzleInstance);
        if (_impactInstance != null) Destroy(_impactInstance);
    }
}