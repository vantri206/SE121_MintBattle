using UnityEngine;
using TMPro;

public class PingPongEffect : MonoBehaviour
{
    [Header("Settings")]
    public float moveDistance = 10f;
    public float moveSpeed = 5f;   

    public Vector2 direction = new Vector2(1, 0);

    private RectTransform rectTransform;
    private Vector2 startPos;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
        rectTransform.anchoredPosition = startPos + (direction * offset);
    }
}