using TMPro;
using UnityEngine; 

public class DirectInputHandler : MonoBehaviour
{
    public TMP_InputField inputField; 
    public TMP_Text displayText;     

    void Start()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(HandleInputChange);
        }
    }
    void HandleInputChange(string inputText)
    {
        Debug.Log("ETH input text: " + inputText);

        if (displayText != null)
        {
            displayText.text = inputText;
        }

        Debug.Log("ETH amount text: " + displayText.text);
    }
}
