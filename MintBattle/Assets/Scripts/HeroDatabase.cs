using System.Collections.Generic;
using UnityEngine;

public class HeroDatabase : MonoBehaviour
{
    public static HeroDatabase Instance;

    private Dictionary<string, HeroData> heroMap;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAllHeroes();
    }

    private void LoadAllHeroes()
    {
        heroMap = new Dictionary<string, HeroData>();
        HeroData[] allHeroes = Resources.LoadAll<HeroData>("Data/Heroes");
        foreach (var heroData in allHeroes)
        {
            heroMap.Add(heroData.Id, heroData);
        }
    }

    public HeroData GetHeroById(string id)
    {
        if (heroMap.TryGetValue(id, out HeroData Hero))
            return Hero;
        return null;
    }
}
