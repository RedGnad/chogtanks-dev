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

    private PhotonLauncher launcher;
    private bool isShieldCooldownActive = false; 
    private string shieldDefaultText = ""; 

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
            // Démarrer le timer pour cacher le panel après la durée configurée
            StartCoroutine(HideLoadingPanelAfterDelay());
        }

        if (playerNameInput != null)
        {
            playerNameInput.onEndEdit.AddListener(OnPlayerNameEndEdit);
        }
        
        if (playerNameInput2 != null)
        {
            playerNameInput2.onEndEdit.AddListener(OnPlayerNameEndEdit2);
        }

        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
        
        // Enable menu camera when joinPanel is active
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
            playerNameInput2.text = ""; // Champ vide par défaut
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
        PhotonNetwork.AddCallbackTarget(this);
    }
    
    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
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
        else if (playerName.Length > 20)
        {
            if(createdCodeText != null) createdCodeText.text = "Name cannot exceed 20 characters.";
            playerName = playerName.Substring(0, 20);
            // Ne pas mettre à jour les champs de saisie ici pour éviter une boucle infinie
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
        // Freeze camera BEFORE any other action
        if (MenuCameraController.Instance != null)
        {
            MenuCameraController.Instance.FreezeCameraImmediately();
        }
        
        launcher.CreatePrivateRoom();
        joinPanel.SetActive(false);
        waitingPanel.SetActive(true);
        
        // Complete disable after panel change and start game music
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
            // Freeze camera BEFORE any other action
            if (MenuCameraController.Instance != null)
            {
                MenuCameraController.Instance.FreezeCameraImmediately();
            }
            
            launcher.JoinRoomByCode(code);
            joinPanel.SetActive(false);
            waitingPanel.SetActive(true);
            
            // Complete disable after panel change and start game music
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
        // Freeze camera BEFORE any other action
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
        
        // Complete disable after panel change and start game music
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
        var allTanks = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (var tank in allTanks)
        {
            var photonView = tank.GetComponent<PhotonView>();
            if (photonView != null && photonView.IsMine)
            {
                return tank;
            }
        }
        
        var tankMovement = FindObjectOfType<TankMovement2D>();
        if (tankMovement != null)
        {
            var photonView = tankMovement.GetComponent<PhotonView>();
            if (photonView != null && photonView.IsMine)
            {
                return tankMovement.gameObject;
            }
        }
        
        return null;
    }

    private System.Collections.IEnumerator HideLoadingPanelAfterDelay()
    {
        yield return new WaitForSeconds(loadingPanelDuration);
        
        if (loadingPanel != null)
        {
            Debug.Log($"[LOADING-PANEL] Masquage automatique après {loadingPanelDuration} secondes");
            loadingPanel.SetActive(false);
        }
    }

    public void OnPhotonReady()
    {
        // SUPPRIMÉ: Plus de masquage basé sur Photon - utilise maintenant le timer fixe
        // if (loadingPanel != null)
        // {
        //     loadingPanel.SetActive(false);
        // }
        
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
        
        // Enable menu camera when returning to joinPanel
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
        if (playerListText == null || PhotonNetwork.CurrentRoom == null)
        {
            if(playerListText != null) playerListText.text = "";
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        Dictionary<int, int> playerScores = ScoreManager.Instance ? ScoreManager.Instance.GetPlayerScores() : new Dictionary<int, int>();
        
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            string playerName = string.IsNullOrEmpty(p.NickName) ? $"Player {p.ActorNumber}" : p.NickName;
            int score = 0;
            
            if (playerScores.ContainsKey(p.ActorNumber))
            {
                score = playerScores[p.ActorNumber];
            }
            
            sb.AppendLine($"{playerName} - {score} pts");
        }
        
        playerListText.text = sb.ToString();
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
        
        // Enable menu camera when returning to joinPanel
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
        
        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
        
        // Enable menu camera when returning to joinPanel
        if (MenuCameraController.Instance != null)
        {
            MenuCameraController.Instance.EnableMenuMode();
        }
        
        UpdateMainScreenPlayerName();
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
    
    /// <summary>
    /// Méthode publique pour définir le nom de joueur depuis Monad Games ID
    /// </summary>
    public void SetPlayerNameFromMonadID(string monadUsername)
    {
        if (string.IsNullOrEmpty(monadUsername)) return;
        
        Debug.Log($"[LOBBY-UI] Setting player name from Monad ID: {monadUsername}");
        
        // Mettre à jour le premier champ d'input avec le username Monad
        if (playerNameInput != null)
        {
            playerNameInput.text = monadUsername;
        }
        
        // Vider le second champ
        if (playerNameInput2 != null)
        {
            playerNameInput2.text = "";
        }
        
        // Appeler la méthode existante pour traiter le changement
        CombineAndSetPlayerName();
        
        Debug.Log($"[LOBBY-UI] Player name set to: {monadUsername}");
    }
}