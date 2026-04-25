using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotbarController : MonoBehaviour
{
    [SerializeField] private List<WeaponSO> weapons = new();

    [Header("UI — assign 9 slot Image components")]
    [SerializeField] private Image[] slotIcons = new Image[9];
    [SerializeField] private GameObject[] slotHighlights = new GameObject[9];

    public event Action<int> OnSlotChanged;

    public int SelectedIndex { get; private set; } = 0;
    public WeaponSO SelectedWeapon => (weapons.Count > 0 && SelectedIndex < weapons.Count)
        ? weapons[SelectedIndex] : null;
    public int SlotCount => weapons.Count;

    private void Start()
    {
        RefreshUI();
        SetHighlight(SelectedIndex);
    }

    private void Update()
    {
        HandleScroll();
        HandleNumberKeys();
    }

    private void HandleScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f || weapons.Count == 0) return;

        int dir = scroll > 0f ? -1 : 1;
        SelectSlot((SelectedIndex + dir + weapons.Count) % weapons.Count);
    }

    private void HandleNumberKeys()
    {
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
                return;
            }
        }
    }

    private void SelectSlot(int index)
    {
        if (index == SelectedIndex) return;
        SelectedIndex = index;
        SetHighlight(index);
        OnSlotChanged?.Invoke(SelectedIndex);
    }

    private void RefreshUI()
    {
        for (int i = 0; i < 9; i++)
        {
            if (slotIcons[i] == null) continue;

            WeaponSO weapon = (i < weapons.Count) ? weapons[i] : null;
            slotIcons[i].sprite = weapon?.icon;
            slotIcons[i].enabled = weapon?.icon != null;
        }
    }

    private void SetHighlight(int selected)
    {
        for (int i = 0; i < slotHighlights.Length; i++)
        {
            if (slotHighlights[i] != null)
                slotHighlights[i].SetActive(i == selected);
        }
    }
}
