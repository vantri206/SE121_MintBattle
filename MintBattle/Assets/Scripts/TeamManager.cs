using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance;

    [Header("Settings")]
    public int maxTeams = 3;       
    public int slotsPerTeam = 2;  

    private Dictionary<int, string[]> teams = new Dictionary<int, string[]>();
    public int CurrentBattleTeamIndex { get; private set; } = 0;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject); 
            LoadData(); 
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    public void SetSelectedTeamIndex(int index)
    {
        CurrentBattleTeamIndex = index;
    }
    public void AddHeroToTeam(int teamIndex, int slotIndex, string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return;

        RemoveHeroFromAllTeams(heroId);
        RemoveHeroFromTeam(teamIndex, slotIndex);

        if (!teams.ContainsKey(teamIndex))
        {
            teams.Add(teamIndex, new string[slotsPerTeam]);
        }

        teams[teamIndex][slotIndex] = heroId;

        SaveData();
    }

    public void RemoveHeroFromTeam(int teamIndex, int slotIndex)
    {
        if (teams.ContainsKey(teamIndex))
        {
            teams[teamIndex][slotIndex] = null;
            SaveData();
        }
    }

    public void RemoveHeroFromAllTeams(string heroId)
    {
        bool isChanged = false;
        foreach (var team in teams)
        {
            for (int i = 0; i < team.Value.Length; i++)
            {
                if (team.Value[i] == heroId)
                {
                    team.Value[i] = null;
                    isChanged = true;
                }
            }
        }
        if (isChanged) SaveData();
    }
    public string GetHeroIdInSlot(int teamIndex, int slotIndex)
    {
        if (teams.ContainsKey(teamIndex))
        {
            if (slotIndex >= 0 && slotIndex < teams[teamIndex].Length)
            {
                return teams[teamIndex][slotIndex];
            }
        }
        return null;
    }
    public int GetTeamIndexOfHero(string heroId)
    {
        foreach (var team in teams)
        {
            if (team.Value.Contains(heroId)) return team.Key;
        }
        return -1;
    }

    public string[] GetHeroesInTeam(int teamIndex)
    {
        if (teams.ContainsKey(teamIndex))
            return teams[teamIndex];

        return new string[slotsPerTeam];
    }
    private void SaveData()
    {
        foreach (var team in teams)
        {
            string[] safeData = team.Value.Select(x => x ?? "").ToArray();
            string dataString = string.Join(",", safeData);

            PlayerPrefs.SetString($"TEAM_DATA_{team.Key}", dataString);
        }
        PlayerPrefs.Save();
    }
    private void LoadData()
    {
        teams.Clear();

        for (int i = 0; i < maxTeams; i++)
        {
            string key = $"TEAM_DATA_{i}";
            string[] loadedSlots = new string[slotsPerTeam];

            if (PlayerPrefs.HasKey(key))
            {
                string rawData = PlayerPrefs.GetString(key);
                string[] splitData = rawData.Split(',');

                for (int j = 0; j < slotsPerTeam && j < splitData.Length; j++)
                {
                    if (!string.IsNullOrEmpty(splitData[j]))
                    {
                        loadedSlots[j] = splitData[j];
                    }
                }
            }

            teams.Add(i, loadedSlots);
        }
    }
    public bool IsTeamEmpty(int teamIndex)
    {
        for (int i = 0; i < 2; i++)
        {
            string heroId = GetHeroIdInSlot(teamIndex, i);
            if (!string.IsNullOrEmpty(heroId))
            {
                return false; 
            }
        }
        return true;
    }
    public void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        teams.Clear();
        LoadData();
    }
}