using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;

public class BuyCoinPopupUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField coinAmountInput;
    public Button buyButton;
    public Button closeButton;

    [Header("Optional Feedback")]
    public Text statusText;

    private MainSceneController mainSceneController;

    void Start()
    {
        buyButton.onClick.AddListener(OnBuyClicked);
        closeButton.onClick.AddListener(OnCloseClicked);

        mainSceneController = FindAnyObjectByType<MainSceneController>();
    }
    private void OnCloseClicked()
    {
        if (mainSceneController != null)
        {
            mainSceneController.CloseAllPopups();
        }
        else 
        {
            gameObject.SetActive(false);
        }
    }
    public void ResetCoinUI()
    {
        coinAmountInput.text = "1000";
        if (statusText) statusText.text = "";
        if (buyButton) buyButton.interactable = true;
    }
    private async void OnBuyClicked()
    {
        if (string.IsNullOrEmpty(coinAmountInput.text) ||
            !decimal.TryParse(coinAmountInput.text, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            Debug.LogError("Invalid coin amount");
            return;
        }

        var nftManager = FindAnyObjectByType<NFTManager>();

        if (nftManager != null)
        {
            buyButton.interactable = false;
            SetStatus("Processing transaction...", false);

            await nftManager.MintCoinNFT(coinAmountInput.text);

            SetStatus("Success! Check your wallet.", false);
            Invoke("OnCloseClicked", 2.0f);
        }
        else
        {
            Debug.Log("No wallet manager in game");
        }
    }
    private void SetStatus(string msg, bool isError)
    {
        if (statusText)
        {
            statusText.text = msg;
            statusText.color = isError ? Color.red : Color.green;
        }
        Debug.Log(msg);
    }
}