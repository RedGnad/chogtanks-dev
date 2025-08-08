using UnityEngine;
using Fusion;
using System.Collections.Generic;

[DefaultExecutionOrder(-1000)] 
public class TankComponentAdder : NetworkBehaviour
{
    [SerializeField] private GameObject gameOverUIPrefab;
    
    private List<uint> processedViewIds = new List<uint>();
    
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
        NetworkObject[] views = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        foreach (NetworkObject view in views)
        {
            AddComponentToTank(view);
        }
    }
    
    private void AddComponentToTank(NetworkObject view)
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
                
                string ownerName = view != null ? view.ToString() : "unknown";
                
                if (!processedViewIds.Contains(view.Id.Raw))
                {
                    processedViewIds.Add(view.Id.Raw);
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
    
    private System.Collections.IEnumerator CheckForNewTanks()
    {
        while (true)
        {
            if (Runner.LocalPlayer != null && Runner.IsClient && Runner.IsConnectedToServer)
            {
                NetworkObject[] views = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
                foreach (NetworkObject view in views)
                {
                    AddComponentToTank(view);
                }
            }
            
            yield return new WaitForSeconds(1.0f); 
        }
    }
    
    // OnConnectedToServer removed for Fusion
    public void OnConnectedToServerFusion()
    {
        processedViewIds.Clear();
        TreatExistingTanks();
    }
    
    // OnDisconnectedFromServer removed for Fusion
    public void OnDisconnectedFromServerFusion()
    {
        processedViewIds.Clear();
    }
    
    public void ResetAndTreatAllTanks()
    {
        processedViewIds.Clear();
        TreatExistingTanks();
    }
}
