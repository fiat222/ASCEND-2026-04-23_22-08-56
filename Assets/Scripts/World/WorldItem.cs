using System.Collections.Generic;
using PurrNet;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldItem : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private ItemDatabase database;

    // Set by server before spawning
    private int _itemDbIndex = -1;
    private int _quantity    = 1;

    // Static registry for server-side proximity lookup
    private static readonly List<WorldItem> _all = new();

    // ── Visual ──────────────────────────────────────────────────
    private GameObject _visual;

    // ── PurrNet ─────────────────────────────────────────────────

    protected override void OnSpawned()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        _all.Add(this);

        if (isServer)
            SyncDataRpc(_itemDbIndex, _quantity);
    }

    protected override void OnDespawned()
    {
        _all.Remove(this);
        ClearVisual();
    }

    // ── Server init ──────────────────────────────────────────────

    public void InitServer(int dbIndex, int qty)
    {
        _itemDbIndex = dbIndex;
        _quantity    = qty;
    }

    // ── RPC ──────────────────────────────────────────────────────

    [ObserversRpc(runLocally: true)]
    private void SyncDataRpc(int dbIndex, int qty)
    {
        _itemDbIndex = dbIndex;
        _quantity    = qty;
        RefreshVisual();
    }

    // ── Visual ───────────────────────────────────────────────────

    private void RefreshVisual()
    {
        ClearVisual();

        ItemSO item = database?.Get(_itemDbIndex);
        if (item == null) return;

        GameObject visualPrefab = item.worldPickupPrefab;
        if (visualPrefab == null && item is WeaponSO weapon)
            visualPrefab = weapon.prefab;

        if (visualPrefab != null)
        {
            _visual = Instantiate(visualPrefab, transform);
            _visual.transform.localPosition = Vector3.zero;
            _visual.transform.localRotation = Quaternion.identity;

            // Disable any combat/network components on the visual
            foreach (var nb in _visual.GetComponentsInChildren<NetworkBehaviour>())
                Destroy(nb);
            foreach (var rb in _visual.GetComponentsInChildren<Rigidbody>())
                rb.isKinematic = true;
        }
    }

    private void ClearVisual()
    {
        if (_visual != null) { Destroy(_visual); _visual = null; }
    }

    // ── Public accessors (server) ─────────────────────────────────

    public ItemSO  GetItem()     => database?.Get(_itemDbIndex);
    public int     GetDbIndex()  => _itemDbIndex;
    public int     GetQuantity() => _quantity;

    // ── Static helpers ────────────────────────────────────────────

    public static WorldItem FindNearest(Vector3 pos, float maxDist)
    {
        WorldItem nearest  = null;
        float     bestDist = maxDist;

        foreach (var w in _all)
        {
            if (w == null) continue;
            float d = Vector3.Distance(pos, w.transform.position);
            if (d < bestDist) { bestDist = d; nearest = w; }
        }

        return nearest;
    }
}
