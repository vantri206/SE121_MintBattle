using UnityEngine;
using System.Collections.Generic;

public class BattleSceneDebugger : MonoBehaviour
{
    void Awake()
    {
#if UNITY_EDITOR
        SetupFakeDataIfNeeded();
#endif
    }
#if UNITY_EDITOR
    void SetupFakeDataIfNeeded()
    {
        if (PlayerProfile.Instance == null)
        {
            Debug.LogWarning("Battle Debug Test:");
            GameObject ppObj = new GameObject("Player_Test");
            var pp = ppObj.AddComponent<PlayerProfile>();
            pp.OnWalletConnected("0x00");
        }
    }
}
#endif