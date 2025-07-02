using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Text;
using System.Collections.Generic;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance { get; private set; }
    
    // Player name input is now part of the main join panel
    [Header("UI Elements")]
    public TMP_InputField playerNameInput;
    public TMP_Text waitingForPlayerText;
    public Button createRoomButton;
    public Button joinRoomButton;
    public TMP_InputField joinCodeInput;
    public TMP_Text createdCodeText;
    public GameObject joinPanel;
    public GameObject waitingPanel;
    public TMP_Text playerListText;
    public Button backButton;
    
    [Header("Match UI")]
    public TMP_Text timerText;
    public TMP_Text roomStatusText;

    private PhotonLauncher launcher;

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
        launcher = FindObjectOfType<PhotonLauncher>();

        // Add listeners to buttons
        if (backButton != null)
            backButton.onClick.AddListener(OnBackToLobby);
        
        createRoomButton.onClick.AddListener(OnCreateRoom);
        joinRoomButton.onClick.AddListener(OnJoinRoom);

        // Add listener for player name input field
        if (playerNameInput != null)
        {
            playerNameInput.onEndEdit.AddListener(OnPlayerNameEndEdit);
        }

        // Set initial UI state
        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
        createdCodeText.text = "";
        
        // Initialize player name
        string defaultName = "Player_" + Random.Range(1000, 9999);
        if (playerNameInput != null)
        {
            playerNameInput.text = defaultName;
        }
        // Set the initial name in Photon
        OnPlayerNameEndEdit(defaultName);

        // Initialize player list
        if (playerListText != null)
        {
            playerListText.text = "";
        }
        
        // Initialize timer and status text
        if (timerText != null)
        {
            timerText.text = "";
        }
        
        if (roomStatusText != null)
        {
            roomStatusText.text = "";
        }
    }

    // Called when the player finishes editing their name
    private void OnPlayerNameEndEdit(string newName)
    {
        if (launcher == null) return;
        
        string playerName = newName.Trim();
        
        // Simple validation
        if (string.IsNullOrEmpty(playerName) || playerName.Length < 2)
        {
            if(createdCodeText != null) createdCodeText.text = "Name must be at least 2 characters.";
            // Revert to a default name if input is invalid
            playerName = "Player_" + Random.Range(1000, 9999);
            if (playerNameInput != null) playerNameInput.text = playerName;
        }
        else if (playerName.Length > 20)
        {
            if(createdCodeText != null) createdCodeText.text = "Name cannot exceed 20 characters.";
            // Truncate the name
            playerName = playerName.Substring(0, 20);
            if (playerNameInput != null) playerNameInput.text = playerName;
        }
        
        // Set the player name via the launcher
        launcher.SetPlayerName(playerName);
        Debug.Log($"[LobbyUI] Player name set to: {playerName}");

        // If Photon is ready, update button interactability
        if (launcher.isConnectedAndReady)
        {
            OnPhotonReady();
        }
    }

    // --- Room Creation and Joining ---

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

    // --- Photon Callbacks Handled by UI ---

    public void OnPhotonReady()
    {
        Debug.Log("[LOBBY UI] OnPhotonReady called: Activating buttons!");
        
        // Enable buttons only if a name has been set
        if (!string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            createRoomButton.interactable = true;
            joinRoomButton.interactable = true;
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
        createdCodeText.text = "Room code: " + code;
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
        Debug.Log("[LobbyUI] OnDisconnectedUI - Connection lost");
        
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
    }

    // --- Waiting Text Management ---

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
    
    // --- Match Timer and Status Display ---
    
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
        }
    }
}