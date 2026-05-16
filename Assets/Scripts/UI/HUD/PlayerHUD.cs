using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider      hpSlider;

    private void Start()
    {
        hpSlider.minValue = 0f;
        hpSlider.maxValue = 1f;
        playerStats.OnHpChanged += RefreshHp;
        RefreshHp(playerStats.CurrentHp, playerStats.MaxHP);
    }

    private void OnDestroy() => playerStats.OnHpChanged -= RefreshHp;

    private void RefreshHp(float current, float max) => hpSlider.value = current / max;
}
