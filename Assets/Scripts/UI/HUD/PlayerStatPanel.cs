using UnityEngine;
using TMPro;

public class PlayerStatPanel : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private GameObject  panel;

    [Header("Core Stats")]
    [SerializeField] private TMP_Text vitText;
    [SerializeField] private TMP_Text endText;
    [SerializeField] private TMP_Text agiText;
    [SerializeField] private TMP_Text strText;
    [SerializeField] private TMP_Text dexText;
    [SerializeField] private TMP_Text arcText;

    [Header("Derived Stats")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text stamRecText;
    [SerializeField] private TMP_Text equipLoadText;
    [SerializeField] private TMP_Text critRateText;
    [SerializeField] private TMP_Text critDmgText;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private TMP_Text moveSpeedText;

    private void Awake() => panel.SetActive(false);

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            TogglePanel();
    }

    private void TogglePanel()
    {
        bool next = !panel.activeSelf;
        panel.SetActive(next);
        if (next) Refresh();
    }

    private void Refresh()
    {
        vitText.text = playerStats.VIT.ToString();
        endText.text = playerStats.END.ToString();
        agiText.text = playerStats.AGI.ToString();
        strText.text = playerStats.STR.ToString();
        dexText.text = playerStats.DEX.ToString();
        arcText.text = playerStats.ARC.ToString();

        hpText.text        = Mathf.RoundToInt(playerStats.MaxHP).ToString();
        staminaText.text   = Mathf.RoundToInt(playerStats.MaxStamina).ToString();
        stamRecText.text   = playerStats.StaminaRecovery.ToString("F1");
        equipLoadText.text = Mathf.RoundToInt(playerStats.EquipLoad).ToString();
        critRateText.text  = playerStats.CritRate.ToString("F1") + "%";
        critDmgText.text   = playerStats.CritDamage.ToString("F2") + "x";
        manaText.text      = Mathf.RoundToInt(playerStats.MaxMana).ToString();
        moveSpeedText.text = playerStats.MovementSpeed.ToString("F1");
    }
}
