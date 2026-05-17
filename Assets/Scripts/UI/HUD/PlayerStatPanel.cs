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
        if (Input.GetKeyDown(KeyCode.Tab))
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
        vitText.text = $"VIT  {playerStats.VIT}";
        endText.text = $"END  {playerStats.END}";
        agiText.text = $"AGI  {playerStats.AGI}";
        strText.text = $"STR  {playerStats.STR}";
        dexText.text = $"DEX  {playerStats.DEX}";
        arcText.text = $"ARC  {playerStats.ARC}";

        hpText.text        = $"HP          {Mathf.RoundToInt(playerStats.MaxHP)}";
        staminaText.text   = $"Stamina     {Mathf.RoundToInt(playerStats.MaxStamina)}";
        stamRecText.text   = $"Stam.Rec    {playerStats.StaminaRecovery:F1}/s";
        equipLoadText.text = $"Equip Load  {Mathf.RoundToInt(playerStats.EquipLoad)}";
        critRateText.text  = $"Crit Rate   {playerStats.CritRate:F1}%";
        critDmgText.text   = $"Crit Dmg    {playerStats.CritDamage:F2}x";
        manaText.text      = $"Mana        {Mathf.RoundToInt(playerStats.MaxMana)}";
        moveSpeedText.text = $"Move Speed  {playerStats.MovementSpeed:F1}";
    }
}
