using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviourPunCallbacks
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public Transform[] enemySpawnPoints;
    public float soloDetectionDelay = 5f;
    public float enemySpawnInterval = 4f;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    private bool isPlayerSolo = false;
    private Coroutine soloDetectionCoroutine;
    private Coroutine enemySpawningCoroutine;
    private List<GameObject> activeEnemies = new List<GameObject>();
    
    public static EnemyManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Start monitoring player count
        StartCoroutine(MonitorPlayerCount());
    }
    
    private IEnumerator MonitorPlayerCount()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            if (!PhotonNetwork.InRoom) 
            {
                // If not in room, reset state
                if (isPlayerSolo)
                {
                    Debug.Log("[ENEMY MANAGER] Not in room anymore, resetting solo state");
                    isPlayerSolo = false;
                }
                continue;
            }
            
            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            bool shouldBeSolo = playerCount == 1;
            
            if (shouldBeSolo && !isPlayerSolo)
            {
                // Player just became solo
                OnPlayerBecameSolo();
            }
            else if (!shouldBeSolo && isPlayerSolo)
            {
                // Player is no longer solo
                OnPlayerNoLongerSolo();
            }
        }
    }
    
    private void OnPlayerBecameSolo()
    {
        if (enableDebugLogs)
            Debug.Log("[ENEMY MANAGER] Player became solo, starting enemy spawn timer");
            
        isPlayerSolo = true;
        
        // Start the 5-second delay before spawning first enemy
        if (soloDetectionCoroutine != null)
            StopCoroutine(soloDetectionCoroutine);
            
        soloDetectionCoroutine = StartCoroutine(StartEnemySpawning());
    }
    
    private void OnPlayerNoLongerSolo()
    {
        if (enableDebugLogs)
            Debug.Log("[ENEMY MANAGER] Player no longer solo, stopping enemy spawning");
            
        isPlayerSolo = false;
        
        // Stop all enemy-related coroutines
        if (soloDetectionCoroutine != null)
        {
            StopCoroutine(soloDetectionCoroutine);
            soloDetectionCoroutine = null;
        }
        
        if (enemySpawningCoroutine != null)
        {
            StopCoroutine(enemySpawningCoroutine);
            enemySpawningCoroutine = null;
        }
        
        // Destroy all active enemies
        DestroyAllEnemies();
    }
    
    private IEnumerator StartEnemySpawning()
    {
        // Wait 5 seconds before spawning first enemy
        yield return new WaitForSeconds(soloDetectionDelay);
        
        // Check if player is still solo
        if (!isPlayerSolo || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            yield break;
        }
        
        // Spawn first enemy
        SpawnEnemy();
        
        // Start continuous spawning every 4 seconds
        enemySpawningCoroutine = StartCoroutine(ContinuousEnemySpawning());
    }
    
    private IEnumerator ContinuousEnemySpawning()
    {
        while (isPlayerSolo && PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            yield return new WaitForSeconds(enemySpawnInterval);
            SpawnEnemy();
        }
    }
    
    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[ENEMY MANAGER] Enemy prefab not assigned!");
            return;
        }
        
        if (!PhotonNetwork.IsMasterClient)
        {
            return; // Only master client spawns enemies
        }
        
        Vector3 spawnPosition = GetRandomSpawnPosition();
        
        if (enableDebugLogs)
            Debug.Log($"[ENEMY MANAGER] Spawning enemy at position: {spawnPosition}");
            
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        activeEnemies.Add(enemy);
        
        // Debug enemy spawn
        Debug.Log($"[ENEMY MANAGER] Enemy spawned at {spawnPosition}");
        
        // Check if enemy has renderer components
        SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Debug.Log($"[ENEMY MANAGER] Enemy SpriteRenderer found - Enabled: {renderer.enabled}, Sprite: {renderer.sprite?.name}");
        }
        else
        {
            Debug.LogWarning("[ENEMY MANAGER] No SpriteRenderer found on enemy!");
        }
        
        // Clean up null references
        activeEnemies.RemoveAll(e => e == null);
    }
    
    private Vector3 GetRandomSpawnPosition()
    {
        if (enemySpawnPoints != null && enemySpawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, enemySpawnPoints.Length);
            return enemySpawnPoints[randomIndex].position;
        }
        else
        {
            // Fallback to random position around the map edges
            float mapSize = 8f;
            Vector3 position = Vector3.zero;
            
            int side = Random.Range(0, 4);
            switch (side)
            {
                case 0: // Top
                    position = new Vector3(Random.Range(-mapSize, mapSize), mapSize, 0);
                    break;
                case 1: // Bottom
                    position = new Vector3(Random.Range(-mapSize, mapSize), -mapSize, 0);
                    break;
                case 2: // Left
                    position = new Vector3(-mapSize, Random.Range(-mapSize, mapSize), 0);
                    break;
                case 3: // Right
                    position = new Vector3(mapSize, Random.Range(-mapSize, mapSize), 0);
                    break;
            }
            
            return position;
        }
    }
    
    public void OnEnemyDestroyed(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }
    
    public void CleanupAllEnemies()
    {
        Debug.Log($"[ENEMY MANAGER] Cleaning up {activeEnemies.Count} enemies");
        
        // Destroy all active enemies
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] != null)
            {
                Destroy(activeEnemies[i]);
            }
        }
        
        activeEnemies.Clear();
        
        // Stop spawning coroutines
        if (enemySpawningCoroutine != null)
        {
            StopCoroutine(enemySpawningCoroutine);
            enemySpawningCoroutine = null;
        }
        
        if (soloDetectionCoroutine != null)
        {
            StopCoroutine(soloDetectionCoroutine);
            soloDetectionCoroutine = null;
        }
        
        // Reset state completely
        isPlayerSolo = false;
        Debug.Log("[ENEMY MANAGER] All enemies cleaned up and state reset");
    }
    
    private void DestroyAllEnemies()
    {
        if (enableDebugLogs && activeEnemies.Count > 0)
            Debug.Log($"[ENEMY MANAGER] Destroying {activeEnemies.Count} active enemies");
            
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        
        activeEnemies.Clear();
    }
    
    public override void OnJoinedRoom()
    {
        Debug.Log("[ENEMY MANAGER] Joined room - resetting enemy state");
        // Clean up any leftover enemies from previous sessions
        CleanupAllEnemies();
        // The MonitorPlayerCount coroutine will handle solo detection
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // This will be caught by the MonitorPlayerCount coroutine
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // This will be caught by the MonitorPlayerCount coroutine
        Debug.Log($"[ENEMY MANAGER] Player left room: {otherPlayer.NickName}");
    }
    
    public override void OnLeftRoom()
    {
        Debug.Log("[ENEMY MANAGER] Local player left room - cleaning up all enemies");
        CleanupAllEnemies();
    }
    
    private void OnDestroy()
    {
        if (soloDetectionCoroutine != null)
            StopCoroutine(soloDetectionCoroutine);
            
        if (enemySpawningCoroutine != null)
            StopCoroutine(enemySpawningCoroutine);
    }
}
