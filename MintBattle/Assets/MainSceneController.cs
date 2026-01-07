using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainSceneController : MonoBehaviour
{
    public static MainSceneController Instance { get; private set; }

    [Header("Buttons")]
    public Button inventoryBtn;
    public Button arenaBtn;
    public Button buyCoinBtn;

    [Header("Buy Coin")]
    public GameObject buyCoinPopup;
    public BuyCoinPopupUI buyCoinPopupUI;

    public TeamSelectionUI teamSelectionUI;

    public MatchMakingUI matchMakingUI;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        inventoryBtn.onClick.AddListener(OnInventoryClicked);
        arenaBtn.onClick.AddListener(OnArenaClicked);
        buyCoinBtn.onClick.AddListener(OnBuyCoinClicked);
    }

    void OnInventoryClicked()
    {
        SceneManager.LoadScene("InventoryMenu");
    }

    void OnArenaClicked()
    {
        if (teamSelectionUI != null)
        {
            teamSelectionUI.OpenForBattle(OnTeamSelectedForBattle);
        }
        else
        {
            Debug.LogError("TeamSelectionUI not found in scene!");
        }
    }

    private void OnTeamSelectedForBattle(int teamIndex)
    {

        if (TeamManager.Instance.IsTeamEmpty(teamIndex))
        {
            Debug.LogWarning("No hero in team");

            // UIManager.Instance.ShowMessage("At least one hero needed!");

            return; 
        }
        TeamManager.Instance.SetSelectedTeamIndex(teamIndex);

        EnterBattle();
    }

    void OnBuyCoinClicked()
    {
        if (buyCoinPopup != null)
        {
            buyCoinPopup.SetActive(true);
            if (buyCoinPopupUI != null) buyCoinPopupUI.ResetCoinUI();
        }
    }

    public void CloseAllPopups()
    {
        buyCoinPopup.SetActive(false);
        if (TeamSelectionUI.Instance != null) TeamSelectionUI.Instance.Close();
    }

    public void EnterBattle()
    {
        matchMakingUI.StartMatchMaking();
    }
}