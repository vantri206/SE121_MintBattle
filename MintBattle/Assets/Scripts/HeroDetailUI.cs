using Mono.Cecil;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class HeroDetailUI : MonoBehaviour
{
    public GameObject heroDetailPanel;
    private Hero hero;
    private List<string> heroSkills = new List<string>();

    private GameObject Info;
    private GameObject Stats;
    private GameObject SkillSlot1;
    private GameObject SkillSlot2;

    [SerializeField] private TMP_Text indexText; 
    [SerializeField] private Button btnNext;          
    [SerializeField] private Button btnPrev;

    [Header("Team Logic")]
    [SerializeField] private Button btnAddToTeam;       
    [SerializeField] private GameObject teamInfoGroup;  
    [SerializeField] private TMP_Text teamNameText;     
    [SerializeField] private Button btnRemoveFromTeam;
    public InventoryManager inventoryManager;

    public static Action onNextClick;
    public static Action onPrevClick;
    private void OnEnable()
    {
        HeroCardUI.OnHeroCardClicked += HandleHeroCardClicked;
    }

    private void OnDisable()
    {
        HeroCardUI.OnHeroCardClicked -= HandleHeroCardClicked;
    }
    private void Awake()
    {
        Info = transform.Find("HeroDetailPanel/CenterPanelsHero/Info").gameObject;
        Stats = transform.Find("HeroDetailPanel/LeftPanelStats/Stats").gameObject;
        SkillSlot1 = transform.Find("HeroDetailPanel/RightPanelSkills/SkillSlot1").gameObject;
        SkillSlot2 = transform.Find("HeroDetailPanel/RightPanelSkills/SkillSlot2").gameObject;
        heroDetailPanel = transform.Find("HeroDetailPanel").gameObject;

        heroDetailPanel.SetActive(false);

        btnNext.onClick.AddListener(() => onNextClick?.Invoke());
        btnPrev.onClick.AddListener(() => onPrevClick?.Invoke());

        if (btnAddToTeam)
            btnAddToTeam.onClick.AddListener(OnAddToTeamClicked);

        if (btnRemoveFromTeam)
            btnRemoveFromTeam.onClick.AddListener(OnRemoveFromTeamClicked);
    }
    public void ActiveHeroDetail()
    {
        heroDetailPanel.SetActive(true);
        if (hero != null)
        {
            heroSkills.Clear();
            foreach (string skill in hero.ClassData.Skills)
                heroSkills.Add(skill);
        }

        LoadInfo();
        LoadStats();
        LoadSkill();
        CheckTeamStatus();
    }
    private void CheckTeamStatus()
    {
        if (hero == null) return;

        string heroIdString = hero.Id.ToString();
        int teamIndex = TeamManager.Instance.GetTeamIndexOfHero(heroIdString);

        if (teamIndex != -1)
        {
            if (btnAddToTeam) btnAddToTeam.gameObject.SetActive(false); 

            if (teamInfoGroup)
            {
                teamInfoGroup.SetActive(true); 
                if (teamNameText) teamNameText.text = "CURRENT TEAM: TEAM " + (teamIndex + 1);
            }

            if (btnRemoveFromTeam) btnRemoveFromTeam.gameObject.SetActive(true); 
        }
        else
        {
            if (btnAddToTeam) btnAddToTeam.gameObject.SetActive(true);

            if (teamInfoGroup)
            {
                teamInfoGroup.SetActive(true);
                if (teamNameText) teamNameText.text = "CURRENT TEAM: NONE";
            }

            if (btnRemoveFromTeam) btnRemoveFromTeam.gameObject.SetActive(false); 
        }
    }

    private void OnAddToTeamClicked()
    {
        TeamSelectionUI.Instance.OpenSelection(hero.Id.ToString(), () =>
        {
            CheckTeamStatus();
        });
    }
    private void OnRemoveFromTeamClicked()
    {
        TeamManager.Instance.RemoveHeroFromAllTeams(hero.Id.ToString());
        CheckTeamStatus(); 
    }
    private void LoadInfo()
    {
        Info.transform.Find("IdName/Name").GetComponent<Text>().text = hero.ClassData.Name;
        Info.transform.Find("IdName/Id").GetComponent<Text>().text = "#000" + hero.Id;
        Info.transform.Find("Level").GetComponent<Text>().text = "LV." + hero.Level.ToString();
        Info.transform.Find("HeroAvatar").GetComponent<Animator>().runtimeAnimatorController = hero.ClassData.animator;
    }
    private void LoadStats()
    {
        Stats.transform.Find("Health/Stats").GetComponent<Text>().text = hero.HP.ToString();
        Stats.transform.Find("Attack/Stats").GetComponent<Text>().text = hero.Attack.ToString();
        Stats.transform.Find("Defense/Stats").GetComponent<Text>().text = hero.Defense.ToString();
        Stats.transform.Find("Speed/Stats").GetComponent<Text>().text = hero.Speed.ToString();
    }
    private void LoadSkill()
    {
        SkillSlot1.SetActive(false);
        SkillSlot2.SetActive(false);

        if (heroSkills.Count > 0 && heroSkills[0] != null)
        {
            SkillSlot1.SetActive(true);
            LoadSkillSlot(SkillSlot1, heroSkills[0]);
        }

        if (heroSkills.Count > 1 && heroSkills[1] != null)
        {
            SkillSlot2.SetActive(true);
            LoadSkillSlot(SkillSlot2, heroSkills[1]);
        }
    }

    private void LoadSkillSlot(GameObject SkillSlot, string heroSkill)
    {
        SkillData skillData = SkillDatabase.Instance.GetSkillById(heroSkill);
        if (skillData != null)
        {
            SkillSlot.transform.Find("Name").GetComponent<Text>().text = skillData.Name;
            SkillSlot.transform.Find("Description").GetComponent<Text>().text = skillData.Description;
            SkillSlot.transform.Find("Icon").GetComponent<Image>().sprite = skillData.Image;
            SkillSlot.transform.Find("Cooldown").GetComponent<Text>().text = "Cooldown: " + skillData.Cooldown.ToString() + " turn";
        }
    }
    public void ShowHeroData(Hero heroData, int index)
    {
        this.hero = heroData;
        UpdateNavigationState(index);
        ActiveHeroDetail();

        CheckTeamStatus(); 
    }

    public void HandleHeroCardClicked(HeroCardUI heroCard, int index)
    {
        ShowHeroData(heroCard.GetHero(), index);

        inventoryManager.OnCardClicked(index);
    }
    public void UpdateNavigationState(int index)
    {
        int totalHeroes = PlayerInventory.Instance.GetAllHeroes().Count;
        indexText.text = (index + 1).ToString() + "/" + totalHeroes.ToString();

        if (index <= 0)
        {
            btnPrev.gameObject.SetActive(false);
        }
        else
        {
            btnPrev.gameObject.SetActive(true);
        }

        if (index >= totalHeroes - 1)
        {
            btnNext.gameObject.SetActive(false);
        }
        else
        {
            btnNext.gameObject.SetActive(true);
        }
    }
    public void TurnOffHeroDetail()
    {
        heroDetailPanel.SetActive(false);
    }
}
