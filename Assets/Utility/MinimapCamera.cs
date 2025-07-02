using UnityEngine;
using Photon.Pun;

public class MinimapCamera : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float height = 20f;
    [SerializeField] private float mapSize = 15f;
    
    private Camera minimapCam;
    private Transform playerTarget;
    private bool isInGameMode = false;
    private bool wasInGameMode = false; // NOUVEAU : Pour détecter les transitions
    
    private void Awake()
    {
        minimapCam = GetComponent<Camera>();
        if (minimapCam == null)
        {
            minimapCam = gameObject.AddComponent<Camera>();
        }
        
        // Configuration 2D
        minimapCam.orthographic = true;
        minimapCam.orthographicSize = mapSize;
        minimapCam.depth = 1;
        minimapCam.clearFlags = CameraClearFlags.Depth;
        minimapCam.rect = new Rect(0.75f, 0.0f, 0.25f, 0.25f);
        minimapCam.cullingMask = -1;
        
        transform.position = new Vector3(0, 0, -height);
        transform.rotation = Quaternion.identity;
        
        minimapCam.enabled = false;
    }
    
    private void Start()
    {
        // MODIFICATION : Vérification plus fréquente au début
        InvokeRepeating(nameof(CheckForTanks), 0f, 0.5f);
    }
    
    private void CheckForTanks()
    {
        // MODIFICATION : Utilise PhotonNetwork.InRoom pour une détection plus fiable
        bool shouldBeInGameMode = PhotonNetwork.InRoom;
        
        if (shouldBeInGameMode)
        {
            var tanks = FindObjectsOfType<TankHealth2D>();
            shouldBeInGameMode = tanks.Length > 0;
        }
        
        // NOUVEAU : Détecte les transitions pour reset le système
        if (shouldBeInGameMode != wasInGameMode)
        {
            Debug.Log($"[MinimapCamera] Transition détectée: {wasInGameMode} → {shouldBeInGameMode}");
            
            if (shouldBeInGameMode && !isInGameMode)
            {
                // ENTRÉE en mode jeu
                EnterGameMode();
            }
            else if (!shouldBeInGameMode && isInGameMode)
            {
                // SORTIE du mode jeu
                ExitGameMode();
            }
            
            wasInGameMode = shouldBeInGameMode;
        }
        
        // NOUVEAU : Si en mode jeu mais pas de target, cherche plus fréquemment
        if (isInGameMode && playerTarget == null)
        {
            FindPlayerTarget();
        }
    }
    
    private void EnterGameMode()
    {
        Debug.Log("[MinimapCamera] ENTRÉE en mode jeu - Reset complet");
        
        isInGameMode = true;
        minimapCam.enabled = true;
        
        // RESET complet du target
        playerTarget = null;
        
        // NOUVEAU : Recherche immédiate + répétée jusqu'à trouver
        CancelInvoke(nameof(FindPlayerTarget));
        InvokeRepeating(nameof(FindPlayerTarget), 0f, 0.2f);
    }
    
    private void ExitGameMode()
    {
        Debug.Log("[MinimapCamera] SORTIE du mode jeu - Nettoyage");
        
        isInGameMode = false;
        minimapCam.enabled = false;
        
        // RESET complet
        playerTarget = null;
        
        // ARRÊT de la recherche
        CancelInvoke(nameof(FindPlayerTarget));
    }
    
    private void LateUpdate()
    {
        if (isInGameMode && playerTarget != null)
        {
            Vector3 newPos = playerTarget.position;
            newPos.z = -height;
            transform.position = newPos;
        }
    }
    
    private void FindPlayerTarget()
    {
        if (!isInGameMode) 
        {
            CancelInvoke(nameof(FindPlayerTarget));
            return;
        }
        
        // MÉTHODE 1 : Par tag Player (le plus fiable après respawn)
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerGO in playerObjects)
        {
            var health = playerGO.GetComponent<TankHealth2D>();
            if (health != null && health.photonView != null && health.photonView.IsMine)
            {
                playerTarget = playerGO.transform;
                Debug.Log("[MinimapCamera] Target trouvé via tag Player - SUCCÈS");
                CancelInvoke(nameof(FindPlayerTarget)); // ARRÊT de la recherche
                return;
            }
        }
        
        // MÉTHODE 2 : Fallback via TankHealth2D
        var tanks = FindObjectsOfType<TankHealth2D>();
        foreach (var tank in tanks)
        {
            if (tank.photonView != null && tank.photonView.IsMine && !tank.IsDead)
            {
                playerTarget = tank.transform;
                Debug.Log("[MinimapCamera] Target trouvé via TankHealth2D - SUCCÈS");
                CancelInvoke(nameof(FindPlayerTarget)); // ARRÊT de la recherche
                return;
            }
        }
        
        // DEBUG : Logs détaillés si pas trouvé
        Debug.Log($"[MinimapCamera] Target NON trouvé - Tanks: {tanks.Length}, InRoom: {PhotonNetwork.InRoom}, LocalPlayer: {PhotonNetwork.LocalPlayer?.NickName}");
        foreach (var tank in tanks)
        {
            string owner = tank.photonView.Owner?.NickName ?? "null";
            Debug.Log($"[MinimapCamera] Tank: {owner}, IsMine: {tank.photonView.IsMine}, IsDead: {tank.IsDead}");
        }
    }
    
    // NOUVEAU : Méthode publique pour forcer un reset (appelable depuis PhotonLauncher si nécessaire)
    public void ForceReset()
    {
        Debug.Log("[MinimapCamera] FORCE RESET demandé");
        ExitGameMode();
        wasInGameMode = false;
        
        // Re-check immédiat
        Invoke(nameof(CheckForTanks), 0.1f);
    }
}