using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "ASCEND/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemSO> items = new();

    public ItemSO Get(int index) =>
        index >= 0 && index < items.Count ? items[index] : null;

    public int IndexOf(ItemSO item) => items.IndexOf(item);

    public int Count => items.Count;
}
