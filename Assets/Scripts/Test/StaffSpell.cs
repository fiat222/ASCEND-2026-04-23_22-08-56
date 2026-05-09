// StaffSpell.cs
using UnityEngine;

public class StaffSpell : MonoBehaviour
{
    [Header("AoE Indicator")]
    public GameObject aoeIndicatorPrefab;
    public LayerMask groundLayer;
    public float aoeRadius = 4f;

    [Header("Impact")]
    public GameObject impactVFXPrefab;
    public int damage = 30;
    public LayerMask damageMask;

    private GameObject _indicator;
    private Vector3 _targetPos;
    private Vector3 _targetNormal;  // เพิ่ม: เก็บ normal ของพื้น
    private bool _isCasting;
    
    // StaffSpell.cs - แก้ BeginCast และ Update
    private const float SURFACE_OFFSET = 0.02f;

    private void Update()
    {
        if (!_isCasting) return;
        GetGroundPoint(out _targetPos, out _targetNormal);

        if (_indicator != null)
        {
            // ดันออกตาม normal ไม่ทับพื้น
            _indicator.transform.position = _targetPos + _targetNormal * SURFACE_OFFSET;
            _indicator.transform.rotation = Quaternion.FromToRotation(Vector3.up, _targetNormal);
        }
    }

    public void BeginCast()
    {
        _isCasting = true;
        GetGroundPoint(out _targetPos, out _targetNormal);

        if (aoeIndicatorPrefab != null)
        {
            _indicator = Instantiate(
                aoeIndicatorPrefab,
                _targetPos + _targetNormal * SURFACE_OFFSET,  // ดันออกตาม normal
                Quaternion.FromToRotation(Vector3.up, _targetNormal)
            );
            float s = (aoeRadius * 2f) / 10f;
            _indicator.transform.localScale = new Vector3(s, 1f, s);
        }
    }

    public void Cast()
    {
        _isCasting = false;
        Vector3 pos = _targetPos;
        Vector3 normal = _targetNormal;
        DestroyIndicator();

        if (impactVFXPrefab == null) { Debug.LogWarning("[StaffSpell] impactVFXPrefab is NULL"); return; }

        GameObject vfx = Instantiate(impactVFXPrefab, pos, Quaternion.identity);

        var pillar = vfx.GetComponentInChildren<StaffPillar>(includeInactive: true);
        if (pillar != null)
            pillar.Init(damage, damageMask, aoeRadius, normal);  // ส่ง normal ไปด้วย
        else
            Debug.LogWarning("[StaffSpell] StaffPillar NOT found in VFX prefab children");
    }

    public void CancelCast()
    {
        _isCasting = false;
        DestroyIndicator();
    }

    private void DestroyIndicator()
    {
        if (_indicator != null) { Destroy(_indicator); _indicator = null; }
    }

    private void GetGroundPoint(out Vector3 point, out Vector3 normal)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            point = hit.point;
            normal = hit.normal;
            return;
        }
        point = transform.position + transform.forward * 5f;
        normal = Vector3.up;
    }
}