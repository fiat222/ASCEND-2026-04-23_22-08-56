using System;
using System.Collections.Generic;

public class Inventory
{
    public float MaxWeight { get; set; } = 60f;

    public float CurrentWeight
    {
        get
        {
            float total = 0f;
            foreach (var s in _stacks) total += s.TotalWeight;
            return total;
        }
    }

    public float WeightRatio => MaxWeight > 0f ? CurrentWeight / MaxWeight : 0f;

    private readonly List<ItemStack> _stacks = new();

    public event Action OnChanged;

    public IReadOnlyList<ItemStack> Stacks => _stacks;

    public bool TryAdd(ItemSO item, int qty = 1)
    {
        if (item == null || qty <= 0) return false;

        float addWeight = item.weight * qty;
        if (CurrentWeight + addWeight > MaxWeight) return false;

        // Try to stack onto existing entry
        if (item.maxStackSize > 1)
        {
            foreach (var s in _stacks)
            {
                if (s.item == item && s.quantity < item.maxStackSize)
                {
                    int canAdd = Math.Min(qty, item.maxStackSize - s.quantity);
                    s.quantity += canAdd;
                    qty -= canAdd;
                    if (qty <= 0) { OnChanged?.Invoke(); return true; }
                }
            }
        }

        // New stack (handles remainder or non-stackable)
        while (qty > 0)
        {
            int take = Math.Min(qty, item.maxStackSize);
            _stacks.Add(new ItemStack(item, take));
            qty -= take;
        }

        OnChanged?.Invoke();
        return true;
    }

    public bool TryRemove(ItemSO item, int qty = 1)
    {
        if (item == null || qty <= 0) return false;

        int available = 0;
        foreach (var s in _stacks)
            if (s.item == item) available += s.quantity;

        if (available < qty) return false;

        int remaining = qty;
        for (int i = _stacks.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (_stacks[i].item != item) continue;
            int take = Math.Min(remaining, _stacks[i].quantity);
            _stacks[i].quantity -= take;
            remaining -= take;
            if (_stacks[i].quantity <= 0) _stacks.RemoveAt(i);
        }

        OnChanged?.Invoke();
        return true;
    }

    public bool Contains(ItemSO item) => IndexOf(item) >= 0;

    public int IndexOf(ItemSO item)
    {
        for (int i = 0; i < _stacks.Count; i++)
            if (_stacks[i].item == item) return i;
        return -1;
    }

    public void Clear()
    {
        _stacks.Clear();
        OnChanged?.Invoke();
    }
}
