using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Ajoute automatiquement tous les composants nécessaires aux tanks lors de leur instantiation
/// </summary>
[DefaultExecutionOrder(-1000)] // Priorité MAXIMALE pour s'exécuter avant tous les autres scripts
public class TankComponentAdder : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject gameOverUIPrefab;
    
    // Liste des tanks déjà traités (pour éviter les doublons)
    private List<int> processedViewIds = new List<int>();
    
    // Singleton pour accès global
    public static TankComponentAdder Instance { get; private set; }
    
    void Awake()
    {
        // Configuration du singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Traitement immédiat des tanks existants dans la scène
        TreatExistingTanks();
        
        // Démarrer la routine de surveillance des nouveaux tanks pour ceux créés plus tard
        StartCoroutine(CheckForNewTanks());
    }
    
    private void TreatExistingTanks()
    {
        // Traitement immédiat de tous les tanks déjà existants
        PhotonView[] views = FindObjectsOfType<PhotonView>();
        foreach (PhotonView view in views)
        {
            AddComponentToTank(view);
        }
    }
    
    // Méthode commune pour ajouter le composant à un tank
    private void AddComponentToTank(PhotonView view)
    {
        // Vérifier si c'est un tank avec TankHealth2D
        TankHealth2D health = view.GetComponent<TankHealth2D>();
        if (health == null) return; // Pas un tank, on ignore
        
        // C'est un tank, toujours vérifier et ajouter notre gestionnaire de respawn s'il n'est pas déjà présent
        SimpleTankRespawn respawn = view.GetComponent<SimpleTankRespawn>();
        if (respawn == null)
        {
            try
            {
                respawn = view.gameObject.AddComponent<SimpleTankRespawn>();
                
                // Configurer le composant
                if (gameOverUIPrefab != null)
                {
                    respawn.gameOverUIPrefab = gameOverUIPrefab;
                }
                
                string ownerName = view.Owner != null ? view.Owner.NickName : "unknown";
                Debug.Log($"[TankComponentAdder] Ajout de SimpleTankRespawn au tank {ownerName} (ViewID: {view.ViewID})");
                
                // Marquer comme traité seulement si l'ajout a réussi
                if (!processedViewIds.Contains(view.ViewID))
                {
                    processedViewIds.Add(view.ViewID);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TankComponentAdder] Erreur lors de l'ajout de SimpleTankRespawn: {ex.Message}");
            }
        }
        else
        {
            // Vérifier que le composant est bien configuré
            if (respawn.gameOverUIPrefab == null && gameOverUIPrefab != null)
            {
                respawn.gameOverUIPrefab = gameOverUIPrefab;
                Debug.Log($"[TankComponentAdder] Mise à jour du gameOverUIPrefab pour le tank {view.ViewID}");
            }
        }
    }
    
    private IEnumerator CheckForNewTanks()
    {
        while (true)
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                // Vérifier tous les PhotonView dans la scène
                PhotonView[] views = FindObjectsOfType<PhotonView>();
                foreach (PhotonView view in views)
                {
                    AddComponentToTank(view);
                }
            }
            
            // Attendre avant la prochaine vérification
            yield return new WaitForSeconds(0.2f); // Vérification plus fréquente (5 fois par seconde)
        }
    }
    
    // Réinitialiser la liste des tanks traités quand on entre dans une nouvelle room
    public override void OnJoinedRoom()
    {
        Debug.Log("[TankComponentAdder] OnJoinedRoom: Réinitialisation de la liste des tanks traités");
        processedViewIds.Clear();
        TreatExistingTanks();
    }
    
    // Réinitialiser la liste des tanks traités quand on quitte une room
    public override void OnLeftRoom()
    {
        Debug.Log("[TankComponentAdder] OnLeftRoom: Réinitialisation de la liste des tanks traités");
        processedViewIds.Clear();
    }
    
    // Méthode publique pour forcer la réinitialisation et le traitement
    public void ResetAndTreatAllTanks()
    {
        Debug.Log("[TankComponentAdder] ForceReset: Réinitialisation de la liste des tanks traités");
        processedViewIds.Clear();
        TreatExistingTanks();
    }
}
