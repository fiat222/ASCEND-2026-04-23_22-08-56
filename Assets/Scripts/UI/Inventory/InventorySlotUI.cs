using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image   iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text qtyText;
    [SerializeField] private TMP_Text weightText;

    private ItemStack        _stack;
    private NetworkInventory _inventory;

    public void Bind(ItemStack stack, NetworkInventory inventory)
    {
        _stack     = stack;
        _inventory = inventory;

        if (stack?.item == null) { gameObject.SetActive(false); return; }
        gameObject.SetActive(true);

        iconImage.sprite  = stack.item.icon;
        iconImage.enabled = stack.item.icon != null;
        nameText.text     = stack.item.DisplayName;
        qtyText.text      = stack.quantity > 1 ? $"x{stack.quantity}" : string.Empty;
        weightText.text   = $"{stack.TotalWeight:F1} kg";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_stack == null || _inventory == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
            _inventory.AssignToHotbarSlot(_stack.item, _inventory.SelectedIdx);
    }
}
