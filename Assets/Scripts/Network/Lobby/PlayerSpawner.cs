using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private readonly HashSet<PlayerID> _spawned = new();

    private void OnEnable()
    {
        if (NetworkManager.main)
            NetworkManager.main.onPlayerJoined += OnPlayerJoined;
    }

    private void OnDisable()
    {
        if (NetworkManager.main)
            NetworkManager.main.onPlayerJoined -= OnPlayerJoined;
    }

    private void Start()
    {
        if (!NetworkManager.main || !NetworkManager.main.isServer) return;

        // Spawn for players already connected before this scene loaded
        foreach (var player in NetworkManager.main.playerModule.players)
            SpawnForPlayer(player);
    }

    private void OnPlayerJoined(PlayerID player, bool isReconnect, bool asServer)
    {
        if (!asServer || isReconnect) return;
        SpawnForPlayer(player);
    }

    private void SpawnForPlayer(PlayerID player)
    {
        if (_spawned.Contains(player)) return;
        _spawned.Add(player);

        var obj = Instantiate(playerPrefab, GetSpawnPoint(), Quaternion.identity);
        NetworkManager.main.Spawn(obj);
        obj.GetComponent<NetworkIdentity>().GiveOwnership(player);
    }

    private Vector3 GetSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return Vector3.zero;
        return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
    }
}
