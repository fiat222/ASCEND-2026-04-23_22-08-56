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

            MeshRenderer mr = ghost.AddComponent<MeshRenderer>();
            mr.material = ghostMaterial != null
                ? new Material(ghostMaterial)
                : new Material(Shader.Find("Standard"));
            mr.material.color = ghostColor;
            SetMaterialTransparent(mr.material);

            StartCoroutine(FadeAndDestroy(ghost, mr.material));
        }
    }

    private IEnumerator FadeAndDestroy(GameObject ghost, Material mat)
    {
        float elapsed    = 0f;
        Color startColor = mat.color;

        while (elapsed < ghostLifetime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / ghostLifetime);
            mat.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(ghost);
    }

    private void SetMaterialTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 2);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }
}
