using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BattleHUD : MonoBehaviour
{
    [Header("--- HERO INFO ---")]
    [SerializeField] private Image heroFaceImg;       
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Slider hpSlider;          
    [SerializeField] private TextMeshProUGUI hpText;  

    [Header("--- SKILL PANEL ---")]
    [SerializeField] private Transform skillContainer;
    [SerializeField] private GameObject skillBtnPrefab;

    [Header("--- TURN ORDER PANEL ---")]
    [SerializeField] private Transform turnOrderContainer; 
    [SerializeField] private GameObject turnIconPrefab;

    private List<SkillButtonUI> skillButtons = new List<SkillButtonUI>();

    public void UpdateHeroStatus(BattleUnit unit)
    {
        nameText.text = unit.unitName;

        if (unit.sourceHero.ClassData.Image != null)
        {
            heroFaceImg.sprite = unit.sourceHero.ClassData.Avatar;
        }

        UpdateHP(unit.currentHP, unit.maxHP);
    }

    public void UpdateHP(int current, int max)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = max;
            hpSlider.value = current;
        }

        if (hpText != null)
        {
            hpText.text = current.ToString();
        }
    }
    public void SetupSkillButtons(List<RuntimeSkill> skills)
    {
        foreach (Transform child in skillContainer) Destroy(child.gameObject);
        skillButtons.Clear();

        foreach (var skill in skills)
        {
            GameObject btnObj = Instantiate(skillBtnPrefab, skillContainer);
            SkillButtonUI btnUI = btnObj.GetComponent<SkillButtonUI>();

            if (btnUI != null)
            {
                btnUI.Setup(skill);
                skillButtons.Add(btnUI);
            }
        }
    }

    public void UpdateSkillSelectionUI(int selectedIndex)
    {
        for (int i = 0; i < skillButtons.Count; i++)
        {
            bool isSelected = (i == selectedIndex);
            skillButtons[i].SetSelected(isSelected);
        }
    }
    public void EnableActions(bool isEnabled)
    {
        skillContainer.gameObject.SetActive(isEnabled);
    }
    public void UpdateTurnOrderBar(List<BattleUnit> turnOrder, BattleUnit currentUnit)
    {
        foreach (Transform child in turnOrderContainer) Destroy(child.gameObject);

        foreach (var unit in turnOrder)
        {
            GameObject iconObj = Instantiate(turnIconPrefab, turnOrderContainer);
            Image img = iconObj.GetComponent<Image>();

            if (unit.sourceHero.ClassData.Image != null)
                img.sprite = unit.sourceHero.ClassData.Avatar;

            Transform overlay = iconObj.transform.Find("Overlay");
            Transform deadIcon = iconObj.transform.Find("DeadIcon");
            Transform turnArrow = iconObj.transform.Find("TurnArrow");
            Transform border = iconObj.transform.Find("Border");

            if (unit.ownerId == PlayerProfile.Instance.WalletAddress)
                border.GetComponent<Image>().color = Color.green;
            else
                border.GetComponent<Image>().color = Color.red;

            if (unit.currentHP <= 0)
            {
                if (overlay) overlay.gameObject.SetActive(true);
                if (deadIcon) deadIcon.gameObject.SetActive(true);
                if (turnArrow) turnArrow.gameObject.SetActive(false);
                iconObj.transform.localScale = Vector3.one * 0.75f;
            }
            else if (unit == currentUnit)
            {
                if (overlay) overlay.gameObject.SetActive(false);
                if (deadIcon) deadIcon.gameObject.SetActive(false);
                if (turnArrow) turnArrow.gameObject.SetActive(true);
                iconObj.transform.localScale = Vector3.one * 1.25f;
                iconObj.transform.SetAsLastSibling();
            }
            else
            {
                if (overlay) overlay.gameObject.SetActive(true);
                if (deadIcon) deadIcon.gameObject.SetActive(false);
                if (turnArrow) turnArrow.gameObject.SetActive(false);
                iconObj.transform.localScale = Vector3.one * 0.75f;
            }
        }
    }
}