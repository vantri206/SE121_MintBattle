using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    private List<Hero> playerHeroes = new List<Hero>();

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
    }
    private void Start()
    {
        if (NFTManager.Instance != null && NFTManager.Instance.CachedHeroList.Count > 0)
        {
            SyncDataFromWalletManager();
        }

        LoadTestData();
    }
    void LoadTestData()
    {
        for (int i = 0; i < 30; i++)
        {
            if (i % 2 == 0)
                AddHero(new Hero("HR02", i + 1, (i * 2).ToString()));
            else
                AddHero(new Hero("HR01", i + 1, (i * 2).ToString()));
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

    private void SyncDataFromWalletManager()
    {
        if (NFTManager.Instance == null) return;

        playerHeroes.Clear();

        playerHeroes = new List<Hero>(NFTManager.Instance.CachedHeroList);

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
}