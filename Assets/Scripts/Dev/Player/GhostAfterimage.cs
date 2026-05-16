using System.Collections;
using UnityEngine;

public class GhostAfterimage : MonoBehaviour
{
    [Header("Ghost Settings")]
    [SerializeField] private float spawnInterval      = 0.03f;
    [SerializeField] private float ghostLifetime      = 0.3f;
    [SerializeField] private Color ghostColor         = new Color(0.4f, 0.8f, 1f, 0.6f);
    [SerializeField] private Material ghostMaterial;

    [Header("Dash Detection")]
    [SerializeField] private float dashSpeedThreshold = 12f; // just below dashSpeed (15)

    private CharacterController    _cc;
    private SkinnedMeshRenderer[]  _skinnedRenderers;
    private float                  _spawnTimer;

    private void Awake()
    {
        _cc               = GetComponent<CharacterController>();
        _skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    private void Update()
    {
        Vector3 horizontal = new Vector3(_cc.velocity.x, 0f, _cc.velocity.z);
        if (horizontal.magnitude >= dashSpeedThreshold)
        {
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                SpawnGhost();
                _spawnTimer = spawnInterval;
            }
        }
        else
        {
            _spawnTimer = 0f;
        }
    }

    private void SpawnGhost()
    {
        foreach (var smr in _skinnedRenderers)
        {
            Mesh bakedMesh = new Mesh();
            smr.BakeMesh(bakedMesh);

            GameObject ghost = new GameObject("Ghost");
            ghost.transform.position   = smr.transform.position;
            ghost.transform.rotation   = smr.transform.rotation;
            ghost.transform.localScale = smr.transform.lossyScale;

            ghost.AddComponent<MeshFilter>().mesh = bakedMesh;

            Material mat = new Material(ghostMaterial);
            mat.SetColor("_BaseColor", ghostColor);

            MeshRenderer mr = ghost.AddComponent<MeshRenderer>();
            mr.material = mat;

            StartCoroutine(FadeAndDestroy(ghost, mat, bakedMesh));
        }
    }

    private IEnumerator FadeAndDestroy(GameObject ghost, Material mat, Mesh mesh)
    {
        float elapsed    = 0f;
        Color startColor = ghostColor;

        while (elapsed < ghostLifetime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / ghostLifetime);
            mat.SetColor("_BaseColor", new Color(startColor.r, startColor.g, startColor.b, alpha));
            yield return null;
        }

        Destroy(ghost);
        Destroy(mat);
        Destroy(mesh);
    }
}
