using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ShellCollisionHandler : MonoBehaviourPun
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

    [PunRPC]
    public void SetPrecision(bool isPrecision)
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
    
    [PunRPC]
    public void SetShooter(int actorNumber)
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
        if (!photonView.IsMine) return;

        if (Time.time - spawnTime < 0.3f && shooterActorNumber != -1)
        {
            TankHealth2D health = collision.collider.GetComponentInParent<TankHealth2D>();
            if (health != null && health.photonView.Owner != null && 
                health.photonView.Owner.ActorNumber == shooterActorNumber)
            {
                return; 
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
            
            string tankOwner = health.photonView.Owner != null ? $"{health.photonView.Owner.NickName} (Actor {health.photonView.Owner.ActorNumber})" : "<null>";
            string shellOwner = photonView.Owner != null ? $"{photonView.Owner.NickName} (Actor {photonView.Owner.ActorNumber})" : "<null>";
            
            bool isSelfDamage = health.photonView.Owner != null && photonView.Owner != null && 
                              health.photonView.Owner.ActorNumber == photonView.Owner.ActorNumber;
            
            if (isSelfDamage) continue;
            
            TankShield tankShield = health.GetComponent<TankShield>();
            if (tankShield != null && tankShield.IsShieldActive() && !isPrecisionShot)
            {
                continue; 
            }
            
            int attackerId = photonView.Owner != null ? photonView.Owner.ActorNumber : -1;
            health.photonView.RPC("TakeDamageRPC", RpcTarget.All, explosionDamage, attackerId);
        }

        if (particleOnlyExplosionPrefab != null) {
            Instantiate(particleOnlyExplosionPrefab, explosionPos, Quaternion.identity);
        }
        
        photonView.RPC("PlayParticlesRPC", RpcTarget.Others, explosionPos);
        
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
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