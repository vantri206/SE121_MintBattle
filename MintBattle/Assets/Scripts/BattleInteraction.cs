using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class BattleInteraction : MonoBehaviour
{
    [Header("1. References UI")]
    public HeroDetailUI sharedHeroUI; 

    [Header("2. Buttons Control")]
    public Button btnCurrentInfo;     
    public Button btnSurrender;      

    [Header("3. Surrender Popup")]
    public GameObject surrenderPopup;
    public Button btnConfirmYes;     
    public Button btnConfirmNo;      

    private void Start()
    {
        if (btnCurrentInfo)
            btnCurrentInfo.onClick.AddListener(OnInfoClicked);

        if (btnSurrender)
            btnSurrender.onClick.AddListener(OnSurrenderClicked);

        if (btnConfirmYes)
            btnConfirmYes.onClick.AddListener(OnConfirmSurrender);

        if (btnConfirmNo)
            btnConfirmNo.onClick.AddListener(OnCancelSurrender);

        if (surrenderPopup) surrenderPopup.SetActive(false);
    }
    private void OnInfoClicked()
    {
        if (sharedHeroUI.heroDetailPanel.activeSelf)
        {
            sharedHeroUI.TurnOffHeroDetail();
            return;
        }

        if (BattleSystem.Instance != null)
        {
            BattleUnit currentUnit = BattleSystem.Instance.GetCurrentTurnUnit();

            if (currentUnit != null && currentUnit.SourceHero != null)
            {
                sharedHeroUI.ShowHeroInBattle(currentUnit.SourceHero);
            }
            else
            {
                Debug.LogWarning("No current unit turn!");
            }
        }
    }
    private void OnSurrenderClicked()
    {
       if (surrenderPopup) surrenderPopup.SetActive(true);
    }

    private void OnCancelSurrender()
    {
        if (surrenderPopup) surrenderPopup.SetActive(false);
    }

    private void OnConfirmSurrender()
    {
        string myWallet = PlayerProfile.Instance.WalletAddress;

        if (BattleSystem.Instance != null)
        {
            Debug.Log($"Player {myWallet} Surrender");
            BattleSystem.Instance.RPC_PlayerSurrender(myWallet);
        }
        if (surrenderPopup) surrenderPopup.SetActive(false);
        if (btnSurrender) btnSurrender.interactable = false;
    }
}