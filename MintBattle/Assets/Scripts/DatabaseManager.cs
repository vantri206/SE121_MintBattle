using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}

