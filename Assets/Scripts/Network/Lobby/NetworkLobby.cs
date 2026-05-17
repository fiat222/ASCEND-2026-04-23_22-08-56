using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using PurrNet;
using PurrNet.Transports;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkLobby : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("UI")]
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button joinBtn;
    [SerializeField] private Button stopBtn;
    [SerializeField] private Button startBtn;
    [SerializeField] private Button copyBtn;
    [SerializeField] private TMP_InputField keyInput;
    [SerializeField] private TMP_Text keyDisplay;
    [SerializeField] private TMP_Text statusText;

    private void OnEnable()
    {
        hostBtn.onClick.AddListener(StartHost);
        joinBtn.onClick.AddListener(StartClient);
        stopBtn.onClick.AddListener(Stop);
        if (startBtn) startBtn.onClick.AddListener(StartGame);
        if (copyBtn)  copyBtn.onClick.AddListener(CopyKey);

        if (NetworkManager.main)
            NetworkManager.main.onClientConnectionState += OnConnectionState;

        RefreshStatus();
    }

    private void OnDisable()
    {
        hostBtn.onClick.RemoveListener(StartHost);
        joinBtn.onClick.RemoveListener(StartClient);
        stopBtn.onClick.RemoveListener(Stop);
        if (startBtn) startBtn.onClick.RemoveListener(StartGame);
        if (copyBtn)  copyBtn.onClick.RemoveListener(CopyKey);

        if (NetworkManager.main)
            NetworkManager.main.onClientConnectionState -= OnConnectionState;
    }

    private void StartHost()
    {
        var nm = NetworkManager.main;
        if (!nm) return;

        string localIp = GetLocalIPv4();
        int port = nm.transport is UDPTransport udp ? udp.serverPort : 7777;
        string key = EncodeKey(localIp, port);

        if (keyDisplay) keyDisplay.text = key;
        WriteKeyFile(key);

        nm.StartHost();
    }

    private void StartClient()
    {
        var nm = NetworkManager.main;
        if (!nm) return;

        string key = keyInput ? keyInput.text.Trim() : "";
        if (!DecodeKey(key, out string ip, out int port))
        {
            if (statusText) statusText.text = "Invalid key";
            return;
        }

        if (nm.transport is UDPTransport udpT) { udpT.address = ip; udpT.serverPort = (ushort)port; }
        if (statusText) statusText.text = $"Joining {ip}:{port}...";
        nm.StartClient();
    }

    // Called by ParrelSyncBridge on clone
    public void JoinWithKey(string key)
    {
        var nm = NetworkManager.main;
        if (!nm) return;
        if (!DecodeKey(key, out string ip, out int port))
        {
            if (statusText) statusText.text = "Invalid key";
            return;
        }

        if (nm.clientState == ConnectionState.Connected) return; // already connected

        if (nm.transport is UDPTransport udp) { udp.address = ip; udp.serverPort = (ushort)port; }
        if (statusText) statusText.text = $"Clone joining {ip}:{port}...";
        nm.StartClient();
    }

    // Host only — server loads scene, PurrNet syncs all clients automatically
    private void StartGame()
    {
        var nm = NetworkManager.main;
        if (!nm || !nm.isServer) return;
        nm.sceneModule.LoadSceneAsync(gameSceneName);
    }

    private void Stop()
    {
        var nm = NetworkManager.main;
        if (!nm) return;
        if (nm.isServer) nm.StopServer();
        if (nm.isClient) nm.StopClient();
        if (keyDisplay) keyDisplay.text = "";
    }

    private void CopyKey()
    {
        if (keyDisplay) GUIUtility.systemCopyBuffer = keyDisplay.text;
    }

    private void OnConnectionState(ConnectionState _) => RefreshStatus();

    private void RefreshStatus()
    {
        if (!statusText || !NetworkManager.main) return;
        var nm = NetworkManager.main;
        statusText.text = $"Server: {nm.serverState}  Client: {nm.clientState}";
    }

    private static void WriteKeyFile(string key)
    {
        string path = Path.Combine(Application.dataPath, "..", "parrelkey.txt");
        File.WriteAllText(path, key);
    }

    // ── Key encoding: IP (4 bytes) + port (2 bytes) → base36 string ──

    private const string B36 = "0123456789abcdefghijklmnopqrstuvwxyz";

    private static string EncodeKey(string ip, int port)
    {
        if (!IPAddress.TryParse(ip, out var addr)) return "";
        byte[] b = addr.GetAddressBytes();
        ulong packed = ((ulong)b[0] << 40) | ((ulong)b[1] << 32) |
                       ((ulong)b[2] << 24) | ((ulong)b[3] << 16) |
                       (ulong)(ushort)port;

        if (packed == 0) return "0";
        string result = "";
        while (packed > 0) { result = B36[(int)(packed % 36)] + result; packed /= 36; }
        return result;
    }

    private static bool DecodeKey(string key, out string ip, out int port)
    {
        ip = null;
        port = 0;
        if (string.IsNullOrEmpty(key)) return false;

        ulong packed = 0;
        foreach (char c in key.ToLower())
        {
            int d = B36.IndexOf(c);
            if (d < 0) return false;
            packed = packed * 36 + (ulong)d;
        }

        byte[] b = new byte[4];
        b[0] = (byte)((packed >> 40) & 0xFF);
        b[1] = (byte)((packed >> 32) & 0xFF);
        b[2] = (byte)((packed >> 24) & 0xFF);
        b[3] = (byte)((packed >> 16) & 0xFF);
        port = (int)(packed & 0xFFFF);
        ip = new IPAddress(b).ToString();
        return port > 0;
    }

    private static string GetLocalIPv4()
    {
        foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (iface.OperationalStatus != OperationalStatus.Up) continue;
            if (iface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            foreach (var addr in iface.GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    return addr.Address.ToString();
            }
        }
        return "127.0.0.1";
    }
}
