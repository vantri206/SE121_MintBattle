using UnityEngine;
using UnityEngine.UI;
using System;

public class TeamSlotUI : MonoBehaviour
{
    [Header("CONFIG PREFABS")]
    [SerializeField] private GameObject emptyStatePrefab;
    [SerializeField] private GameObject heroCardPrefab;

    [Header("CONTAINER")]
    [SerializeField] private Transform container;

    private int myTeamIdx;
    private int mySlotIdx;
    private Action<int, int> onClickCallback;
    public void Setup(int teamIdx, int slotIdx, string currentHeroId, Action<int, int> onClick)
    {
        this.myTeamIdx = teamIdx;
        this.mySlotIdx = slotIdx;
        this.onClickCallback = onClick;
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        if (!string.IsNullOrEmpty(currentHeroId))
        {
            GameObject cardObj = Instantiate(heroCardPrefab, container);

            HeroCardUI cardUI = cardObj.GetComponent<HeroCardUI>();
            Hero heroData = PlayerInventory.Instance.GetHeroById(currentHeroId); 

            if (cardUI != null && heroData != null)
            {
                cardUI.LoadHero(heroData, 0);
                cardUI.SetClickAction(() =>
                {
                    onClickCallback?.Invoke(myTeamIdx, mySlotIdx);
                });
            }
            else
            {
                //Debug.Log(currentHeroId);
            }
        }
        else
        {
            GameObject emptyObj = Instantiate(emptyStatePrefab, container);

            Button btn = emptyObj.GetComponentInChildren<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onClickCallback?.Invoke(myTeamIdx, mySlotIdx));
            }
        }
    }
}