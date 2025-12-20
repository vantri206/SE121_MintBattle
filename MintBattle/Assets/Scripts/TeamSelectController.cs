using UnityEngine;
using UnityEngine.UI;

public class TeamSelectionUI : MonoBehaviour
{
    public static TeamSelectionUI Instance;

    [Header("Settings")]
    public int teamCount = 3;    
    public int slotsPerTeam = 2;  
    [Header("UI Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform mainContainer; 

    [Header("Prefabs")]
    [SerializeField] private GameObject heroTeamPrefab; 
    [SerializeField] private GameObject teamSlotPrefab; 

    private string pendingHeroId;
    private System.Action onInventoryCompleteCallback;
    private System.Action<int> onBattleTeamSelected;
    private TeamSelectionMode currentMode;
    public enum TeamSelectionMode
    {
        Inventory,
        Battle
    }
    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void OpenSelection(string heroId, System.Action onComplete)
    {
        currentMode = TeamSelectionMode.Inventory;
        this.pendingHeroId = heroId;
        this.onInventoryCompleteCallback = onComplete;

        panel.SetActive(true);
        RenderTeams();
    }
    public void OpenForBattle(System.Action<int> onTeamSelected)
    {
        currentMode = TeamSelectionMode.Battle;
        this.onBattleTeamSelected = onTeamSelected;

        panel.SetActive(true);
        RenderTeams();
    }
    public void RenderTeams()
    {
        foreach (Transform child in mainContainer) Destroy(child.gameObject);
        for (int i = 0; i < teamCount; i++)
        {

            GameObject teamObj = Instantiate(heroTeamPrefab, mainContainer);
            HeroTeamUI teamUI = teamObj.GetComponent<HeroTeamUI>();

            teamUI.Setup(i, slotsPerTeam, teamSlotPrefab, currentMode, OnSlotClicked, OnTeamSelectButtonClicked);
        }
    }
    private void OnSlotClicked(int teamIdx, int slotIdx)
    {
        if (currentMode == TeamSelectionMode.Inventory)
        {
            TeamManager.Instance.AddHeroToTeam(teamIdx, slotIdx, pendingHeroId);
            onInventoryCompleteCallback?.Invoke();
            RenderTeams();
        }
    }
    private void OnTeamSelectButtonClicked(int teamIdx)
    {
        if (currentMode == TeamSelectionMode.Battle)
        {
            onBattleTeamSelected?.Invoke(teamIdx);
            Close();
        }
    }
    public void Close() => panel.SetActive(false);
}