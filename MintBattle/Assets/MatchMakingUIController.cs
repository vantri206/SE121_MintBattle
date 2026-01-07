using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MatchMakingUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panelMatchmaking;
    public TMP_Text statusText;
    public Button cancelButton;

    private void Start()
    {
        if (panelMatchmaking != null) panelMatchmaking.SetActive(false);
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelMatchMaking);
    }

    private void OnEnable()
    {
        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.OnStatusChanged += UpdateStatusText;
            MatchmakingManager.Instance.OnMatchmakingStateChanged += TogglePanel;
        }
    }

    private void OnDisable()
    {
        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.OnStatusChanged -= UpdateStatusText;
            MatchmakingManager.Instance.OnMatchmakingStateChanged -= TogglePanel;
        }
    }

    public void StartMatchMaking()
    {
        if (MatchmakingManager.Instance == null)
        {
            MatchmakingManager.Instance = FindFirstObjectByType<MatchmakingManager>();
        }

        if (MatchmakingManager.Instance != null)
        {
            TogglePanel(true);
            UpdateStatusText("Initializing...");
            MatchmakingManager.Instance.StartMatchmaking();
        }
        else
        {
            Debug.LogError("FATAL: MatchmakingManager missing! Please restart the game.");
            if (statusText != null) statusText.text = "Error: Manager Missing";
        }
    }

    public void CancelMatchMaking()
    {
        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.CancelMatchmaking();
            TogglePanel(false);
        }
    }

    void UpdateStatusText(string msg) { if (statusText != null) statusText.text = msg; }

    void TogglePanel(bool isActive)
    {
        if (panelMatchmaking != null)
        {
            panelMatchmaking.SetActive(isActive);
            if (cancelButton != null) cancelButton.interactable = isActive;
        }
    }
}