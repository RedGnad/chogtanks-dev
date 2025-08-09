using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ShellCollisionHandler : NetworkBehaviour
{
    [SerializeField] private LayerMask collisionLayers;

    [Header("Explosion par Raycast (shell)")]
    [SerializeField] private float explosionRadius = 2f;
    [Header("Dégâts")]
    [SerializeField] private float normalDamage = 25f;
    [SerializeField] private float precisionDamage = 50f;
    [SerializeField] private LayerMask tankLayerMask;

    [SerializeField] private GameObject particleOnlyExplosionPrefab;

    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite precisionSprite;

    [Header("Trail Settings")]
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private Color normalTrailColor = Color.blue;
    [SerializeField] private Color precisionTrailColor = Color.red;
    [SerializeField] private float normalTrailWidth = 0.1f;
    [SerializeField] private float precisionTrailWidth = 0.2f;
    [SerializeField] private float normalTrailTime = 0.3f;
    [SerializeField] private float precisionTrailTime = 0.6f;

    private SpriteRenderer sr;
    private float explosionDamage;
    private bool isPrecisionShot = false;
    
    private int shooterActorNumber = -1;
    private float spawnTime; 

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
        
        spawnTime = Time.time;
        
        if (normalSprite != null && sr != null)
            sr.sprite = normalSprite;
            
        SetupTrail(false);
        explosionDamage = normalDamage; 
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void SetPrecisionRpc(bool isPrecision)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (isPrecision && precisionSprite != null)
            sr.sprite = precisionSprite;
        else if (normalSprite != null)
            sr.sprite = normalSprite;

        explosionDamage = isPrecision ? precisionDamage : normalDamage;
        isPrecisionShot = isPrecision;
        
        SetupTrail(isPrecision);
    }
    
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void SetShooterRpc(int actorNumber)
    {
        shooterActorNumber = actorNumber;
    }
    
    private void SetupTrail(bool isPrecision)
    {
        if (trailRenderer == null) return;
        
        if (isPrecision)
        {
            trailRenderer.startColor = precisionTrailColor;
            trailRenderer.endColor = new Color(precisionTrailColor.r, precisionTrailColor.g, precisionTrailColor.b, 0f);
            trailRenderer.startWidth = precisionTrailWidth;
            trailRenderer.endWidth = precisionTrailWidth * 0.3f;
            trailRenderer.time = precisionTrailTime;
        }
        else
        {
            trailRenderer.startColor = normalTrailColor;
            trailRenderer.endColor = new Color(normalTrailColor.r, normalTrailColor.g, normalTrailColor.b, 0f);
            trailRenderer.startWidth = normalTrailWidth;
            trailRenderer.endWidth = normalTrailWidth * 0.3f;
            trailRenderer.time = normalTrailTime;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!Object) return;

        // 🔧 FIX: Empêcher TOUTE collision avec le tank tireur (pas seulement les dégâts)
        TankHealth2D collidedTankHealth = collision.collider.GetComponentInParent<TankHealth2D>();
        if (collidedTankHealth != null)
        {
            // Vérification par InputAuthority (méthode principale)
            bool isSelfCollisionByAuthority = Object.InputAuthority != null && 
                                            collidedTankHealth.Object.InputAuthority != null &&
                                            Object.InputAuthority.PlayerId == collidedTankHealth.Object.InputAuthority.PlayerId;
            
            // Vérification par shooterActorNumber (méthode de backup)
            bool isSelfCollisionByShooter = shooterActorNumber != -1 && 
                                          collidedTankHealth.Object.InputAuthority != null &&
                                          collidedTankHealth.Object.InputAuthority.PlayerId == shooterActorNumber;
            
            if (isSelfCollisionByAuthority || isSelfCollisionByShooter)
            {
                Debug.Log($"[SHELL] Collision ignorée avec le tank tireur (Authority: {isSelfCollisionByAuthority}, Shooter: {isSelfCollisionByShooter})");
                return; // Ignorer complètement la collision avec le tank tireur
            }
        }

        int layerMaskCollision = 1 << collision.gameObject.layer;
        bool isValid = (layerMaskCollision & collisionLayers) != 0;
        if (!isValid) return;

        Vector2 explosionPos = transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(explosionPos, explosionRadius, tankLayerMask);
        foreach (var hit in hits)
        {
            TankHealth2D health = hit.GetComponentInParent<TankHealth2D>();
            if (health == null) continue;
            
            string tankOwner = health.Object.InputAuthority != null ? $"{health.Object.InputAuthority.ToString()} (Actor {health.Object.InputAuthority.PlayerId})" : "<null>";
            string shellOwner = Object.InputAuthority != null ? $"{Object.InputAuthority.ToString()} (Actor {Object.InputAuthority.PlayerId})" : "<null>";
            
            // 🔧 DOUBLE VÉRIFICATION : Utiliser à la fois InputAuthority ET shooterActorNumber
            bool isSelfDamageByAuthority = health.Object.InputAuthority != null && Object.InputAuthority != null && 
                                         health.Object.InputAuthority.PlayerId == Object.InputAuthority.PlayerId;
            
            bool isSelfDamageByShooter = health.Object.InputAuthority != null && shooterActorNumber != -1 &&
                                       health.Object.InputAuthority.PlayerId == shooterActorNumber;
            
            bool isSelfDamage = isSelfDamageByAuthority || isSelfDamageByShooter;
            
            Debug.Log($"[SHELL] Collision check - Tank: {tankOwner}, Shell: {shellOwner}, ShooterID: {shooterActorNumber}");
            Debug.Log($"[SHELL] Self-damage check - ByAuthority: {isSelfDamageByAuthority}, ByShooter: {isSelfDamageByShooter}, Final: {isSelfDamage}");
            
            if (isSelfDamage) 
            {
                Debug.Log($"[SHELL] PREVENTED self-damage for tank {tankOwner}");
                continue;
            }
            
            TankShield tankShield = health.GetComponent<TankShield>();
            if (tankShield != null && tankShield.IsShieldActive() && !isPrecisionShot)
            {
                continue; 
            }
            
            int attackerId = Object.InputAuthority != null ? Object.InputAuthority.PlayerId : -1;
            health.TakeDamageRPC(explosionDamage, attackerId);
        }

        if (particleOnlyExplosionPrefab != null) {
            Instantiate(particleOnlyExplosionPrefab, explosionPos, Quaternion.identity);
        }
        
        PlayParticlesRPC(explosionPos);
        
        Runner.Despawn(Object);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void PlayParticlesRPC(Vector2 pos)
    {
        if (particleOnlyExplosionPrefab == null) return;
        Instantiate(particleOnlyExplosionPrefab, pos, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}