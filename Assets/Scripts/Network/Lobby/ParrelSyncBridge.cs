using System.Collections;
using System.IO;
using PurrNet;
using UnityEngine;
#if UNITY_EDITOR
using ParrelSync;
#endif

public class ParrelSyncBridge : MonoBehaviour
{
    [SerializeField] private NetworkLobby lobby;

    private void Start()
    {
#if UNITY_EDITOR
        if (!ClonesManager.IsClone()) return;
        StartCoroutine(AutoJoinRoutine());
#endif
    }

#if UNITY_EDITOR
    private IEnumerator AutoJoinRoutine()
    {
        string keyPath = Path.Combine(Application.dataPath, "..", "parrelkey.txt");

        while (!File.Exists(keyPath))
            yield return new WaitForSeconds(0.5f);

        string key = File.ReadAllText(keyPath).Trim();

        yield return new WaitUntil(() => NetworkManager.main != null);

        lobby?.JoinWithKey(key);
    }
#endif
}
