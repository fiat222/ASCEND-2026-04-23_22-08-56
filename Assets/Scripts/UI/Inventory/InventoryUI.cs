using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject          panel;
    [SerializeField] private Transform           listRoot;
    [SerializeField] private GameObject          slotPrefab;
    [SerializeField] private TMP_Text            weightText;
    [SerializeField] private TMP_Dropdown        categoryFilter;
    [SerializeField] private PlayerStatPanel     statPanel;

    private NetworkInventory         _inventory;
    private readonly List<GameObject> _rows = new();

    private ItemCategory? _filterCategory = null;

    // ── Connect ───────────────────────────────────────────────────

    public void ConnectInventory(NetworkInventory inv)
    {
        if (_inventory != null) _inventory.OnInventoryChanged -= Refresh;
        _inventory = inv;
        if (inv != null) inv.OnInventoryChanged += Refresh;
        Refresh();
    }

    // ── Unity ─────────────────────────────────────────────────────

    private void Awake()
    {
        panel.SetActive(false);

        if (categoryFilter != null)
        {
            categoryFilter.ClearOptions();
            var opts = new List<string> { "All" };
            opts.AddRange(Enum.GetNames(typeof(ItemCategory)));
            categoryFilter.AddOptions(opts);
            categoryFilter.onValueChanged.AddListener(OnFilterChanged);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            TogglePanel();
    }

    // ── Toggle ────────────────────────────────────────────────────

    private void TogglePanel()
    {
        bool next = !panel.activeSelf;
        panel.SetActive(next);
        statPanel?.SetVisible(next);
        if (next) Refresh();
    }

    // ── Refresh ───────────────────────────────────────────────────

    private void Refresh()
    {
        if (_inventory == null) return;

        ClearRows();

        var stacks = _inventory != null ? FilteredStacks() : Array.Empty<ItemStack>();

        foreach (var stack in stacks)
        {
            var row = Instantiate(slotPrefab, listRoot);
            row.GetComponent<InventorySlotUI>()?.Bind(stack, _inventory);
            _rows.Add(row);
        }

        if (weightText != null)
            weightText.text = $"{_inventory.CurrentWeight:F1} / {_inventory.MaxWeight:F1} kg";
    }

    private IEnumerable<ItemStack> FilteredStacks()
    {
        if (_inventory == null) yield break;
        foreach (var stack in _inventory.GetInventoryStacks())
        {
            if (stack?.item == null) continue;
            if (_filterCategory.HasValue && stack.item.category != _filterCategory.Value) continue;
            yield return stack;
        }
    }

    private void ClearRows()
    {
        foreach (var r in _rows)
            if (r != null) Destroy(r);
        _rows.Clear();
    }

    // ── Filter ────────────────────────────────────────────────────

    private void OnFilterChanged(int idx)
    {
        _filterCategory = idx == 0 ? (ItemCategory?)null : (ItemCategory)(idx - 1);
        Refresh();
    }

    private void OnDestroy()
    {
        if (_inventory != null) _inventory.OnInventoryChanged -= Refresh;
    }
}
