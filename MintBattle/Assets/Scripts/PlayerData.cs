using UnityEngine;

public class PlayerProfile : MonoBehaviour
{
    public static PlayerProfile Instance;

    public string WalletAddress; 
    public string PlayerName;   

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void OnWalletConnected(string address)
    {
        this.WalletAddress = address;
        LoadProfile();
    }

    private void LoadProfile()
    {
        string saveKey = $"PlayerName_{WalletAddress}";

        if (PlayerPrefs.HasKey(saveKey))
        {
            this.PlayerName = PlayerPrefs.GetString(saveKey);
        }
        else
        {
            this.PlayerName = ShortenAddress(WalletAddress);
            SaveCustomName(this.PlayerName);
        }

        Debug.Log($"Login Success: {WalletAddress} as {PlayerName}");
    }

    public void SaveCustomName(string newName)
    {
        this.PlayerName = newName;
        PlayerPrefs.SetString($"PlayerName_{WalletAddress}", newName);
        PlayerPrefs.Save();
    }

    private string ShortenAddress(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length < 5) return "0000";
        return address.Substring(address.Length - 3);
    }
}