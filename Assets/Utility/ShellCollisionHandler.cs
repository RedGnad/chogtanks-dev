// Assets/Utility/ShellCollisionHandler.cs

using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ShellCollisionHandler : MonoBehaviourPun
{
    [Tooltip("Couches avec lesquelles le shell explose (par ex. Ground ou Tank).")]
    [SerializeField] private LayerMask collisionLayers;

    [Header("Explosion par Raycast (shell)")]
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float explosionDamage = 25f;
    [SerializeField] private LayerMask tankLayerMask;

    [Tooltip("Prefab contenant uniquement le ParticleSystem (local-only).")]
    [SerializeField] private GameObject particleOnlyExplosionPrefab;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Ne traiter la collision que sur l'instance possédée localement
        if (!photonView.IsMine) return;

        int layerMaskCollision = 1 << collision.gameObject.layer;
        bool isValid = (layerMaskCollision & collisionLayers) != 0;
        if (!isValid) return;

        Vector2 explosionPos = transform.position;

        // Appliquer les dégâts via RPC Photon à chaque tank touché par l'explosion
        Collider2D[] hits = Physics2D.OverlapCircleAll(explosionPos, explosionRadius, tankLayerMask);
        foreach (var hit in hits)
        {
            TankHealth2D health = hit.GetComponentInParent<TankHealth2D>();
            if (health == null) continue;
            string tankOwner = health.photonView.Owner != null ? $"{health.photonView.Owner.NickName} (Actor {health.photonView.Owner.ActorNumber})" : "<null>";
            string shellOwner = photonView.Owner != null ? $"{photonView.Owner.NickName} (Actor {photonView.Owner.ActorNumber})" : "<null>";
            Debug.Log($"[SHELL DEBUG] Tank touché: {tankOwner}, Shell owner: {shellOwner}, Health: {health.CurrentHealth}, IsDead: {health.IsDead}, IsSelfDamage: {(health.photonView.Owner != null && photonView.Owner != null && health.photonView.Owner.ActorNumber == photonView.Owner.ActorNumber)}");
            // Ignore le tank du propriétaire du shell (comparaison par ActorNumber pour robustesse)
            if (health.photonView.Owner != null && photonView.Owner != null && health.photonView.Owner.ActorNumber == photonView.Owner.ActorNumber) continue;
            health.photonView.RPC("TakeDamageRPC", RpcTarget.All, explosionDamage);
        }

        photonView.RPC("PlayParticlesRPC", RpcTarget.All, explosionPos);
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
