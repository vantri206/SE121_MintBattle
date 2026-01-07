using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Hero
{
    public HeroData ClassData;
    public string Id;
    public int Level;

    public List<SkillData> Skills = new List<SkillData>();

    public int itemBonusAttack = 0;
    public int itemBonusHP = 0;
    public Item equippedWeapon;

    private int _finalHP;
    private int _finalAttack;
    private int _finalDefense;
    private int _finalSpeed;
    private int _critRate; 

    public Hero(string heroClassId, int level, int nftPassiveId, string id = "0")
    {
        this.ClassData = HeroDatabase.Instance.GetHeroById(heroClassId);
        this.Level = level;
        this.Id = id;


        foreach (var skillId in ClassData.Skills)
        {
            var s = SkillDatabase.Instance.GetSkillById(skillId);
            if (s != null) Skills.Add(s);
        }

        SkillData passiveSkill = SkillDatabase.Instance.GetSkillFromNftId(nftPassiveId);
        if (passiveSkill != null)
        {
            Skills.Add(passiveSkill);
        }

        RecalculateStats();
    }

    public void RecalculateStats()
    {
        int rawHP = ClassData.BaseHP + (Level * ClassData.growthStats);
        int rawAtk = ClassData.BaseAttack + (Level * ClassData.growthStats);
        int rawDef = ClassData.BaseDefense + (Level * ClassData.growthStats);
        int rawSpd = ClassData.BaseSpeed + (Level * ClassData.growthStats);

        rawHP += itemBonusHP;
        rawAtk += itemBonusAttack;

        float hpMultiplier = 0f;
        float atkMultiplier = 0f;
        float spdMultiplier = 0f;
        float defMultiplier = 0f;
        int critBonus = 0;

        foreach (var skill in Skills)
        {
            if (skill.type == SkillType.Passive)
            {
                switch (skill.passiveTarget)
                {
                    case PassiveTarget.HpBoost:
                        hpMultiplier += skill.passiveValue; 
                        break;
                    case PassiveTarget.AttackBoost:
                        atkMultiplier += skill.passiveValue;
                        break;
                    case PassiveTarget.AllStatsBoost:
                        hpMultiplier += skill.passiveValue;
                        atkMultiplier += skill.passiveValue;
                        spdMultiplier += skill.passiveValue;
                        defMultiplier += skill.passiveValue;
                        break;
                    case PassiveTarget.CritRate:
                        critBonus += (int)skill.passiveValue;
                        break;
                }
            }
        }

        _finalHP = Mathf.RoundToInt(rawHP * (1 + hpMultiplier));
        _finalAttack = Mathf.RoundToInt(rawAtk * (1 + atkMultiplier));
        _finalDefense = Mathf.RoundToInt(rawDef * (1 + defMultiplier));
        _finalSpeed = Mathf.RoundToInt(rawSpd * (1 + spdMultiplier));

        _critRate = 5 + critBonus;

        _finalDefense = rawDef;
        _finalSpeed = rawSpd;
    }

    public int HP => _finalHP;
    public int Attack => _finalAttack;
    public int Defense => _finalDefense;
    public int Speed => _finalSpeed;
    public int CritRate => _critRate;

    public int Power
    {
        get
        {
            return (int)((HP + Attack + Defense + Speed + (CritRate * 5)) / 4 * ClassData.powerScale);
        }
    }
    public void Equip(Item newItem)
    {
        Unequip();

        if (newItem == null) return;

        equippedWeapon = newItem;

        equippedWeapon.equippedByHeroId = this.Id;

        equippedWeapon.data.OnEquip(this);

        RecalculateStats();
    }

    public void Unequip()
    {
        if (equippedWeapon != null)
        {
            equippedWeapon.data.OnUnequip(this);

            equippedWeapon.equippedByHeroId = null;

            equippedWeapon = null;

            RecalculateStats();
        }
    }
}