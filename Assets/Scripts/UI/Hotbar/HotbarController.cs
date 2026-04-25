using System;
using System.Collections.Generic;
using UnityEngine;

public class HotbarController : MonoBehaviour
{
    [SerializeField] private List<WeaponSO> weapons = new();

    public event Action<int> OnSlotChanged;

    public int SelectedIndex { get; private set; } = 0;
    public WeaponSO SelectedWeapon => (weapons.Count > 0 && SelectedIndex < weapons.Count)
        ? weapons[SelectedIndex] : null;
    public int SlotCount => weapons.Count;

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
        for (int i = 0; i < Mathf.Min(weapons.Count, 9); i++)
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
        OnSlotChanged?.Invoke(SelectedIndex);
    }
}
