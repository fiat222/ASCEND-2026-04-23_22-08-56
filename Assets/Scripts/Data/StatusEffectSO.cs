using UnityEngine;

[CreateAssetMenu(fileName = "Bleeding", menuName = "ASCEND/StatusEffect/Bleeding")]
public class StatusEffectSO : ScriptableObject
{
    public string effectName = "Bleeding";
    public float buildupThreshold = 100f;
    public float duration = 20f;

    [Header("Bleeding")]
    public float bonusDamagePercentMaxHP = 3f;

    [Header("Poison")]
    public bool blocksHealing = true;

    [Header("Freezing")]
    public float staminaRecoveryMultiplier = 0.25f;
    public float selfStaggerDamageMultiplier = 1.5f;
}