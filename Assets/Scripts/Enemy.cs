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
        
        rb.gravityScale = 0f;
        rb.drag = 1f;
        rb.angularDrag = 5f;
        
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;
            collider.isTrigger = true; 
        }
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        if (spriteRenderer != null)
        {
            
            if (spriteRenderer.sprite == null)
            {
                //Debug.LogWarning("[ENEMY] No sprite assigned to SpriteRenderer!");
            }
            
            spriteRenderer.color = enemyColor;
        }
        else
        {
            //Debug.LogError("[ENEMY] No SpriteRenderer component found!");
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = enemyColor;
        }
        
        targetUpdateCoroutine = StartCoroutine(UpdateTargetPeriodically());
        
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
        
        if (IsAnyPlayerCloaked())
        {
            rb.velocity = Vector2.zero;
            return;
        }
        
        Vector2 directionToTarget = (targetPlayer.position - transform.position).normalized;
        
        rb.velocity = directionToTarget * moveSpeed;
        
        if (directionToTarget != Vector2.zero)
        {
            float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            float targetAngle = angle - 90f; 
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                Quaternion.AngleAxis(targetAngle, Vector3.forward), 
                rotationSpeed * Time.deltaTime
            );
        }
    }
    
    private bool IsAnyPlayerCloaked()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject player in players)
        {
            TankShoot2D tankShoot = player.GetComponent<TankShoot2D>();
            if (tankShoot != null && tankShoot.hasCloakPowerup)
            {
                return true;
            }
        }
        
        return false;
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
        float[] angles = { -45f, 45f, -90f, 90f, -135f, 135f, 180f };
        
        foreach (float angle in angles)
        {
            Vector2 testDirection = RotateVector(originalDirection, angle);
            if (!IsWallInDirection(testDirection))
            {
                return testDirection;
            }
        }
        
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
        if (other.CompareTag("Shell"))
        {
            OnHitByShell();
            return;
        }
        
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
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyDestroyed(gameObject);
        }
        
        Destroy(gameObject);
    }
    
    private void OnTouchPlayer(GameObject player)
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        
        EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager != null)
        {
            enemyManager.CleanupAllEnemies();
        }
        
        if (ScoreManager.Instance != null)
        {
            int currentScore = ScoreManager.Instance.GetPlayerScore(PhotonNetwork.LocalPlayer.ActorNumber);
            
            ScoreManager.Instance.SubmitScoreToFirebase(currentScore, 0); // 0 bonus for enemy kill
            
            ChogTanksNFTManager nftManager = FindObjectOfType<ChogTanksNFTManager>();
            if (nftManager != null)
            {
                nftManager.ForceRefreshAfterMatch(currentScore);
            }
        }
        
        LobbyUI lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.SetDelayOnNextReturn();
        }
        
        PhotonLauncher launcher = FindObjectOfType<PhotonLauncher>();
        if (launcher != null)
        {
            launcher.photonView.RPC("ShowWinnerToAllRPC", RpcTarget.All, "Enemy Victory!", -1);
        }
        
        Destroy(gameObject);
    }
    
    private void OnDestroy()
    {
        if (targetUpdateCoroutine != null)
        {
            StopCoroutine(targetUpdateCoroutine);
        }
        
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyDestroyed(gameObject);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wallCheckDistance);
        
        if (targetPlayer != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPlayer.position);
        }
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, moveDirection * 2f);
    }
}
