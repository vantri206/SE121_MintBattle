using UnityEngine;

public class Hero
{
    public HeroData ClassData;
    public int Level;
    public string Id;
    public Hero(string heroClassId, int level, string id = "unknow")
    {
        this.ClassData = HeroDatabase.Instance.GetHeroById(heroClassId);
        this.Level = level;
        Id = id;
    }
    public int HP
    {
        get
        {
            return ClassData.BaseHP + Level * ClassData.growthStats;
        }
    }
    public int Attack
    {
        get
        {
            return ClassData.BaseAttack + Level * ClassData.growthStats;
        }
    }
    public int Defense
    {
        get
        {
            return ClassData.BaseDefense + Level * ClassData.growthStats;
        }
    }
    public int Speed
    {
        get
        {
            return ClassData.BaseSpeed + Level * ClassData.growthStats;
        }
    }
    public int Power
    {
        get
        {
            return (int)((HP + Attack + Defense + Speed)/ 4 * ClassData.powerScale);
        }
    }
}
