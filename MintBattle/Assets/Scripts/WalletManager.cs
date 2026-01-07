using Nethereum.Util;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities;
using Reown.AppKit.Unity;
using Reown.Sign.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Thirdweb;
using Thirdweb.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class NFTManager : MonoBehaviour
{
    public static NFTManager Instance { get; private set; }

    public string CachedAddress { get; private set; } = "";
    public string CachedEth { get; private set; } = "0";
    public string CachedCoin { get; private set; } = "0";
    public string CachedHeroes { get; private set; } = "0";
    public List<Hero> CachedHeroList { get; private set; } = new List<Hero>();
    public List<Item> CachedItemList { get; private set; } = new List<Item>();

    private bool isLoading = false;
    private bool isWalletReady = false;
    public static event Action OnWalletDataUpdated;

    [Header("Contract Configuration")]
    private string tokenContractAddress = "0x6806c72Bcc5c0aF846FA3eCeAc2bbfA5613379AE";
    private string NFTContractAddress = "0x59977A2a233AD617c4EE3a0D5C54a85Ad8b20c02";
    private string itemContractAddress = "0x1a4dF372D0090F27B206DadF89edD1Be3cC46591";
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
    private const string ENUMERABLE_ABI = @"[{""inputs"":[{""internalType"":""address"",""name"":""owner"",""type"":""address""},{""internalType"":""uint256"",""name"":""index"",""type"":""uint256""}],""name"":""tokenOfOwnerByIndex"",""outputs"":[{""internalType"":""uint256"",""name"":"""",""type"":""uint256""}],""stateMutability"":""view"",""type"":""function""}]";

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
                Debug.Log("AppKit Initialized. Waiting for account sync...");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CRITICAL ERROR in Start: {e.Message}");
        }
    }

    private async Task InitializeWallet()
    {
        Debug.Log(Application.persistentDataPath);
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

        try
        {
            await AppKit.InitializeAsync(config);

            int retry = 0;
            while ((AppKit.NetworkController == null || !AppKit.IsInitialized) && retry < 25)
            {
                await Task.Delay(200);
                retry++;
            }

            AppKit.AccountConnected -= OnAccountConnected;
            AppKit.AccountDisconnected -= OnAccountDisconnected;

            AppKit.AccountConnected += OnAccountConnected;
            AppKit.AccountDisconnected += OnAccountDisconnected;

            if (AppKit.Account != null)
            {
                Debug.Log($"Existing session found for: {AppKit.Account}");
                await Task.Yield();
                OnAccountConnected(this, EventArgs.Empty);
            }

            Debug.Log("Mint Battle wallet initialized successfully!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Wallet Init Failed: {ex.Message}");
        }
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
        isWalletReady = false;
        UpdateAccount("", true);
        UpdateEthBalance("0 ETH", true);
        UpdateTokenBalance("0 <sprite=0>", true);
        UpdateNFTBalance("0 Heroes", true);
        CachedHeroList.Clear();
        CachedItemList.Clear();
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
            UpdateTokenBalance($"{CachedCoin} <sprite=0>", silent);
        }
        catch
        {
            UpdateTokenBalance("0 <sprite=0>", silent);
        }
    }
    public async Task CheckNFTBalance(bool silent = false)
    {
        try
        {
            var account = AppKit.Account;
            if (account == null) return;

            var balanceBigInt = await AppKit.Evm.ReadContractAsync<BigInteger>(
                NFTContractAddress,
                NFT_BALANCE_ABI,
                "balanceOf",
                new object[] { account.Address }
            );

            int balance = (int)balanceBigInt;
            UpdateNFTBalance($"{balance} Heroes", silent);

            if (ThirdwebManager.Instance != null && balance > 0)
            {
                List<Hero> tempHeroList = new List<Hero>();

                try
                {
                    var heroContract = await ThirdwebManager.Instance.GetContract(NFTContractAddress, chainId: chainId);
                    Debug.Log($"[NFTManager] Detected {balance} Heroes. Fetching...");

                    for (int i = 0; i < balance; i++)
                    {
                        try
                        {
                            BigInteger tokenId = await AppKit.Evm.ReadContractAsync<BigInteger>(
                                NFTContractAddress,
                                ENUMERABLE_ABI,
                                "tokenOfOwnerByIndex",
                                new object[] { account.Address, i }
                            );

                            NFT nft = await FetchNFTData(heroContract, tokenId);
                            Hero newHero = ConvertNftToHero(nft);
                            if (newHero != null)
                            {
                                tempHeroList.Add(newHero);
                            }
                        }
                        catch (Exception innerEx)
                        {
                            Debug.LogError($"Failed index {i}: {innerEx.Message}");
                            continue;
                        }
                    }
                }
                catch (Exception ex) { Debug.LogError($"Fetch Loop Error: {ex.Message}"); }


                CachedHeroList.Clear();
                CachedHeroList.AddRange(tempHeroList);
                Debug.Log($"[NFTManager] Updated CachedHeroList: {CachedHeroList.Count} items.");

                List<Item> tempItemList = new List<Item>();
                try
                {
                    var itemContract = await ThirdwebManager.Instance.GetContract(itemContractAddress, chainId: chainId);
                    var itemNFTList = await itemContract.ERC1155_GetOwnedNFTs(account.Address);

                    foreach (var nft in itemNFTList)
                    {
                        int quantity = (int)nft.QuantityOwned;
                        ItemData dataSO = LoadItemDataFromNFT(nft);
                        if (dataSO != null)
                        {
                            for (int i = 0; i < quantity; i++)
                            {
                                tempItemList.Add(new Item(nft.Metadata.Id, dataSO));
                            }
                        }
                    }
                }
                catch (Exception ex) { Debug.LogError($"Item Fetch Error: {ex.Message}"); }


                CachedItemList.Clear();
                CachedItemList.AddRange(tempItemList);

                if (!silent) OnWalletDataUpdated?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"CRITICAL ERROR CheckNFTBalance: {ex.Message}");
            UpdateNFTBalance("Error", silent);
        }
    }
    private async Task<NFT> FetchNFTData(ThirdwebContract contract, BigInteger tokenId, bool fillOwner = true)
    {
        if (contract == null) throw new ArgumentNullException("contract");
        if (tokenId < 0) throw new ArgumentOutOfRangeException("tokenId", "Token ID must be >= 0");

        NFTMetadata tempMeta = new NFTMetadata();
        tempMeta.Id = tokenId.ToString();

        NFT nft = new NFT
        {
            Owner = "0x0000000000000000000000000000000000000000",
            Type = NFTType.ERC721,
            Supply = 1,
            QuantityOwned = 1,
            Metadata = tempMeta
        };

        if (fillOwner)
        {
            try
            {
                nft.Owner = await contract.ERC721_OwnerOf(tokenId).ConfigureAwait(false);
            }
            catch
            {

            }
        }

        try
        {
            string uri = await contract.ERC721_TokenURI(tokenId).ConfigureAwait(false);
            var downloadedMeta = await ThirdwebStorage.Download<NFTMetadata>(contract.Client, uri).ConfigureAwait(false);
            downloadedMeta.Id = tokenId.ToString();
            nft.Metadata = downloadedMeta;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FetchNFTData] Metadata error for ID {tokenId}: {ex.Message}");
            NFTMetadata errorMeta = nft.Metadata;
            errorMeta.Description = "Metadata load failed";
            errorMeta.Name = $"Unknown Hero #{tokenId}";
            nft.Metadata = errorMeta;
        }

        return nft;
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
            // Default
            string classId = "Wizard";
            int level = 1;
            int passiveId = 0;
            string tokenId = nft.Metadata.Id; 
            if (nft.Metadata.Attributes == null)
            {
                Debug.LogWarning($"[NFTManager] Metadata is null for Token ID {tokenId}");
                return null;
            }

            if (nft.Metadata.Attributes != null)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(nft.Metadata.Attributes);
                    var traits = JsonConvert.DeserializeObject<List<NftTrait>>(json);

                    if (traits != null)
                    {
                        foreach (var trait in traits)
                        {
                            if (trait.value == null) continue;

                            string valueStr = trait.value.ToString();
                            string typeStr = trait.trait_type; 

                            if (string.IsNullOrEmpty(typeStr)) continue;

                            if (typeStr == "Class") classId = valueStr;
                            else if (valueStr.StartsWith("HR")) classId = valueStr;
                            else if (typeStr == "Level" && int.TryParse(valueStr, out int l)) level = l;
                            else if (typeStr == "Passive Skill" || typeStr == "Passive Effect")
                            {
                                passiveId = ParseIdFromDescription(valueStr);
                            }
                            else if (typeStr == "Passive ID")
                            {
                                int.TryParse(valueStr, out passiveId);
                            }
                        }
                    }
                }
                catch (Exception attrEx)
                {
                    Debug.LogWarning($"[NFTManager] Error parsing attributes for Hero {tokenId}: {attrEx.Message}. Using default stats.");
                }
            }

            return new Hero(classId, level, passiveId, tokenId);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NFTManager] FATAL Error converting Hero Token {nft.Metadata.Id}: {ex.Message}");
            return null; 
        }
    }
    private int ParseIdFromDescription(string description)
    {
        string desc = description.ToLower();
        if (desc.Contains("20% attack")) return 1;
        if (desc.Contains("20% health")) return 2;
        if (desc.Contains("20% all")) return 3;
        if (desc.Contains("100% crit")) return 4;
        if (desc.Contains("health = 30%") || desc.Contains("turn")) return 5;
        return 1;
    }
    private ItemData LoadItemDataFromNFT(NFT nft)
    {
        try
        { 
            if (string.IsNullOrEmpty(nft.Metadata.Id))
            {
                Debug.LogError($"[NFTManager] NFT Metadata ID is empty. Token might be invalid.");
                return null;
            }
            if (string.IsNullOrEmpty(nft.Metadata.Name))
            {
                Debug.LogError($"[NFTManager] NFT Name is empty (TokenID: {nft.Metadata.Id}).");
                return null;
            }
            string itemName = nft.Metadata.Name;
            string resourceName = itemName.Trim();

            ItemData dataSO = Resources.Load<ItemData>($"Data/Items/{resourceName}");

            if (dataSO == null && !string.IsNullOrEmpty(nft.Metadata.Image))
            {
                try
                {
                    if (!nft.Metadata.Image.StartsWith("ipfs://"))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(nft.Metadata.Image);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            dataSO = Resources.Load<ItemData>($"Data/Items/{fileName}");
                        }
                    }
                }
                catch { }
            }

            if (dataSO == null)
            {
                Debug.LogError($"[NFTManager] Missing .asset file for: '{itemName}'. Please create: 'Resources/Data/Items/{resourceName}.asset'");
            }

            return dataSO;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFTManager] Error loading ItemData: {ex.Message}");
            return null;
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
        if (isLoading) return;
        isLoading = true;

        try
        {
            var account = AppKit.Account;
            if (account != null)
            {
                Debug.Log("Connected! Checking Network...");
                string currentChainId = AppKit.NetworkController.ActiveChain.ToString();
                Debug.Log($"Net: ({currentChainId})");

                UpdateStatus("Wallet connected. Loading data...");

                UpdateAccount(account.Address, true);
                PlayerProfile.Instance.OnWalletConnected(account.Address);

                isWalletReady = true;

                var t1 = CheckEthBalance(true);
                var t2 = CheckTokenBalance(true);
                var t3 = CheckNFTBalance(true);

                await Task.WhenAll(t1, t2, t3);

                OnWalletDataUpdated?.Invoke();
                UpdateStatus("Ready!");
                SetButtonStates(connectEnabled: false, disconnectEnabled: true, mintEnabled: true);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"OnAccountConnected Error: {ex.Message}");
        }
        finally
        {
            isLoading = false;
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
        if (!isWalletReady || isLoading || !AppKit.IsInitialized) return;
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