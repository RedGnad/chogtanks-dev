using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

[DefaultExecutionOrder(-1000)] // Priorité MAXIMALE pour s'exécuter avant tous les autres scripts
public class TankComponentAdder : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject gameOverUIPrefab;
    
    private List<int> processedViewIds = new List<int>();
    
    public static TankComponentAdder Instance { get; private set; }
    
    void Awake()
    {
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
        
        TreatExistingTanks();
        
        StartCoroutine(CheckForNewTanks());
    }
    
    private void TreatExistingTanks()
    {
        PhotonView[] views = FindObjectsOfType<PhotonView>();
        foreach (PhotonView view in views)
        {
            AddComponentToTank(view);
        }
    }
    
    private void AddComponentToTank(PhotonView view)
    {
        TankHealth2D health = view.GetComponent<TankHealth2D>();
        if (health == null) return; // Pas un tank, on ignore
        
        SimpleTankRespawn respawn = view.GetComponent<SimpleTankRespawn>();
        if (respawn == null)
        {
            try
            {
                respawn = view.gameObject.AddComponent<SimpleTankRespawn>();
                
                if (gameOverUIPrefab != null)
                {
                    respawn.gameOverUIPrefab = gameOverUIPrefab;
                }
                
                string ownerName = view.Owner != null ? view.Owner.NickName : "unknown";
                
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
            if (respawn.gameOverUIPrefab == null && gameOverUIPrefab != null)
            {
                respawn.gameOverUIPrefab = gameOverUIPrefab;
            }
        }
    }
    
    private IEnumerator CheckForNewTanks()
    {
        while (true)
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                PhotonView[] views = FindObjectsOfType<PhotonView>();
                foreach (PhotonView view in views)
                {
                    AddComponentToTank(view);
                }
            }
            
            yield return new WaitForSeconds(0.2f); // Vérification plus fréquente (5 fois par seconde)
        }
    }
    
    public override void OnJoinedRoom()
    {
        processedViewIds.Clear();
        TreatExistingTanks();
    }
    
    public override void OnLeftRoom()
    {
        processedViewIds.Clear();
    }
    
    public void ResetAndTreatAllTanks()
    {
        processedViewIds.Clear();
        TreatExistingTanks();
    }
}
