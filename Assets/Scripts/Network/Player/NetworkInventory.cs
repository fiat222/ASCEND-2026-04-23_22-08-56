using System;
using PurrNet;
using UnityEngine;

public class NetworkInventory : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private ItemDatabase database;
    [SerializeField] private GameObject   worldItemPrefab;
    [SerializeField] private float        maxCarryWeight = 60f;
    [SerializeField] private float        pickupRadius   = 2.5f;
    [SerializeField] private int          hotbarSize     = 9;

    [Header("Starting Items")]
    [SerializeField] private WeaponSO[] startingWeapons = new WeaponSO[0];

    // ── Owner-only state ──────────────────────────────────────────
    private Inventory _inv;
    private ItemSO[]  _hotbarItems;
    private int       _selectedIdx;
    private ItemSO    _offHandItem;

    // ── Synced to ALL clients (for visual weapon display) ──────────
    private int _equippedDbIndex = -1;
    private int _offHandDbIndex  = -1;

    // ── Public read ───────────────────────────────────────────────
    public float  WeightRatio   => _inv != null ? _inv.WeightRatio : 0f;
    public float  CurrentWeight => _inv != null ? _inv.CurrentWeight : 0f;
    public float  MaxWeight     => maxCarryWeight;
    public int    SelectedIdx   => _selectedIdx;
    public ItemSO[] HotbarItems => _hotbarItems;

    public WeaponSO EquippedWeapon =>
        _hotbarItems != null && _selectedIdx < _hotbarItems.Length
            ? _hotbarItems[_selectedIdx] as WeaponSO : null;

    public WeaponSO OffHandWeapon => _offHandItem as WeaponSO;

    public System.Collections.Generic.IReadOnlyList<ItemStack> GetInventoryStacks()
        => _inv?.Stacks ?? System.Array.Empty<ItemStack>();

    // ── Events (owner UI) ─────────────────────────────────────────
    public event Action OnInventoryChanged;
    public event Action OnHotbarChanged;

    // ── Event (all clients — weapon visual sync) ──────────────────
    public event Action<WeaponSO, WeaponSO> OnEquippedChanged;

    // ── Lifecycle ─────────────────────────────────────────────────

    protected override void OnSpawned()
    {
        Debug.Log($"[NetworkInventory] OnSpawned isOwner={isOwner} go={gameObject.name}");
        if (!isOwner) return;
        InitOwner();
    }

    protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
    {
        if (asServer) return;
        Debug.Log($"[NetworkInventory] OnOwnerChanged isOwner={isOwner} go={gameObject.name}");
        if (!isOwner) return;
        if (_inv == null) InitOwner();
    }

    private void InitOwner()
    {
        _inv         = new Inventory { MaxWeight = maxCarryWeight };
        _hotbarItems = new ItemSO[hotbarSize];
        _inv.OnChanged += HandleInventoryChanged;
        Debug.Log($"[NetworkInventory] InitOwner go={gameObject.name}");

        FindObjectOfType<HotbarController>()?.ConnectInventory(this);
        FindObjectOfType<InventoryUI>(true)?.ConnectInventory(this);

        for (int i = 0; i < startingWeapons.Length && i < hotbarSize; i++)
        {
            if (startingWeapons[i] == null) continue;
            _inv.TryAdd(startingWeapons[i], 1);
            _hotbarItems[i] = startingWeapons[i];
        }
        if (startingWeapons.Length > 0) BroadcastEquipped();
    }

    private float _autoPickupTimer;

    private void Update()
    {
        if (!isOwner) return;
        HandleHotbarInput();

        _autoPickupTimer -= Time.deltaTime;
        if (_autoPickupTimer <= 0f && WorldItem.FindNearest(transform.position, pickupRadius) != null)
        {
            _autoPickupTimer = 0.3f;
            CmdPickUp();
        }

        if (Input.GetKeyDown(KeyCode.G)) CmdDrop(_selectedIdx);
    }

    // ── Hotbar input ──────────────────────────────────────────────

    private void HandleHotbarInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
            SelectSlot((_selectedIdx + (scroll > 0f ? -1 : 1) + hotbarSize) % hotbarSize);

        for (int i = 0; i < Mathf.Min(hotbarSize, 9); i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { SelectSlot(i); break; }
    }

    private void SelectSlot(int idx)
    {
        if (idx == _selectedIdx) return;
        _selectedIdx = idx;
        OnHotbarChanged?.Invoke();
        BroadcastEquipped();
    }

    // ── Pickup ────────────────────────────────────────────────────

    [ServerRpc]
    private void CmdPickUp()
    {
        WorldItem worldItem = WorldItem.FindNearest(transform.position, pickupRadius);
        if (worldItem == null) return;

        int    dbIdx = worldItem.GetDbIndex();
        int    qty   = worldItem.GetQuantity();
        ItemSO so    = database?.Get(dbIdx);
        if (so == null) return;

        Destroy(worldItem.gameObject);

        if (owner.HasValue)
            SyncPickupRpc(owner.Value, dbIdx, qty);
    }

    [TargetRpc]
    private void SyncPickupRpc(PlayerID target, int dbIdx, int qty)
    {
        ItemSO so = database?.Get(dbIdx);
        if (so == null) return;

        if (_inv == null) { _inv = new Inventory { MaxWeight = maxCarryWeight }; _inv.OnChanged += HandleInventoryChanged; }
        if (_hotbarItems == null) _hotbarItems = new ItemSO[hotbarSize];

        _inv.TryAdd(so, qty);
        AutoAssignToHotbar(so, dbIdx);
        OnInventoryChanged?.Invoke();
        OnHotbarChanged?.Invoke();
        BroadcastEquipped();
    }

    // ── Drop ──────────────────────────────────────────────────────

    [ServerRpc]
    private void CmdDrop(int hotbarSlot)
    {
        if (owner.HasValue)
            SyncDropRpc(owner.Value, hotbarSlot);
    }

    [TargetRpc]
    private void SyncDropRpc(PlayerID target, int hotbarSlot)
    {
        if (_hotbarItems == null || hotbarSlot < 0 || hotbarSlot >= hotbarSize) return;
        ItemSO item = _hotbarItems[hotbarSlot];
        if (item == null) return;

        int dbIdx = database?.IndexOf(item) ?? -1;
        _inv.TryRemove(item, 1);
        _hotbarItems[hotbarSlot] = null;
        if (_offHandItem == item) _offHandItem = null;

        OnInventoryChanged?.Invoke();
        OnHotbarChanged?.Invoke();
        BroadcastEquipped();

        // Tell server to spawn WorldItem
        if (dbIdx >= 0) SpawnWorldItemServerRpc(dbIdx, 1);
    }

    [ServerRpc]
    private void SpawnWorldItemServerRpc(int dbIdx, int qty)
    {
        if (worldItemPrefab == null) return;
        Vector3 dropPos = transform.position + transform.forward * 1.2f + Vector3.up * 0.3f;
        var go = Instantiate(worldItemPrefab, dropPos, Quaternion.identity);
        go.GetComponent<WorldItem>()?.InitServer(dbIdx, qty);
        NetworkManager.main?.Spawn(go);
    }

    // ── Equip broadcast ───────────────────────────────────────────

    private void BroadcastEquipped()
    {
        int mainIdx    = database?.IndexOf(_hotbarItems?[_selectedIdx]) ?? -1;
        int offHndIdx  = database?.IndexOf(_offHandItem) ?? -1;
        SyncEquipServerRpc(mainIdx, offHndIdx);
    }

    [ServerRpc]
    private void SyncEquipServerRpc(int mainIdx, int offHndIdx)
        => SyncEquipObserversRpc(mainIdx, offHndIdx);

    [ObserversRpc(runLocally: true)]
    private void SyncEquipObserversRpc(int mainIdx, int offHndIdx)
    {
        _equippedDbIndex = mainIdx;
        _offHandDbIndex  = offHndIdx;
        var main    = database?.Get(mainIdx) as WeaponSO;
        var offhand = database?.Get(offHndIdx) as WeaponSO;
        OnEquippedChanged?.Invoke(main, offhand);
    }

    // ── Off-hand ──────────────────────────────────────────────────

    public void SetOffHand(ItemSO item)
    {
        if (!isOwner) return;
        _offHandItem = item;
        OnHotbarChanged?.Invoke();
        BroadcastEquipped();
    }

    // ── Hotbar slot assignment ────────────────────────────────────

    public void AssignToHotbarSlot(ItemSO item, int slot)
    {
        if (!isOwner || _hotbarItems == null) return;
        if (slot < 0 || slot >= hotbarSize) return;
        if (item != null && !_inv.Contains(item)) return;

        _hotbarItems[slot] = item;
        OnHotbarChanged?.Invoke();
        if (slot == _selectedIdx) BroadcastEquipped();
    }

    private void AutoAssignToHotbar(ItemSO item, int dbIdx)
    {
        if (_hotbarItems == null) return;

        foreach (var h in _hotbarItems)
            if (h == item) return; // already on hotbar

        for (int i = 0; i < hotbarSize; i++)
        {
            if (_hotbarItems[i] == null)
            {
                _hotbarItems[i] = item;
                if (i == _selectedIdx) BroadcastEquipped();
                return;
            }
        }
    }

    // ── Inventory change ──────────────────────────────────────────

    private void HandleInventoryChanged()
    {
        if (_hotbarItems == null) return;
        bool changed = false;

        for (int i = 0; i < hotbarSize; i++)
        {
            if (_hotbarItems[i] != null && !_inv.Contains(_hotbarItems[i]))
            { _hotbarItems[i] = null; changed = true; }
        }

        if (_offHandItem != null && !_inv.Contains(_offHandItem))
        { _offHandItem = null; changed = true; }

        if (changed) OnHotbarChanged?.Invoke();
    }
}
