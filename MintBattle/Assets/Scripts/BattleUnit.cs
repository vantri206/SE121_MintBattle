using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleUnit : NetworkBehaviour
{
    public Hero SourceHero { get; private set; }

    [Networked] public NetworkString<_64> OwnerId { get; set; }
    [Networked] public NetworkString<_32> OwnerName { get; set; }
    [Networked] public NetworkString<_64> HeroNftId { get; set; }
    [Networked] public NetworkString<_32> HeroClassId { get; set; }

    [Networked] public int HeroLevel { get; set; }
    [Networked] public int PassiveId { get; set; } 
    [Networked] public int TeamPositionIndex { get; set; }

    // Stats
    [Networked] public int MaxHP { get; set; }
    [Networked] public int Damage { get; set; }
    [Networked] public int Defense { get; set; }
    [Networked] public int Speed { get; set; }
    [Networked] public int CritRate { get; set; }

    [Networked, OnChangedRender(nameof(OnHPChanged))]
    public int CurrentHP { get; set; }

    [Header("Visual References")]
    [SerializeField] private SpriteRenderer bodySprite;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private GameObject unitVisual;
    public Animator animator;
    public GameObject damagePopupPrefab;
    public Transform damageSpawnPoint;
    public Transform attackSpawnPoint;

    public List<RuntimeSkill> ActiveSkills = new List<RuntimeSkill>();
    public string unitName;
    public void NetworkSetup(string ownerId, string ownerName, Hero hero, int posIndex)
    {
        OwnerId = ownerId;
        OwnerName = ownerName;
        HeroNftId = hero.Id;
        HeroClassId = hero.ClassData.Id;
        HeroLevel = hero.Level;
        TeamPositionIndex = posIndex;


        PassiveId = GetPassiveIdFromHero(hero);

        MaxHP = hero.HP;
        CurrentHP = hero.HP;
        Damage = hero.Attack;
        Defense = hero.Defense;
        Speed = hero.Speed;
        CritRate = hero.CritRate;
    }

    private int GetPassiveIdFromHero(Hero hero)
    {
        foreach (var skill in hero.Skills)
        {
            if (skill.Id.StartsWith("passive_"))
            {
                string numberPart = skill.Id.Replace("passive_", "");
                if (int.TryParse(numberPart, out int id)) return id;
            }
        }
        return 0;
    }

    public override void Spawned()
    {

        SourceHero = new Hero(HeroClassId.ToString(), HeroLevel, PassiveId, HeroNftId.ToString());

        UpdateVisualPosition();
        LoadVisualFromSourceHero();
        LoadSkillsLocal();
        UpdateHealthBar();

        if (BattleSystem.Instance != null)
        {
            BattleSystem.Instance.RegisterUnit(this);
        }
    }

    void LoadSkillsLocal()
    {
        ActiveSkills.Clear();

        if (SourceHero != null)
        {
            foreach (var skillData in SourceHero.Skills)
            {
                if (skillData != null)
                {
                    ActiveSkills.Add(new RuntimeSkill(skillData));
                }
            }
        }
    }

    public void UpdateVisualPosition()
    {
        if (PlayerProfile.Instance == null) return;

        string myWallet = PlayerProfile.Instance.WalletAddress;
        Transform targetTransform;

        if (OwnerId.ToString() == myWallet)
        {
            targetTransform = BattleSystem.Instance.heroSpawnPoints[TeamPositionIndex];
            unitVisual.transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            targetTransform = BattleSystem.Instance.enemySpawnPoints[TeamPositionIndex];
            unitVisual.transform.localScale = new Vector3(-1, 1, 1);
        }

        if (targetTransform != null)
        {
            transform.position = targetTransform.position;
        }
    }

    void LoadVisualFromSourceHero()
    {
        if (SourceHero != null && SourceHero.ClassData != null)
        {
            if (bodySprite != null) bodySprite.sprite = SourceHero.ClassData.Image;

            if (animator != null && SourceHero.ClassData.animator != null)
                animator.runtimeAnimatorController = SourceHero.ClassData.animator;

            unitName = $"{OwnerName}_{SourceHero.ClassData.Id}";
            gameObject.name = $"Unit_{HeroClassId}_{HeroNftId}";
        }
    }

    public void TakeDamage(int rawDmg)
    {    
        if (!Object.HasStateAuthority) return;

        int actualDamage = Mathf.Max(1, rawDmg - Defense);
        CurrentHP -= actualDamage;
        if (CurrentHP < 0) CurrentHP = 0;
        RPC_ShowDamageVisual(actualDamage, false);
    }

    public void Heal(int amount)
    {
        if (!Object.HasStateAuthority) return;

        CurrentHP += amount;
        if (CurrentHP > MaxHP) CurrentHP = MaxHP;

        RPC_ShowDamageVisual(amount, true);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowDamageVisual(int value, bool isHeal)
    {
        if (damagePopupPrefab != null && damageSpawnPoint != null)
        {
            GameObject popup = Instantiate(damagePopupPrefab, damageSpawnPoint.position, Quaternion.identity);
            popup.GetComponent<DamagePopup>()?.Setup(value, isHeal);
        }

        if (!isHeal && CurrentHP <= 0)
        {
            Die();
        }
    }
    public void OnHPChanged()
    {
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = MaxHP;
            hpSlider.value = CurrentHP;
        }
    }

    public void OnTurnStart()
    {
        if (ActiveSkills != null)
        {
            foreach (var skill in ActiveSkills)
            {
                skill.DecreaseCooldown(1);
            }
        }
        if (Object.HasStateAuthority)
        {
            if (PassiveId == 5)
            {
                int healAmount = Mathf.RoundToInt(Damage * 0.3f);
                if (healAmount > 0 && CurrentHP < MaxHP)
                {
                    Heal(healAmount);
                    Debug.Log($"Passive 5 triggered: Healed {healAmount} HP");
                }
            }
        }
    }

    void Die()
    {
        StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(1.0f);
        gameObject.SetActive(false);
    }
}