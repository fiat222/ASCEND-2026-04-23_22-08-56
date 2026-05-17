using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider      hpSlider;
    [SerializeField] private Slider      staminaSlider;
    [SerializeField] private Slider      manaSlider;

    private void Start()
    {
        // Local dev scene: playerStats assigned in Inspector
        if (playerStats != null) ConnectStats(playerStats);
    }

    // Called at runtime by NetworkPlayerStats when local player spawns
    public void ConnectStats(PlayerStats stats)
    {
        if (stats == null) return;

        // Unsubscribe old if reconnecting
        if (playerStats != null)
        {
            playerStats.OnHpChanged      -= RefreshHp;
            playerStats.OnStaminaChanged -= RefreshStamina;
            playerStats.OnManaChanged    -= RefreshMana;
        }

        playerStats = stats;

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            playerStats.OnHpChanged += RefreshHp;
            RefreshHp(playerStats.CurrentHp, playerStats.MaxHP);
        }

        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f;
            playerStats.OnStaminaChanged += RefreshStamina;
            RefreshStamina(playerStats.CurrentStamina, playerStats.MaxStamina);
        }

        if (manaSlider != null)
        {
            manaSlider.minValue = 0f;
            manaSlider.maxValue = 1f;
            playerStats.OnManaChanged += RefreshMana;
            RefreshMana(playerStats.CurrentMana, playerStats.MaxMana);
        }
    }

    private void OnDestroy()
    {
        if (playerStats == null) return;
        playerStats.OnHpChanged      -= RefreshHp;
        playerStats.OnStaminaChanged -= RefreshStamina;
        playerStats.OnManaChanged    -= RefreshMana;
    }

    private void RefreshHp(float current, float max)      => hpSlider.value      = current / max;
    private void RefreshStamina(float current, float max) => staminaSlider.value = current / max;
    private void RefreshMana(float current, float max)    => manaSlider.value    = current / max;
}
