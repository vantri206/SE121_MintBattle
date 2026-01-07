using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;

public class MatchmakingManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static MatchmakingManager Instance;

    public string menuSceneName = "MainScene";
    public string battleSceneName = "BattleScene";

    [Header("Settings")]
    public float searchTimeout = 5.0f;

    private NetworkRunner _runner;
    private bool _isMatchmaking = false;
    private bool _foundRoom = false;
    private float _timer = 0f;

    public Action<string> OnStatusChanged;
    public Action<bool> OnMatchmakingStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void StartMatchmaking()
    {
        await CleanupRunner();

        GameObject runnerObj = new GameObject("Session_Runner");
        runnerObj.transform.SetParent(transform); 

        _runner = runnerObj.AddComponent<NetworkRunner>();

        runnerObj.AddComponent<NetworkSceneManagerDefault>();

        _foundRoom = false;
        _timer = 0;
        _isMatchmaking = false;

        UpdateStatus("Connecting to Lobby...");


        _runner.AddCallbacks(this);

        var result = await _runner.JoinSessionLobby(SessionLobby.Shared);

        if (result.Ok)
        {
            UpdateStatus("In Lobby. Scanning for rooms...");
            _isMatchmaking = true;
            OnMatchmakingStateChanged?.Invoke(true);
        }
        else
        {
            UpdateStatus($"Failed to join Lobby: {result.ShutdownReason}");
            _isMatchmaking = false;
            OnMatchmakingStateChanged?.Invoke(false);

            await CleanupRunner();
        }
    }

    private async Task CleanupRunner()
    {
        if (_runner != null)
        {
            if (_runner.IsRunning)
            {
                await _runner.Shutdown();
            }

            if (_runner.gameObject != null)
            {
                Destroy(_runner.gameObject);
            }

            _runner = null;
        }
    }

    private void Update()
    {
        if (_isMatchmaking && !_foundRoom)
        {
            _timer += Time.deltaTime;
            if (Time.frameCount % 60 == 0) UpdateStatus($"Scanning... {(int)(searchTimeout - _timer)}s");

            if (_timer >= searchTimeout)
            {
                CreateRoom();
            }
        }
    }

    public void CancelMatchmaking()
    {
        _isMatchmaking = false;
        UpdateStatus("Cancelled.");
        DisconnectAndReturnToMenu(); 
    }


    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (!_isMatchmaking || _foundRoom) return;

        SessionInfo bestRoom = null;
        foreach (var session in sessionList)
        {
            if (session.IsOpen && session.PlayerCount < 2)
            {
                bestRoom = session;
                break;
            }
        }

        if (bestRoom != null)
        {
            _foundRoom = true;
            UpdateStatus($"Found room: {bestRoom.Name}. Joining...");
            JoinRoom(bestRoom.Name);
        }
    }

    void CreateRoom()
    {
        _foundRoom = true;
        UpdateStatus("No room found. Creating new room...");
        string randomName = "Arena_" + System.Guid.NewGuid().ToString().Substring(0, 5);
        JoinRoom(randomName);
    }

    async void JoinRoom(string roomName)
    {
        _isMatchmaking = false;
        Debug.Log($"--- Starting Game: {roomName} ---");

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = roomName,
            PlayerCount = 2,
            Scene = SceneRef.FromIndex(currentSceneIndex),
            SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            UpdateStatus("Joined Room! Waiting for opponent...");
        }
        else
        {
            UpdateStatus($"Join Failed: {result.ShutdownReason}");
            _foundRoom = false;
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.SessionInfo.PlayerCount == 2)
        {
            UpdateStatus("Opponent Found! Starting Battle...");
            if (runner.IsSharedModeMasterClient)
            {
                LoadBattleScene();
            }
        }
    }

    void LoadBattleScene()
    {
        int battleIndex = SceneUtility.GetBuildIndexByScenePath($"Scenes/{battleSceneName}");
        if (battleIndex == -1) battleIndex = SceneUtility.GetBuildIndexByScenePath(battleSceneName);

        if (battleIndex >= 0)
        {
            _runner.LoadScene(SceneRef.FromIndex(battleIndex));
        }
        else
        {
            Debug.LogError($"Can't find scene: {battleSceneName}");
        }
    }

    public void DisconnectAndReturnToMenu()
    {
        if (_runner != null)
        {
            UpdateStatus("Shutting down runner...");
            _runner.Shutdown();
        }
        else
        {
            if (SceneManager.GetActiveScene().name != menuSceneName)
                SceneManager.LoadScene(menuSceneName);
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        UpdateStatus($"Shutdown: {shutdownReason}");
        _isMatchmaking = false;
        OnMatchmakingStateChanged?.Invoke(false);
        _foundRoom = false;
        _timer = 0;
        if (_runner != null && _runner.gameObject != null)
        {
            Destroy(_runner.gameObject);
        }
        _runner = null;

        if (SceneManager.GetActiveScene().name != menuSceneName)
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == battleSceneName || SceneManager.GetActiveScene().path.Contains(battleSceneName))
        {
            OnMatchmakingStateChanged?.Invoke(false);
        }
    }

    void UpdateStatus(string msg)
    {
        Debug.Log($"[Matchmaking] {msg}");
        OnStatusChanged?.Invoke(msg);
    }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { UpdateStatus($"Disconnected: {reason}"); }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}