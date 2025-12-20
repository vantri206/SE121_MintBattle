using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("Settings")]
    public float amplitude = 0.3f; 
    public float frequency = 2f; 

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;

        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}