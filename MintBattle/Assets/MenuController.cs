using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button connectButton;
    public Button disconnectButton;
    public Button playButton;

    public string playSceneName = "MainScene";

    void Start()
    {
        connectButton.onClick.AddListener(() => NFTManager.Instance.ConnectWallet());
        disconnectButton.onClick.AddListener(() => NFTManager.Instance.DisconnectWallet());
        playButton.onClick.AddListener(PlayGame);

        CheckConnectionStatus();
    }

    void OnEnable()
    {
        NFTManager.OnWalletDataUpdated += CheckConnectionStatus;
    }

    void OnDisable()
    {
        NFTManager.OnWalletDataUpdated -= CheckConnectionStatus;
    }
    void CheckConnectionStatus()
    {
        if (NFTManager.Instance == null) return;

        bool isConnected = !string.IsNullOrEmpty(NFTManager.Instance.CachedAddress);

        UpdateUI(isConnected);
    }

    void UpdateUI(bool isConnected)
    {
        if (connectButton) connectButton.gameObject.SetActive(!isConnected);
        if (disconnectButton) disconnectButton.gameObject.SetActive(isConnected);

        if (playButton)
        {
            playButton.interactable = isConnected;
        }
    }

    void PlayGame()
    {
        SceneManager.LoadScene(playSceneName);
    }
}