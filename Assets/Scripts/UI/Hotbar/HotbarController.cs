using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotbarController : MonoBehaviour
{
    // ── Standalone mode (dev / local scene) ───────────────────────
    [Header("Standalone Weapons (dev scene only — leave empty in network scene)")]
    [SerializeField] private List<WeaponSO> weapons = new();

    [Header("UI")]
    [SerializeField] private Image[]      slotIcons      = new Image[9];
    [SerializeField] private GameObject[] slotHighlights = new GameObject[9];

    [Header("Off-hand Slot UI")]
    [SerializeField] private Image      offHandIcon;
    [SerializeField] private GameObject offHandHighlight;

    // ── Runtime state ─────────────────────────────────────────────
    private NetworkInventory _netInventory;
    private bool             UseInventory => _netInventory != null;

    // ── Events ─────────────────────────────────────────────────────
    public event Action<int>      OnSlotChanged;
    public event Action<WeaponSO> OnOffHandChanged;

    // ── Public state ──────────────────────────────────────────────
    public int SelectedIndex { get; private set; } = 0;

    public WeaponSO SelectedWeapon => UseInventory
        ? _netInventory.EquippedWeapon
        : (SelectedIndex < weapons.Count ? weapons[SelectedIndex] : null);

    public WeaponSO OffHandWeapon => UseInventory
        ? _netInventory.OffHandWeapon
        : _standaloneOffHand;

    public int SlotCount => UseInventory ? (_netInventory.HotbarItems?.Length ?? 0) : weapons.Count;

    // Legacy helpers (used by NetworkPlayerCombat / PlayerCombat subscriber)
    public WeaponSO GetWeaponAt(int index) => UseInventory
        ? (_netInventory.HotbarItems != null && index < _netInventory.HotbarItems.Length
            ? _netInventory.HotbarItems[index] as WeaponSO : null)
        : (index >= 0 && index < weapons.Count ? weapons[index] : null);

    public int IndexOf(WeaponSO so) => UseInventory
        ? Array.IndexOf(_netInventory.HotbarItems, so)
        : weapons.IndexOf(so);

    private WeaponSO _standaloneOffHand;

    // ── Connect (called by NetworkPlayerStats.OnSpawned for owner) ─
    public void ConnectInventory(NetworkInventory inv)
    {
        if (_netInventory != null)
        {
            _netInventory.OnHotbarChanged -= RefreshUI;
            _netInventory.OnHotbarChanged -= FireSlotChanged;
        }

        _netInventory = inv;

        if (inv != null)
        {
            inv.OnHotbarChanged += RefreshUI;
            inv.OnHotbarChanged += FireSlotChanged;
        }

        RefreshUI();
    }

    // ── Unity ────────────────────────────────────────────────────

    private void Start()
    {
        RefreshUI();
        SetHighlight(SelectedIndex);
        RefreshOffHandUI();
    }

    private void Update()
    {
        if (UseInventory) return; // NetworkInventory handles input

        HandleScroll();
        HandleNumberKeys();
        HandleOffHandKey();
    }

    // ── Standalone input (dev scene) ─────────────────────────────

    private void HandleScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f || weapons.Count == 0) return;
        SelectSlotStandalone((SelectedIndex + (scroll > 0f ? -1 : 1) + weapons.Count) % weapons.Count);
    }

    private void HandleNumberKeys()
    {
        for (int i = 0; i < 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { SelectSlotStandalone(i); return; }
    }

    private void HandleOffHandKey()
    {
        if (!Input.GetKeyDown(KeyCode.L)) return;
        WeaponSO current = SelectedWeapon;
        if (current == null) return;

        if (current == _standaloneOffHand) { SetStandaloneOffHand(null); return; }
        if (!current.CanEquipOffHand) return;
        SetStandaloneOffHand(current);
    }

    private void SelectSlotStandalone(int index)
    {
        if (index == SelectedIndex) return;
        SelectedIndex = index;
        SetHighlight(index);
        OnSlotChanged?.Invoke(SelectedIndex);
    }

    private void SetStandaloneOffHand(WeaponSO so)
    {
        _standaloneOffHand = so;
        RefreshOffHandUI();
        OnOffHandChanged?.Invoke(so);
    }

    public void DisableOffHand()
    {
        if (!UseInventory && _standaloneOffHand != null) SetStandaloneOffHand(null);
    }

    // ── Inventory-mode helpers ────────────────────────────────────

    private void FireSlotChanged()
    {
        if (!UseInventory) return;
        SelectedIndex = _netInventory.SelectedIdx;
        SetHighlight(SelectedIndex);
        OnSlotChanged?.Invoke(SelectedIndex);
        OnOffHandChanged?.Invoke(_netInventory.OffHandWeapon);
    }

    // ── UI refresh ────────────────────────────────────────────────

    private void RefreshUI()
    {
        for (int i = 0; i < 9; i++)
        {
            if (slotIcons[i] == null) continue;
            WeaponSO weapon = GetWeaponAt(i);
            slotIcons[i].sprite  = weapon?.icon;
            slotIcons[i].enabled = weapon?.icon != null;
        }
        RefreshOffHandUI();
    }

    private void RefreshOffHandUI()
    {
        if (offHandIcon != null)
        {
            offHandIcon.sprite  = OffHandWeapon?.icon;
            offHandIcon.enabled = OffHandWeapon?.icon != null;
        }
        if (offHandHighlight != null)
            offHandHighlight.SetActive(OffHandWeapon != null);
    }

    private void SetHighlight(int selected)
    {
        for (int i = 0; i < slotHighlights.Length; i++)
            if (slotHighlights[i] != null)
                slotHighlights[i].SetActive(i == selected);
    }
}
