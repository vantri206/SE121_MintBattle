using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class HeroTeamUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TextMeshProUGUI teamNameText;
    [SerializeField] private Transform slotsContainer;

    [SerializeField] private Button selectTeamButton;

    public void Setup(
        int teamIndex,
        int slotsPerTeam,
        GameObject slotPrefab,
        TeamSelectionUI.TeamSelectionMode mode,
        Action<int, int> onSlotClicked,
        Action<int> onTeamSelectClicked 
    )
    {
        if (teamNameText)
            teamNameText.text = $"TEAM {teamIndex + 1}";
        if (selectTeamButton != null)
        {
            if (mode == TeamSelectionUI.TeamSelectionMode.Inventory)
            {
                selectTeamButton.gameObject.SetActive(false);
            }
            else 
            {
                selectTeamButton.gameObject.SetActive(true);

                selectTeamButton.onClick.RemoveAllListeners();
                selectTeamButton.onClick.AddListener(() =>
                {
                    onTeamSelectClicked?.Invoke(teamIndex);
                });
            }
        }

        foreach (Transform child in slotsContainer) Destroy(child.gameObject);

        for (int slotIndex = 0; slotIndex < slotsPerTeam; slotIndex++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            TeamSlotUI slotUI = slotObj.GetComponent<TeamSlotUI>();

            string heroInSlot = TeamManager.Instance.GetHeroIdInSlot(teamIndex, slotIndex);

            slotUI.Setup(teamIndex, slotIndex, heroInSlot, onSlotClicked);
        }
    }
}