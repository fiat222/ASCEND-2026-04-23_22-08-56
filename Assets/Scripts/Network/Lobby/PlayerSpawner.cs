using PurrNet;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

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

    private void OnPlayerJoined(PlayerID player, bool isReconnect, bool asServer)
    {
        if (!asServer || isReconnect) return;

        Vector3 pos = GetSpawnPoint();
        var obj = Instantiate(playerPrefab, pos, Quaternion.identity);

        NetworkManager.main.Spawn(obj);
        obj.GetComponent<NetworkIdentity>().GiveOwnership(player);
    }

    private Vector3 GetSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return Vector3.zero;

        int index = Random.Range(0, spawnPoints.Length);
        return spawnPoints[index].position;
    }
}
