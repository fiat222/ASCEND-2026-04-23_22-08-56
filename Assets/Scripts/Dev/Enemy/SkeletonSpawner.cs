using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PurrNet;

public class SkeletonSpawner : NetworkBehaviour
{
    public enum SpawnType { Spawn, Revive, Random }

    [Serializable]
    public struct SkeletonEntry
    {
        public GameObject prefab;
        public int        count;
    }

    [Header("Skeleton Types")]
    [SerializeField] private SkeletonEntry meleeEntry;
    [SerializeField] private SkeletonEntry throwerEntry;

    [Header("Grave Area")]
    [SerializeField] private int      maxAlive      = 2;
    [SerializeField] private float    spawnRadius   = 3f;
    [SerializeField] private float    detectionRange = 12f;
    [SerializeField] private SpawnType spawnType    = SpawnType.Spawn;

    [Header("Respawn")]
    [SerializeField] private bool  canRespawn   = true;
    [SerializeField] private float respawnDelay = 15f;

    private readonly List<EnemyAI>  _active     = new();
    private          Queue<GameObject> _spawnQueue = new();
    private bool _playerEntered;

    private void Start()
    {
        BuildQueue();
    }

    private void BuildQueue()
    {
        var entries = new List<GameObject>();
        for (int i = 0; i < meleeEntry.count; i++)   entries.Add(meleeEntry.prefab);
        for (int i = 0; i < throwerEntry.count; i++)  entries.Add(throwerEntry.prefab);

        // Shuffle
        for (int i = entries.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (entries[i], entries[j]) = (entries[j], entries[i]);
        }

        _spawnQueue = new Queue<GameObject>(entries);
    }

    private void Update()
    {
        if (!isServer) return;

        if (!_playerEntered)
        {
            if (PlayerInRange()) { _playerEntered = true; SpawnBatch(); }
            return;
        }

        if (!canRespawn) return;

        _active.RemoveAll(e => e == null);

        int missing = Mathf.Min(maxAlive - _active.Count, _spawnQueue.Count);
        for (int i = 0; i < missing; i++)
            StartCoroutine(RespawnRoutine());
    }

    private void SpawnBatch()
    {
        int count = Mathf.Min(maxAlive, _spawnQueue.Count);
        for (int i = 0; i < count; i++)
            SpawnOne();
    }

    private void SpawnOne()
    {
        if (_spawnQueue.Count == 0) return;

        var prefab = _spawnQueue.Dequeue();
        if (prefab == null) return;

        Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRadius;
        Vector3 pos    = transform.position + new Vector3(offset.x, 0f, offset.y);
        var go         = Instantiate(prefab, pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
        NetworkManager.main.Spawn(go);

        var ai         = go.GetComponent<EnemyAI>();
        bool useRevive = spawnType == SpawnType.Revive ||
                         (spawnType == SpawnType.Random && UnityEngine.Random.value > 0.5f);
        ai.TriggerSpawn(useRevive);
        _active.Add(ai);
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        if (_spawnQueue.Count > 0 && _active.Count < maxAlive) SpawnOne();
    }

    private bool PlayerInRange()
    {
        foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
            if (Vector3.Distance(transform.position, p.transform.position) <= detectionRange)
                return true;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, spawnRadius);
    }
}
