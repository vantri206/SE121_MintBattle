using NBitcoin;
using Nethereum.Model;
using Nethereum.Util;
using Reown.AppKit.Unity;
using Reown.Sign.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using Thirdweb;
using Thirdweb.Api;
using Thirdweb.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Nethereum.Contracts.Standards.ERC721;

public class NFTManager : MonoBehaviour
{
    public static NFTManager Instance { get; private set; }

    public string CachedAddress { get; private set; } = "";
    public string CachedEth { get; private set; } = "0";
    public string CachedCoin { get; private set; } = "0";
    public string CachedHeroes { get; private set; } = "0";
    public List<Hero> CachedHeroList { get; private set; } = new List<Hero>();

    private bool isLoading = false;
    public static event Action OnWalletDataUpdated;

    [Header("Contract Configuration")]
    public string tokenContractAddress = "0x62AEC02A9773FBFa4A02455F05b850E542A9e5Db";
    public string NFTContractAddress = "0x512763F3721cf5FDB5d6a86b50281EFC08ffEb6e";
    public int chainId = 84532;

    [Header("UI References")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI accountText;
    public TextMeshProUGUI ethBalanceText;
    public TextMeshProUGUI tokenBalanceText;
    public TextMeshProUGUI nftBalanceText;
    public TextMeshProUGUI coinAmountText;

    [Header("Buttons")]
    public Button connectButton;
    public Button disconnectButton;
    public Button mintNFTButton;
    public Button buyCoinNFTButton;

    private const string TOKEN_ABI = @"[{""constant"":true,""inputs"":[{""name"":""account"",""type"":""address""}],""name"":""balanceOf"",""outputs"":[{""name"":""balance"",""type"":""uint256""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":true,""inputs"":[],""name"":""decimals"",""outputs"":[{""name"":""decimals"",""type"":""uint8""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""to"",""type"":""address""}],""name"":""mint"",""outputs"":[],""payable"":true,""stateMutability"":""payable"",""type"":""function""}]";
    private const string MINT_ABI = @"[{""inputs"":[{""internalType"":""address"",""name"":""to"",""type"":""address""}],""name"":""mint"",""outputs"":[],""stateMutability"":""payable"",""type"":""function""}]";
    private const string NFT_BALANCE_ABI = "[{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"}]";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private async void Start()
    {
        Debug.Log("Starting AppKit initialization...");
        try
        {
            await InitializeWallet();
            SetupUI();

            if (AppKit.IsInitialized)
            {
                if (AppKit.Account != null)
                {
                    Debug.Log("Found existing session. Reconnecting...");
                    OnAccountConnected(this, EventArgs.Empty);
                }
                else
                {
                    InitializeDisconnectedState();
                }
            }
            else
            {
                Debug.LogError("AppKit failed to initialize!");
                InitializeDisconnectedState();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CRITICAL ERROR in Start: {e.Message}");
            InitializeDisconnectedState();
        }
    }

    private async Task InitializeWallet()
    {
        var config = new AppKitConfig
        {
            projectId = "964f616b8d0f4370b9c2892038388eb7",
            metadata = new Reown.AppKit.Unity.Metadata(
                "Mint Battle",
                "Mint Battle NFT game for project 1",
                "https://vantri06.itch.io/mint-battle",
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSEeZ_JvUSmWFysFkplvBK1asUnVH9io63lZQ&s"
            ),
            supportedChains = new[]
            {
                new Chain(ChainConstants.Namespaces.Evm,
                chainReference: "84532",
                name: "Base Sepolia",
                nativeCurrency: new Currency("ETH", "ETH", 18),
                blockExplorer: new BlockExplorer("BaseScan", "https://sepolia.basescan.org/"),
                rpcUrl: "https://84532.rpc.thirdweb.com",
                isTestnet: true,
                imageUrl: "https://avatars.githubusercontent.com/u/108554348?s=200&v=4")
            },
            enableAnalytics = false
        };

        await AppKit.InitializeAsync(config);
        AppKit.AccountConnected += OnAccountConnected;
        AppKit.AccountDisconnected += OnAccountDisconnected;

        Debug.Log("Mint Battle wallet initialized successfully!");
    }

    private void SetupUI()
    {
        if (connectButton != null) connectButton.onClick.AddListener(ConnectWallet);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(DisconnectWallet);
        if (mintNFTButton != null) mintNFTButton.onClick.AddListener(ClaimNFT);
    }

    private void InitializeDisconnectedState()
    {
        UpdateStatus("Login wallet to play game");
        UpdateAccount("", true);
        UpdateEthBalance("0 ETH", true);
        UpdateTokenBalance("0 Coin", true);
        UpdateNFTBalance("0 Heroes", true);
        CachedHeroList.Clear();
        OnWalletDataUpdated?.Invoke();

        SetButtonStates(connectEnabled: true, disconnectEnabled: false, mintEnabled: false);
    }

    public void ConnectWallet()
    {
        if (!AppKit.IsInitialized) return;
        UpdateStatus("Opening wallet selection...");
        AppKit.OpenModal();
    }

    public async void DisconnectWallet()
    {
        UpdateStatus("Disconnecting...");
        try
        {
            await AppKit.DisconnectAsync();
        }
        catch (System.Exception) { }
        finally
        {
            InitializeDisconnectedState();
        }
    }

    public void ClaimNFT()
    {
        _ = MintNFT();
    }

    public async Task CheckEthBalance(bool silent = false)
    {
        try
        {
            var account = AppKit.Account;
            if (account == null) return;
            BigInteger balance = await AppKit.Evm.GetBalanceAsync(account.Address);
            decimal eth = (decimal)balance / (decimal)BigInteger.Pow(10, 18);
            UpdateEthBalance($"{eth:F4} ETH", silent);
        }
        catch
        {
            UpdateEthBalance("Error", silent);
        }
    }

    private async Task CheckTokenBalance(bool silent = false)
    {
        try
        {
            var account = AppKit.Account;
            if (account == null) return;

            BigInteger rawBalance = await AppKit.Evm.ReadContractAsync<BigInteger>(
                tokenContractAddress,
                TOKEN_ABI,
                "balanceOf",
                new object[] { account.Address }
            );

            decimal readableBalance = (decimal)rawBalance / (decimal)BigInteger.Pow(10, 18);
            CachedCoin = readableBalance.ToString("0.##", CultureInfo.InvariantCulture);
            UpdateTokenBalance($"{CachedCoin} Coin", silent);
        }
        catch
        {
            UpdateTokenBalance("0 Coin", silent);
        }
    }

    public async Task CheckNFTBalance(bool silent = false)
    {
        try
        {
            var account = AppKit.Account;
            if (account == null) return;

            var balance = await AppKit.Evm.ReadContractAsync<BigInteger>(
                NFTContractAddress,
                NFT_BALANCE_ABI,
                "balanceOf",
                new object[] { account.Address }
            );

            UpdateNFTBalance($"{balance} Heroes", silent);

            if (ThirdwebManager.Instance != null)
            {
                var contract = await ThirdwebManager.Instance.GetContract(NFTContractAddress, chainId: 84532);
                var nftList = await contract.ERC721_GetOwnedNFTs(account.Address);

                CachedHeroList.Clear();
                foreach (var nft in nftList)
                {
                    Hero newHero = ConvertNftToHero(nft);
                    if (newHero != null)
                    {
                        CachedHeroList.Add(newHero);
                    }
                }
                if (!silent) OnWalletDataUpdated?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error CheckNFTBalance: {ex.Message}");
            UpdateNFTBalance("Error", silent);
        }
    }

    [System.Serializable]
    public class NftTrait
    {
        public string trait_type;
        public object value;
    }

    private Hero ConvertNftToHero(NFT nft)
    {
        try
        {
            string classId = "HR01";
            int level = 1;

            if (nft.Metadata.Attributes != null)
            {
                string json = JsonConvert.SerializeObject(nft.Metadata.Attributes);
                var traits = JsonConvert.DeserializeObject<List<NftTrait>>(json);

                if (traits != null)
                {
                    foreach (var trait in traits)
                    {
                        if (trait.trait_type == "Base ID" || trait.trait_type == "Class" || trait.trait_type == "heroType")
                            classId = trait.value.ToString();

                        if (trait.trait_type == "Level")
                            int.TryParse(trait.value.ToString(), out level);
                    }
                }
            }
            return new Hero(classId, level);
        }
        catch
        {
            return new Hero("HR01", 1);
        }
    }

    public async Task MintNFT()
    {
        try
        {
            var account = AppKit.Account;
            if (account == null) return;

            UpdateStatus("Minting NFT...");
            string txHash = await AppKit.Evm.WriteContractAsync(
                NFTContractAddress,
                MINT_ABI,
                "safeMint",
                account.Address
            );
            UpdateStatus($"NFT minted! TX: {txHash.Substring(0, 10)}...");
            await CheckNFTBalance();
            UpdateStatus("NFT successfully minted!");
        }
        catch (System.Exception e)
        {
            UpdateStatus($"Mint failed: {e.Message}");
        }
    }

    private async void OnAccountConnected(object sender, System.EventArgs e)
    {
        var account = AppKit.Account;
        if (account != null)
        {
            UpdateStatus("Wallet connected. Loading data...");

            UpdateAccount(account.Address, true);
            PlayerProfile.Instance.OnWalletConnected(account.Address);

            var t1 = CheckEthBalance(true);
            var t2 = CheckTokenBalance(true);
            var t3 = CheckNFTBalance(true);

            await Task.WhenAll(t1, t2, t3);

            OnWalletDataUpdated?.Invoke();
            UpdateStatus("Ready!");
            SetButtonStates(connectEnabled: false, disconnectEnabled: true, mintEnabled: true);
        }
    }

    private void OnAccountDisconnected(object sender, System.EventArgs e)
    {
        InitializeDisconnectedState();
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && !string.IsNullOrEmpty(CachedAddress) && !isLoading)
        {
            RefreshAllBalances();
        }
    }

    public async void RefreshAllBalances()
    {
        if (isLoading) return; 
        isLoading = true;

        UpdateStatus("Refreshing data...");

        try
        {
            var t1 = CheckEthBalance(true);
            var t2 = CheckTokenBalance(true);
            var t3 = CheckNFTBalance(true);

            await Task.WhenAll(t1, t2, t3);

            OnWalletDataUpdated?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Refresh Error: {e.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }
    private void UpdateAccount(string address, bool silent = false)
    {
        CachedAddress = address;
        if (accountText != null)
        {
            if (address.Length > 10)
                accountText.text = $"{address.Substring(0, 3)}...{address.Substring(address.Length - 4)}";
            else
                accountText.text = address;
        }
        if (!silent) OnWalletDataUpdated?.Invoke();
    }

    private void UpdateEthBalance(string balance, bool silent = false)
    {
        CachedEth = balance;
        if (ethBalanceText != null) ethBalanceText.text = balance;
        if (!silent) OnWalletDataUpdated?.Invoke();
    }

    private void UpdateTokenBalance(string balance, bool silent = false)
    {
        CachedCoin = balance;
        if (tokenBalanceText != null) tokenBalanceText.text = balance;
        if (!silent) OnWalletDataUpdated?.Invoke();
    }

    private void UpdateNFTBalance(string balance, bool silent = false)
    {
        CachedHeroes = balance;
        if (nftBalanceText != null) nftBalanceText.text = balance;
        if (!silent) OnWalletDataUpdated?.Invoke();
    }

    private void SetButtonStates(bool connectEnabled, bool disconnectEnabled, bool mintEnabled)
    {
        if (connectButton != null) connectButton.interactable = connectEnabled;
        if (disconnectButton != null) disconnectButton.interactable = disconnectEnabled;
        if (mintNFTButton != null) mintNFTButton.interactable = mintEnabled;
    }
    public async Task MintCoinNFT(string coinAmountText)
    {
        try
        {
            var account = AppKit.Account;
            if (account == null) return;


            if (coinAmountText == null || !decimal.TryParse(coinAmountText, NumberStyles.Any, CultureInfo.InvariantCulture, out var coinAmount))
            {
                UpdateStatus("Invalid coin amount");
                return;
            }
            decimal wei = coinAmount * 0.0000001m;
            BigInteger weiValue = UnitConversion.Convert.ToWei(wei);
            string txHash = await AppKit.Evm.WriteContractAsync(
                tokenContractAddress,
                TOKEN_ABI,
                "mint",
                value: weiValue,
                gas: 0,
                account.Address
            );
            UpdateStatus($"Coin minted! TX: {txHash.Substring(0, 10)}...");
            await CheckTokenBalance();
            await CheckEthBalance();
            UpdateStatus("Coin successfully minted!");
        }
        catch (System.Exception e)
        {
            UpdateStatus($"Mint failed: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (AppKit.IsInitialized)
        {
            AppKit.AccountConnected -= OnAccountConnected;
            AppKit.AccountDisconnected -= OnAccountDisconnected;
        }
    }

    private void OnApplicationQuit()
    {
        if (AppKit.IsInitialized)
        {
            AppKit.AccountConnected -= OnAccountConnected;
            AppKit.AccountDisconnected -= OnAccountDisconnected;
        }
    }
}