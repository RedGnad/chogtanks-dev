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

    public void CreatePrivateRoom()
    {
        roomName = GenerateRoomCode();
        // RoomOptions - removed for Fusion options = new // RoomOptions - removed for Fusion { MaxPlayers = maxPlayers, IsVisible = true, IsOpen = true };
        // Fusion room creation handled differently
    }

    public void JoinRoomByCode(string code)
    {
        roomName = code.ToUpper();
        // Fusion room joining handled differently
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
    public void OnJoinedRoomFusion()
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
        
        var spawner = FindObjectOfType<PhotonTankSpawner>();
        if (spawner != null)
        {
            spawner.SpawnTank();
        }
        else
        {
            Debug.LogError("[PhotonLauncher] PhotonTankSpawner non trouvé dans la scène !");
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

    public void JoinRandomPublicRoom()
    {
        string publicRoomName = "PublicRoom";
        roomName = publicRoomName; 
        // RoomOptions - removed for Fusion
        // options = new RoomOptions
        // {
        //     MaxPlayers = maxPlayers,
        //     IsVisible = true,
        //     IsOpen = true
        // };
        // Fusion join or create room handled differently
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