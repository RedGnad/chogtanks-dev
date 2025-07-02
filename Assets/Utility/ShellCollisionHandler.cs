using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ShellCollisionHandler : MonoBehaviourPun
{
    [Tooltip("Couches avec lesquelles le shell explose (par ex. Ground ou Tank).")]
    [SerializeField] private LayerMask collisionLayers;

    [Header("Explosion par Raycast (shell)")]
    [SerializeField] private float explosionRadius = 2f;
    [Header("Dégâts")]
    [SerializeField] private float normalDamage = 25f;
    [SerializeField] private float precisionDamage = 50f;
    [SerializeField] private LayerMask tankLayerMask;

    [Tooltip("Prefab contenant uniquement le ParticleSystem (local-only).")]
    [SerializeField] private GameObject particleOnlyExplosionPrefab;

    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite precisionSprite;

    private SpriteRenderer sr;
    private float explosionDamage; // Sera défini selon le type de tir

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (normalSprite != null && sr != null)
            sr.sprite = normalSprite;
        explosionDamage = normalDamage; // Par défaut
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
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!photonView.IsMine) return;

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
            Debug.Log($"[SHELL DEBUG] Tank touché: {tankOwner}, Shell owner: {shellOwner}, Health: {health.CurrentHealth}, IsDead: {health.IsDead}");
            
            // Vérifier si c'est des dégâts auto-infligés
            bool isSelfDamage = health.photonView.Owner != null && photonView.Owner != null && 
                              health.photonView.Owner.ActorNumber == photonView.Owner.ActorNumber;
            
            // Ne pas causer de dégâts auto-infligés
            if (isSelfDamage) continue;
            
            // Envoyer l'ID du joueur attaquant avec les dégâts
            int attackerId = photonView.Owner != null ? photonView.Owner.ActorNumber : -1;
            health.photonView.RPC("TakeDamageRPC", RpcTarget.All, explosionDamage, attackerId);
        }

        // Créer les particules localement avant de détruire l'objet
        if (particleOnlyExplosionPrefab != null) {
            Instantiate(particleOnlyExplosionPrefab, explosionPos, Quaternion.identity);
        }
        
        // Informer les autres clients de créer les particules aussi
        photonView.RPC("PlayParticlesRPC", RpcTarget.Others, explosionPos);
        
        // Détruire l'objet après avoir créé les particules
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