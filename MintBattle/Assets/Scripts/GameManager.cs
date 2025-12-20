using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Application.runInBackground = true;

        DontDestroyOnLoad(gameObject);
    }

    public void StartPlayScene()
    {
        //SceneManager.LoadScene("PlayScene");
        SceneManager.LoadScene("MainScene");
    }
    public void QuitGame()
    {
        Console.Write("QuitGame");
        Application.Quit();
    }    
}

