using Fusion;
using UnityEngine;
using System.Linq;
using System.Collections;

public class TankHealth2D : NetworkBehaviour
{
    [Header("Paramètres de santé")]
    [SerializeField] private float maxHealth = 100f;

    
    private float currentHealth = 0f;
    private bool _isDead = false;
    private int lastDamageDealer = -1;
    
    public float CurrentHealth => currentHealth;
    public bool IsDead => _isDead;
    
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        _isDead = false;
    }

    private void Start()
    {
        if (Object)
        {
            currentHealth = maxHealth;
            _isDead = false;
        }
    }

    private void EnableInputs()
    {
        var move = GetComponent<TankMovement2D>();
        if (move != null) move.enabled = true;
        var shoot = GetComponent<TankShoot2D>();
        if (shoot != null) shoot.enabled = true;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void TakeDamageRPC(float amount, int damageDealer)
    {
        if (_isDead) return;
        
        lastDamageDealer = damageDealer;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        
        
        if (currentHealth <= 0 && !_isDead)
        {
            _isDead = true; // Marquer comme mort immédiatement
            
            SimpleTankRespawn respawnHandler = GetComponent<SimpleTankRespawn>();
            if (respawnHandler != null)
            {
                respawnHandler.DieRpc(damageDealer);
            }
            else
            {
                Debug.LogError("[TankHealth2D] Impossible d'appeler Die: SimpleTankRespawn non trouvé!");
            }
        }
    }
}