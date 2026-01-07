using UnityEngine;
using UnityEngine.UI;

public class FrameAnimator : MonoBehaviour
{
    public Image image;
    [Header("Settings")]
    public Sprite[] frames;
    public float frameRate = 90f;

    private float timer = 0;
    private int currentFrameIndex;

    void Start()
    {
        if(image == null)
            image = GetComponent<Image>();
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= 1f / frameRate)
        {
            timer -= 1f / frameRate; 

            currentFrameIndex++;
            if (currentFrameIndex >= frames.Length)
            {
                currentFrameIndex = 0;
            }

            image.sprite = frames[currentFrameIndex];
        }
    }
}