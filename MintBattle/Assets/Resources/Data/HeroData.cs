using UnityEngine;
using System.Collections;

public class HeroData : ScriptableObject
{
    public string Id;

    public string Name;

    public int  BaseHP;
    public int BaseAttack;
    public int BaseDefense;
    public int BaseSpeed;

    public int growthStats;
    public float powerScale;

    public string[] Skills;

    public Sprite Image;
    public Sprite Avatar;

    public RuntimeAnimatorController animator;
}
