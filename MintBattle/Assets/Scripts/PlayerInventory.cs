using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    private List<Hero> playerHeroes = new List<Hero>();
    private List<Item> playerItems = new List<Item>();

    public static event Action OnInventoryUpdate;
   
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //LoadTestData();
    }
    private void Start()
    {
        if (NFTManager.Instance != null &&
        (NFTManager.Instance.CachedHeroList.Count > 0 || NFTManager.Instance.CachedItemList.Count > 0))
        {
            SyncDataFromWalletManager();
        }
    }
    private void OnEnable()
    {
        NFTManager.OnWalletDataUpdated += SyncDataFromWalletManager;
    }

    private void OnDisable()
    {
        NFTManager.OnWalletDataUpdated -= SyncDataFromWalletManager;
    }

    void LoadTestData()
    {
        for (int i = 0; i < 30; i++)
        {
            if (i % 2 == 0)
                AddHero(new Hero("Wizard", i + 1, 1, (i * 2).ToString()));
            else
                AddHero(new Hero("Warrior", i + 1, 2, (i * 2).ToString()));
        }

        ItemData data1 = Resources.Load<ItemData>("Data/Items/BronzeSword");
        if (data1 != null)
        {
            AddItem(new Item("1", data1));
        }
        else
        {
            Debug.LogError("Not found IE01");
        }

        ItemData data2 = Resources.Load<ItemData>("Data/Items/IronSword");
        if (data2 != null)
        {
            AddItem(new Item("2", data2));
            AddItem(new Item("2", data2));
        }
        else
        {
            Debug.LogError("Not found IE02");
        }

        ItemData data3 = Resources.Load<ItemData>("Data/Items/GoldenSword");
        if (data3 != null)
        {
            AddItem(new Item("3", data3));
            AddItem(new Item("3", data3));
        }
        else
        {
            Debug.LogError("Not found IE03");
        }
    }
    private void SyncDataFromWalletManager()
    {
        if (NFTManager.Instance == null) return;

        playerHeroes = new List<Hero>(NFTManager.Instance.CachedHeroList);

        playerItems = new List<Item>(NFTManager.Instance.CachedItemList);

        Debug.Log($"[Inventory] Updated: {playerHeroes.Count} Heroes || {playerItems.Count} Items");

        LoadLoadout();
        OnInventoryUpdate?.Invoke();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            if (NFTManager.Instance != null)
            {
                Debug.Log("Game Focus -> Request Refresh Balance");
                NFTManager.Instance.RefreshAllBalances();
            }
        }
    }
    public void AddHero(Hero newHero)
    {
        playerHeroes.Add(newHero);
        OnInventoryUpdate?.Invoke();
    }
    public Hero GetHeroById(string id)
    {
        return playerHeroes.Find(x => x.Id == id);
    }
    public Hero GetHero(int index)
    {
        if (index >= 0 && index < playerHeroes.Count)
            return playerHeroes[index];
        return null;
    }
    public List<Hero> GetAllHeroes()
    {
        return playerHeroes;
    }

    public void AddItem(Item newItem)
    {
        if (newItem == null || newItem.data == null) return;

        playerItems.Add(newItem);
        OnInventoryUpdate?.Invoke();
    }
    public Item GetItemByTokenId(string tokenId)
    {
        return playerItems.Find(x => x.tokenId == tokenId);
    }
    public List<Item> GetAllItems()
    {
        return playerItems;
    }
    /* Save player pref for equipment, only for local test */
    public void SaveLoadout()
    {
        EquipmentSaveData data = new EquipmentSaveData();

        foreach (var hero in playerHeroes)
        {
            if (hero.equippedWeapon != null)
            {
                HeroEquipment record = new HeroEquipment();
                record.heroId = hero.Id;
                record.weaponTokenId = hero.equippedWeapon.tokenId;

                data.records.Add(record);
            }
        }

        string json = JsonConvert.SerializeObject(data);
        PlayerPrefs.SetString("EQUIPMENT_DATA", json);
        PlayerPrefs.Save();

        Debug.Log("[System] Saved Loadout to PlayerPrefs");
    }

    private void LoadLoadout()
    {
        string keys = "EQUIPMENT_DATA";
        if (!PlayerPrefs.HasKey(keys)) return;

        string json = PlayerPrefs.GetString(keys);

        try
        {
            EquipmentSaveData data = JsonConvert.DeserializeObject<EquipmentSaveData>(json);

            if (data == null || data.records == null) return;

            Debug.Log($"[System] Found saved loadout for {data.records.Count} heroes.");

            foreach (var record in data.records)
            {
                Hero targetHero = GetHeroById(record.heroId);
                Item targetWeapon = GetItemByTokenId(record.weaponTokenId);
                if (targetHero != null && targetWeapon != null)
                {
                    targetHero.Equip(targetWeapon);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Load Loadout Error: {ex.Message}");    
            PlayerPrefs.DeleteKey(keys);
        }
    }
    [System.Serializable]
    public class HeroEquipment
    {
        public string heroId;      
        public string weaponTokenId; 
    }
    [System.Serializable]
    public class EquipmentSaveData
    {
        public List<HeroEquipment> records = new List<HeroEquipment>();
    }
}