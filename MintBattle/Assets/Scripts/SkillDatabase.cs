using System.Collections.Generic;
using UnityEngine;

public class SkillDatabase : MonoBehaviour
{
    public static SkillDatabase Instance;

    private Dictionary<string, SkillData> skillMap;

    public string[] nftPassiveIdMapping;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAllSkills();
    }

    private void LoadAllSkills()
    {
        skillMap = new Dictionary<string, SkillData>();

        SkillData[] allSkills = Resources.LoadAll<SkillData>("Data/Skills");
        foreach (var skill in allSkills)
        {
            if (skill != null && !skillMap.ContainsKey(skill.Id))
            {
                skillMap.Add(skill.Id, skill);
            }
            else
            {
                Debug.LogWarning($"Duplicate or Null Skill ID: {skill.name}");
            }
        }
        Debug.Log($"Loaded {skillMap.Count} skills.");
    }

    public SkillData GetSkillById(string id)
    {
        if (skillMap.TryGetValue(id, out SkillData skill))
            return skill;
        return null;
    }

    public SkillData GetSkillFromNftId(int nftId)
    {
        if (nftId > 0 && nftId < nftPassiveIdMapping.Length)
        {
            string stringId = nftPassiveIdMapping[nftId];
            return GetSkillById(stringId);
        }

        return null; 
    }
}
