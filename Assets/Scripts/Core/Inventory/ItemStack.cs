using System;
using UnityEngine;

[Serializable]
public class ItemStack
{
    public ItemSO item;
    public int    quantity;

    public ItemStack(ItemSO item, int quantity = 1)
    {
        this.item     = item;
        this.quantity = quantity;
    }

    public float TotalWeight => item != null ? item.weight * quantity : 0f;
}
