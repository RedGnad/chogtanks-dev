using Photon.Pun;
using UnityEngine;
using System.Linq;
using System.Collections;

public class TankHealth2D : MonoBehaviourPun
{
    [Header("Paramètres de santé")]
    [SerializeField] private float maxHealth = 100f;

    // Nous utiliserons SimpleTankRespawn pour la gestion du respawn
    // Ne pas garder de référence ici car le composant est ajouté dynamiquement après notre Start()

    private float currentHealth = 0f;
    private bool _isDead = false;
    private int lastDamageDealer = -1; // ActorNumber du dernier joueur qui a infligé des dégâts
    
    public float CurrentHealth => currentHealth;
    public bool IsDead => _isDead;
    
    // Pour réinitialiser la santé lors du respawn
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        _isDead = false;
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            currentHealth = maxHealth;
            _isDead = false;
            // Les inputs sont maintenant gérés par SimpleTankRespawn
        }
    }

    private void EnableInputs()
    {
        var move = GetComponent<TankMovement2D>();
        if (move != null) move.enabled = true;
        var shoot = GetComponent<TankShoot2D>();
        if (shoot != null) shoot.enabled = true;
    }

    [PunRPC]
    public void TakeDamageRPC(float amount, int damageDealer)
    {
        if (_isDead) return;
        
        lastDamageDealer = damageDealer;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        
        Debug.Log($"[TANK DAMAGE] {photonView.Owner?.NickName} (Actor {photonView.Owner?.ActorNumber}): Health={currentHealth}, damageDealer={damageDealer}");
        
        // Si la vie tombe à zéro et que le tank n'est pas déjà mort...
        if (currentHealth <= 0 && !_isDead)
        {
            _isDead = true; // Marquer comme mort immédiatement
            
            // Utiliser le nouveau système de respawn simplifié
            // Chercher le composant à la volée, car il pourrait avoir été ajouté après notre Start()
            SimpleTankRespawn respawnHandler = GetComponent<SimpleTankRespawn>();
            if (respawnHandler != null)
            {
                Debug.Log($"[TANK DEATH] Appel de SimpleTankRespawn.Die pour {photonView.Owner?.NickName}");
                respawnHandler.photonView.RPC("Die", RpcTarget.All, damageDealer);
            }
            else
            {
                Debug.LogError("[TankHealth2D] Impossible d'appeler Die: SimpleTankRespawn non trouvé!");
            }
        }
    }

    // MÉTHODES SUPPRIMÉES - Simplification du système de mort/respawn

    // Cette fonctionnalité est maintenant gérée par SimpleTankRespawn

    // Cette fonctionnalité est maintenant gérée par SimpleTankRespawn
}