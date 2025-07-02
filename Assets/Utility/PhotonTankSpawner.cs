using Photon.Pun;
using UnityEngine;

public class PhotonTankSpawner : MonoBehaviourPunCallbacks
{
    [Header("Spawns multiples")]
    public Transform[] spawnPoints; // À assigner dans l'inspecteur Unity (drag & drop)

    public string tankPrefabName = "TankPrefab"; // Mets ici le nom EXACT de ton prefab (sans .prefab)
    public Vector2 fallbackSpawnPosition = new Vector2(0, 0); // Utilisé si pas de spawnPoints

    private void Start()
    {
        // Si déjà dans une room, spawn le tank (utile après reload)
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("[PhotonTankSpawner] Start: InRoom detected, spawning tank...");
            SpawnTank();
        }
    }

    public void SpawnTank()
    {
        Vector2 spawnPos = fallbackSpawnPosition;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Utilise l'index unique du joueur dans la room pour choisir le spawn
            int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
            int spawnIdx = playerIndex % spawnPoints.Length;
            spawnPos = spawnPoints[spawnIdx].position;
        }

        GameObject tank = PhotonNetwork.Instantiate(tankPrefabName, spawnPos, Quaternion.identity);
        var view = tank.GetComponent<PhotonView>();
        Debug.Log("[SPAWN DEBUG] Owner du tank instancié : " + (view.Owner != null ? view.Owner.NickName : "null") + " (IsMine=" + view.IsMine + ") sur client " + PhotonNetwork.LocalPlayer.NickName);

        // AJOUT : S'assurer que le nom est affiché correctement
        var nameDisplay = tank.GetComponent<PlayerNameDisplay>();
        if (nameDisplay != null)
        {
            Debug.Log("[SPAWN DEBUG] PlayerNameDisplay trouvé et configuré pour " + PhotonNetwork.LocalPlayer.NickName);
        }
        else
        {
            Debug.LogWarning("[SPAWN DEBUG] PlayerNameDisplay non trouvé sur le prefab TankPrefab");
        }

        // Affiche le code de la room pour tous les joueurs
        var lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null && PhotonNetwork.CurrentRoom != null)
        {
            lobbyUI.createdCodeText.text = "Room code : " + PhotonNetwork.CurrentRoom.Name;
        }
    }
}