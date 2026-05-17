using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BowStringController : MonoBehaviour
{
    public Transform stringTop;
    public Transform stringBottom;
    [HideInInspector] public Transform handStringPoint;
    [HideInInspector] public Animator playerAnim;

    private LineRenderer _lineRenderer;
    private string _holdParam = "Hold_Bow";

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    void LateUpdate()
    {
        if (stringTop == null || stringBottom == null) return;

        _lineRenderer.SetPosition(0, stringTop.position);
        _lineRenderer.SetPosition(2, stringBottom.position);

        if (playerAnim != null && playerAnim.GetBool(_holdParam) && handStringPoint != null)
            _lineRenderer.SetPosition(1, handStringPoint.position);
        else
            _lineRenderer.SetPosition(1, Vector3.Lerp(stringTop.position, stringBottom.position, 0.5f));
    }
}