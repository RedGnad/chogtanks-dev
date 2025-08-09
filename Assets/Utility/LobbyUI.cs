using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Text;
using System.Collections.Generic;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance { get; private set; }
    
    [Header("UI Elements")]
    public TMP_InputField playerNameInput;
    public TMP_Text waitingForPlayerText;
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button goButton; 
    public TMP_InputField joinCodeInput;
    public TMP_Text createdCodeText;
    public GameObject joinPanel;
    public GameObject waitingPanel;
    public TMP_Text playerListText;
    public Button backButton;
    public TMP_Text killFeedText;
    
    [Header("Player Name Display")]
    public TMP_Text mainScreenPlayerNameText; 
    
    [Header("Match UI")]
    public TMP_Text timerText;
    public TMP_Text roomStatusText;
    public TMP_Text roomStatusTextBig; 
    public Button shieldButton; 
    public TMP_Text shieldCooldownText; 
    public bool showShieldCountdownText = true;
    
    [Header("Loading Panel")]
    public GameObject loadingPanel;

    private PhotonLauncher launcher;
    private bool isShieldCooldownActive = false; 
    private string shieldDefaultText = ""; 

    private void Awake()
    {
        Debug.Log($"[LOBBY] 🔍 LobbyUI.Awake called on GameObject: {gameObject.name}");
        
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"[LOBBY] ✅ LobbyUI Instance set to: {gameObject.name}");
            
            // 🔧 FUSION: Marquer cet objet comme persistant entre les sessions
            DontDestroyOnLoad(gameObject);
            Debug.Log("[LOBBY] LobbyUI marqué comme persistant avec DontDestroyOnLoad");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"[LOBBY] ❌ DUPLICATE LobbyUI detected! Destroying: {gameObject.name} (keeping: {Instance.gameObject.name})");
            Destroy(gameObject);
        }
        
    }
    
    void Start()
    {
        // 🌐 FUSION PURE: Utiliser PhotonLauncher.Instance persistant
        launcher = PhotonLauncher.Instance;
        if (launcher == null)
        {
            Debug.LogWarning("[LOBBY] PhotonLauncher.Instance not found - will retry on button click");
        }
        
        // 🌐 FUSION: S'abonner aux events NetworkUIManager
        SubscribeToNetworkEvents();
        
        if (backButton != null)
            backButton.onClick.AddListener(OnBackToLobby);
        
        createRoomButton.onClick.AddListener(OnCreateRoom);
        joinRoomButton.onClick.AddListener(OnJoinRoom);
        
        if (shieldButton != null)
        {
            shieldButton.onClick.AddListener(OnShieldButtonClicked);
        }
        
        if (goButton != null) {
            goButton.onClick.AddListener(OnGoButtonClicked);
            // ✅ TEMPORAIRE : Bouton toujours activé pour les tests
            goButton.interactable = true;
            var goText = goButton.GetComponentInChildren<TMP_Text>();
            if (goText != null) goText.text = "Brawl";
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        if (playerNameInput != null)
        {
            playerNameInput.onEndEdit.AddListener(OnPlayerNameEndEdit);
        }

        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
        
        createdCodeText.text = "";
        
        string defaultName = "Newbie_" + Random.Range(100, 999);
        if (playerNameInput != null)
        {
            playerNameInput.text = defaultName;
        }
        OnPlayerNameEndEdit(defaultName);

        if (playerListText != null)
        {
            playerListText.text = "";
        }
        
        if (timerText != null)
        {
            timerText.text = "";
        }
        
        if (roomStatusText != null)
        {
            roomStatusText.text = "";
        }
        
        if (roomStatusTextBig != null)
        {
            roomStatusTextBig.text = "";
        }
        
        if (shieldCooldownText != null)
        {
            shieldDefaultText = shieldCooldownText.text;
            
            shieldCooldownText.gameObject.SetActive(true);
        }
        
        UpdateMainScreenPlayerName();
    }
    
    void Update()
    {
        MonitorShieldState();
        
        if (Application.isMobilePlatform)
        {
            CheckAndEnforceOrientation();
        }
    }
    
    private void CheckAndEnforceOrientation()
    {
        if (Screen.orientation == ScreenOrientation.Portrait || 
            Screen.orientation == ScreenOrientation.PortraitUpsideDown)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
    }
    
    private void MonitorShieldState()
    {
        if (isShieldCooldownActive) return;
        
        var localTank = FindLocalPlayerTank();
        if (localTank != null)
        {
            var tankShield = localTank.GetComponent<TankShield>();
            if (tankShield != null && shieldButton != null)
            {
                if (tankShield.IsShieldActive() && shieldButton.interactable)
                {
                    StartCoroutine(ShieldCooldownCountdown());
                }
            }
        }
    }
    
    private void OnEnable()
    {
        // Fusion callbacks handled differently
    }
    
    private void OnDisable()
    {
        // Fusion callbacks handled differently
    }

    private void OnPlayerNameEndEdit(string newName)
    {
        if (launcher == null) return;
        
        string playerName = newName.Trim();
        
        if (string.IsNullOrEmpty(playerName) || playerName.Length < 2)
        {
            if(createdCodeText != null) createdCodeText.text = "Name must be at least 2 characters.";
            playerName = "Newbie_" + Random.Range(100, 999);
            if (playerNameInput != null) playerNameInput.text = playerName;
        }
        else if (playerName.Length > 20)
        {
            if(createdCodeText != null) createdCodeText.text = "Name cannot exceed 20 characters.";
            playerName = playerName.Substring(0, 20);
            if (playerNameInput != null) playerNameInput.text = playerName;
        }
        
        launcher.SetPlayerName(playerName);
        
        UpdateMainScreenPlayerName();
        
        if (launcher.isConnectedAndReady)
        {
            OnPhotonReady();
        }
    }

    void OnCreateRoom()
    {
        launcher.CreatePrivateRoom();
        joinPanel.SetActive(false);
        waitingPanel.SetActive(true);
    }

    void OnJoinRoom()
    {
        string code = joinCodeInput.text.Trim().ToUpper();
        if (code.Length == 4)
        {
            launcher.JoinRoomByCode(code);
            joinPanel.SetActive(false);
            waitingPanel.SetActive(true);
        }
        else
        {
            createdCodeText.text = "Invalid code (must be 4 characters)";
        }
    }

    void OnGoButtonClicked()
    {
        Debug.Log($"[LOBBY] {System.DateTime.Now:HH:mm:ss.fff} - GO BUTTON CLICKED - starting join sequence");
        
        // 🌐 FUSION PURE: Utiliser PhotonLauncher.Instance persistant
        launcher = PhotonLauncher.Instance;
        if (launcher != null)
        {
            launcher.JoinRandomPublicRoom();
        }
        else
        {
            Debug.LogError("[LOBBY] PhotonLauncher.Instance not found! Architecture problem.");
            return;
        }
        
        // Masquer les panels UI
        joinPanel.SetActive(false);
        waitingPanel.SetActive(true);
        
        if (createdCodeText != null)
            createdCodeText.text = "Joining public room...";
            
        if (goButton != null) {
            // ✅ TEMPORAIRE : Bouton reste activé pendant les tests
            // goButton.interactable = false;
            var goText = goButton.GetComponentInChildren<TMP_Text>();
            if (goText != null) goText.text = "JOINING...";
        }
    }
    
    void StartGame()
    {
        Debug.Log("[LOBBY] Starting Fusion game session");
        
        // Trouver le NetworkRunner (créé par BasicSpawner)
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("[LOBBY] NetworkRunner not found! Cannot start game.");
            return;
        }
        
        Debug.Log($"[LOBBY] Found NetworkRunner: {runner.name}");
        
        // Spawner les tanks pour tous les joueurs connectés
        if (runner.IsServer)
        {
            Debug.Log("[LOBBY] We are server - spawning tanks for all players");
            SpawnTanksForAllPlayers(runner);
        }
        else
        {
            Debug.Log("[LOBBY] We are client - requesting tank spawn");
            // En tant que client, on peut demander au serveur de spawner notre tank
            // ou attendre que le serveur le fasse automatiquement
        }
        
        // Démarrer le timer de jeu si nécessaire
        StartGameTimer();
    }
    
    void SpawnTanksForAllPlayers(NetworkRunner runner)
    {
        Debug.Log($"[LOBBY] Spawning tanks for {runner.ActivePlayers.Count()} players");
        
        // Chercher un prefab de tank dans les ressources
        var tankPrefab = Resources.Load<GameObject>("TankPlayer");
        if (tankPrefab == null)
        {
            // Essayer d'autres noms possibles
            tankPrefab = Resources.Load<GameObject>("TankPrefab");
            if (tankPrefab == null)
            {
                tankPrefab = Resources.Load<GameObject>("Tank");
                if (tankPrefab == null)
                {
                    tankPrefab = Resources.Load<GameObject>("Player");
                }
            }
        }
        
        if (tankPrefab == null)
        {
            Debug.LogError("[LOBBY] Tank prefab not found in Resources! Cannot spawn tanks.");
            return;
        }
        
        Debug.Log($"[LOBBY] Found tank prefab: {tankPrefab.name}");
        
        // Spawner un tank pour chaque joueur
        foreach (var player in runner.ActivePlayers)
        {
            Debug.Log($"[LOBBY] Spawning tank for player: {player}");
            
            // Position de spawn aléatoire
            Vector3 spawnPosition = new Vector3(
                UnityEngine.Random.Range(-5f, 5f), 
                0f, 
                UnityEngine.Random.Range(-5f, 5f)
            );
            
            // Spawner le tank via Fusion
            var tankObject = runner.Spawn(tankPrefab, spawnPosition, Quaternion.identity, player);
            
            // S'assurer que le tank a le tag "Player" pour la caméra
            if (tankObject != null)
            {
                tankObject.tag = "Player";
                Debug.Log($"[LOBBY] Tank spawned for {player}: {tankObject} with tag: {tankObject.tag}");
            }
            else
            {
                Debug.LogError($"[LOBBY] Failed to spawn tank for {player}");
            }
        }
    }
    
    void StartGameTimer()
    {
        Debug.Log("[LOBBY] Starting game timer");
        
        // Trouver le GameManager ou créer la logique de timer
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            Debug.Log("[LOBBY] Found GameManager - starting game");
            // gameManager.StartGame(); // Si cette méthode existe
        }
        else
        {
            Debug.Log("[LOBBY] GameManager not found - implementing basic timer");
            // Implémenter un timer basique si nécessaire
        }
    }
    
    void OnShieldButtonClicked()
    {
        
        if (isShieldCooldownActive)
        {
            return;
        }
        
        var localTank = FindLocalPlayerTank();
        if (localTank != null)
        {
            var tankShield = localTank.GetComponent<TankShield>();
            if (tankShield != null)
            {
                if (tankShield.CanUseShield() && !tankShield.IsShieldActive())
                {
                    tankShield.ActivateShield();
                    
                    if (shieldButton != null)
                    {
                        StartCoroutine(ShieldCooldownCountdown());
                    }
                }
                else
                {
                    
                    if (shieldButton != null)
                    {
                        StartCoroutine(FlashButtonUnavailable());
                    }
                }
            }
            else
            {
                Debug.LogWarning("[SHIELD BUTTON] Composant TankShield non trouvé sur le tank local");
            }
        }
        else
        {
            Debug.LogWarning("[SHIELD BUTTON] Tank local non trouvé");
        }
    }
    
    private System.Collections.IEnumerator ShieldCooldownCountdown()
    {
        if (shieldButton == null) yield break;
        
        isShieldCooldownActive = true;
        
        shieldButton.interactable = false;
        var buttonImage = shieldButton.GetComponent<Image>();
        var originalColor = buttonImage != null ? buttonImage.color : Color.white;
        
        if (buttonImage != null)
        {
            buttonImage.color = Color.gray;
        }
        
        if (shieldCooldownText != null)
        {
            shieldCooldownText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[SHIELD COUNTDOWN] shieldCooldownText est null ! Vérifiez l'assignation dans l'Inspector");
        }
        
        for (int countdown = 8; countdown >= 1; countdown--)
        {
            if (shieldCooldownText != null)
            {
                if (showShieldCountdownText)
                {
                    shieldCooldownText.text = countdown.ToString();
                }
                else
                {
                    shieldCooldownText.text = " ";
                }
            }
            yield return new WaitForSeconds(1f);
        }
        
        if (shieldCooldownText != null)
        {
            shieldCooldownText.text = shieldDefaultText;
        }
        
        shieldButton.interactable = true;
        if (buttonImage != null)
        {
            buttonImage.color = originalColor;
        }
        
        isShieldCooldownActive = false;
        
    }
    
    private System.Collections.IEnumerator FlashButtonUnavailable()
    {
        if (shieldButton == null) yield break;
        
        var buttonImage = shieldButton.GetComponent<Image>();
        if (buttonImage == null) yield break;
        
        var originalColor = buttonImage.color;
        
        for (int i = 0; i < 3; i++)
        {
            buttonImage.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            buttonImage.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private GameObject FindLocalPlayerTank()
    {
        var allTanks = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (var tank in allTanks)
        {
            var networkObject = tank.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject)
            {
                return tank;
            }
        }
        
        var tankMovement = FindFirstObjectByType<TankMovement2D>();
        if (tankMovement != null)
        {
            var networkObject = tankMovement.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject)
            {
                return tankMovement.gameObject;
            }
        }
        
        return null;
    }

    public void OnPhotonReady()
    {
        // 🔧 FORCER le masquage du loadingPanel seulement au premier appel
        if (loadingPanel != null && loadingPanel.activeInHierarchy)
        {
            loadingPanel.SetActive(false);
            Debug.Log("[LOBBY] LoadingPanel hidden by OnPhotonReady");
        }
        
        // Activer les boutons même sans session Fusion active
        // BasicSpawner appelle cette méthode quand les composants Fusion sont prêts
        createRoomButton.interactable = true;
        joinRoomButton.interactable = true;
        if (goButton != null) {
            goButton.interactable = true;
            var goText = goButton.GetComponentInChildren<TMP_Text>();
            if (goText != null) goText.text = "Brawl";
        }
    }

    public void OnJoinRoomFailedUI()
    {
        createdCodeText.text = "No room found with this code.";
        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
    }

    public void OnJoinedRoomUI(string code)
    {
        if (launcher != null && launcher.roomName == "")
        {
            createdCodeText.text = "";
        }
        else if (!string.IsNullOrEmpty(code) && code.Length == 36 && code.Contains("-"))
        {
            createdCodeText.text = "";
        }
        else if (string.IsNullOrEmpty(code) || code.Length > 8 || code.Contains("-"))
        {
            createdCodeText.text = "";
        }
        else
        {
            createdCodeText.text = "Room code: " + code;
        }
        joinPanel.SetActive(false);
        waitingPanel.SetActive(true);
        UpdatePlayerList();
        HideWaitingForPlayerTextIfRoomFull();
    }

    public void OnJoinedRandomRoomUI()
    {
        if (createdCodeText != null)
            createdCodeText.text = "Joined public match!";
            
        joinPanel.SetActive(false);
        waitingPanel.SetActive(true);
        UpdatePlayerList();
        HideWaitingForPlayerTextIfRoomFull();
    }

    public void UpdatePlayerList()
    {
        // 🌐 FUSION PURE: Déléguer à NetworkUIManager pour la synchronisation réseau
        var networkUIManager = FindFirstObjectByType<NetworkUIManager>();
        if (networkUIManager != null)
        {
            Debug.Log("[LOBBY] 📝 Demande de mise à jour PlayerList via NetworkUIManager");
            networkUIManager.UpdatePlayerList();
        }
        else
        {
            Debug.LogWarning("[LOBBY] ⚠️ NetworkUIManager introuvable pour UpdatePlayerList");
            
            // Fallback : affichage local basique
            var currentRunner = FindFirstObjectByType<NetworkRunner>();
            if (playerListText != null && currentRunner != null && currentRunner.IsConnectedToServer)
            {
                StringBuilder sb = new StringBuilder();
                foreach (PlayerRef player in currentRunner.ActivePlayers)
                {
                    sb.AppendLine($"Player {player.PlayerId}");
                }
                playerListText.text = sb.ToString();
            }
            else if (playerListText != null)
            {
                playerListText.text = "No players";
            }
        }
    }

    public void OnBackToLobby()
    {
        Debug.Log("[LOBBY] 🚪 OnBackToLobby called - starting FUSION PURE quit process");
        
        // 🌐 FUSION PURE: Architecture simplifiée
        // 1. Reset UI immédiat (objets persistants)
        // 2. Shutdown NetworkRunner (objets temporaires)
        // 3. Les events NetworkUIManager.OnRoomLeft vont déclencher le reset final
        
        // 1. RESET UI immédiat pour éviter les problèmes de timing
        ResetUIToLobby();
        
        // 2. SHUTDOWN propre du NetworkRunner et objets temporaires
        var currentRunner = FindFirstObjectByType<NetworkRunner>();
        if (currentRunner != null && currentRunner.IsRunning)
        {
            Debug.Log("[LOBBY] 🔌 Shutting down NetworkRunner - objets temporaires seront détruits");
            
            try
            {
                // Le shutdown va automatiquement:
                // - Despawn tous les NetworkObjects (tanks, NetworkUIManager)
                // - Déclencher NetworkUIManager.OnRoomLeft via Despawned()
                // - Détruire le GameObject NetworkRunner
                currentRunner.Shutdown();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LOBBY] Exception during shutdown: {ex.Message}");
            }
        }
        
        Debug.Log("[LOBBY] ✅ Quit terminé - objets persistants préservés, temporaires détruits");
    }
    
    private void ResetUIToLobby()
    {
        Debug.Log($"[LOBBY] Resetting UI - joinPanel: {joinPanel}, waitingPanel: {waitingPanel}");
        
        // Activer le panel de lobby
        if (joinPanel != null)
        {
            joinPanel.SetActive(true);
            Debug.Log($"[LOBBY] joinPanel activated - active: {joinPanel.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("[LOBBY] joinPanel is null!");
        }
        
        // Désactiver le panel de jeu
        if (waitingPanel != null)
        {
            waitingPanel.SetActive(false);
            Debug.Log($"[LOBBY] waitingPanel deactivated - active: {waitingPanel.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("[LOBBY] waitingPanel is null!");
        }
        
        // Reset des textes UI
        if (timerText != null) timerText.text = "";
        if (roomStatusText != null) roomStatusText.text = "";
        if (roomStatusTextBig != null) roomStatusTextBig.text = "";
        if (createdCodeText != null) createdCodeText.text = "";
        if (playerListText != null) playerListText.text = "";
        
        // 🌐 FUSION PURE: Pas besoin de surveillance complexe
        // Les events NetworkUIManager gèrent le reset UI automatiquement
        Debug.Log("[LOBBY] ✅ ResetUIToLobby terminé - architecture Fusion pure");
    }
    


    public void HideWaitingForPlayerTextIfRoomFull()
    {
        var currentRunner = FindFirstObjectByType<NetworkRunner>();
        if (currentRunner != null && currentRunner.IsConnectedToServer && waitingForPlayerText != null)
        {
            waitingForPlayerText.gameObject.SetActive(currentRunner.ActivePlayers.Count() < 2);
        }
    }

    public void ShowWaitingForPlayerTextIfNotFull()
    {
        var currentRunner = FindFirstObjectByType<NetworkRunner>();
        if (currentRunner != null && currentRunner.IsConnectedToServer && waitingForPlayerText != null)
        {
            waitingForPlayerText.gameObject.SetActive(currentRunner.ActivePlayers.Count() < 2);
        }
    }
    
    public void UpdateTimer(int remainingSeconds)
    {
        if (timerText != null)
        {
            int minutes = remainingSeconds / 60;
            int seconds = remainingSeconds % 60;
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }
    
    public void UpdateRoomStatus(string status)
    {
        if (roomStatusText != null)
        {  
            roomStatusText.text = status;
            ForceEasyTextEffectUpdate(roomStatusText);
        }
        
        if (roomStatusTextBig != null)
        {
            if (!string.IsNullOrEmpty(status) && 
                (status.ToLower().Contains("winner") || 
                 status.ToLower().Contains("win") || 
                 status.ToLower().Contains("victory")))
            {
                roomStatusTextBig.text = status;
                ForceEasyTextEffectUpdate(roomStatusTextBig);
            }
            else
            {
                roomStatusTextBig.text = ""; 
            }
        }
    }
    
    private void ForceEasyTextEffectUpdate(TMP_Text textComponent)
    {
        if (textComponent == null) return;
        
        var easyTextEffect = textComponent.GetComponent<MonoBehaviour>();
        
        var allComponents = textComponent.GetComponents<MonoBehaviour>();
        
        foreach (var component in allComponents)
        {
            if (component != null)
            {
                var componentType = component.GetType().Name;
                
                if (componentType.Contains("Effect") || componentType.Contains("Text") && componentType.Contains("Easy"))
                {
                    
                    try
                    {
                        component.enabled = false;
                        component.enabled = true;
                        
                        var updateMethod = component.GetType().GetMethod("UpdateEffect");
                        if (updateMethod != null)
                        {
                            updateMethod.Invoke(component, null);
                        }
                        
                        var refreshMethod = component.GetType().GetMethod("RefreshEffect");
                        if (refreshMethod != null)
                        {
                            refreshMethod.Invoke(component, null);
                        }
                        
                        var applyMethod = component.GetType().GetMethod("ApplyEffect");
                        if (applyMethod != null)
                        {
                            applyMethod.Invoke(component, null);
                        }
                        
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[EASY TEXT EFFECT] Erreur lors de la mise à jour: {e.Message}");
                    }
                }
            }
        }
        
        textComponent.ForceMeshUpdate();
        textComponent.SetAllDirty();
    }
    
    public void OnLeftRoom()
    {
        SimpleSkinSelector skinSelector = FindFirstObjectByType<SimpleSkinSelector>();
        if (skinSelector != null)
        {
            skinSelector.HideSkinPanel();
        }
        
        SettingsPanelManager settingsManager = FindFirstObjectByType<SettingsPanelManager>();
        if (settingsManager != null)
        {
            settingsManager.HideSettingsPanel();
        }
        
        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
        
        UpdateMainScreenPlayerName();
    }
    
    // public void OnFriendListUpdate - FriendInfo not available in Fusion
    
    /// <summary>
    /// Méthode publique pour cacher le waiting panel (appelée par le tank local après spawn)
    /// </summary>
    public void HideWaitingPanel()
    {
        if (waitingPanel != null)
        {
            waitingPanel.SetActive(false);
            Debug.Log("[LOBBY] Waiting panel caché après spawn du tank local");
        }
    }
    public void OnCreatedRoom() { }
    public void OnCreateRoomFailed(short returnCode, string message) { }
    public void OnJoinedRoom() { }
    public void OnJoinRoomFailed(short returnCode, string message) { }
    public void OnJoinRandomFailed(short returnCode, string message) { }
    
    #region NetworkUIManager Event Handlers
    
    /// <summary>S'abonner aux events du NetworkUIManager temporaire</summary>
    private void SubscribeToNetworkEvents()
    {
        // Désabonnement préventif pour éviter les doublons
        UnsubscribeFromNetworkEvents();
        
        NetworkUIManager.OnRoomJoined += HandleRoomJoined;
        NetworkUIManager.OnRoomLeft += HandleRoomLeft;
        NetworkUIManager.OnLocalTankSpawned += HandleLocalTankSpawned;
        NetworkUIManager.OnPlayerListUpdated += HandlePlayerListUpdated;
        NetworkUIManager.OnMatchTimerUpdated += HandleMatchTimerUpdated;
        NetworkUIManager.OnKillFeedMessage += HandleKillFeedMessage;
        
        Debug.Log("[LOBBY] 🔗 Abonné aux events NetworkUIManager");
    }
    
    /// <summary>Se désabonner des events du NetworkUIManager</summary>
    private void UnsubscribeFromNetworkEvents()
    {
        NetworkUIManager.OnRoomJoined -= HandleRoomJoined;
        NetworkUIManager.OnRoomLeft -= HandleRoomLeft;
        NetworkUIManager.OnLocalTankSpawned -= HandleLocalTankSpawned;
        NetworkUIManager.OnPlayerListUpdated -= HandlePlayerListUpdated;
        NetworkUIManager.OnMatchTimerUpdated -= HandleMatchTimerUpdated;
        NetworkUIManager.OnKillFeedMessage -= HandleKillFeedMessage;
    }
    
    /// <summary>Gérer l'événement de join de room</summary>
    private void HandleRoomJoined(string roomName)
    {
        Debug.Log($"[LOBBY] 🚪 Room rejointe: {roomName}");
        
        // Passer en mode "waiting" (UI de jeu)
        joinPanel.SetActive(false);
        waitingPanel.SetActive(true);
        
        // Mettre à jour le statut de la room
        if (roomStatusText != null)
            roomStatusText.text = $"Room: {roomName}";
        if (roomStatusTextBig != null)
            roomStatusTextBig.text = $"Room: {roomName}";
    }
    
    /// <summary>Gérer l'événement de quit de room</summary>
    private void HandleRoomLeft()
    {
        Debug.Log("[LOBBY] 🚪 Room quittée - retour au lobby");
        
        // Retour au lobby (même logique que OnLeftRoom PUN2)
        OnLeftRoom();
    }
    
    /// <summary>Gérer le spawn du tank local</summary>
    private void HandleLocalTankSpawned()
    {
        Debug.Log("[LOBBY] 🚗 Tank local spawné - masquage du loading panel");
        
        // Cacher le loading panel quand le tank est spawné
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }
    
    /// <summary>Gérer la mise à jour de la liste des joueurs</summary>
    private void HandlePlayerListUpdated(string playerList)
    {
        Debug.Log($"[LOBBY] 📝 HandlePlayerListUpdated appelé avec: '{playerList}'");
        Debug.Log($"[LOBBY] 📝 playerListText null? {playerListText == null}");
        
        if (playerListText != null)
        {
            playerListText.text = $"Players: {playerList}";
            Debug.Log($"[LOBBY] ✅ PlayerList UI mise à jour: '{playerListText.text}'");
        }
        else
        {
            Debug.LogWarning("[LOBBY] ⚠️ playerListText est null - impossible de mettre à jour l'UI");
        }
    }
    
    /// <summary>Gérer la mise à jour du timer de match</summary>
    private void HandleMatchTimerUpdated(float timeRemaining)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    
    /// <summary>Gérer les messages du killfeed</summary>
    private void HandleKillFeedMessage(string message)
    {
        if (killFeedText != null)
        {
            killFeedText.text = message;
            
            // Optionnel: faire disparaître le message après quelques secondes
            CancelInvoke(nameof(ClearKillFeed));
            Invoke(nameof(ClearKillFeed), 3f);
        }
    }
    
    /// <summary>Effacer le killfeed</summary>
    private void ClearKillFeed()
    {
        if (killFeedText != null)
            killFeedText.text = "";
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // 🧹 Nettoyage: se désabonner des events
        UnsubscribeFromNetworkEvents();
        Debug.Log("[LOBBY] 🧹 LobbyUI destroyed - events unsubscribed");
    }
    
    private void UpdateMainScreenPlayerName()
    {
        if (mainScreenPlayerNameText != null)
        {
            string playerName = "Player_" + Random.Range(100, 999); // Temporary - PlayerRef doesn't convert to string directly
            mainScreenPlayerNameText.text = " " + playerName;
        }
    }
}