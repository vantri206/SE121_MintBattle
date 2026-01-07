using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class JuicyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.1f;   
    public float clickScale = 0.95f; 
    public bool isPulsing = true;     

    private Vector3 originalScale;
    private Tween pulseTween;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void Start()
    {
        if (isPulsing)
        {
            StartPulsing();
        }
    }

    private void StartPulsing()
    {
        pulseTween = transform.DOScale(originalScale * 1.05f, 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (pulseTween != null) pulseTween.Pause();

        transform.DOScale(originalScale * hoverScale, 0.2f).SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad)
            .OnComplete(() => {
                if (isPulsing && pulseTween != null) pulseTween.Play();
            });
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOScale(originalScale * clickScale, 0.1f).SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOScale(originalScale * hoverScale, 0.1f).SetEase(Ease.OutBack);
    }

    private void OnDisable()
    {
        transform.localScale = originalScale; 
    }
}