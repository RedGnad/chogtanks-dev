using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class SimpleTankRespawn : MonoBehaviourPun, IMatchmakingCallbacks
{
    [Header("Respawn")]
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] public GameObject gameOverUIPrefab; // Rendu public pour être accessible depuis TankComponentAdder
    
    private bool isDead = false;
    private GameObject gameOverUI;
    
    // Composants du tank à désactiver quand mort
    private List<Renderer> renderers;
    private List<Collider2D> colliders;
    private TankMovement2D movement;
    private TankShoot2D shooting;
    private TankHealth2D healthScript;
    
    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true).ToList();
        colliders = GetComponentsInChildren<Collider2D>(true).ToList();
        movement = GetComponent<TankMovement2D>();
        shooting = GetComponent<TankShoot2D>();
        healthScript = GetComponent<TankHealth2D>();
    }
    
    private void Start()
    {
        InitializeComponents();
        
        // S'inscrire aux événements de callback Photon
        PhotonNetwork.AddCallbackTarget(this);
        
        isDead = false;
        
        Debug.Log($"[TANK] Start pour {photonView.Owner?.NickName}, {renderers.Count} renderers, {colliders.Count} colliders");
    }
    
    // Méthode séparée pour l'initialisation des composants pour pouvoir la réutiliser
    private void InitializeComponents()
    {
        // Trouver tous les renderers enfants
        renderers = GetComponentsInChildren<Renderer>(true).ToList();
        colliders = GetComponentsInChildren<Collider2D>(true).ToList();
        healthScript = GetComponent<TankHealth2D>();
        
        if (renderers.Count == 0)
        {
            Debug.LogWarning($"[TANK] Aucun renderer trouvé pour {photonView.Owner?.NickName}");
        }
        
        if (colliders.Count == 0)
        {
            Debug.LogWarning($"[TANK] Aucun collider trouvé pour {photonView.Owner?.NickName}");
        }
    }
    
    private void OnDestroy()
    {
        // Se désinscrire des événements de matchmaking
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    
    // Réinitialisation complète    // Méthode appelée pour réinitialiser l'état du tank entre les parties
    public void ResetTankState()
    {
        // Stopper toutes les coroutines en cours
        StopAllCoroutines();
        
        // Réinitialiser les composants si nécessaire
        InitializeComponents();
        
        // Réactiver le tank visuellement
        SetTankActive(true);
        
        // Réinitialiser l'état de mort
        isDead = false;
        
        // Détruire l'UI Game Over s'il existe
        if (gameOverUI != null)
        {
            Destroy(gameOverUI);
            gameOverUI = null;
        }
        
        // Réinitialiser la santé si le script existe
        if (healthScript != null)
        {
            healthScript.ResetHealth();
        }
        else
        {
            // Essayer de retrouver le script de santé
            healthScript = GetComponent<TankHealth2D>();
            if (healthScript != null)
            {
                healthScript.ResetHealth();
            }
            else
            {
                Debug.LogWarning($"[TANK] Impossible de trouver TankHealth2D pour {photonView.Owner?.NickName}");
            }
        }
        
        Debug.Log($"[TANK] État réinitialisé pour le tank {photonView.Owner?.NickName}");
        
        // Vérifier que TankComponentAdder est présent et enregistré
        if (TankComponentAdder.Instance != null && PhotonNetwork.IsConnected)
        {
            Debug.Log($"[TANK] Vérification des composants via TankComponentAdder pour {photonView.Owner?.NickName}");
        }
    }
    
    // Implémentation de IMatchmakingCallbacks
    public void OnJoinedRoom()
    {
        Debug.Log($"[TANK] OnJoinedRoom pour {photonView.Owner?.NickName} - Réinitialisation de l'état");
        
        // Un court délai pour s'assurer que tous les composants sont bien chargés
        StartCoroutine(DelayedReset());
    }

    private IEnumerator DelayedReset()
    {
        yield return new WaitForSeconds(0.2f);
        ResetTankState();
        
        // Forçage de l'état visuel après réinitialisation
        if (!isDead)
        {
            SetTankActive(true);
            Debug.Log($"[TANK] Forçage de l'activation visuelle après reset pour {photonView.Owner?.NickName}");
        }
    }

    public void OnLeftRoom()
    {
        Debug.Log($"[TANK] OnLeftRoom pour {photonView.Owner?.NickName}");
        // Réinitialiser l'état au cas où le tank persiste après avoir quitté la room
        ResetTankState();
    }
    
    public void OnFriendListUpdate(List<FriendInfo> friendList) { /* Non utilisé */ }
    public void OnCreatedRoom() { /* Non utilisé */ }
    public void OnCreateRoomFailed(short returnCode, string message) { /* Non utilisé */ }
    public void OnJoinRoomFailed(short returnCode, string message) { /* Non utilisé */ }
    public void OnJoinRandomFailed(short returnCode, string message) { /* Non utilisé */ }
    
    [PunRPC]
    public void Die(int killerActorNumber)
    {
        if (isDead)
        {
            Debug.Log($"[TANK] Ignoré Die RPC pour {photonView.Owner?.NickName}, déjà mort.");
            return;
        }
        isDead = true;
        
        Debug.Log($"[TANK] Die RPC reçu pour {photonView.Owner?.NickName}, killer: {killerActorNumber}, sur {(photonView.IsMine ? "notre client" : "un autre client")}");
        
        // Vérifier que les composants sont bien initialisés
        if (renderers == null || renderers.Count == 0 || colliders == null || colliders.Count == 0)
        {
            Debug.LogWarning($"[TANK] Renderers ou colliders non initialisés dans Die() - Réinitialisation");
            InitializeComponents();
        }
        
        // Désactiver visuellement le tank sur TOUS les clients (ne pas le détruire)
        SetTankActive(false);
        
        if (renderers.Count > 0)
        {
            Debug.Log($"[TANK] Renderers désactivés pour {photonView.Owner?.NickName}, renderer[0].enabled = {renderers[0].enabled}");
        }
        else
        {
            Debug.LogWarning($"[TANK] Pas de renderers à désactiver pour {photonView.Owner?.NickName}");
        }
        
        // IMPORTANT: Attribution des scores par le MasterClient indépendamment du propriétaire du tank
        if (PhotonNetwork.IsMasterClient && killerActorNumber > 0 && killerActorNumber != photonView.Owner.ActorNumber)
        {
            var scoreManager = ScoreManager.Instance;
            if (scoreManager != null)
            {
                Debug.Log($"[TANK] MasterClient attribue un kill à {killerActorNumber}");
                scoreManager.AddKill(killerActorNumber);
            }
            else
            {
                Debug.LogError("[TANK] ScoreManager.Instance est null, impossible d'attribuer le kill");
            }
        }

        // Afficher l'UI Game Over localement pour le propriétaire du tank
        if (photonView.IsMine)
        {
            ShowGameOverUI();
            // Démarrer la routine de respawn
            StartCoroutine(RespawnCoroutine());
        }
    }
    
    private void SetTankActive(bool active)
    {
        if (renderers == null || colliders == null)
        {
            Debug.LogWarning($"[TANK] Renderers ou colliders null dans SetTankActive({active}) - Réinitialisation");
            InitializeComponents();
        }
        
        // Sécurité supplémentaire contre les composants null
        var validRenderers = renderers?.Where(r => r != null).ToList() ?? new List<Renderer>();
        var validColliders = colliders?.Where(c => c != null).ToList() ?? new List<Collider2D>();
        
        foreach (var rend in validRenderers)
        {
            try { rend.enabled = active; }
            catch (System.Exception ex) { Debug.LogError($"[TANK] Erreur lors de l'activation du renderer: {ex.Message}"); }
        }
        
        foreach (var col in validColliders)
        {
            try { col.enabled = active; }
            catch (System.Exception ex) { Debug.LogError($"[TANK] Erreur lors de l'activation du collider: {ex.Message}"); }
        }
        
        Debug.Log($"[TANK] SetTankActive({active}) pour {photonView.Owner?.NickName}: {validRenderers.Count} renderers, {validColliders.Count} colliders");
        
        // Désactiver/activer les contrôles
        if (movement) movement.enabled = active;
        if (shooting) shooting.enabled = active;
        
        // Désactiver/activer les scripts pertinents
        var health = GetComponent<TankHealth2D>();
        if (health) health.enabled = active;
    }
    
    private IEnumerator RespawnCoroutine()
    {
        Debug.Log($"[TANK] Début du respawn pour {photonView.Owner.NickName} dans {respawnTime} secondes");
        yield return new WaitForSeconds(respawnTime);
        
        // Déterminer la position de respawn
        Vector3 respawnPosition = transform.position;
        var spawner = FindObjectOfType<PhotonTankSpawner>();
        if (spawner != null && spawner.spawnPoints != null && spawner.spawnPoints.Length > 0)
        {
            int spawnIdx = photonView.Owner.ActorNumber % spawner.spawnPoints.Length;
            respawnPosition = spawner.spawnPoints[spawnIdx].position;
        }
        
        // Appeler un RPC pour synchroniser le respawn sur tous les clients
        photonView.RPC("RespawnRPC", RpcTarget.All, respawnPosition.x, respawnPosition.y, respawnPosition.z);
        
        // Détruire l'UI GameOver
        if (gameOverUI != null)
        {
            Destroy(gameOverUI);
            gameOverUI = null;
        }
        
        Debug.Log($"[TANK] RPC de Respawn envoyé pour {photonView.Owner.NickName}");
    }
    
    [PunRPC]
    public void RespawnRPC(float x, float y, float z, PhotonMessageInfo info)
    {
        // Réactiver le tank VISUELLEMENT sur TOUS les clients
        Debug.Log($"[TANK] RespawnRPC reçu pour {photonView.Owner?.NickName} sur {(photonView.IsMine ? "notre client" : "un autre client")}!");
        
        // Réactiver le tank
        SetTankActive(true);
        isDead = false;
        
        // Repositionner à la position envoyée
        transform.position = new Vector3(x, y, z);
        
        // Restaurer la santé
        var health = GetComponent<TankHealth2D>();
        if (health != null)
        {
            health.ResetHealth();
        }
        
        Debug.Log($"[TANK] Respawn terminé pour {photonView.Owner?.NickName}, position: {transform.position}, renderers actifs: {(renderers.Count > 0 ? renderers[0].enabled : false)}");
    }
    
    private void ShowGameOverUI()
    {
        if (gameOverUIPrefab == null) return;
        
        // Détruire l'ancien UI si existant
        if (gameOverUI != null)
        {
            Destroy(gameOverUI);
        }
        
        // Créer la nouvelle UI
        Camera mainCam = Camera.main;
        if (mainCam == null) return;
        
        gameOverUI = Instantiate(gameOverUIPrefab, mainCam.transform);
        
        // Configurer l'UI
        RectTransform rt = gameOverUI.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localPosition = new Vector3(0f, 0f, 1f);
            rt.localRotation = Quaternion.identity;
            float baseScale = 1f;
            float dist = Vector3.Distance(mainCam.transform.position, rt.position);
            float scaleFactor = baseScale * (dist / mainCam.orthographicSize) * 0.1f;
            rt.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        }
        
        // Afficher GameOver
        var controller = gameOverUI.GetComponent<GameOverUIController>();
        if (controller != null)
        {
            controller.ShowGameOver();
        }
    }
}
