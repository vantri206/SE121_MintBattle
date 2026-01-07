using Reown.AppKit.Unity;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;

public class HeroUpgradeManager : MonoBehaviour
{
    public static HeroUpgradeManager Instance { get; private set; }

    [Header("Blockchain Config")]
    private string heroContractAddress = "0x59977A2a233AD617c4EE3a0D5C54a85Ad8b20c02";
    private string coinContractAddress = "0x6806c72Bcc5c0aF846FA3eCeAc2bbfA5613379AE";

    private const string CHAIN_ID_STRING = "eip155:84532";

    private const string ERC20_ABI = @"[
        {""constant"":true,""inputs"":[{""name"":""owner"",""type"":""address""}],""name"":""balanceOf"",""outputs"":[{""name"":""balance"",""type"":""uint256""}],""type"":""function""},
        {""constant"":false,""inputs"":[{""name"":""spender"",""type"":""address""},{""name"":""amount"",""type"":""uint256""}],""name"":""approve"",""outputs"":[{""name"":""success"",""type"":""bool""}],""type"":""function""},
        {""constant"":true,""inputs"":[{""name"":""owner"",""type"":""address""},{""name"":""spender"",""type"":""address""}],""name"":""allowance"",""outputs"":[{""name"":""remaining"",""type"":""uint256""}],""type"":""function""}
    ]";

    private const string UPGRADE_ABI = @"[{""inputs"":[{""internalType"":""uint256"",""name"":""tokenId"",""type"":""uint256""},{""internalType"":""uint256"",""name"":""cost"",""type"":""uint256""},{""internalType"":""uint256"",""name"":""newLevel"",""type"":""uint256""}],""name"":""upgradeHero"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""}]";

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task OnUpgradeClick(Hero hero)
    {
        if (AppKit.Account == null)
        {
            MessageBox.Instance.ShowSuccess("Please connect wallet first!");
            return;
        }

        await EnsureBaseSepoliaNetwork();

        try
        {
            int costVal = hero.Level * 10;
            int newLevel = hero.Level + 1;

            BigInteger costWei = BigInteger.Parse(costVal.ToString()) * BigInteger.Pow(10, 18);
            BigInteger tokenIdBigInt = BigInteger.Parse(hero.Id);
            BigInteger newLevelBigInt = new BigInteger(newLevel);

            BigInteger currentBalance = await GetTokenBalance();

            if (currentBalance < costWei)
            {
                string msg = $"Not enough Token! Need: {costVal}, Have: {currentBalance / BigInteger.Pow(10, 18)}";
                Debug.LogError(msg);
                MessageBox.Instance.ShowSuccess(msg);
                return;
            }

            BigInteger currentAllowance = await GetAllowance();

            Debug.Log($"[Upgrade Debug] Current Allowance: {currentAllowance}");
            Debug.Log($"[Upgrade Debug] Required Cost:     {costWei}");

            if (currentAllowance < costWei)
            {
                MessageBox.Instance.ShowLoading("Approving Token...");

                BigInteger safeMaxApproval = BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000");

                string txApprove = await AppKit.Evm.WriteContractAsync(
                    coinContractAddress,
                    ERC20_ABI,
                    "approve",
                    new object[] { heroContractAddress, safeMaxApproval }
                );

                Debug.Log($"Approve Hash: {txApprove}. Waiting for confirmation...");

                bool approved = await WaitForTransaction(txApprove);
                if (!approved)
                {
                    MessageBox.Instance.ShowSuccess("Approve Failed or Timed Out!");
                    return;
                }
                Debug.Log("Approve Confirmed on Blockchain!");
            }

            MessageBox.Instance.ShowLoading($"Upgrading Hero #{hero.Id} to Lv.{newLevel}...");

            string txUpgrade = await AppKit.Evm.WriteContractAsync(
                heroContractAddress,
                UPGRADE_ABI,
                "upgradeHero",
                new object[] {
                    tokenIdBigInt,
                    costWei,
                    newLevelBigInt
                }
            );

            Debug.Log("Upgrade TX: " + txUpgrade);

            if (!string.IsNullOrEmpty(txUpgrade))
            {
                hero.Level = newLevel;
                hero.RecalculateStats();

                if (HeroDetailUI.Instance != null)
                    HeroDetailUI.Instance.RefreshCurrentHeroUI();

                MessageBox.Instance.ShowSuccess($"Upgrade Success!\nHero #{hero.Id} reached Lv.{newLevel}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Upgrade Failed: {ex.Message}");
            if (ex.Message.Contains("reverted"))
                MessageBox.Instance.ShowSuccess("Transaction Reverted (Check Balance/Allowance)");
            else
                MessageBox.Instance.ShowSuccess("Transaction Failed");
        }
    }

    private async Task<bool> WaitForTransaction(string txHash)
    {
        int retries = 0;
        int maxRetries = 30;

        while (retries < maxRetries)
        {
            try
            {
                var receipt = await AppKit.Evm.GetTransactionReceiptAsync(txHash);
                if (receipt != null)
                {
                    return true;
                }
            }
            catch
            {
            }

            await Task.Delay(1000);
            retries++;
        }

        Debug.LogError("Wait for transaction timed out!");
        return false;
    }

    private string GetCleanAddress()
    {
        if (AppKit.Account == null) return "";
        string addr = AppKit.Account.Address;
        if (addr.Contains(":"))
        {
            addr = addr.Split(':').Last();
        }
        return addr;
    }

    public async Task<BigInteger> GetTokenBalance()
    {
        string userAddress = GetCleanAddress();
        if (string.IsNullOrEmpty(userAddress)) return 0;
        try
        {
            return await AppKit.Evm.ReadContractAsync<BigInteger>(
                coinContractAddress,
                ERC20_ABI,
                "balanceOf",
                new object[] { userAddress }
            );
        }
        catch { return 0; }
    }

    public async Task<BigInteger> GetAllowance()
    {
        string userAddress = GetCleanAddress();
        if (string.IsNullOrEmpty(userAddress)) return 0;
        try
        {
            return await AppKit.Evm.ReadContractAsync<BigInteger>(
                coinContractAddress,
                ERC20_ABI,
                "allowance",
                new object[] { userAddress, heroContractAddress }
            );
        }
        catch { return 0; }
    }

    public async Task EnsureBaseSepoliaNetwork()
    {
        if (!AppKit.IsInitialized || AppKit.NetworkController == null) return;
        string currentChain = AppKit.Account.ChainId;

        if (currentChain != CHAIN_ID_STRING && currentChain != "84532")
        {
            if (AppKit.NetworkController.Chains.TryGetValue(CHAIN_ID_STRING, out var baseChain))
            {
                try
                {
                    await AppKit.NetworkController.ChangeActiveChainAsync(baseChain);
                    await Task.Delay(1000);
                }
                catch { }
            }
        }
    }
}