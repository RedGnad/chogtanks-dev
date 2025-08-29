using UnityEngine;
using Photon.Pun;
using System.Collections;

public class PowerupSpawner : MonoBehaviourPun
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 30f; 
    [SerializeField] private int maxPowerupsInScene = 3;
    
    [Header("Powerup Prefabs (must be in Resources/Powerups/)")]
    [SerializeField] private string[] powerupPrefabNames = {
        "RicochetPowerup",
        "ExplosivePowerup", 
        "CloakPowerup"
    };
    
    private int currentPowerupCount = 0;
    
    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SpawnPowerupsRoutine());
        }
    }
    
    void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += OnMasterClientSwitched;
    }
    
    void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnMasterClientSwitched;
    }
    
    private void OnMasterClientSwitched(ExitGames.Client.Photon.EventData eventData)
    {
        if (eventData.Code == 208) 
        {
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(SpawnPowerupsRoutine());
            }
        }
    }
    
    private IEnumerator SpawnPowerupsRoutine()
    {
        while (PhotonNetwork.IsMasterClient)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            if (currentPowerupCount < maxPowerupsInScene && spawnPoints.Length > 0)
            {
                SpawnRandomPowerup();
            }
        }
    }
    
    private void SpawnRandomPowerup()
    {
        if (powerupPrefabNames.Length == 0 || spawnPoints.Length == 0) return;
        
        string randomPowerup = powerupPrefabNames[Random.Range(0, powerupPrefabNames.Length)];
        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        Collider2D existingPowerup = Physics2D.OverlapCircle(randomSpawnPoint.position, 2f);
        if (existingPowerup != null && existingPowerup.GetComponent<MonoBehaviourPun>() != null)
        {
            return;
        }
        
        string prefabPath = "Powerups/" + randomPowerup;
        GameObject powerup = PhotonNetwork.Instantiate(prefabPath, randomSpawnPoint.position, Quaternion.identity);
        
        if (powerup != null)
        {
            currentPowerupCount++;
            Debug.Log($"[PowerupSpawner] Spawned {randomPowerup} at {randomSpawnPoint.name}. Total: {currentPowerupCount}");
            
            StartCoroutine(WaitForPowerupDestruction(powerup));
        }
    }
    
    private IEnumerator WaitForPowerupDestruction(GameObject powerup)
    {
        while (powerup != null)
        {
            yield return new WaitForSeconds(1f);
        }
        
        currentPowerupCount--;
    }
    
    [ContextMenu("Spawn Random Powerup")]
    public void SpawnRandomPowerupManual()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnRandomPowerup();
        }
    }
}
