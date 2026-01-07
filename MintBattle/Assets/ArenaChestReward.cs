using DG.Tweening;
using Reown.AppKit.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class ChestRewardManager : MonoBehaviour
{
    public static ChestRewardManager Instance { get; private set; }

    [Header("Blockchain Config")]
    private string itemContractAddress = "0x1a4dF372D0090F27B206DadF89edD1Be3cC46591";
    private const string MINT_ABI = @"[{""inputs"":[{""internalType"":""uint256"",""name"":""tokenId"",""type"":""uint256""},{""internalType"":""uint256"",""name"":""amount"",""type"":""uint256""},{""internalType"":""bytes"",""name"":""signature"",""type"":""bytes""},{""internalType"":""uint256"",""name"":""_uid"",""type"":""uint256""}],""name"":""mintWithSignature"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""}]";

    [Header("UI & Animation References")]
    public Canvas canvas;
    public CanvasGroup canvasGroup;
    public GameObject blocker;
    public GameObject chest;
    public GameObject claimButton;
    public Animator chestAnimator;
    public GameObject itemCardPrefab;
    public GameObject itemContent;
    public ParticleSystem openEffect;

    private int tokenId;
    private string signature;
    private bool isProcessing = false;
    private int amount = 1; 
    private BigInteger uid;

    private Action onChestClosedCallback;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (chestAnimator == null && chest != null)
            chestAnimator = chest.GetComponent<Animator>();

        CloseReward();
    }
    public void Update()
    {

    }
    public void SetupReward(int tokenId, int amount, string uidStr, string stringId, string signature, Action onClosed = null)
    {
        this.tokenId = tokenId;
        this.amount = amount;
        this.uid = BigInteger.Parse(uidStr);
        this.signature = signature.StartsWith("0x") ? signature : "0x" + signature;

        this.onChestClosedCallback = onClosed;
        
        isProcessing = false;
        canvas.enabled = true;
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;

        itemContent.SetActive(false);
        chest.SetActive(true);
        chestAnimator.SetBool("isOpened", false);
        claimButton.SetActive(true);
        blocker.SetActive(true);
        LoadItem(stringId);
    }

    private void LoadItem(string stringId)
    {
        foreach (Transform child in itemContent.transform)
        {
            Destroy(child.gameObject);
        }

        ItemData item = Resources.Load<ItemData>($"Data/Items/{stringId}");

        if (item != null)
        {
            GameObject itemCard = Instantiate(itemCardPrefab, itemContent.transform);

            ItemCardUI cardUI = itemCard.GetComponent<ItemCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(item, true);
            }

            Debug.Log($"[Chest] Loaded item: {item.Id} from SO");
        }
        else
        {
            Debug.LogError($"[Chest] Cant find item: {stringId}");
        }
    }
    public async void OnChestClick()
    {
        Debug.Log("Chest Click");
        if (isProcessing) return;
        isProcessing = true;

        await CallMintWithSignature();

        chestAnimator.SetBool("isOpened", true);

        StartCoroutine(RevealItemRoutine());
    }
    private IEnumerator RevealItemRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        if (openEffect != null) openEffect.Play();

        itemContent.SetActive(true);
        itemContent.transform.localScale = Vector3.zero;

        RectTransform rect = itemContent.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, -100);

        rect.localRotation = UnityEngine.Quaternion.Euler(0, 90, 0);

        Sequence seq = DOTween.Sequence();

        seq.Append(rect.DOAnchorPos(new Vector2(0, 120), 0.6f).SetEase(Ease.OutBack));
        seq.Join(rect.DOScale(1f, 0.6f).SetEase(Ease.OutBack));

        seq.Insert(0.2f, rect.DORotate(Vector3.zero, 0.5f).SetEase(Ease.OutBack));

        yield return seq.WaitForCompletion();

        Invoke(nameof(CloseReward), 5f); 
    }
    public void CloseReward()
    {
        canvas.enabled = false;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        chest.SetActive(false);
        claimButton.SetActive(false);
        blocker.SetActive(false);
        itemContent.SetActive(false);

        if (openEffect != null)
        {
            openEffect.Stop();
            openEffect.Clear();
        }

        isProcessing = false;

        if (onChestClosedCallback != null)
        {
            Debug.Log("Chest Closed -> Back to Menu");
            onChestClosedCallback.Invoke();
            onChestClosedCallback = null; 
        }
    }

    private async Task CallMintWithSignature()
    {
        if (AppKit.Account == null) return;

        try
        {
            if (AppKit.Account.ChainId.ToString() != "eip155:84532")
            {
                if (AppKit.NetworkController.Chains.TryGetValue("eip155:84532", out var baseSepolia))
                {
                    await AppKit.NetworkController.ChangeActiveChainAsync(baseSepolia);
                }
            }

            string currentWallet = AppKit.Account.Address.ToLower();

            Debug.Log($"[Client Mint] Wallet: {currentWallet}");
            Debug.Log($"[Client Mint] TokenID: {tokenId} | Amount: {amount} | UID: {uid}");
            Debug.Log($"[Client Mint] Signature: {signature}");

            string txHash = await AppKit.Evm.WriteContractAsync(
                itemContractAddress,
                MINT_ABI,
                "mintWithSignature",
                (BigInteger)0,      
                (BigInteger)500000,  
                (BigInteger)tokenId,
                (BigInteger)amount,  
                signature,          
                (BigInteger)uid      
            );

            Debug.Log("Mint Item TX Sent: " + txHash);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Minting failed: " + ex.Message);
        }
    }
}