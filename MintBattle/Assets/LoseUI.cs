using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Fusion;

public class LoseUI : MonoBehaviour
{
    public static LoseUI Instance;

    [Header("UI References")]
    public GameObject panel;
    public Button btnQuit;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        panel.SetActive(false);

        if (btnQuit) btnQuit.onClick.AddListener(OnQuitClicked);
    }

    public void Show()
    {
        panel.transform.localScale = Vector3.zero;
        panel.SetActive(true);
        panel.transform.DOScale(Vector3.one, 1.0f).SetEase(Ease.OutBack);
    }

    private void OnQuitClicked()
    {
        Debug.Log("Lose -> Quit to Menu");
        ReturnToMainScene();
    }
    private void ReturnToMainScene()
    {
        Debug.Log("LoseUI: Requesting return to menu...");

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