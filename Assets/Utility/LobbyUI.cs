using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Photon.Pun;
using Photon.Realtime;
using System.Text;

public class LobbyUI : MonoBehaviour
{
    public TMP_Text waitingForPlayerText; // Assigne ce champ dans l'inspecteur Unity

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
    // ... (autres membres)
    // Obsolète : remplacé par HideWaitingForPlayerTextIfRoomFull/ShowWaitingForPlayerTextIfNotFull
    [System.Obsolete("Utilise HideWaitingForPlayerTextIfRoomFull à la place")]
    public void HideWaitingPanelIfRoomFull() {}
    [System.Obsolete("Utilise ShowWaitingForPlayerTextIfNotFull à la place")]
    public void ShowWaitingPanelIfNotFull() {}
    public Button createRoomButton;
    public Button joinRoomButton;
    public TMP_InputField joinCodeInput;
    public TMP_Text createdCodeText;
    public GameObject joinPanel;
    public GameObject waitingPanel;
    public TMP_Text playerListText;
    public Button backButton;

    private PhotonLauncher launcher;

    void Start()
    {
        // Ajout listeners bouton retour
        if (backButton != null)
            backButton.onClick.AddListener(OnBackToLobby);

        launcher = FindObjectOfType<PhotonLauncher>();
        createRoomButton.onClick.AddListener(OnCreateRoom);
        joinRoomButton.onClick.AddListener(OnJoinRoom);
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
        createdCodeText.text = "";
        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
    }

    void OnCreateRoom()
    {
        if (!launcher.isConnectedAndReady) return;
        launcher.CreatePrivateRoom();
        createdCodeText.text = "Room code : " + launcher.roomName;
        joinPanel.SetActive(false);
        waitingPanel.SetActive(true);
    }

    void OnJoinRoom()
    {
        if (!launcher.isConnectedAndReady) return;
        string code = joinCodeInput.text.Trim().ToUpper();
        if (code.Length == 4)
        {
            launcher.JoinRoomByCode(code);
            joinPanel.SetActive(false);
            waitingPanel.SetActive(true);
        }
        else
        {
            createdCodeText.text = "Code invalide (4 caractères)";
        }
    }

    // Appelée par PhotonLauncher quand la connexion est prête
    public void OnPhotonReady()
    {
        Debug.Log("[LOBBY UI] OnPhotonReady appelé : boutons activés !");
        createRoomButton.interactable = true;
        joinRoomButton.interactable = true;
    }

    // Appelée par PhotonLauncher quand le join échoue
    public void OnJoinRoomFailedUI()
    {
        createdCodeText.text = "Aucune room trouvée avec ce code.";
        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
    }

    // Affiche le code de la room rejointe/créée dans le panneau d'attente
    public void OnJoinedRoomUI(string code)
    {
        createdCodeText.text = "Room code : " + code;
        joinPanel.SetActive(false);
        waitingPanel.SetActive(true);
        UpdatePlayerList();
    }

    // Met à jour dynamiquement la liste des joueurs
    public void UpdatePlayerList()
    {
        if (playerListText == null) return;
        StringBuilder sb = new StringBuilder();
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            sb.AppendLine(p.NickName != null && p.NickName != "" ? p.NickName : $"Player {p.ActorNumber}");
        }
        playerListText.text = sb.ToString();
    }

    // Bouton retour : quitte la room et revient au lobby
    public void OnBackToLobby()
    {
        PhotonNetwork.LeaveRoom();
        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
        createdCodeText.text = "";
        playerListText.text = "";
        // Détruit toutes les UI GameOver/Win dans la scène
        foreach (var ui in GameObject.FindGameObjectsWithTag("GameOverUI"))
        {
            Destroy(ui);
        }
    }
}
