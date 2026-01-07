using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using Thirdweb.Api;

public class ItemTooltip : MonoBehaviour
{
    public static ItemTooltip Instance;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI contentText;
    private RectTransform rectTransform;

    public CanvasGroup canvasGroup;

    void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        HideTooltip();
    }

    public void ShowTooltip(ItemData item)
    {
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;

        titleText.text = item.Id;

        if (item is WeaponItem weapon)
        {
            if (weapon != null)
            {
                string stats = "";
                if (weapon.attackBonus != 0) stats += $"+{weapon.attackBonus} ATK\n";
                if (weapon.hpBonus != 0) stats += $"+{weapon.hpBonus} ATK\n";
                if (weapon.defenseBonus != 0) stats += $"+{weapon.defenseBonus} ATK\n";
                if (weapon.speedBonus != 0) stats += $"+{weapon.speedBonus} ATK\n";
                contentText.text = stats;
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform); 
    }
    public void HideTooltip()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }

    void Update()
    {
        transform.position = Input.mousePosition;
    }
}