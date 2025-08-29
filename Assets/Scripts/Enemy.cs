using Photon.Pun;
using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 180f;
    public float targetUpdateInterval = 0.5f;
    
    [Header("Collision Settings")]
    public LayerMask wallLayerMask = 1;
    public float wallCheckDistance = 0.6f;
    
    [Header("Visual Settings")]
    public SpriteRenderer spriteRenderer;
    public Color enemyColor = Color.red;
    
    private Transform targetPlayer;
    private Vector2 moveDirection;
    private Rigidbody2D rb;
    private Coroutine targetUpdateCoroutine;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // Configure rigidbody for smooth movement
        rb.gravityScale = 0f;
        rb.drag = 1f;
        rb.angularDrag = 5f;
        
        // Set up collider if not present
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;
            collider.isTrigger = true; // Trigger for player collision detection
        }
        
        // Set up visual appearance
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Debug sprite setup
        if (spriteRenderer != null)
        {
            Debug.Log($"[ENEMY] SpriteRenderer found - Enabled: {spriteRenderer.enabled}, Sprite: {spriteRenderer.sprite?.name}, Color: {spriteRenderer.color}");
            
            // Ensure sprite is visible
            if (spriteRenderer.sprite == null)
            {
                Debug.LogWarning("[ENEMY] No sprite assigned to SpriteRenderer!");
            }
            
            // Set enemy color
            spriteRenderer.color = enemyColor;
        }
        else
        {
            Debug.LogError("[ENEMY] No SpriteRenderer component found!");
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = enemyColor;
        }
        
        // Start targeting system
        targetUpdateCoroutine = StartCoroutine(UpdateTargetPeriodically());
        
        Debug.Log("[ENEMY] Enemy spawned and initialized");
    }
    
    private IEnumerator UpdateTargetPeriodically()
    {
        while (true)
        {
            FindNearestPlayer();
            yield return new WaitForSeconds(targetUpdateInterval);
        }
    }
    
    private void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closestPlayer = null;
        float closestDistance = Mathf.Infinity;
        
        foreach (GameObject player in players)
        {
            PhotonView playerView = player.GetComponent<PhotonView>();
            if (playerView != null && playerView.IsMine)
            {
                float distance = Vector2.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player.transform;
                }
            }
        }
        
        targetPlayer = closestPlayer;
    }
    
    private void Update()
    {
        MoveTowardsTarget();
    }
    
    private void MoveTowardsTarget()
    {
        if (targetPlayer == null) return;
        
        // Simple direct movement towards player
        Vector2 directionToTarget = (targetPlayer.position - transform.position).normalized;
        
        // Move directly towards the player
        rb.velocity = directionToTarget * moveSpeed;
        
        // Rotate towards movement direction
        if (directionToTarget != Vector2.zero)
        {
            float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            float targetAngle = angle - 90f; // Adjust for sprite orientation
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                Quaternion.AngleAxis(targetAngle, Vector3.forward), 
                rotationSpeed * Time.deltaTime
            );
        }
    }
    
    private bool IsWallInDirection(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            direction, 
            wallCheckDistance, 
            wallLayerMask
        );
        
        return hit.collider != null;
    }
    
    private Vector2 FindAlternativeDirection(Vector2 originalDirection)
    {
        // Try different angles to find a clear path
        float[] angles = { -45f, 45f, -90f, 90f, -135f, 135f, 180f };
        
        foreach (float angle in angles)
        {
            Vector2 testDirection = RotateVector(originalDirection, angle);
            if (!IsWallInDirection(testDirection))
            {
                return testDirection;
            }
        }
        
        // If no clear path found, stop moving
        return Vector2.zero;
    }
    
    private Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if hit by shell
        if (other.CompareTag("Shell"))
        {
            OnHitByShell();
            return;
        }
        
        // Check if touching player
        if (other.CompareTag("Player"))
        {
            PhotonView playerView = other.GetComponent<PhotonView>();
            if (playerView != null && playerView.IsMine)
            {
                OnTouchPlayer(other.gameObject);
            }
        }
    }
    
    public void OnHitByShell()
    {
        Debug.Log("[ENEMY] Hit by shell, destroying enemy");
        
        // Notify EnemyManager
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyDestroyed(gameObject);
        }
        
        // Destroy the enemy (local object, no networking needed)
        Destroy(gameObject);
    }
    
    private void OnTouchPlayer(GameObject player)
    {
        Debug.Log("[ENEMY] Touched player, ejecting player from room");
        
        // Clean up all enemies before ejecting player
        EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager != null)
        {
            Debug.Log("[ENEMY] Cleaning up all enemies before player ejection");
            enemyManager.CleanupAllEnemies();
        }
        
        // Send score to Firebase before ejecting player using proper ScoreManager method
        if (ScoreManager.Instance != null)
        {
            int currentScore = ScoreManager.Instance.GetPlayerScore(PhotonNetwork.LocalPlayer.ActorNumber);
            Debug.Log($"[ENEMY] Sending score {currentScore} to Firebase before ejection");
            
            // Use the proper Firebase score submission method from ScoreManager
            ScoreManager.Instance.SubmitScoreToFirebase(currentScore, 0); // 0 bonus for enemy kill
            
            // Trigger NFT manager refresh to update UI
            ChogTanksNFTManager nftManager = FindObjectOfType<ChogTanksNFTManager>();
            if (nftManager != null)
            {
                Debug.Log("[ENEMY] Triggering NFT manager refresh for UI update");
                nftManager.ForceRefreshAfterMatch(currentScore);
            }
        }
        
        // Eject the player using the same mechanism as end of match
        PhotonLauncher launcher = FindObjectOfType<PhotonLauncher>();
        if (launcher != null)
        {
            // Show game over UI and return to lobby
            launcher.photonView.RPC("ShowWinnerToAllRPC", RpcTarget.All, "Enemy Victory!", -1);
        }
        
        // Destroy this enemy after touching player (local object)
        Destroy(gameObject);
    }
    
    private void OnDestroy()
    {
        if (targetUpdateCoroutine != null)
        {
            StopCoroutine(targetUpdateCoroutine);
        }
        
        // Notify EnemyManager
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyDestroyed(gameObject);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw wall detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wallCheckDistance);
        
        // Draw direction to target
        if (targetPlayer != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPlayer.position);
        }
        
        // Draw current movement direction
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, moveDirection * 2f);
    }
}
