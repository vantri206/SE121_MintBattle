using Org.BouncyCastle.Asn1.X509;
using System.Collections;
using System.Collections.Generic;
using Thirdweb.Api;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BattleUnit : MonoBehaviour
{
    public Hero sourceHero;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer bodySprite;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private GameObject unitVisual;

    public Animator animator;

    [Header("Stats")]
    public string unitName;
    public int damage;
    public int defense;
    public int speed;

    public int maxHP;
    public int currentHP;

    [Header("Skills")]
    public List<RuntimeSkill> ActiveSkills = new List<RuntimeSkill>();

    [Header("Player setting")]
    public string ownerId;
    public string ownerName;

    [Header("Damage Popup Settings")]
    public GameObject damagePopupPrefab;
    public Transform damageSpawnPoint;

    public Transform attackSpawnPoint;

    public bool IsDead => currentHP <= 0;
    public void SetupHero(Hero hero, string assignedOwnerId)
    {
        sourceHero = hero;

        ownerId = assignedOwnerId;
        ownerName = PlayerProfile.Instance.PlayerName;

        unitName = $"{ownerName}_{hero.ClassData.Name}";
        gameObject.name = $"BattleUnit - {assignedOwnerId}_{unitName}";

        damage = hero.Attack;
        defense = hero.Defense;
        speed = hero.Speed;
        maxHP = hero.HP;
        currentHP = maxHP;

        ActiveSkills.Clear();

        foreach (string skillId in hero.ClassData.Skills)
        {
            SkillData skillData = Resources.Load<SkillData>($"Data/Skills/{skillId}");

            if (skillData != null)
            {
                ActiveSkills.Add(new RuntimeSkill(skillData));
            }
            else
            {
                Debug.LogWarning($"Skill Data not found: {skillId}");
            }
        }

        if (bodySprite != null) bodySprite.sprite = hero.ClassData.Image;
        if (animator != null && hero.ClassData.animator != null)
        {
            animator.runtimeAnimatorController = hero.ClassData.animator;
        }

        bool isMyUnit = (assignedOwnerId == PlayerProfile.Instance.WalletAddress);
        if (isMyUnit)
        {
            unitVisual.transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            unitVisual.transform.localScale = new Vector3(-1, 1, 1);
        }

        UpdateHealthBar();
    }

    public void TakeDamage(int dmg)
    {
        int actualDamage = Mathf.Max(1, dmg - (defense / 2));

        currentHP -= actualDamage;

        Debug.Log($"{unitName} lost {actualDamage} HP!");

        if (animator) animator.SetTrigger("Hurt");

        if (damagePopupPrefab != null && damageSpawnPoint != null)
        {
            GameObject popup = Instantiate(damagePopupPrefab, damageSpawnPoint.position, Quaternion.identity);
            DamagePopup popupScript = popup.GetComponent<DamagePopup>();
            if (popupScript != null)
            {
                popupScript.Setup(actualDamage, false);
            }
        }
        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }

        UpdateHealthBar();
    }
    public void Heal(int amount)
    {
        currentHP += amount;

        if (currentHP > maxHP) currentHP = maxHP;

        UpdateHealthBar();

        Debug.Log($"{unitName} gain heal {amount} HP");
    }
    void UpdateHealthBar()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
    }

    void Die()
    {
        if (animator) animator.SetTrigger("Die");

        StartCoroutine(FadeOut());
    }
    public void OnTurnStart()
    {
        foreach (var skill in ActiveSkills)
        {
            skill.DecreaseCooldown(1);
        }
    }
    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(1.0f);
        gameObject.SetActive(false);
    }
}