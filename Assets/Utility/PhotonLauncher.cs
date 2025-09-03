using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Linq; 
using System.Collections.Generic; 

public class PhotonLauncher : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverUIPrefab;

    [Header("Gestion de déconnexion")]
    [SerializeField] private float autoReconnectDelay = 2f;
    [SerializeField] private string lobbySceneName = "LobbyScene";
    [SerializeField] private GameObject reconnectionNotificationPrefab;
    
    private bool isWaitingForReconnection = false;
    private bool wasDisconnected = false;

    private List<RoomInfo> cachedRoomList = new List<RoomInfo>();

    [PunRPC]
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
            if (t.photonView.IsMine)
            {
                myTank = t;
                break;
            }
        }
        if (myTank != null)
        {
            PhotonNetwork.Destroy(myTank.gameObject);
        }

        var spawner = FindObjectOfType<PhotonTankSpawner>();
        if (spawner != null)
        {
            spawner.SpawnTank();
        }
        
        soundAlreadyPlayed = false;
    }

    public void RespawnTank()
    {
        TankHealth2D myTank = null;
        foreach (var t in FindObjectsOfType<TankHealth2D>())
        {
            if (t.photonView.IsMine)
            {
                myTank = t;
                break;
            }
        }
        if (myTank != null)
        {
            PhotonNetwork.Destroy(myTank.gameObject);
        }

        var spawner = FindObjectOfType<PhotonTankSpawner>();
        if (spawner != null)
        {
            spawner.SpawnTank();
        }
        
        soundAlreadyPlayed = false;
    }

    [Header("Winner Sound")]
    [SerializeField] private AudioClip[] winnerSoundClips;
    private static bool soundAlreadyPlayed = false;
    
    [PunRPC]
    public void ShowWinnerToAllRPC(string winnerName, int winnerActorNumber)
    {
        PlayWinnerSoundLocal();
        
        bool isWinner = PhotonNetwork.LocalPlayer.ActorNumber == winnerActorNumber;
        
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

    private void PlayWinnerSoundLocal()
    {
        
        if (soundAlreadyPlayed)
        {
            return;
        }
        
        if (winnerSoundClips == null) 
        {
            return;
        }
        
        if (winnerSoundClips.Length == 0)
        {
            return;
        }
        
        soundAlreadyPlayed = true;
        
        foreach (AudioClip clip in winnerSoundClips)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
            }
            else
            {
                //Debug.LogWarning("[PHOTON-WINNER-SOUND] Clip audio null détecté !");
            }
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
            if (launcher.photonView != null)
            {
                launcher.photonView.RPC("RestartMatchSoftRPC", RpcTarget.All);
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
    public byte maxPlayers =8;

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

    public void CreatePrivateRoom()
    {
        roomName = GenerateRoomCode();
        RoomOptions options = new RoomOptions { MaxPlayers = maxPlayers, IsVisible = true, IsOpen = true };
        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
    }

    [Header("Admin System")]
    [SerializeField] private string[] adminWallets = { 
        " " 
    };
    
    public void JoinRoomByCode(string code)
    {
        roomName = code.ToUpper();
        
        if (IsAdminWallet(PlayerSession.WalletAddress))
        {
            PhotonNetwork.JoinRoom(roomName);
        }
        else
        {
            PhotonNetwork.JoinRoom(roomName);
        }
    }
    
    private bool IsAdminWallet(string walletAddress)
    {
        if (string.IsNullOrEmpty(walletAddress)) return false;
        
        foreach (string adminWallet in adminWallets)
        {
            if (walletAddress.Equals(adminWallet, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
    
    [PunRPC]
    private void RequestAdminAccess(string adminWallet)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        if (!IsAdminWallet(adminWallet))
        {
            return;
        }
        
        Player playerToKick = null;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            string playerWallet = player.CustomProperties.ContainsKey("wallet") ? 
                player.CustomProperties["wallet"].ToString() : "";
            
            if (!IsAdminWallet(playerWallet))
            {
                playerToKick = player;
            }
        }
        
        if (playerToKick != null)
        {
            PhotonNetwork.CloseConnection(playerToKick);
            
            photonView.RPC("NotifyAdminCanJoin", RpcTarget.All, adminWallet);
        }
        else
        {
            //Debug.LogWarning("[ADMIN] No non-admin players found to kick!");
        }
    }
    
    [PunRPC]
    private void NotifyAdminCanJoin(string adminWallet)
    {
        if (PlayerSession.WalletAddress == adminWallet)
        {
            StartCoroutine(DelayedJoinRoom());
        }
    }
    
    private System.Collections.IEnumerator DelayedJoinRoom()
    {
        yield return new WaitForSeconds(1f); 
        PhotonNetwork.JoinRoom(roomName);
    }

    public void SetPlayerName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            PhotonNetwork.NickName = "Newbie_" + Random.Range(100, 999);
        }
        else
        {
            // Cap à 11 caractères par sécurité
            if (playerName.Length > 11)
            {
                playerName = playerName.Substring(0, 11);
            }
            PhotonNetwork.NickName = playerName;
        }
    }

    private void Start()
    {
        if (GetComponent<PhotonView>() == null)
        {
            //Debug.LogError("[PhotonLauncher] PhotonView manquant sur l'objet PhotonLauncher ! Merci d'ajouter un PhotonView dans l'inspecteur AVANT de lancer la scène.");
        }
        
    // Slightly higher rates improve remote movement smoothness without big bandwidth cost
    PhotonNetwork.SendRate = 30;          // default is 20
    PhotonNetwork.SerializationRate = 15; // default is 10

        if (!PhotonNetwork.IsConnected)
        {
            
            PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 300000; 
            PhotonNetwork.NetworkingClient.LoadBalancingPeer.TimePingInterval = 5000; 
            PhotonNetwork.KeepAliveInBackground = 60; 
            
            PhotonNetwork.ConnectUsingSettings();
        }
        
        StartCoroutine(ConnectionHeartbeat());
    }
    
    private System.Collections.IEnumerator ConnectionHeartbeat()
    {
        WaitForSeconds wait = new WaitForSeconds(20f); 
        
        while (true)
        {
            yield return wait;
            
            if (PhotonNetwork.IsConnected)
            {
                
                if (PhotonNetwork.InRoom)
                {
                    photonView.RPC("HeartbeatPing", RpcTarget.MasterClient);
                }
            }
        }
    }
    
    [PunRPC]
    private void HeartbeatPing()
    {
        // ...
    }

    public override void OnConnectedToMaster()
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
            //Debug.LogError("[PHOTON LAUNCHER] lobbyUI est null dans OnConnectedToMaster !");
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
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
            //Debug.LogWarning("[PhotonLauncher] reconnectionNotificationPrefab non assigné");
        }
    }
    
    private System.Collections.IEnumerator ReturnToLobby()
    {
        yield return new WaitForSeconds(autoReconnectDelay);
        
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        
        // Cleanup enemies when disconnected
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.CleanupAllEnemies();
        }
        
        LobbyUI lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.OnBackToLobby();
        }
        else
        {
            //Debug.LogWarning("[PHOTON] LobbyUI non trouvé pour le retour au lobby après déconnexion");
        }
    }

    public override void OnJoinedRoom()
    {
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.OnJoinedRoomUI(PhotonNetwork.CurrentRoom.Name);
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isGameOver = false;
        }
        
        soundAlreadyPlayed = false;
        
        if (ScoreManager.Instance != null) 
        {
            ScoreManager.Instance.ResetManager();
        }
        
        var spawner = FindObjectOfType<PhotonTankSpawner>();
        if (spawner != null)
        {
            spawner.SpawnTank();
        }
        else
        {
            //Debug.LogError("[PhotonLauncher] PhotonTankSpawner non trouvé dans la scène !");
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (returnCode == 32765 && IsAdminWallet(PlayerSession.WalletAddress))
        {
            photonView.RPC("RequestAdminAccess", RpcTarget.MasterClient, PlayerSession.WalletAddress);
            return;
        }
        
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.OnJoinRoomFailedUI();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.HideWaitingForPlayerTextIfRoomFull();
            lobbyUI.UpdatePlayerList();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.ShowWaitingForPlayerTextIfNotFull();
            lobbyUI.UpdatePlayerList();
        }
    }

    public void JoinRandomPublicRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }
    
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        string globalRoomName = "GlobalBrawl";
        roomName = globalRoomName;
        
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = true,
            IsOpen = true,
            PublishUserId = false,
            CleanupCacheOnLeave = true
        };
        
        PhotonNetwork.JoinOrCreateRoom(globalRoomName, options, TypedLobby.Default);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        cachedRoomList = roomList;
    }


    public void JoinOrCreatePublicRoom()
    {
        JoinRandomPublicRoom();
    }
}