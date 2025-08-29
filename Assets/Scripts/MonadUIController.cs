using UnityEngine;
using UnityEngine.UI;
using Sample;

public class MonadUIController : MonoBehaviour
{
    [Header("UI Elements")]
    public Image monadUIImage; // Image à contrôler selon le statut Monad ID
    
    private MonadGamesIDManager monadManager;
    
    private void Start()
    {
        // Trouver le MonadGamesIDManager
        monadManager = FindObjectOfType<MonadGamesIDManager>();
        if (monadManager == null)
        {
            Debug.LogError("[MONAD-UI] MonadGamesIDManager not found!");
            return;
        }
        
        // Vérifier le statut au démarrage
        UpdateMonadUIVisibility();
    }
    
    private void Update()
    {
        // Vérifier périodiquement (toutes les 2 secondes)
        if (Time.time % 2f < Time.deltaTime)
        {
            UpdateMonadUIVisibility();
        }
    }
    
    /// <summary>
    /// Met à jour la visibilité de l'image selon le statut Monad ID
    /// </summary>
    private void UpdateMonadUIVisibility()
    {
        if (monadUIImage == null) 
        {
            Debug.LogWarning("[MONAD-UI] monadUIImage is NULL!");
            return;
        }
        
        if (monadManager == null)
        {
            Debug.LogWarning("[MONAD-UI] monadManager is NULL!");
            return;
        }
        
        // Solution ultra simple : utiliser directement IsSignedIn()
        bool isSignedIn = monadManager.IsSignedIn();
        monadUIImage.gameObject.SetActive(isSignedIn);
        
        Debug.Log($"[MONAD-UI] Monad IsSignedIn: {isSignedIn}");
        Debug.Log($"[MONAD-UI] Image UI set to: {isSignedIn}");
    }
}
