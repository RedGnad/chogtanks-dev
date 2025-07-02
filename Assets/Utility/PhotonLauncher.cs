using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonLauncher : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverUIPrefab;

    [PunRPC]
    public void RestartMatchSoftRPC()
    {
        Debug.Log($"[RESET SOFT] RPC reçu sur client {PhotonNetwork.LocalPlayer.NickName} (Actor {PhotonNetwork.LocalPlayer.ActorNumber})");
        // Détruit toutes les UI GameOver/Win
        foreach (var ui in GameObject.FindGameObjectsWithTag("GameOverUI"))
        {
            Destroy(ui);
        }

        // NOUVEAU : Reset de la minimap
        var minimapCam = FindObjectOfType<MinimapCamera>();
        if (minimapCam != null)
        {
            minimapCam.ForceReset();
            Debug.Log("[RESET SOFT] Minimap reset forcé");
        }

        // Détruit le tank existant (s'il y en a un)
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

        // Respawn réseau : chaque client ré-instancie son tank après destruction
        var spawner = FindObjectOfType<PhotonTankSpawner>();
        if (spawner != null)
        {
            Debug.Log("[RESET SOFT] Appel SpawnTank() sur le client local");
            spawner.SpawnTank();
        }
    }

    // RPC UNIQUE pour tout le monde
    [PunRPC]
    public void ShowWinnerToAllRPC(string winnerName, int winnerActorNumber)
    {
        Debug.Log($"[PhotonLauncher] ShowWinnerToAllRPC reçu - Gagnant: {winnerName}, Mon ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");
        
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
                    Debug.Log($"[PhotonLauncher] UI gagnant affichée pour {winnerName}");
                }
                else
                {
                    controller.ShowWinner(winnerName);
                    Debug.Log($"[PhotonLauncher] UI perdant affichée - {winnerName} a gagné");
                }
            }
            
            StartCoroutine(AutoDestroyAndRestart(uiInstance));
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

    // Méthode statique utilitaire pour déclencher le reset soft depuis n'importe où
    public static void CallRestartMatchSoft()
    {
        Debug.Log("[PhotonLauncher] CallRestartMatchSoft() appelé");
        var launcher = FindObjectOfType<PhotonLauncher>();
        if (launcher != null)
        {
            Debug.Log("[PhotonLauncher] launcher trouvé, envoi du RPC RestartMatchSoftRPC");
            if (launcher.photonView != null)
            {
                launcher.photonView.RPC("RestartMatchSoftRPC", RpcTarget.All);
                Debug.Log("[PhotonLauncher] RPC RestartMatchSoftRPC envoyé à tous");
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
    public byte maxPlayers = 8;

    public LobbyUI lobbyUI;

    private static readonly string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
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
        Debug.Log($"[PhotonLauncher] Room privée créée avec le code : {roomName}");
    }

    public void JoinRoomByCode(string code)
    {
        roomName = code.ToUpper();
        PhotonNetwork.JoinRoom(roomName);
        Debug.Log($"[PhotonLauncher] Tentative de rejoindre la room avec le code : {roomName}");
    }

    public void SetPlayerName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            PhotonNetwork.NickName = "Player_" + Random.Range(1000, 9999);
        }
        else
        {
            PhotonNetwork.NickName = playerName;
        }
        Debug.Log("[PhotonLauncher] NickName défini : " + PhotonNetwork.NickName);
    }

    private void Start()
    {
        if (GetComponent<PhotonView>() == null)
        {
            Debug.LogError("[PhotonLauncher] PhotonView manquant sur l'objet PhotonLauncher ! Merci d'ajouter un PhotonView dans l'inspecteur AVANT de lancer la scène.");
        }
        
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[PhotonLauncher] Connexion à Photon...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[PHOTON LAUNCHER] OnConnectedToMaster appelé !");
        isConnectedAndReady = true;
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            Debug.Log("[PHOTON LAUNCHER] Appel lobbyUI.OnPhotonReady()");
            lobbyUI.OnPhotonReady();
        }
        else
        {
            Debug.LogError("[PHOTON LAUNCHER] lobbyUI est null dans OnConnectedToMaster !");
        }
        Debug.Log("[PhotonLauncher] Connected to Master Server - UI Ready");
    }

    public override void OnJoinedRoom()
    {
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.OnJoinedRoomUI(PhotonNetwork.CurrentRoom.Name);
        }
        
        Debug.Log($"[PhotonLauncher] Joined room: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log("[PhotonLauncher] OnJoinedRoom appelé sur " + PhotonNetwork.LocalPlayer.NickName);
        
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

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"JoinRoomFailed: {message} (code {returnCode})");
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
        Debug.Log($"[PhotonLauncher] Player {newPlayer.NickName} entered the room");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.ShowWaitingForPlayerTextIfNotFull();
            lobbyUI.UpdatePlayerList();
        }
        Debug.Log($"[PhotonLauncher] Player {otherPlayer.NickName} left the room");
    }
}