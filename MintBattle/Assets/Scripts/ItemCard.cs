using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;

public class ItemCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Components")]
    public Image iconImage;
    public Image borderImage;
    public Image backgroundImage;
    public Canvas canvas;
    [Header("Config Colors")]
    public Color legendColor = new Color(1f, 0.8f, 0f); 

    private ItemData itemData;
    private Vector3 originalScale;
    public Action<ItemData> OnClickAction; 

    private bool isInteractable = true; 

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void Setup(ItemData data, bool interactable)
    {
        this.itemData = data;
        this.isInteractable = interactable;

        if (iconImage) iconImage.sprite = data.icon;

        UpdateRarityColor(data);

        float targetAlpha = interactable ? 1f : 0.5f;
        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (iconImage != null)
        {
            Color c = iconImage.color;
            c.a = alpha;
            iconImage.color = c;
        }

        if (borderImage != null)
        {
            Color c = borderImage.color;
            c.a = alpha;
            borderImage.color = c;
        }

        if (backgroundImage != null)
        {
            Color c = backgroundImage.color;
            c.a = alpha;
            backgroundImage.color = c;
        }
    }

    private void UpdateRarityColor(ItemData data)
    {
        if (borderImage != null)
        {
            Color baseColor = legendColor;
            borderImage.color = baseColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isInteractable)
        {
            transform.DOScale(originalScale * 1.1f, 0.2f).SetEase(Ease.OutBack);
        }

        if (ItemTooltip.Instance != null && itemData != null)
            ItemTooltip.Instance.ShowTooltip(itemData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad);

        if (ItemTooltip.Instance != null)
            ItemTooltip.Instance.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable) return;

        transform.DOScale(originalScale * 1.1f, 0.2f).SetEase(Ease.OutBack);
        OnClickAction?.Invoke(itemData);
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
        if (ItemTooltip.Instance != null)
        {
            ItemTooltip.Instance.HideTooltip();
        }
    }
}