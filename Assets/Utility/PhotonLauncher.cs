using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Linq; 
using System.Collections.Generic; 

public class PhotonLauncher : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverUIPrefab;

    [Header("Gestion de déconnexion")]
    [SerializeField] private float autoReconnectDelay = 2f;
    [SerializeField] private string lobbySceneName = "LobbyScene";
    [SerializeField] private GameObject reconnectionNotificationPrefab;
    
    private bool isWaitingForReconnection = false;
    private bool wasDisconnected = false;

    // private List<RoomInfo> cachedRoomList = new List<RoomInfo>(); // RoomInfo not available in Fusion

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RestartMatchSoftRPC()
    {
        foreach (var ui in GameObject.FindGameObjectsWithTag("GameOverUI"))
        {
            Destroy(ui);
        }

        var minimapCam = FindObjectOfType<MinimapCamera>();
        if (minimapCam != null)
        {
            minimapCam.ForceReset();
        }

        TankHealth2D myTank = null;
        foreach (var t in FindObjectsOfType<TankHealth2D>())
        {
            if (t.Object)
            {
                myTank = t;
                break;
            }
        }
        if (myTank != null)
        {
            Runner.Despawn(myTank.Object);
        }

        var spawner = FindObjectOfType<PhotonTankSpawner>();
        if (spawner != null)
        {
            spawner.SpawnTank();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void ShowWinnerToAllRPC(string winnerName, int winnerActorNumber)
    {
        
        bool isWinner = Runner.LocalPlayer.PlayerId == winnerActorNumber;
        
        GameObject prefabToUse = gameOverUIPrefab;
        if (prefabToUse == null)
        {
            var tankHealth = FindObjectOfType<TankHealth2D>();
            if (tankHealth != null)
            {
                var field = typeof(TankHealth2D).GetField("gameOverUIPrefab", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    prefabToUse = field.GetValue(tankHealth) as GameObject;
                }
            }
        }
        
        Camera mainCam = Camera.main;
        if (mainCam != null && prefabToUse != null)
        {
            GameObject uiInstance = Instantiate(prefabToUse, mainCam.transform);
            RectTransform rt = uiInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localPosition = new Vector3(0f, 0f, 1f);
                rt.localRotation = Quaternion.identity;
                float baseScale = 1f;
                float dist = Vector3.Distance(mainCam.transform.position, rt.position);
                float scaleFactor = baseScale * (dist / mainCam.orthographicSize) * 0.1f;
                rt.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            }
            
            var controller = uiInstance.GetComponent<GameOverUIController>();
            if (controller != null)
            {
                if (isWinner)
                {
                    controller.ShowWin(winnerName);
                }
                else
                {
                    controller.ShowWinner(winnerName);
                }
                
                StartCoroutine(ReturnToLobbyAfterDelay(6));
            }
            
            StartCoroutine(AutoDestroyAndRestart(uiInstance));
        }
    }

    private System.Collections.IEnumerator ReturnToLobbyAfterDelay(int seconds)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameOver();
        }
        
        yield return new WaitForSeconds(seconds);
        
        LobbyUI lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.OnBackToLobby();
        }
        else
        {
            Debug.LogError("[PHOTON] LobbyUI non trouvé !");
        }
    }
    
    private System.Collections.IEnumerator AutoDestroyAndRestart(GameObject uiInstance)
    {
        yield return new WaitForSeconds(3f);
        if (uiInstance != null)
        {
            Destroy(uiInstance);
        }
        CallRestartMatchSoft();
    }

    public static void CallRestartMatchSoft()
    {
        var launcher = FindObjectOfType<PhotonLauncher>();
        if (launcher != null)
        {
            if (launcher.Object != null)
            {
                launcher.RestartMatchSoftRPC();
            }
            else
            {
                Debug.LogError("[PhotonLauncher] PhotonView manquant sur PhotonLauncher !");
            }
        }
        else
        {
            Debug.LogError("[PhotonLauncher] Impossible de trouver PhotonLauncher pour le reset soft!");
        }
    }

    public bool isConnectedAndReady = false;

    [Header("Room Settings")]
    public string roomName = "";
    public byte maxPlayers =10;

    public LobbyUI lobbyUI;

    private static readonly string chars = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789";
    private System.Random rng = new System.Random();

    public string GenerateRoomCode()
    {
        char[] code = new char[4];
        for (int i = 0; i < 4; i++)
        {
            code[i] = chars[rng.Next(chars.Length)];
        }
        return new string(code);
    }

    public async void CreatePrivateRoom()
    {
        roomName = GenerateRoomCode();
        Debug.Log($"[PHOTON] Creating private room with code: {roomName}");
        
        // Créer une session Fusion avec le code comme nom de room
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            var scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            var sceneInfo = new NetworkSceneInfo();
            if (scene.IsValid) 
            {
                sceneInfo.AddSceneRef(scene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            }
            
            var result = await runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Host,
                SessionName = roomName,
                Scene = sceneInfo,
                PlayerCount = 4 // Max 4 joueurs
            });
            
            if (result.Ok)
            {
                Debug.Log($"[PHOTON] Private room created successfully: {roomName}");
                OnJoinedRoomFusion(runner);
            }
            else
            {
                Debug.LogError($"[PHOTON] Failed to create private room: {result.ErrorMessage}");
            }
        }
    }

    public async void JoinRoomByCode(string code)
    {
        roomName = code.ToUpper();
        Debug.Log($"[PHOTON] Joining room with code: {roomName}");
        
        // Rejoindre une session Fusion avec le code
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            var scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            var sceneInfo = new NetworkSceneInfo();
            if (scene.IsValid) 
            {
                sceneInfo.AddSceneRef(scene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            }
            
            var result = await runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Client,
                SessionName = roomName,
                Scene = sceneInfo
            });
            
            if (result.Ok)
            {
                Debug.Log($"[PHOTON] Joined room successfully: {roomName}");
                OnJoinedRoomFusion(runner);
            }
            else
            {
                Debug.LogError($"[PHOTON] Failed to join room: {result.ErrorMessage}");
                OnJoinRoomFailedFusion(0, result.ErrorMessage);
            }
        }
    }

    public void SetPlayerName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            // Fusion nickname handled differently
        }
        else
        {
            // Fusion nickname handled differently
        }
    }

    private void Start()
    {
        if (GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("[PhotonLauncher] NetworkObject manquant sur l'objet PhotonLauncher ! Merci d'ajouter un NetworkObject dans l'inspecteur AVANT de lancer la scène.");
        }
        
        if (Runner == null || !Runner.IsConnectedToServer)
        {
            
            // Fusion connection settings handled differently 
            // Fusion ping settings handled differently 
            // Fusion keep alive handled differently 
            
            // Fusion connection handled differently
        }
        
        StartCoroutine(ConnectionHeartbeat());
    }
    
    private System.Collections.IEnumerator ConnectionHeartbeat()
    {
        WaitForSeconds wait = new WaitForSeconds(20f); 
        
        while (true)
        {
            yield return wait;
            
            if (Runner != null && Runner.IsConnectedToServer)
            {
                
                if (Runner != null && Runner.IsConnectedToServer)
                {
                    HeartbeatPingRpc();
                }
            }
        }
    }
    
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void HeartbeatPingRpc()
    {
        // ...
    }

    // OnConnectedToMaster removed for Fusion
    public void OnConnectedToMasterFusion()
    {
        isConnectedAndReady = true;
        wasDisconnected = false; 
        
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.OnPhotonReady();
        }
        else
        {
            Debug.LogError("[PHOTON LAUNCHER] lobbyUI est null dans OnConnectedToMaster !");
        }
    }

    // OnDisconnected removed for Fusion
    public void OnDisconnectedFusion()
    {
        
        wasDisconnected = true;
        isConnectedAndReady = false;
        
        ShowReconnectionNotification();
        
        StartCoroutine(ReturnToLobby());
    }
    
    private void ShowReconnectionNotification()
    {
        if (reconnectionNotificationPrefab != null)
        {
            GameObject notif = Instantiate(reconnectionNotificationPrefab);
            Destroy(notif, 3f);
        }
        else
        {
            Debug.LogWarning("[PhotonLauncher] reconnectionNotificationPrefab non assigné");
        }
    }
    
    private System.Collections.IEnumerator ReturnToLobby()
    {
        yield return new WaitForSeconds(autoReconnectDelay);
        
        if (Runner != null && Runner.IsConnectedToServer)
        {
            // Fusion disconnect handled differently
        }
        
        LobbyUI lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.OnBackToLobby();
        }
        else
        {
            Debug.LogWarning("[PHOTON] LobbyUI non trouvé pour le retour au lobby après déconnexion");
        }
    }

    // OnJoinedRoom removed for Fusion
    public void OnJoinedRoomFusion(NetworkRunner runner)
    {
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            // Fusion room name handled differently
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isGameOver = false;
        }
        
        if (ScoreManager.Instance != null) 
        {
            ScoreManager.Instance.ResetManager();
        }
        
        // Spawn tank directly using the provided NetworkRunner
        SpawnTankFusion(runner);
    }

    private void SpawnTankFusion(NetworkRunner runner)
    {
        Debug.Log("[PhotonLauncher] SpawnTankFusion() called");
        
        if (runner == null)
        {
            Debug.LogError("[PhotonLauncher] NetworkRunner is null, cannot spawn tank");
            return;
        }
        
        Debug.Log($"[PhotonLauncher] NetworkRunner found: True, IsRunning: {runner.IsRunning}, IsServer: {runner.IsServer}, GameMode: {runner.GameMode}");
        
        if (!runner.IsRunning)
        {
            Debug.LogWarning("[PhotonLauncher] NetworkRunner is not running, cannot spawn tank");
            return;
        }
        
        if (!runner.IsServer)
        {
            Debug.LogWarning("[PhotonLauncher] NetworkRunner is not server, cannot spawn tank");
            return;
        }
        
        // Vérifier si le match est terminé avant de spawner un tank
        if (ScoreManager.Instance != null && ScoreManager.Instance.IsMatchEnded())
        {
            Debug.Log("[PhotonLauncher] Match ended, not spawning tank");
            return;
        }
        
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            Debug.Log("[PhotonLauncher] Game over, not spawning tank");
            return;
        }
        
        // Trouver les spawn points
        Vector2 spawnPos = Vector2.zero;
        var spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIdx = UnityEngine.Random.Range(0, spawnPoints.Length);
            spawnPos = spawnPoints[spawnIdx].transform.position;
            
            // Ajouter un petit offset aléatoire
            float offsetX = UnityEngine.Random.Range(-0.5f, 0.5f);
            float offsetY = UnityEngine.Random.Range(-0.5f, 0.5f);
            spawnPos += new Vector2(offsetX, offsetY);
        }
        else
        {
            Debug.LogWarning("[PhotonLauncher] No spawn points found, using default position");
        }

        // Charger le prefab tank depuis Resources
        GameObject tankPrefab = Resources.Load<GameObject>("TankPlayer");
        if (tankPrefab == null)
        {
            Debug.LogError("[PhotonLauncher] TankPlayer prefab not found in Resources!");
            return;
        }
        
        // Spawner le tank via Fusion
        Debug.Log($"[PhotonLauncher] Spawning tank at position {spawnPos} for player {runner.LocalPlayer}");
        var tankNetworkObject = runner.Spawn(tankPrefab.GetComponent<NetworkObject>(), spawnPos, Quaternion.identity, runner.LocalPlayer);
        
        if (tankNetworkObject != null)
        {
            Debug.Log("✅ [PhotonLauncher] Tank spawned successfully!");
            GameObject tank = tankNetworkObject.gameObject;
            
            var nameDisplay = tank.GetComponent<PlayerNameDisplay>();
            if (nameDisplay != null)
            {
                Debug.Log("[PhotonLauncher] PlayerNameDisplay found and configured for " + runner.LocalPlayer.ToString());
            }
            else
            {
                Debug.LogWarning("[PhotonLauncher] PlayerNameDisplay not found on tank prefab");
            }
        }
        else
        {
            Debug.LogError("[PhotonLauncher] Failed to spawn tank!");
        }
    }

    // OnJoinRoomFailed removed for Fusion
    public void OnJoinRoomFailedFusion(short returnCode, string message)
    {
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.OnJoinRoomFailedUI();
        }
    }

    // OnPlayerEnteredRoom removed for Fusion
    public void OnPlayerEnteredRoomFusion()
    {
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.HideWaitingForPlayerTextIfRoomFull();
            lobbyUI.UpdatePlayerList();
        }
    }

    // OnPlayerLeftRoom removed for Fusion
    public void OnPlayerLeftRoomFusion()
    {
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.ShowWaitingForPlayerTextIfNotFull();
            lobbyUI.UpdatePlayerList();
        }
    }

    public async void JoinRandomPublicRoom()
    {
        string roomName = "PublicRoom_" + UnityEngine.Random.Range(1000, 9999);
        Debug.Log($"[PHOTON] Joining/Creating public room: {roomName}");
        
        // Use existing NetworkRunner (prepared by BasicSpawner but not started)
        var existingRunner = FindObjectOfType<NetworkRunner>();
        if (existingRunner != null)
        {
            Debug.Log("[PHOTON] Using existing NetworkRunner for public room");
            
            // If already in a session, leave it first
            if (existingRunner.IsRunning && (existingRunner.IsServer || existingRunner.IsClient))
            {
                Debug.Log("[PHOTON] Leaving current session to join public room...");
                await existingRunner.Shutdown();
                
                // Wait a bit for clean shutdown
                await System.Threading.Tasks.Task.Delay(500);
            }
            
            // Start new session with existing runner
            var scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            var sceneInfo = new NetworkSceneInfo();
            if (scene.IsValid) 
            {
                sceneInfo.AddSceneRef(scene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            }
            
            var result = await existingRunner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = roomName,
                Scene = sceneInfo,
                PlayerCount = 4
            });
            
            if (result.Ok && existingRunner.IsRunning)
            {
                Debug.Log($"[PHOTON] Joined/Created public room successfully");
                OnJoinedRoomFusion(existingRunner);
            }
            else
            {
                Debug.LogError($"[PHOTON] Failed to join/create public room: {result.ErrorMessage ?? "NetworkRunner not running"}");
                OnJoinRoomFailedFusion(0, result.ErrorMessage ?? "NetworkRunner not running");
            }
        }
        else
        {
            Debug.LogError("[PHOTON] No NetworkRunner found or not running");
            OnJoinRoomFailedFusion(0, "No NetworkRunner found");
        }
    }

    // OnRoomListUpdate removed for Fusion
    public void OnRoomListUpdateFusion()
    {
        // cachedRoomList = roomList; // RoomInfo not available in Fusion
    }


    public void JoinOrCreatePublicRoom()
    {
        JoinRandomPublicRoom();
    }
}