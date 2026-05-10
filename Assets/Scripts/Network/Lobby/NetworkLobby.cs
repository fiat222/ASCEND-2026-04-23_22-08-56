using PurrNet;
using PurrNet.Transports;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkLobby : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button joinBtn;
    [SerializeField] private Button stopServerBtn;
    [SerializeField] private Button stopClientBtn;
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TMP_Text statusText;

    private void OnEnable()
    {
        hostBtn.onClick.AddListener(StartHost);
        joinBtn.onClick.AddListener(StartClient);
        stopServerBtn.onClick.AddListener(StopServer);
        stopClientBtn.onClick.AddListener(StopClient);

        if (NetworkManager.main)
        {
            NetworkManager.main.onServerConnectionState += OnServerState;
            NetworkManager.main.onClientConnectionState += OnClientState;
        }

        RefreshStatus();
    }

    private void OnDisable()
    {
        hostBtn.onClick.RemoveListener(StartHost);
        joinBtn.onClick.RemoveListener(StartClient);
        stopServerBtn.onClick.RemoveListener(StopServer);
        stopClientBtn.onClick.RemoveListener(StopClient);

        if (NetworkManager.main)
        {
            NetworkManager.main.onServerConnectionState -= OnServerState;
            NetworkManager.main.onClientConnectionState -= OnClientState;
        }
    }

    private void StartHost()
    {
        NetworkManager.main.StartHost();
    }

    private void StartClient()
    {
        var nm = NetworkManager.main;
        if (!nm) return;

        string ip = ipInput ? ipInput.text.Trim() : "";
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

        if (nm.transport is UDPTransport udp)
            udp.address = ip;

        nm.StartClient();
    }

    private void StopServer()
    {
        NetworkManager.main?.StopServer();
    }

    private void StopClient()
    {
        NetworkManager.main?.StopClient();
    }

    private void OnServerState(ConnectionState state) => RefreshStatus();
    private void OnClientState(ConnectionState state) => RefreshStatus();

    private void RefreshStatus()
    {
        if (!statusText || !NetworkManager.main) return;

        var nm = NetworkManager.main;
        statusText.text = $"Server: {nm.serverState}  Client: {nm.clientState}";
    }
}
