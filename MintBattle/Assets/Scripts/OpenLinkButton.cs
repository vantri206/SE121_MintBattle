using UnityEngine;

public class OpenLinkButton : MonoBehaviour
{
    public string urlToOpen;

    public void OpenLink()
    {
        if (!string.IsNullOrEmpty(urlToOpen))
        {
            Application.OpenURL(urlToOpen);
        }
        else
        {
            Debug.LogWarning("Fill Link On Inspector!");
        }
    }
}