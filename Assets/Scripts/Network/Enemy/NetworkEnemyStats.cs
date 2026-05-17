using PurrNet;
using UnityEngine;

public class NetworkEnemyStats : NetworkBehaviour, IDamageable
{
    private EnemyStats _stats;

    private void Awake() => _stats = GetComponent<EnemyStats>();

    public void TakeDamage(float raw)
    {
        if (isServer) ApplyDamage(raw);
        else          CmdTakeDamage(raw);
    }

    [ServerRpc(requireOwnership: false)]
    private void CmdTakeDamage(float raw) => ApplyDamage(raw);

    private void ApplyDamage(float raw)
    {
        _stats.TakeDamage(raw);
        SyncHpRpc(_stats.CurrentHp);
    }

    [ObserversRpc(excludeOwner: true)]
    private void SyncHpRpc(float hp) => _stats.SyncHp(hp);
}
