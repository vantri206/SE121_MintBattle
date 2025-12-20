using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject heroCardPrefab;
    [SerializeField] private Button btnNext;
    [SerializeField] private Button btnPrev;
    [SerializeField] private TextMeshProUGUI pageText;
    [SerializeField] private HeroDetailUI heroDetail;

    [Header("Settings")]
    private int itemsPerPage = 8;
    private int currentPage = 0;

    [Header("Detail Popup")]
    [SerializeField] private HeroDetailUI heroDetailPanel;
    private int currentDetailIndex = -1;

    private List<Hero> allHeroes = new List<Hero>();

    private void OnEnable()
    {
        InventoryUpdate();

        PlayerInventory.OnInventoryUpdate += InventoryUpdate;
        HeroDetailUI.onNextClick += OnNextDetail;
        HeroDetailUI.onPrevClick += OnPrevDetail;
    }

    private void OnDisable()
    {
        PlayerInventory.OnInventoryUpdate -= InventoryUpdate;
        HeroDetailUI.onNextClick -= OnNextDetail;
        HeroDetailUI.onPrevClick -= OnPrevDetail;
    }
    void Start()
    {
        RenderPage(0);

        btnNext.onClick.AddListener(NextPage);
        btnPrev.onClick.AddListener(PrevPage);
    }

    void RenderPage(int pageIndex)
    {
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        int startIndex = pageIndex * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, allHeroes.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            GameObject cardObj = Instantiate(heroCardPrefab, cardContainer);
            HeroCardUI cardUI = cardObj.GetComponent<HeroCardUI>();
            cardUI.LoadHero(allHeroes[i], i);
            int indexInList = i;
            cardUI.SetInventoryInteraction(indexInList);
        }

        UpdateUIState();
    }
    public void NextPage()
    {
        if ((currentPage + 1) * itemsPerPage < allHeroes.Count)
        {
            currentPage++;
            RenderPage(currentPage);
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RenderPage(currentPage);
        }
    }

    void UpdateUIState()
    {
        int totalPages = Mathf.CeilToInt((float)allHeroes.Count / itemsPerPage);
        pageText.text = $"{currentPage + 1}/{totalPages}";

        btnPrev.interactable = (currentPage > 0);
        btnNext.interactable = ((currentPage + 1) < totalPages);
    }
    public void InventoryUpdate()
    {
        allHeroes.Clear();
        allHeroes = PlayerInventory.Instance.GetAllHeroes();
        Debug.Log(allHeroes.Count);
        RenderPage(0);
    }
    public void OnCardClicked(int index)
    {
        currentDetailIndex = index;
        RefreshDetailView();
    }

    void RefreshDetailView()
    {
        if (currentDetailIndex < 0 || currentDetailIndex >= allHeroes.Count) return;
        Hero hero = allHeroes[currentDetailIndex];
        heroDetail.ShowHeroData(hero, currentDetailIndex);
    }

    void OnNextDetail()
    {
        if (currentDetailIndex < allHeroes.Count - 1)
        {
            currentDetailIndex++;
            RefreshDetailView();
        }
    }

    void OnPrevDetail()
    {
        if (currentDetailIndex > 0)
        {
            currentDetailIndex--;
            RefreshDetailView();
        }
    }
}