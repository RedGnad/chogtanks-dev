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
        StartCoroutine(MonitorPlayerCount());
    }
    
    private IEnumerator MonitorPlayerCount()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            if (!PhotonNetwork.InRoom) 
            {
                if (isPlayerSolo)
                {
                    isPlayerSolo = false;
                }
                continue;
            }
            
            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            bool shouldBeSolo = playerCount == 1;
            
            if (shouldBeSolo && !isPlayerSolo)
            {
                OnPlayerBecameSolo();
            }
            else if (!shouldBeSolo && isPlayerSolo)
            {
                OnPlayerNoLongerSolo();
            }
        }
    }
    
    private void OnPlayerBecameSolo()
    {
        if (enableDebugLogs)
            Debug.Log("[ENEMY MANAGER] Player became solo, starting enemy spawn timer");
            
        isPlayerSolo = true;
        
        if (soloDetectionCoroutine != null)
            StopCoroutine(soloDetectionCoroutine);
            
        soloDetectionCoroutine = StartCoroutine(StartEnemySpawning());
    }
    
    private void OnPlayerNoLongerSolo()
    {
        if (enableDebugLogs)
            Debug.Log("[ENEMY MANAGER] Player no longer solo, stopping enemy spawning");
            
        isPlayerSolo = false;
        
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
        
        DestroyAllEnemies();
    }
    
    private IEnumerator StartEnemySpawning()
    {
        yield return new WaitForSeconds(soloDetectionDelay);
        
        if (!isPlayerSolo || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            yield break;
        }
        
        SpawnEnemy();
        
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
            return;
        }
        
        if (!PhotonNetwork.IsMasterClient)
        {
            return; 
        }
        
        Vector3 spawnPosition = GetRandomSpawnPosition();
        
            
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        activeEnemies.Add(enemy);
        
        
        SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            //Debug.Log($"[ENEMY MANAGER] Enemy SpriteRenderer found - Enabled: {renderer.enabled}, Sprite: {renderer.sprite?.name}");
        }
        else
        {
            //Debug.LogWarning("[ENEMY MANAGER] No SpriteRenderer found on enemy!");
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
            float mapSize = 8f;
            Vector3 position = Vector3.zero;
            
            int side = Random.Range(0, 4);
            switch (side)
            {
                case 0: 
                    position = new Vector3(Random.Range(-mapSize, mapSize), mapSize, 0);
                    break;
                case 1:
                    position = new Vector3(Random.Range(-mapSize, mapSize), -mapSize, 0);
                    break;
                case 2: 
                    position = new Vector3(-mapSize, Random.Range(-mapSize, mapSize), 0);
                    break;
                case 3: 
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
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] != null)
            {
                Destroy(activeEnemies[i]);
            }
        }
        
        activeEnemies.Clear();
        
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
        
        isPlayerSolo = false;
    }
    
    private void DestroyAllEnemies()
    {
            
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
        CleanupAllEnemies();
        
        isPlayerSolo = false;
        
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // This will be caught by the MonitorPlayerCount coroutine
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // This will be caught by the MonitorPlayerCount coroutine
    }
    
    public override void OnLeftRoom()
    {
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
