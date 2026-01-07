using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroCardUI : MonoBehaviour
{
    public int index;
    private Image CardBackground;
    private Color normalColor;
    private Color selectedColor = new Color(255, 255, 255, 0);

    private Hero hero;

    public GameObject heroAvatar;
    public GameObject heroLevel;
    public GameObject heroId;


    public Button button;
    public static event Action<HeroCardUI, int> OnHeroCardClicked;

    private void Awake()
    {
        ColorUtility.TryParseHtmlString("#FFED3CFF", out selectedColor);

        CardBackground = transform.Find("Background").GetComponentInChildren<Image>();
        normalColor = CardBackground.color;

        button.onClick.RemoveAllListeners();
        //button.onClick.AddListener(NotifyClicked);
    }
    public void LoadHero(Hero hero, int index = 0)
    {
        this.hero = hero;
        this.index = index;
        heroAvatar.GetComponent<Animator>().runtimeAnimatorController = hero.ClassData.animator;
        heroLevel.GetComponent<TMP_Text>().text = "Lv." + hero.Level.ToString();
        heroId.GetComponent<TMP_Text>().text = $"#{int.Parse(hero.Id):D4}";
    }
    public void CardSelected()
    {
        //CardBackground.color = selectedColor;
    }
    public void CardUnselected()
    {
        //CardBackground.color = normalColor;
    }
    public Hero GetHero()
    {
        return hero;
    }
    private void NotifyClicked()
    {
        OnHeroCardClicked?.Invoke(this, this.index);
    }
    public void SetInventoryInteraction(int indexInList)
    {
        button.onClick.RemoveAllListeners();
        this.index = indexInList;
        button.onClick.AddListener(NotifyClicked);
    }
    public void SetClickAction(Action onClick)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());
    }
}
