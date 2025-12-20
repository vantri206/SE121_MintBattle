using UnityEngine;

[System.Serializable]
public class RuntimeSkill
{
    public SkillData skillData;    
    public int currentCooldown;

    public RuntimeSkill(SkillData data)
    {
        skillData = data;
        currentCooldown = 0;
    }
    public void DecreaseCooldown(int turn)
    {
        currentCooldown = Mathf.Max(0, currentCooldown - turn); 
    }
}