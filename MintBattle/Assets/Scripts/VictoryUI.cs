using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryUI : MonoBehaviour
{
    public static VictoryUI Instance;

    [Header("UI References")]
    public GameObject panel;
    public Button btnQuit; 
    public Button btnNext; 

    private int tokenId;
    private string itemId;
    private string signature;

    private int amount = 1;
    private string uid;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        panel.SetActive(false);

        if (btnQuit) btnQuit.onClick.AddListener(OnQuitClicked);
        if (btnNext) btnNext.onClick.AddListener(OnNextClicked);
    }

    public void SetupVictory(int tokenId, int amount, string uid, string itemId, string signature)
    {
        this.tokenId = tokenId;
        this.itemId = itemId;
        this.signature = signature;

        this.amount = amount;
        this.uid = uid;

        panel.transform.localScale = Vector3.zero;
        panel.SetActive(true);
        panel.transform.DOScale(Vector3.one, 1.0f).SetEase(Ease.OutBack);
    }
    private void OnNextClicked()
    {
        panel.SetActive(false);

        if (ChestRewardManager.Instance != null)
        {
            ChestRewardManager.Instance.SetupReward(
                tokenId,
                amount,      
                uid,         
                itemId,
                signature,
                ReturnToMainScene
            );
        }
        else
        {
            Debug.LogError("ChestRewardManager not found!");
            ReturnToMainScene();
        }
    }
    private void OnQuitClicked()
    {
        Debug.Log("Lose -> Quit to Menu");
        ReturnToMainScene();
    }
    private void ReturnToMainScene()
    {
        Debug.Log("VictoryUI: Requesting return to menu...");

        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.DisconnectAndReturnToMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
        }
    }
}