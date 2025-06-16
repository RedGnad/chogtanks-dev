using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonLauncher : MonoBehaviourPunCallbacks
{
    [PunRPC]
    public void RestartMatchSoftRPC()
    {
        Debug.Log($"[RESET SOFT] RPC reçu sur client {PhotonNetwork.LocalPlayer.NickName} (Actor {PhotonNetwork.LocalPlayer.ActorNumber})");
        // Détruit toutes les UI GameOver/Win
        foreach (var ui in GameObject.FindGameObjectsWithTag("GameOverUI"))
        {
                        Destroy(ui);
        }

        // Détruit le tank existant (s’il y en a un)
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
        else
        {
                    }

        // Respawn réseau : chaque client ré-instancie son tank après destruction
        var spawner = FindObjectOfType<PhotonTankSpawner>();
        if (spawner != null)
        {
            Debug.Log("[RESET SOFT] Appel SpawnTank() sur le client local");
            spawner.SpawnTank();
        }
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

    // ... (autres champs)

    [Header("Room Settings")]
    public string roomName = "";
    public byte maxPlayers = 8;

    private static readonly string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private System.Random rng = new System.Random();

    // Génère un code aléatoire à 4 caractères
    public string GenerateRoomCode()
    {
        char[] code = new char[4];
        for (int i = 0; i < 4; i++)
        {
            code[i] = chars[rng.Next(chars.Length)];
        }
        return new string(code);
    }

    // Appel public pour créer une room privée (code aléatoire)
    public void CreatePrivateRoom()
    {
        roomName = GenerateRoomCode();
        RoomOptions options = new RoomOptions { MaxPlayers = maxPlayers, IsVisible = true, IsOpen = true };
        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
        Debug.Log($"[PhotonLauncher] Room privée créée avec le code : {roomName}");
    }

    // Appel public pour rejoindre une room par code
    public void JoinRoomByCode(string code)
    {
        roomName = code.ToUpper();
        RoomOptions options = new RoomOptions { MaxPlayers = maxPlayers };
        PhotonNetwork.JoinRoom(roomName);
        Debug.Log($"[PhotonLauncher] Tentative de rejoindre la room avec le code : {roomName}");
    }

    private void Start()
    {
        // Vérifie que l'objet possède un PhotonView pour pouvoir envoyer les RPC
        if (GetComponent<PhotonView>() == null)
        {
            Debug.LogError("[PhotonLauncher] PhotonView manquant sur l'objet PhotonLauncher ! Merci d'ajouter un PhotonView dans l'inspecteur AVANT de lancer la scène.");
        }
        // Définit un NickName unique si non déjà défini
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Joueur_" + Random.Range(1000, 9999);
            Debug.Log("[PhotonLauncher] NickName attribué : " + PhotonNetwork.NickName);
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
        var lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
            lobbyUI.HideWaitingForPlayerTextIfRoomFull();
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

    public LobbyUI lobbyUI;

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
        var lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
            lobbyUI.HideWaitingForPlayerTextIfRoomFull();
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.UpdatePlayerList();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        var lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
            lobbyUI.ShowWaitingForPlayerTextIfNotFull();
        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.UpdatePlayerList();
        }
    }
}