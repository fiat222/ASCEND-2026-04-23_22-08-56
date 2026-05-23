using PurrNet;
using UnityEngine;

public class NetworkPlayerStats : NetworkBehaviour, IDamageable
{
    private PlayerStats _ps;

    private void Awake() => _ps = GetComponent<PlayerStats>();

    protected override void OnSpawned()
    {
        if (!isOwner) return;
        FindObjectOfType<PlayerHUD>()?.ConnectStats(_ps);
        FindObjectOfType<PlayerStatPanel>()?.ConnectStats(_ps);

        var inv = GetComponent<NetworkInventory>();
        if (inv != null)
        {
            FindObjectOfType<HotbarController>()?.ConnectInventory(inv);
            FindObjectOfType<InventoryUI>()?.ConnectInventory(inv);
        }
    }

    public void TakeDamage(float raw)
    {
        if (isServer) ApplyDamage(raw);
        else          CmdTakeDamage(raw);
    }

    [ServerRpc(requireOwnership: false)]
    private void CmdTakeDamage(float raw) => ApplyDamage(raw);

    private void ApplyDamage(float raw)
    {
        _ps.TakeDamage(raw);
        SyncHpRpc(_ps.CurrentHp);
    }

    [ObserversRpc]
    private void SyncHpRpc(float hp) => _ps.SyncHp(hp);
}
