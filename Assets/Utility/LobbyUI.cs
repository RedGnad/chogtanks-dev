using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Text;
using System.Collections.Generic;

public class LobbyUI : MonoBehaviourPun, IMatchmakingCallbacks
{
    public static LobbyUI Instance { get; private set; }
    
    [Header("UI Elements")]
    public TMP_InputField playerNameInput;
    public TMP_InputField playerNameInput2; 
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
    
    [Header("Monad Badge System")]
    public GameObject monadBadgePrefab; 
    private Dictionary<string, GameObject> playerBadges = new Dictionary<string, GameObject>(); 
    
    [Header("Match UI")]
    public TMP_Text timerText;
    public TMP_Text roomStatusText;
    public TMP_Text roomStatusTextBig; 
    public Button shieldButton; 
    public TMP_Text shieldCooldownText; 
    public bool showShieldCountdownText = true;
    
    [Header("Loading Panel")]
    public GameObject loadingPanel;
    [SerializeField] private float loadingPanelDuration = 3f;
    
    [Header("Tap to Hide Text")]
    public TMP_Text tapToHideText; // Texte qui se cache au premier touch dans le waiting panel
    private bool tapToHideTextHidden = false;
    private string originalTapToHideText = ""; // Stocker le texte original

    private PhotonLauncher launcher;
    private bool isShieldCooldownActive = false; 
    private string shieldDefaultText = ""; 

    private bool needsDelayOnReturn = false;

    // Cache local tank to avoid per-frame scene scans
    private GameObject cachedLocalTank;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
        
    }
    
    void Start()
    {
        launcher = FindObjectOfType<PhotonLauncher>();        if (backButton != null)
            backButton.onClick.AddListener(OnBackToLobby);
        
        createRoomButton.onClick.AddListener(OnCreateRoom);
        joinRoomButton.onClick.AddListener(OnJoinRoom);
        
        if (shieldButton != null)
        {
            shieldButton.onClick.AddListener(OnShieldButtonClicked);
        }
        
        if (goButton != null) {
            goButton.onClick.AddListener(OnGoButtonClicked);
            goButton.interactable = false;
            var goText = goButton.GetComponentInChildren<TMP_Text>();
            if (goText != null) goText.text = "WAIT";
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            StartCoroutine(HideLoadingPanelAfterDelay());
        }

        if (playerNameInput != null)
        {
            playerNameInput.characterLimit = 11;
            playerNameInput.onEndEdit.AddListener(OnPlayerNameEndEdit);
            
            UpdatePlayerNameInputState();
        }
        
        if (playerNameInput2 != null)
        {
            playerNameInput2.characterLimit = 11;
            playerNameInput2.onEndEdit.AddListener(OnPlayerNameEndEdit2);
            
            UpdatePlayerNameInputState();
        }

        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
        
        if (MenuCameraController.Instance != null)
        {
            MenuCameraController.Instance.EnableMenuMode();
        }
        
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
        
        createdCodeText.text = "";
        
        string defaultName = "Newbie_" + Random.Range(100, 999);
        if (playerNameInput != null)
        {
            playerNameInput.text = defaultName.Split('_')[0];
        }
        if (playerNameInput2 != null)
        {
            playerNameInput2.text = "";
        }
        CombineAndSetPlayerName();

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
        
        if (tapToHideText != null)
        {
            // Sauvegarder le texte original de l'Inspector AVANT de le modifier
            if (string.IsNullOrEmpty(originalTapToHideText))
            {
                originalTapToHideText = tapToHideText.text;
            }
            // NE PAS activer ici - seulement dans ShowTapToHideText()
            tapToHideText.gameObject.SetActive(false);
            tapToHideTextHidden = true;
        }
    }
    
    void Update()
    {
        if (Screen.orientation != ScreenOrientation.LandscapeLeft)
        {
            CheckAndEnforceOrientation();
        }
        
        // Vérifier les touches/clics pour cacher le texte seulement si waiting panel est actif ET texte visible
        if (!tapToHideTextHidden && tapToHideText != null && waitingPanel != null && 
            waitingPanel.activeInHierarchy && tapToHideText.gameObject.activeInHierarchy)
        {
            bool inputDetected = false;
            
            // Détecter clic souris
            if (Input.GetMouseButtonDown(0))
            {
                inputDetected = true;
            }
            
            // Détecter touch sur mobile
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                inputDetected = true;
            }
            
            if (inputDetected)
            {
                tapToHideText.gameObject.SetActive(false);
                tapToHideTextHidden = true;
            }
        }
        
        MonitorShieldState();
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
        PhotonNetwork.AddCallbackTarget(this);
        PhotonTankSpawner.OnTankSpawned += HandleTankSpawned;
    }
    
    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        PhotonTankSpawner.OnTankSpawned -= HandleTankSpawned;
    }

    private void HandleTankSpawned(GameObject tank, PhotonView view)
    {
        if (view != null && view.IsMine)
        {
            cachedLocalTank = tank;
        }
    }

    private void OnPlayerNameEndEdit(string newName)
    {
        CombineAndSetPlayerName();
    }
    
    private void OnPlayerNameEndEdit2(string newName)
    {
        CombineAndSetPlayerName();
    }
    
    private void CombineAndSetPlayerName()
    {
        if (launcher == null) return;
        
        string firstName = playerNameInput != null ? playerNameInput.text.Trim() : "";
        string lastName = playerNameInput2 != null ? playerNameInput2.text.Trim() : "";
        
        string playerName;
        
        // Si le deuxième champ est rempli, utiliser uniquement sa valeur comme nom du joueur
        if (!string.IsNullOrEmpty(lastName))
        {
            playerName = lastName;
        }
        else
        {
            playerName = firstName;
        }
        
        if (string.IsNullOrEmpty(playerName) || playerName.Length < 2)
        {
            if(createdCodeText != null) createdCodeText.text = "Name must be at least 2 characters.";
            playerName = "Newbie_" + Random.Range(100, 999);
            if (playerNameInput != null) playerNameInput.text = playerName.Split('_')[0];
            if (playerNameInput2 != null) playerNameInput2.text = playerName.Contains("_") ? playerName.Split('_')[1] : "";
        }
        else if (playerName.Length > 11)
        {
            if(createdCodeText != null) createdCodeText.text = "Name cannot exceed 11 characters.";
            playerName = playerName.Substring(0, 11);
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
        if (MenuCameraController.Instance != null)
        {
            MenuCameraController.Instance.FreezeCameraImmediately();
        }
        
        launcher.CreatePrivateRoom();
        joinPanel.SetActive(false);
        waitingPanel.SetActive(true);
        
        if (MenuCameraController.Instance != null)
        {
            MenuCameraController.Instance.DisableMenuMode();
            MenuCameraController.Instance.StartGameMusic();
        }
    }

    void OnJoinRoom()
    {
        string code = joinCodeInput.text.Trim().ToUpper();
        if (code.Length == 4)
        {
            if (MenuCameraController.Instance != null)
            {
                MenuCameraController.Instance.FreezeCameraImmediately();
            }
            
            launcher.JoinRoomByCode(code);
            joinPanel.SetActive(false);
            waitingPanel.SetActive(true);
            
            if (MenuCameraController.Instance != null)
            {
                MenuCameraController.Instance.DisableMenuMode();
                MenuCameraController.Instance.StartGameMusic();
            }
        }
        else
        {
            createdCodeText.text = "Invalid code (must be 4 characters)";
        }
    }

    void OnGoButtonClicked()
    {
        if (MenuCameraController.Instance != null)
        {
            MenuCameraController.Instance.FreezeCameraImmediately();
        }
        
        launcher.JoinRandomPublicRoom();
        joinPanel.SetActive(false);
        waitingPanel.SetActive(true);
        
        if (createdCodeText != null)
            createdCodeText.text = "Searching for players...";
        if (goButton != null) {
            goButton.interactable = false;
            var goText = goButton.GetComponentInChildren<TMP_Text>();
            if (goText != null) goText.text = "WAIT ";
        }
        
        if (MenuCameraController.Instance != null)
        {
            MenuCameraController.Instance.DisableMenuMode();
            MenuCameraController.Instance.StartGameMusic();
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
        // Use cached reference first when available
        if (cachedLocalTank != null)
        {
            var pv = cachedLocalTank.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return cachedLocalTank;
            }
            // if ownership changed or object destroyed, clear cache
            if (cachedLocalTank == null || pv == null || !pv.IsMine)
            {
                cachedLocalTank = null;
            }
        }

        // Fallback: one-time scan to refresh cache if needed
        var allTanks = GameObject.FindGameObjectsWithTag("Player");
        foreach (var tank in allTanks)
        {
            var photonView = tank.GetComponent<PhotonView>();
            if (photonView != null && photonView.IsMine)
            {
                cachedLocalTank = tank;
                return tank;
            }
        }

        // Secondary fallback by component type (legacy)
        var tankMovement = FindObjectOfType<TankMovement2D>();
        if (tankMovement != null)
        {
            var photonView = tankMovement.GetComponent<PhotonView>();
            if (photonView != null && photonView.IsMine)
            {
                cachedLocalTank = tankMovement.gameObject;
                return cachedLocalTank;
            }
        }

        return null;
    }

    private System.Collections.IEnumerator HideLoadingPanelAfterDelay()
    {
        yield return new WaitForSeconds(loadingPanelDuration);
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }
    
    public void OnPhotonReady()
    {
        if (!string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            createRoomButton.interactable = true;
            joinRoomButton.interactable = true;
            if (goButton != null) {
                goButton.interactable = true;
                var goText = goButton.GetComponentInChildren<TMP_Text>();
                if (goText != null) goText.text = "Brawl";
            }
        }
    }

    public void OnJoinRoomFailedUI()
    {
        createdCodeText.text = "No room found with this code.";
        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
        
        if (MenuCameraController.Instance != null)
        {
            MenuCameraController.Instance.EnableMenuMode();
        }
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
        ShowTapToHideText();
    }

    public void OnJoinedRandomRoomUI()
    {
        if (createdCodeText != null)
            createdCodeText.text = "Joined public match!";
            
        joinPanel.SetActive(false);
        waitingPanel.SetActive(true);
        UpdatePlayerList();
        HideWaitingForPlayerTextIfRoomFull();
        ShowTapToHideText();
    }

    public void UpdatePlayerList()
    {
        if (playerListText == null) 
        {
            Debug.LogWarning("[LOBBYUI] playerListText is null!");
            return;
        }
        
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log("[LOBBYUI] UpdatePlayerList called");
    #endif
        
        // Clear existing badges
        foreach (var badge in playerBadges.Values)
        {
            if (badge != null) Destroy(badge);
        }
        playerBadges.Clear();
        
        playerListText.text = "";
        
        int index = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int score = 0;
            if (p.CustomProperties.ContainsKey("score"))
            {
                score = (int)p.CustomProperties["score"];
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[LOBBYUI] Player {p.ActorNumber} score from CustomProperties: {score}");
                #endif
            }
            else
            {
                if (ScoreManager.Instance != null)
                {
                    score = ScoreManager.Instance.GetPlayerScore(p.ActorNumber);
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[LOBBYUI] Player {p.ActorNumber} score from ScoreManager: {score}");
                    #endif
                }
                else
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("[LOBBYUI] ScoreManager.Instance is null!");
                    #endif
                }
            }
            
            string playerName = string.IsNullOrEmpty(p.NickName) ? $"Player {p.ActorNumber}" : p.NickName;
            playerListText.text += $"{playerName} - {score} pts\n";
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[LOBBYUI] Added to display: {playerName} - {score} pts");
            #endif
            
            if (IsPlayerMonadVerified(p) && monadBadgePrefab != null && playerListText.transform.parent != null)
            {
                try
                {
                    GameObject badge = Instantiate(monadBadgePrefab, playerListText.transform.parent);
                    // Use ActorNumber as a stable non-null key (PublishUserId can be false)
                    playerBadges[p.ActorNumber.ToString()] = badge;
                    
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[LOBBY-BADGE] Badge créé pour {p.NickName} (Monad verified) - GameObject: {badge.name}");
                    #endif
                    
                    RectTransform badgeRect = badge.GetComponent<RectTransform>();
                    if (badgeRect != null)
                    {
                        // Align badge with the corresponding line using the running index
                        badgeRect.anchoredPosition = new Vector2(-20f, -20f * index);
                        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"[LOBBY-BADGE] Badge positionné à: {badgeRect.anchoredPosition}");
                        #endif
                    }
                }
                catch (System.Exception ex)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"[LOBBY-BADGE] Failed to create/place badge for {p.NickName}: {ex.Message}");
                    #endif
                }
            }
            index++;
        }
    }

    public void OnDisconnectedUI()
    {
        
        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
        
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
        
        if (createdCodeText != null)
        {
            createdCodeText.text = "Connection lost... Reconnecting...";
        }
        
        if (playerListText != null)
        {
            playerListText.text = "";
        }
    }

    public void OnBackToLobby()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
        createdCodeText.text = "";
        playerListText.text = "";
        
        foreach (var ui in GameObject.FindGameObjectsWithTag("GameOverUI"))
        {
            Destroy(ui);
        }
        
        if (MenuCameraController.Instance != null)
        {
            MenuCameraController.Instance.EnableMenuMode();
        }
    }

    public void HideWaitingForPlayerTextIfRoomFull()
    {
        if (PhotonNetwork.CurrentRoom != null && waitingForPlayerText != null)
        {
            waitingForPlayerText.gameObject.SetActive(PhotonNetwork.CurrentRoom.PlayerCount < 2);
        }
    }

    public void ShowWaitingForPlayerTextIfNotFull()
    {
        if (PhotonNetwork.CurrentRoom != null && waitingForPlayerText != null)
        {
            waitingForPlayerText.gameObject.SetActive(PhotonNetwork.CurrentRoom.PlayerCount < 2);
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
        SimpleSkinSelector skinSelector = FindObjectOfType<SimpleSkinSelector>();
        if (skinSelector != null)
        {
            skinSelector.HideSkinPanel();
        }
        
        SettingsPanelManager settingsManager = FindObjectOfType<SettingsPanelManager>();
        if (settingsManager != null)
        {
            settingsManager.HideSettingsPanel();
        }
        
        if (needsDelayOnReturn)
        {
            StartCoroutine(EnableUIAfterDelay());
            needsDelayOnReturn = false; // Reset flag
        }
        else
        {
            joinPanel.SetActive(true);
            waitingPanel.SetActive(false);
        }
        
        if (MenuCameraController.Instance != null)
        {
            MenuCameraController.Instance.EnableMenuMode();
        }
        
        UpdateMainScreenPlayerName();
    }
    
    public void SetDelayOnNextReturn()
    {
        needsDelayOnReturn = true;
    }
    
    private System.Collections.IEnumerator EnableUIAfterDelay()
    {
        yield return new WaitForSeconds(4.83f);
        
        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
    }
    
    public void OnFriendListUpdate(System.Collections.Generic.List<FriendInfo> friendList) { }
    public void OnCreatedRoom() { }
    public void OnCreateRoomFailed(short returnCode, string message) { }
    public void OnJoinedRoom() { }
    public void OnJoinRoomFailed(short returnCode, string message) { }
    public void OnJoinRandomFailed(short returnCode, string message) { }
    
    private void UpdateMainScreenPlayerName()
    {
        if (mainScreenPlayerNameText != null)
        {
            string playerName = PhotonNetwork.NickName;
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "Newbie_" + Random.Range(100, 999);
            }
            
            mainScreenPlayerNameText.text = " " + playerName;
        }
    }
    
    public void SetPlayerNameFromMonadID(string monadUsername)
    {
        if (string.IsNullOrEmpty(monadUsername)) return;
        
        
        if (playerNameInput != null)
        {
            playerNameInput.text = monadUsername;
        }
        
        if (playerNameInput2 != null)
        {
            playerNameInput2.text = "";
        }
        
        CombineAndSetPlayerName();
        
    }
    
    private bool IsPlayerMonadVerified(Player player)
    {
        if (player == null) return false;
        // Utiliser d'abord l'état runtime alimenté par RPC
        if (MonadBadgeState.TryGet(player.ActorNumber, out bool cached))
        {
            return cached;
        }
        // Fallback local uniquement pour soi-même
        if (player == PhotonNetwork.LocalPlayer)
        {
            return PlayerPrefs.GetInt("MonadVerified", 0) == 1;
        }
        return false;
    }
    
    private void UpdatePlayerNameInputState()
    {
        if (playerNameInput != null)
        {
            playerNameInput.interactable = !IsPlayerMonadVerified(PhotonNetwork.LocalPlayer);
        }
        
        if (playerNameInput2 != null)
        {
            playerNameInput2.interactable = !IsPlayerMonadVerified(PhotonNetwork.LocalPlayer);
        }
    }

    public void ShowTapToHideText()
    {
        Debug.Log("[TAP-TO-HIDE] ShowTapToHideText() called");
        if (tapToHideText != null)
        {
            
            if (string.IsNullOrEmpty(originalTapToHideText))
            {
                originalTapToHideText = tapToHideText.text;
            }
            
            tapToHideText.text = originalTapToHideText;
            tapToHideText.gameObject.SetActive(true);
            tapToHideTextHidden = false; 
        }
        else
        {
            Debug.LogError("[TAP-TO-HIDE] ⚠️ ERREUR: tapToHideText est NULL! Veuillez l'assigner dans l'Inspector Unity sur le GameObject LobbyUI.");
        }
    }
}