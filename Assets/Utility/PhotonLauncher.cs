using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Linq; 
using System.Collections.Generic; 

// 🌐 FUSION PURE: PhotonLauncher est maintenant persistant (MonoBehaviour + DontDestroyOnLoad)
// Les RPCs sont déplacés vers NetworkUIManager temporaire
public class PhotonLauncher : MonoBehaviour
{
    public static PhotonLauncher Instance { get; private set; }
    [Header("UI References")]
    [SerializeField] private GameObject gameOverUIPrefab;

    [Header("Gestion de déconnexion")]
    [SerializeField] private float autoReconnectDelay = 2f;
    [SerializeField] private string lobbySceneName = "LobbyScene";
    [SerializeField] private GameObject reconnectionNotificationPrefab;
    
    private bool isWaitingForReconnection = false;
    private bool wasDisconnected = false;

    // 🌐 FUSION PURE: RPCs déplacés vers NetworkUIManager temporaire
    // PhotonLauncher persistant ne gère plus les RPCs

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

    // 🌐 FUSION PURE: Méthode obsolète - RPCs gérés par NetworkUIManager
    public static void CallRestartMatchSoft()
    {
        Debug.Log("[PHOTON] CallRestartMatchSoft - utiliser NetworkUIManager.RestartMatchSoftRPC()");
        
        var networkUI = FindFirstObjectByType<NetworkUIManager>();
        if (networkUI != null)
        {
            networkUI.RestartMatchSoftRPC();
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
        
        // 🔧 FUSION: Détruire l'ancien runner et créer un nouveau (éviter la réutilisation)
        var existingRunner = FindObjectOfType<NetworkRunner>();
        if (existingRunner != null)
        {
            Debug.Log("[PHOTON] Found existing NetworkRunner - destroying it to create a fresh one");
            
            if (existingRunner.IsRunning)
            {
                existingRunner.Shutdown();
            }
            
            Destroy(existingRunner.gameObject);
            await System.Threading.Tasks.Task.Delay(100); // Petit délai pour la destruction
        }
        
        // Créer un nouveau NetworkRunner
        GameObject runnerGO = new GameObject("NetworkRunner");
        var newRunner = runnerGO.AddComponent<NetworkRunner>();
        var spawner = runnerGO.AddComponent<BasicSpawner>();
        newRunner.AddCallbacks(spawner);
        
        var scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid) 
        {
            sceneInfo.AddSceneRef(scene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }
        
        var result = await newRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            Scene = sceneInfo,
            PlayerCount = 4 // Max 4 joueurs
        });
        
        if (result.Ok && newRunner.IsRunning)
        {
            Debug.Log($"[PHOTON] Private room created successfully: {roomName}");
            OnJoinedRoomFusion(newRunner);
        }
        else
        {
            Debug.LogError($"[PHOTON] Failed to create private room: {result.ErrorMessage ?? "NetworkRunner not running"}");
        }
    }

    public async void JoinRoomByCode(string code)
    {
        roomName = code.ToUpper();
        Debug.Log($"[PHOTON] Joining room with code: {roomName}");
        
        // 🔧 FUSION: Détruire l'ancien runner et créer un nouveau (éviter la réutilisation)
        var existingRunner = FindObjectOfType<NetworkRunner>();
        if (existingRunner != null)
        {
            Debug.Log("[PHOTON] Found existing NetworkRunner - destroying it to create a fresh one");
            
            if (existingRunner.IsRunning)
            {
                existingRunner.Shutdown();
            }
            
            Destroy(existingRunner.gameObject);
            await System.Threading.Tasks.Task.Delay(100); // Petit délai pour la destruction
        }
        
        // Créer un nouveau NetworkRunner
        GameObject runnerGO = new GameObject("NetworkRunner");
        var newRunner = runnerGO.AddComponent<NetworkRunner>();
        var spawner = runnerGO.AddComponent<BasicSpawner>();
        newRunner.AddCallbacks(spawner);
        
        var scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid) 
        {
            sceneInfo.AddSceneRef(scene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }
        
        var result = await newRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = roomName,
            Scene = sceneInfo
        });
        
        if (result.Ok && newRunner.IsRunning)
        {
            Debug.Log($"[PHOTON] Joined room successfully: {roomName}");
            OnJoinedRoomFusion(newRunner);
        }
        else
        {
            Debug.LogError($"[PHOTON] Failed to join room: {result.ErrorMessage ?? "NetworkRunner not running"}");
            OnJoinRoomFailedFusion(0, result.ErrorMessage ?? "NetworkRunner not running");
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

    private void Awake()
    {
        Debug.Log($"[PHOTON] 🔍 PhotonLauncher.Awake called on GameObject: {gameObject.name}");
        
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"[PHOTON] ✅ PhotonLauncher Instance set to: {gameObject.name}");
            
            // 🔧 FUSION PURE: Marquer cet objet comme persistant entre les sessions
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PHOTON] PhotonLauncher marqué comme persistant avec DontDestroyOnLoad");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"[PHOTON] ❌ DUPLICATE PhotonLauncher detected! Destroying: {gameObject.name} (keeping: {Instance.gameObject.name})");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 🌐 FUSION PURE: Plus besoin de NetworkObject sur PhotonLauncher persistant
        // Les RPCs sont gérés par NetworkUIManager temporaire
        
        Debug.Log("[PHOTON] PhotonLauncher persistant initialisé - prêt pour créer des sessions");
        
        StartCoroutine(ConnectionHeartbeat());
    }
    
    private System.Collections.IEnumerator ConnectionHeartbeat()
    {
        WaitForSeconds wait = new WaitForSeconds(20f); 
        
        while (true)
        {
            yield return wait;
            
            // 🌐 FUSION PURE: Vérifier qu'une session Fusion existe
            var currentRunner = FindFirstObjectByType<NetworkRunner>();
            if (currentRunner != null && currentRunner.IsRunning)
            {
                // 🌐 FUSION PURE: Heartbeat simplifié - plus de RPC nécessaire
                Debug.Log("[PHOTON] Heartbeat - PhotonLauncher persistant actif");
                Debug.Log("[PHOTON] Session Fusion active détectée");
            }
            else
            {
                Debug.Log("[PHOTON] Pas de session active - heartbeat en attente");
            }
        }
    }
    
    // 🌐 FUSION PURE: HeartbeatPingRpc supprimé - plus de RPC dans PhotonLauncher persistant

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
        
        // 🌐 FUSION PURE: Vérifier la session via FindFirstObjectByType
        var activeRunner = FindFirstObjectByType<NetworkRunner>();
        if (activeRunner != null && activeRunner.IsRunning)
        {
            Debug.Log("[PHOTON] Session Fusion active détectée lors de la déconnexion");
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
        Debug.Log($"[PHOTON] {System.DateTime.Now:HH:mm:ss.fff} - OnJoinedRoomFusion called");
        SpawnNetworkUIManager(runner);
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.UpdateRoomStatus($"In Room: {runner.SessionInfo.Name} ({runner.ActivePlayers.Count()}/{runner.SessionInfo.MaxPlayers} players)");
        }
        Debug.Log($"[PHOTON] {System.DateTime.Now:HH:mm:ss.fff} - Starting player list update...");
        StartCoroutine(UpdatePlayerListOnJoin());
        
        // Reset game state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isGameOver = false;
        }
        
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetManager();
        }
        
        Debug.Log("[PHOTON] Tank spawn will be handled by BasicSpawner.OnPlayerJoined() callback");
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
        
        // 🔧 FUSION: En mode Shared, tous les clients peuvent spawner
        // Pas de vérification d'autorité nécessaire en mode Shared
        Debug.Log($"[PhotonLauncher] GameMode: {runner.GameMode}, IsServer: {runner.IsServer}, IsClient: {runner.IsClient}");
        
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
        var startTime = System.DateTime.Now;
        Debug.Log($"[PHOTON] {startTime:HH:mm:ss.fff} - GO BUTTON: Starting join sequence");
        
        // 🎯 ROOM FIXE : Tous les joueurs rejoignent la même room persistante
        string roomName = "MainTankBattleRoom";
        Debug.Log($"[PHOTON] {System.DateTime.Now:HH:mm:ss.fff} - Target room: {roomName}");
        
        // 🔧 ARCHITECTURE FIXE : Utiliser le BasicSpawner existant et créer le NetworkRunner sur le même GameObject
        var basicSpawner = FindFirstObjectByType<BasicSpawner>();
        if (basicSpawner == null)
        {
            Debug.LogError("[PHOTON] BasicSpawner not found! Cannot start Fusion session.");
            return;
        }
        
        // Nettoyer l'ancien runner s'il existe
        var existingRunner = FindObjectOfType<NetworkRunner>();
        if (existingRunner != null)
        {
            Debug.Log($"[PHOTON] {System.DateTime.Now:HH:mm:ss.fff} - Found existing NetworkRunner - shutting down...");
            
            if (existingRunner.IsRunning)
            {
                await existingRunner.Shutdown();
                Debug.Log($"[PHOTON] {System.DateTime.Now:HH:mm:ss.fff} - NetworkRunner shutdown completed");
            }
            
            // Détruire le GameObject NetworkRunner entier (maintenant séparé)
            Destroy(existingRunner.gameObject);
            Debug.Log($"[PHOTON] {System.DateTime.Now:HH:mm:ss.fff} - NetworkRunner GameObject destroyed");
        }
        
        // CRÉER le NetworkRunner sur un GameObject SÉPARÉ pour éviter d'affecter LobbyUI/GameManager
        Debug.Log($"[PHOTON] {System.DateTime.Now:HH:mm:ss.fff} - Creating new NetworkRunner...");
        var runnerObject = new GameObject("NetworkRunner");
        var newRunner = runnerObject.AddComponent<NetworkRunner>();
        newRunner.ProvideInput = true;
        
        // Ajouter NetworkSceneManager sur le même GameObject que NetworkRunner
        var sceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>();
        
        // Attacher BasicSpawner comme callback (BasicSpawner reste sur son GameObject original)
        newRunner.AddCallbacks(basicSpawner);
        Debug.Log($"[PHOTON] {System.DateTime.Now:HH:mm:ss.fff} - NetworkRunner created, starting game...");
        
        // Start new session with fresh runner - pas de scene reload si déjà dans la bonne scène
        var result = await newRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,  // Mode Shared pour rooms persistantes (pas de Host unique)
            SessionName = roomName,
            Scene = null, // Pas de scene reload - on est déjà dans la bonne scène
            PlayerCount = 4
        });
        
        Debug.Log($"[PHOTON] {System.DateTime.Now:HH:mm:ss.fff} - StartGame completed, result: {(result.Ok ? "SUCCESS" : "FAILED")}");
        
        if (result.Ok && newRunner.IsRunning)
        {
            Debug.Log($"[PHOTON] Joined/Created public room successfully");
            OnJoinedRoomFusion(newRunner);
        }
        else
        {
            Debug.LogError($"[PHOTON] Failed to join/create public room: {result.ErrorMessage ?? "NetworkRunner not running"}");
            OnJoinRoomFailedFusion(0, result.ErrorMessage ?? "NetworkRunner not running");
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
    
    // 🌐 FUSION PURE: Ancienne coroutine GameTimerCoroutine supprimée
    // Le timer est maintenant géré par NetworkUIManager (synchronisé réseau)
    
    /// <summary>
    /// 🌐 FUSION: Spawner NetworkUIManager pour la synchronisation UI
    /// </summary>
    private void SpawnNetworkUIManager(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
        {
            Debug.LogWarning("[PHOTON] Cannot spawn NetworkUIManager - NetworkRunner not running");
            return;
        }
        
        // Vérifier si NetworkUIManager existe déjà
        var existingUIManager = FindFirstObjectByType<NetworkUIManager>();
        if (existingUIManager != null)
        {
            Debug.Log("[PHOTON] NetworkUIManager already exists - skipping spawn");
            return;
        }
        
        // Charger le prefab NetworkUIManager depuis Resources
        GameObject uiManagerPrefab = Resources.Load<GameObject>("NetworkUIManager");
        if (uiManagerPrefab == null)
        {
            Debug.LogError("[PHOTON] NetworkUIManager prefab not found in Resources! Creating temporary GameObject...");
            
            // Créer un GameObject temporaire avec NetworkUIManager
            GameObject tempUIManager = new GameObject("NetworkUIManager_Temp");
            tempUIManager.AddComponent<NetworkUIManager>();
            
            // Spawner via Fusion
            var networkObject = tempUIManager.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                networkObject = tempUIManager.AddComponent<NetworkObject>();
            }
            
            var spawnedObject = runner.Spawn(networkObject, Vector3.zero, Quaternion.identity);
            Debug.Log("[PHOTON] 🌐 NetworkUIManager temporaire spawné avec succès");
            
            // Démarrer le timer synchronisé (seulement si on a l'autorité)
            StartNetworkTimer(spawnedObject);
        }
        else
        {
            // Spawner le prefab NetworkUIManager
            var networkObject = uiManagerPrefab.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                var spawnedObject = runner.Spawn(networkObject, Vector3.zero, Quaternion.identity);
                Debug.Log("[PHOTON] 🌐 NetworkUIManager spawné depuis prefab avec succès");
                
                // Démarrer le timer synchronisé (seulement si on a l'autorité)
                StartNetworkTimer(spawnedObject);
            }
            else
            {
                Debug.LogError("[PHOTON] NetworkUIManager prefab missing NetworkObject component!");
            }
        }
    }
    
    /// <summary>
    /// Démarre le timer synchronisé via NetworkUIManager
    /// </summary>
    private void StartNetworkTimer(NetworkObject spawnedUIManager)
    {
        if (spawnedUIManager == null) return;
        
        var networkUIManager = spawnedUIManager.GetComponent<NetworkUIManager>();
        if (networkUIManager != null)
        {
            // Démarrer le timer avec un délai pour s'assurer que l'objet est bien initialisé
            StartCoroutine(StartTimerDelayed(networkUIManager));
        }
        else
        {
            Debug.LogWarning("[PHOTON] NetworkUIManager component not found on spawned object");
        }
    }
    
    /// <summary>
    /// Coroutine pour démarrer le timer avec un petit délai
    /// </summary>
    private System.Collections.IEnumerator StartTimerDelayed(NetworkUIManager networkUIManager)
    {
        // Démarrer le timer immédiatement - pas besoin d'attendre
        networkUIManager.StartMatchTimer(300f);
        Debug.Log("[PHOTON] ⏰ Timer synchronisé démarré via NetworkUIManager");
        yield break;
    }
    
    /// <summary>
    /// Coroutine pour mettre à jour la PlayerList dès l'arrivée dans la room
    /// </summary>
    private System.Collections.IEnumerator UpdatePlayerListOnJoin()
    {
        // Attendre seulement une frame pour que NetworkUIManager soit spawné
        yield return null;
        
        // Trouver le NetworkUIManager spawné
        var networkUIManager = FindFirstObjectByType<NetworkUIManager>();
        if (networkUIManager != null)
        {
            Debug.Log("[PHOTON] 📝 Mise à jour de la PlayerList dès l'arrivée dans la room");
            networkUIManager.UpdatePlayerList();
        }
        else
        {
            Debug.LogWarning("[PHOTON] ⚠️ NetworkUIManager non trouvé pour mettre à jour la PlayerList");
        }
    }
}