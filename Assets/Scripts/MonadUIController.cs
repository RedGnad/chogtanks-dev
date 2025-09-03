using UnityEngine;
using UnityEngine.UI;
using Sample;

public class MonadUIController : MonoBehaviour
{
    [Header("UI Elements")]
    public Image monadUIImage; 
    
    private MonadGamesIDManager monadManager;
    private float _nextPollTime;
    private const float PollInterval = 1f;

    private void OnEnable()
    {
        // Mettre à jour immédiatement quand le username change (connexion/restauration)
        MonadGamesIDManager.OnUsernameChanged += HandleUsernameChanged;
    }

    private void OnDisable()
    {
        MonadGamesIDManager.OnUsernameChanged -= HandleUsernameChanged;
    }
    
    private void Start()
    {
        // Trouver le MonadGamesIDManager
        monadManager = FindObjectOfType<MonadGamesIDManager>();
        
        UpdateMonadUIVisibility();
    }
    
    private void Update()
    {
        // Récupérer le manager s'il apparaît après Start
        if (monadManager == null)
        {
            monadManager = FindObjectOfType<MonadGamesIDManager>();
            if (monadManager != null)
            {
                UpdateMonadUIVisibility();
            }
        }

        // Léger polling pour couvrir les restaurations PlayerPrefs silencieuses
        if (Time.time >= _nextPollTime)
        {
            UpdateMonadUIVisibility();
            _nextPollTime = Time.time + PollInterval;
        }
    }
    
    private void UpdateMonadUIVisibility()
    {
        if (monadUIImage == null) 
        {
            Debug.LogWarning("[MONAD-UI] monadUIImage is NULL!");
            return;
        }
        
        // Simple et local: "connecté" si le manager dit connecté OU si un état sauvegardé indique une reconnexion
        bool connected = false;
        if (monadManager != null)
        {
            connected = monadManager.IsSignedIn();
        }

        if (!connected)
        {
            // Fallback reconnection: flags et usernames persistés
            bool prefSigned = PlayerPrefs.GetInt("monadGamesSignedIn", 0) == 1;
            string u1 = PlayerPrefs.GetString("monadGamesUsername", "");
            string u2 = PlayerPrefs.GetString("MonadGamesID_Username", "");
            connected = prefSigned || (!string.IsNullOrEmpty(u1) || !string.IsNullOrEmpty(u2));
        }

        // Important: ne pas désactiver le GameObject porteur; on toggle seulement l'Image
        if (monadUIImage.enabled != connected)
        {
            monadUIImage.enabled = connected;
        }
        
    }

    private void HandleUsernameChanged(string _)
    {
        UpdateMonadUIVisibility();
    }
}
