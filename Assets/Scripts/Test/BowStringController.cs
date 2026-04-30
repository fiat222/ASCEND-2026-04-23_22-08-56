using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BowStringController : MonoBehaviour
{
    public Transform stringTop;
    public Transform stringBottom;
    [HideInInspector] public Transform handStringPoint; // ตัว PlayerTest จะส่งค่ามาให้เอง

    private LineRenderer _lineRenderer;
    private Animator _playerAnim;
    private string _holdParam = "Hold_Bow";

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        // ตรวจสอบ Animator จาก Object พ่อ (ตัวละคร)
        _playerAnim = GetComponentInParent<Animator>();
    }

    void LateUpdate()
    {
        if (stringTop == null || stringBottom == null) return;

        // จุดปลายทั้งสองด้าน
        _lineRenderer.SetPosition(0, stringTop.position);
        _lineRenderer.SetPosition(2, stringBottom.position);

        // จุดกลางเชือก
        if (_playerAnim != null && _playerAnim.GetBool(_holdParam) && handStringPoint != null)
        {
            // ขณะง้าง: ให้เชือกไปอยู่ที่มือ
            _lineRenderer.SetPosition(1, handStringPoint.position);
        }
        else
        {
            // ขณะพัก: ให้เชือกตึงเป็นเส้นตรง
            _lineRenderer.SetPosition(1, Vector3.Lerp(stringTop.position, stringBottom.position, 0.5f));
        }
    }
}