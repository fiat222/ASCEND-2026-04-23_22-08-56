using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotbarController : MonoBehaviour
{
    [SerializeField] private List<WeaponSO> weapons = new();

    [Header("UI — assign 9 slot Image components")]
    [SerializeField] private Image[] slotIcons      = new Image[9];
    [SerializeField] private GameObject[] slotHighlights = new GameObject[9];

    [Header("Off-hand Slot UI")]
    [SerializeField] private Image       offHandIcon;       // icon ของ slot มือซ้าย
    [SerializeField] private GameObject  offHandHighlight;  // frame/highlight ของ slot นั้น

    // ─── Events ───────────────────────────────────────────────
    public event Action<int>      OnSlotChanged;
    /// <summary>
    /// เรียกเมื่อ off-hand เปลี่ยน (null = ถอดออก / disable)
    /// </summary>
    public event Action<WeaponSO> OnOffHandChanged;

    // ─── Public State ─────────────────────────────────────────
    public int      SelectedIndex  { get; private set; } = 0;
    public WeaponSO SelectedWeapon => (weapons.Count > 0 && SelectedIndex < weapons.Count)
                                        ? weapons[SelectedIndex] : null;
    public int      SlotCount      => weapons.Count;

    public WeaponSO OffHandWeapon  { get; private set; }

    // ─── Unity ────────────────────────────────────────────────
    private void Start()
    {
        RefreshUI();
        SetHighlight(SelectedIndex);
        RefreshOffHandUI();
    }

    private void Update()
    {
        HandleScroll();
        HandleNumberKeys();
        HandleOffHandKey();
    }

    // ─── Input ────────────────────────────────────────────────
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

    /// <summary>
    /// กด L → ถ้า slot ปัจจุบัน equip off-hand ได้ → ย้ายไป off-hand slot
    ///         ถ้า slot ปัจจุบันคือ off-hand ที่ใส่อยู่แล้ว → ถอดออก
    /// </summary>
    private void HandleOffHandKey()
    {
        if (!Input.GetKeyDown(KeyCode.L)) return;

        WeaponSO current = SelectedWeapon;

        // กด L บน slot ที่ไม่มีอาวุธ → ไม่ทำอะไร
        if (current == null) return;

        // กด L บนอาวุธที่อยู่ใน off-hand อยู่แล้ว → ถอดออก
        if (current == OffHandWeapon)
        {
            SetOffHand(null);
            return;
        }

        // เช็คว่า equip off-hand ได้ไหม (Shield หรือ 1H เท่านั้น)
        if (!current.CanEquipOffHand)
        {
            Debug.Log($"[Hotbar] '{current.weaponName}' ไม่สามารถใส่มือซ้ายได้");
            return;
        }

        SetOffHand(current);
    }

    // ─── Slot Selection ───────────────────────────────────────
    private void SelectSlot(int index)
    {
        if (index == SelectedIndex) return;

        SelectedIndex = index;
        SetHighlight(index);
        OnSlotChanged?.Invoke(SelectedIndex);

        // Off-hand slot stays — PlayerCombat handles hide/show based on weapon type
    }

    // ─── Off-Hand ─────────────────────────────────────────────
    private void SetOffHand(WeaponSO so)
    {
        OffHandWeapon = so;
        RefreshOffHandUI();
        OnOffHandChanged?.Invoke(OffHandWeapon);
    }

    /// <summary>
    /// เรียกจากภายนอก (เช่น PlayerCombat) เพื่อ force disable off-hand
    /// เมื่อเปลี่ยนไปอาวุธที่ใช้สองมือ
    /// </summary>
    public void DisableOffHand()
    {
        if (OffHandWeapon != null)
            SetOffHand(null);
    }

    // ─── UI ───────────────────────────────────────────────────
    private void RefreshUI()
    {
        for (int i = 0; i < 9; i++)
        {
            if (slotIcons[i] == null) continue;

            WeaponSO weapon = (i < weapons.Count) ? weapons[i] : null;
            slotIcons[i].sprite  = weapon?.icon;
            slotIcons[i].enabled = weapon?.icon != null;
        }
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
        {
            if (slotHighlights[i] != null)
                slotHighlights[i].SetActive(i == selected);
        }
    }
}