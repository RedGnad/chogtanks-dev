using Photon.Pun;
using UnityEngine;

public class PhotonTankSpawner : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        // Si déjà dans une room, spawn le tank (utile après reload)
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("[PhotonTankSpawner] Start: InRoom detected, spawning tank...");
            SpawnTank();
        }
    }

    public string tankPrefabName = "TankPrefab"; // Mets ici le nom EXACT de ton prefab (sans .prefab)
    public Vector2 spawnPosition = new Vector2(0, 0);

    public void SpawnTank()
    {
        GameObject tank = PhotonNetwork.Instantiate(tankPrefabName, spawnPosition, Quaternion.identity);
        var view = tank.GetComponent<PhotonView>();
        Debug.Log("[SPAWN DEBUG] Owner du tank instancié : " + (view.Owner != null ? view.Owner.NickName : "null") + " (IsMine=" + view.IsMine + ") sur client " + PhotonNetwork.LocalPlayer.NickName);

        // Masque le panneau d’attente dès que le tank est spawné
        var lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            // Ne masque plus le panel d’attente ici !
            // Affiche le code de la room pour tous les joueurs
            if (PhotonNetwork.CurrentRoom != null)
                lobbyUI.createdCodeText.text = "Room code : " + PhotonNetwork.CurrentRoom.Name;
        }
    }
}
