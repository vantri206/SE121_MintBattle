using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroDetailUI : MonoBehaviour
{
    public GameObject heroDetailPanel;
    private Hero hero;
    private bool isBattleMode = false;

    private GameObject Info;
    private GameObject Stats;
    private GameObject SkillSlot1;
    private GameObject SkillSlot2;
    private GameObject PassiveSlot;

    [SerializeField] private TMP_Text indexText;
    [SerializeField] private Button btnNext;
    [SerializeField] private Button btnPrev;

    [Header("Team Logic")]
    [SerializeField] private Button btnAddToTeam;
    [SerializeField] private GameObject teamInfoGroup;
    [SerializeField] private TMP_Text teamNameText;
    [SerializeField] private Button btnRemoveFromTeam;
    public InventoryManager inventoryManager;

    [Header("Upgrade Logic")]
    [SerializeField] private Button btnLevelUp;
    [SerializeField] private TMP_Text levelUpCostText;

    [Header("Equipment Logic")]
    [SerializeField] private ItemCardUI currentWeaponSlot;
    [SerializeField] private Button btnWeaponSlot;
    [SerializeField] private GameObject emptySlotVisual;

    [Header("Equipment Selection Popup")]
    [SerializeField] private GameObject equipmentSelectPanel;
    [SerializeField] private Transform equipmentContent;
    [SerializeField] private GameObject itemCardPrefab;
    [SerializeField] private PointerClickHandler weaponSlotClicker;

    [Header("Navigation")]
    [SerializeField] private Button btnClose;

    public static HeroDetailUI Instance;

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
        Instance = this;

        Info = transform.Find("HeroDetailPanel/CenterPanelsHero/Info").gameObject;
        Stats = transform.Find("HeroDetailPanel/LeftPanelStats/Stats").gameObject;
        SkillSlot1 = transform.Find("HeroDetailPanel/RightPanelSkills/SkillSlot1").gameObject;
        SkillSlot2 = transform.Find("HeroDetailPanel/RightPanelSkills/SkillSlot2").gameObject;
        PassiveSlot = transform.Find("HeroDetailPanel/RightPanelSkills/PassiveSkill").gameObject;
        heroDetailPanel = transform.Find("HeroDetailPanel").gameObject;

        heroDetailPanel.SetActive(false);

        btnNext.onClick.AddListener(() => onNextClick?.Invoke());
        btnPrev.onClick.AddListener(() => onPrevClick?.Invoke());

        if (btnAddToTeam)
            btnAddToTeam.onClick.AddListener(OnAddToTeamClicked);

        if (btnRemoveFromTeam)
            btnRemoveFromTeam.onClick.AddListener(OnRemoveFromTeamClicked);

        if (btnLevelUp)
            btnLevelUp.onClick.AddListener(OnLevelUpClicked);

        if (btnWeaponSlot)
            btnWeaponSlot.onClick.AddListener(OpenEquipmentSelection);

        if (weaponSlotClicker != null)
        {
            weaponSlotClicker.OnLeftClickEvent += OpenEquipmentSelection;
            weaponSlotClicker.OnRightClickEvent += OnUnequipWeapon;
        }

        if (btnClose)
        {
            btnClose.onClick.AddListener(OnCloseClicked);
        }
    }
    private void OnCloseClicked()
    {
        TurnOffHeroDetail();
    }

    public void TurnOffHeroDetail()
    {
        heroDetailPanel.SetActive(false);
        isBattleMode = false;
        CloseEquipmentSelection();
    }
    public void ShowHeroData(Hero heroData, int index)
    {
        this.hero = heroData;
        isBattleMode = false; 

        UpdateNavigationState(index);
        ActiveHeroDetail();
        CheckTeamStatus();
    }
    public void ShowHeroInBattle(Hero heroData)
    {
        this.hero = heroData;
        isBattleMode = true;

        ActiveHeroDetail();
    }

    public void ActiveHeroDetail()
    {
        heroDetailPanel.SetActive(true);

        LoadInfo();
        LoadStats();
        LoadSkill();
        LoadEquipment();


        if (!isBattleMode)
        {
            UpdateUpgradeUI();
        }

        UpdateUIByMode();
    }

    private void UpdateUIByMode()
    {
        bool isLobby = !isBattleMode;

        if (btnLevelUp) btnLevelUp.gameObject.SetActive(isLobby);
        if (levelUpCostText) levelUpCostText.transform.parent.gameObject.SetActive(isLobby); 


        if (teamInfoGroup) teamInfoGroup.SetActive(isLobby);

        if (isBattleMode)
        {
            if (btnAddToTeam) btnAddToTeam.gameObject.SetActive(false);
            if (btnRemoveFromTeam) btnRemoveFromTeam.gameObject.SetActive(false);

            if (btnNext) btnNext.gameObject.SetActive(false);
            if (btnPrev) btnPrev.gameObject.SetActive(false);
            if (indexText) indexText.gameObject.SetActive(false);
        }

        if (btnWeaponSlot)
        {
            btnWeaponSlot.interactable = isLobby;
        }

        if (weaponSlotClicker != null)
        {
            weaponSlotClicker.enabled = isLobby;
        }
    }
    private void OnAddToTeamClicked()
    {
        if (isBattleMode) return;
        TeamSelectionUI.Instance.OpenSelection(hero.Id.ToString(), () =>
        {
            CheckTeamStatus();
        });
    }
    private void OnRemoveFromTeamClicked()
    {
        if (isBattleMode) return;
        TeamManager.Instance.RemoveHeroFromAllTeams(hero.Id.ToString());
        CheckTeamStatus();
    }

    private async void OnLevelUpClicked()
    {
        if (isBattleMode) return;

        if (hero == null || btnLevelUp == null) return;

        if (HeroUpgradeManager.Instance != null)
        {
            btnLevelUp.interactable = false;
            try
            {
                await HeroUpgradeManager.Instance.OnUpgradeClick(hero);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during upgrade: {ex.Message}");
            }
            finally
            {
                btnLevelUp.interactable = true;
            }
        }
    }

    private void UpdateUpgradeUI()
    {
        if (hero == null || btnLevelUp == null) return;
        int cost = hero.Level * 10;

        if (levelUpCostText != null)
        {
            levelUpCostText.text = $"{cost}";
        }
    }

    public void RefreshCurrentHeroUI()
    {
        if (hero != null && heroDetailPanel.activeSelf)
        {
            LoadInfo();
            LoadStats();
            LoadEquipment();

            if (!isBattleMode)
            {
                UpdateUpgradeUI();
            }
        }
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

    private void LoadInfo()
    {
        Info.transform.Find("IdName/Name").GetComponent<Text>().text = hero.ClassData.Id;
        Info.transform.Find("IdName/Id").GetComponent<Text>().text = $"#{int.Parse(hero.Id):D4}";
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
        PassiveSlot.SetActive(false);

        if (hero == null || hero.Skills == null) return;

        int activeSkillCount = 0;

        foreach (var skillData in hero.Skills)
        {
            if (skillData.type == SkillType.Active)
            {
                if (activeSkillCount == 0)
                {
                    SkillSlot1.SetActive(true);
                    LoadSkillSlot(SkillSlot1, skillData);
                }
                else if (activeSkillCount == 1)
                {
                    SkillSlot2.SetActive(true);
                    LoadSkillSlot(SkillSlot2, skillData);
                }
                activeSkillCount++;
            }
            else if (skillData.type == SkillType.Passive)
            {
                PassiveSlot.SetActive(true);
                LoadSkillSlot(PassiveSlot, skillData);
            }
        }
    }

    private void LoadSkillSlot(GameObject slotObj, SkillData data)
    {
        if (data == null) return;

        var nameTxt = slotObj.transform.Find("Name").GetComponent<Text>();
        var descTxt = slotObj.transform.Find("Description").GetComponent<Text>();
        var iconImg = slotObj.transform.Find("Icon").GetComponent<Image>();

        Transform cdObj = slotObj.transform.Find("Cooldown");
        Text cdTxt = cdObj != null ? cdObj.GetComponent<Text>() : null;

        nameTxt.text = data.Name;
        descTxt.text = data.Description;
        iconImg.sprite = data.Image;


        if (cdTxt != null)
        {
            if (data.type == SkillType.Active)
            {
                cdTxt.text = "Cooldown: " + data.Cooldown.ToString() + " turn";
                cdTxt.gameObject.SetActive(true);
            }
            else
            {
                cdTxt.text = "";
            }
        }
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

        if (index <= 0) btnPrev.gameObject.SetActive(false);
        else btnPrev.gameObject.SetActive(true);

        if (index >= totalHeroes - 1) btnNext.gameObject.SetActive(false);
        else btnNext.gameObject.SetActive(true);
    }

    private void LoadEquipment()
    {
        if (hero.equippedWeapon != null && hero.equippedWeapon.data != null)
        {
            currentWeaponSlot.gameObject.SetActive(true);
            emptySlotVisual.SetActive(false);
            currentWeaponSlot.Setup(hero.equippedWeapon.data, true);
        }
        else
        {
            currentWeaponSlot.gameObject.SetActive(false);
            emptySlotVisual.SetActive(true);
        }
    }

    private void OnUnequipWeapon()
    {
        if (isBattleMode) return;
        if (hero == null || hero.equippedWeapon == null) return;
        hero.Unequip();

        PlayerInventory.Instance.SaveLoadout();

        RefreshCurrentHeroUI();
    }
    private void OpenEquipmentSelection()
    {
        if (isBattleMode) return;

        equipmentSelectPanel.SetActive(true);

        foreach (Transform child in equipmentContent) Destroy(child.gameObject);

        List<Item> allItems = PlayerInventory.Instance.GetAllItems();

        if (allItems.Count == 0) return;

        foreach (var item in allItems)
        {
            if (hero.equippedWeapon == item) continue;

            GameObject cardObj = Instantiate(itemCardPrefab, equipmentContent);
            ItemCardUI cardUI = cardObj.GetComponent<ItemCardUI>();

            bool canEquip = item.CanEquip();

            cardUI.Setup(item.data, canEquip);

            if (canEquip)
            {
                cardUI.OnClickAction = (data) =>
                {
                    OnEquipItemClicked(item);
                };
            }
        }
    }

    public void CloseEquipmentSelection()
    {
        if (equipmentSelectPanel) equipmentSelectPanel.SetActive(false);
    }

    private void OnEquipItemClicked(Item newItem)
    {
        if (hero == null) return;

        if (!newItem.CanEquip())
        {
            OpenEquipmentSelection();
            return;
        }

        hero.Equip(newItem);

        PlayerInventory.Instance.SaveLoadout();

        RefreshCurrentHeroUI();
        CloseEquipmentSelection(); 
    }

    private void OnDestroy()
    {
        if (weaponSlotClicker != null)
        {
            weaponSlotClicker.OnLeftClickEvent -= OpenEquipmentSelection;
            weaponSlotClicker.OnRightClickEvent -= OnUnequipWeapon;
        }
    }
}