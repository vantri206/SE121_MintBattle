using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using System.Collections;

public class FrameButtonAnimation : MonoBehaviour, IPointerDownHandler
{
    [Header("Settings")]
    public Image targetImage;      
    public Sprite[] clickFrames;    
    public float frameSpeed = 0.2f; 

    private Sprite originalSprite;   

    private void Awake()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (targetImage != null) originalSprite = targetImage.sprite;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(PlayClickRoutine());
    }
    IEnumerator PlayClickRoutine()
    {
        foreach (var frame in clickFrames)
        {
            targetImage.sprite = frame;
            yield return new WaitForSeconds(frameSpeed);
        }

        targetImage.sprite = originalSprite;
    }
}