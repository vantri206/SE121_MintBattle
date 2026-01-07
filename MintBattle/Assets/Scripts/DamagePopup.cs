using UnityEngine;
using TMPro; 

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private Color textColor;
    private float disappearTimer;
    private const float disappearTime = 1.0f; 

    private Vector3 moveVector;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(int damageAmount, bool isHeal)
    {
        string rawText = damageAmount.ToString();
        string spriteText = "";

        if (isHeal) spriteText += "<sprite name=\"+\">";

        foreach (char c in rawText)
        {
            spriteText += $"<sprite name=\"{c}\">";
        }

        textMesh.text = spriteText;
        if (isHeal)
        {
            textMesh.fontSize = 6;
            textColor = Color.green;
        }
        else
        {
            textMesh.fontSize = 6;
            textColor = Color.red;
        }

        textMesh.color = textColor;
        disappearTimer = disappearTime;

        moveVector = new Vector3(0f, 0.5f) * 1.0f;
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }

    private void Update()
    {
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 1.0f * Time.deltaTime;

        textMesh.color = textColor;

        disappearTimer -= Time.deltaTime;

        if (textColor.a < 0 || disappearTimer < 0)
        {
            Destroy(gameObject);
        }
    }
}