using UnityEngine;

public abstract class ItemSO : ScriptableObject
{
    [Header("Item")]
    [SerializeField] protected string displayName;
    public Sprite       icon;
    public float        weight       = 1f;
    public int          maxStackSize = 1;
    public ItemCategory category;

    [Header("World")]
    [Tooltip("Prefab shown when item is dropped in the world. Falls back to WeaponSO.prefab for weapons.")]
    public GameObject worldPickupPrefab;

    public virtual string DisplayName => displayName;
}
