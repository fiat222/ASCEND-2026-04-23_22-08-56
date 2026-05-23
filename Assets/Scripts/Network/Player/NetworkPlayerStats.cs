using PurrNet;
using UnityEngine;
using ASCEND.Core;

public class NetworkPlayerStats : NetworkBehaviour, IDamageable
{
    private PlayerStats _ps;

    private void Awake() => _ps = GetComponent<PlayerStats>();

    protected override void OnSpawned()
    {
        if (!isOwner) return;
        // Wire local player's stats to the HUD (player spawns at runtime, can't assign in Inspector)
        var hud = FindObjectOfType<PlayerHUD>();
        hud?.ConnectStats(_ps);

        // Subscribe to stat level up events for network sync
        _ps.OnStatLevelUp += HandleStatLevelUp;
    }

    private void OnDestroy()
    {
        if (_ps != null)
            _ps.OnStatLevelUp -= HandleStatLevelUp;
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

    // ── IDamageable ──────────────────────────────────────────────────────────

    public void TakeStagger(float staggerDmg)
    {
        // Stagger is local-only for now, no network sync needed
        // Enemy AI runs on server, stagger state managed there
        Debug.Log($"[NetworkPlayerStats] TakeStagger: {staggerDmg:F1} (local only)");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Stat Level Up Sync
    // ═══════════════════════════════════════════════════════════════════════════

    private void HandleStatLevelUp(CoreStatType stat, int newLevel)
    {
        if (!isOwner) return;
        Debug.Log($"[NetworkPlayerStats] Owner stat level up: {stat}={newLevel}. Broadcasting to network.");
        CmdBroadcastStatLevelUp(stat, newLevel);
    }

    [ServerRpc(requireOwnership: false)]
    private void CmdBroadcastStatLevelUp(CoreStatType stat, int newLevel)
    {
        Debug.Log($"[NetworkPlayerStats] CmdBroadcastStatLevelUp: {stat}={newLevel}");
        RpcSyncStatLevelUp(stat, newLevel);
    }

    [ObserversRpc]
    private void RpcSyncStatLevelUp(CoreStatType stat, int newLevel)
    {
        if (isOwner) return; // Owner already applied locally

        Debug.Log($"[NetworkPlayerStats] RpcSyncStatLevelUp: {stat}={newLevel} on non-owner");

        // Apply the stat level up to the local PlayerStats
        // We need to sync the stat value and recalculate derived values
        switch (stat)
        {
            case CoreStatType.VIT: _ps.VIT = newLevel; break;
            case CoreStatType.END: _ps.END = newLevel; break;
            case CoreStatType.AGI: _ps.AGI = newLevel; break;
            case CoreStatType.STR: _ps.STR = newLevel; break;
            case CoreStatType.DEX: _ps.DEX = newLevel; break;
            case CoreStatType.ARC: _ps.ARC = newLevel; break;
        }

        // Recalculate threshold for this stat
        _ps.RecalculateThreshold(stat);
        Debug.Log($"[NetworkPlayerStats] Synced stat {stat} to level {newLevel} on client.");
    }
}