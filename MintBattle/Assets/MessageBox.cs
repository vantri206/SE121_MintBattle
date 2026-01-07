using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class MessageBox : MonoBehaviour
{
    public static MessageBox Instance;

    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private GameObject loadingIcon;
    [SerializeField] private float fadeDuration = 0.2f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
        canvas.enabled = false;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }
    public void ShowLoading(string message)
    {
        messageText.text = message;
        confirmButton.gameObject.SetActive(false);
        loadingIcon.SetActive(true);

        if (!canvas.enabled) FadeIn();
    }

    public void ShowSuccess(string message, Action onConfirm = null)
    {
        messageText.text = message;
        confirmButton.gameObject.SetActive(true);
        loadingIcon.SetActive(false);

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() => {
            Hide(); 
            onConfirm?.Invoke();
        });

        if (!canvas.enabled) FadeIn();
    }
    private void FadeIn()
    {
        canvas.enabled = true;
        StartFade(1f, true);
    }

    public void Hide()
    {
        StartFade(0f, false, () => {
            canvas.enabled = false; 
        });
    }

    private void StartFade(float targetAlpha, bool shouldBlockRaycasts, Action onComplete = null)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        canvasGroup.blocksRaycasts = true;
        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
    }

    private IEnumerator FadeRoutine(float targetAlpha, Action onComplete)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }
}