using UnityEngine;
using TMPro;
using System.Collections;

public class MainGameUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI addressText;
    public TextMeshProUGUI ethText;
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI heroCountText;

    private void OnEnable()
    {
        //Debug.Log("MainGameUI: OnEnable");
        NFTManager.OnWalletDataUpdated += UpdateDisplay;
        StartCoroutine(SafeUpdateRoutine());
    }

    private void OnDisable()
    {
        //Debug.Log("MainGameUI: OnDisable");
        NFTManager.OnWalletDataUpdated -= UpdateDisplay;
    }

    IEnumerator SafeUpdateRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (NFTManager.Instance == null) return;

        if (addressText != null)
        {
            string addr = NFTManager.Instance.CachedAddress;
            if (string.IsNullOrEmpty(addr)) addressText.text = "0x...";
            else if (addr.Length >= 10) addressText.text = $"{addr.Substring(0, 6)}...{addr.Substring(addr.Length - 4)}";
            else addressText.text = addr;
        }

        if (ethText != null) ethText.text = NFTManager.Instance.CachedEth ?? "0";
        if (coinText != null) coinText.text = NFTManager.Instance.CachedCoin ?? "0";
        if (heroCountText != null) heroCountText.text = NFTManager.Instance.CachedHeroes ?? "0";
    }
}