using System.Collections.Generic;
using UnityEngine;

public class SkillDatabase : MonoBehaviour
{
    public static SkillDatabase Instance;

    private Dictionary<string, SkillData> skillMap;

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
            skillMap.Add(skill.Id, skill);
        }
    }

    public SkillData GetSkillById(string id)
    {
        if (skillMap.TryGetValue(id, out SkillData skill))
            return skill;
        return null;
    }
}
