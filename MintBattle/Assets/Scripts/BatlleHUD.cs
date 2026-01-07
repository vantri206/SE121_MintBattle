using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BattleHUD : MonoBehaviour
{
    [Header("--- HERO INFO ---")]
    [SerializeField] private GameObject heroInfo;
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

    [Header("--- TOP BAR NAME TEXT  ---")]
    public TextMeshProUGUI leftPlayerNameText;  
    public TextMeshProUGUI rightPlayerNameText;
    public GameObject leftArrow;
    public GameObject rightArrow;


    private List<SkillButtonUI> skillButtons = new List<SkillButtonUI>();

    public void UpdateHeroStatus(BattleUnit unit)
    {
        heroInfo.SetActive(true);

        nameText.text = unit.unitName;

        if (unit.SourceHero.ClassData.Image != null)
        {
            heroFaceImg.sprite = unit.SourceHero.ClassData.Avatar;
        }

        UpdateHP(unit.CurrentHP, unit.MaxHP);
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

                if (skill.skillData.type == SkillType.Passive)
                {
                    btnUI.border.color = Color.gray;
                }

                if (skill.skillData.Id == "SK00")
                {
                    btnUI.border.color = Color.red;
                }

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

            if (unit.SourceHero.ClassData.Image != null)
                img.sprite = unit.SourceHero.ClassData.Avatar;

            Transform overlay = iconObj.transform.Find("Overlay");
            Transform deadIcon = iconObj.transform.Find("DeadIcon");
            Transform turnArrow = iconObj.transform.Find("TurnArrow");
            Transform border = iconObj.transform.Find("Border");

            if (unit.OwnerId == PlayerProfile.Instance.WalletAddress)
                border.GetComponent<Image>().color = Color.green;
            else
                border.GetComponent<Image>().color = Color.red;

            if (unit.CurrentHP <= 0)
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
    public void UpdatePlayerName(bool isMySide, string name)
    {
        if (isMySide)
        {
            if (leftPlayerNameText != null) leftPlayerNameText.text = name;
        }
        else
        {
            if (rightPlayerNameText != null) rightPlayerNameText.text = name;
        }
    }
    public void UpdateTurnIndicator(bool isMyTurn)
    {
        if (leftArrow != null) leftArrow.SetActive(isMyTurn);
        if (rightArrow != null) rightArrow.SetActive(!isMyTurn);
    }
}