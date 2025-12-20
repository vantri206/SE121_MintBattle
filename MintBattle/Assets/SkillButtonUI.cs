using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillButtonUI : MonoBehaviour
{
    [Header("Components")]
    public Image iconImage;
    public Image border;
    public GameObject cooldownOverlay; 
    public TextMeshProUGUI cooldownText;

    [Header("Selection Visuals")]
    public GameObject dimOverlay;     
    public GameObject selectionBorder; 
    public bool IsOnCooldown { get; private set; }

    public void Setup(RuntimeSkill skill)
    {
        if (skill.skillData.Image != null)
            iconImage.sprite = skill.skillData.Image;

        IsOnCooldown = skill.currentCooldown > 0;

        if (IsOnCooldown)
        {
            if (cooldownOverlay) cooldownOverlay.SetActive(true);
            if (cooldownText)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = skill.currentCooldown.ToString();
            }
        }
        else
        {
            if (cooldownOverlay) cooldownOverlay.SetActive(false);
            if (cooldownText) cooldownText.gameObject.SetActive(false);
        }
    }
    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            if (dimOverlay) dimOverlay.SetActive(false);
            if (selectionBorder) selectionBorder.SetActive(true);
            iconImage.color = Color.white;
            border.color = Color.white;
        }
        else
        {
            if (dimOverlay) dimOverlay.SetActive(true);
            if (selectionBorder) selectionBorder.SetActive(false);

            transform.localScale = Vector3.one;

            iconImage.color = Color.red;
            border.color = Color.red;
        }
    }
}