using UnityEngine;
public enum SkillType { Active, Passive }
public enum PassiveTarget { None, AttackBoost, HpBoost, AllStatsBoost, CritRate, Lifesteal }
public enum SkillTargetType
{
    SingleEnemy,    
    AllEnemies,              
    Ally           
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "New Data/New Skill")]
public class SkillData : ScriptableObject
{
    [Header("General Info")]
    public string Id;             
    public string Name;          
    [TextArea]
    public string Description;     
    public Sprite Image;

    public SkillType type;
    public int Cooldown;         

    public SkillTargetType TargetType;

    public float AttackMultiplier = 1.0f;

    public GameObject vfxPrefab;
    public float castDelay = 1.5f;

    public bool isSkillAttack = true;
    public bool isSkillHeal = false;

    [Header("Passive Config")]
    public PassiveTarget passiveTarget;
    public float passiveValue;
}