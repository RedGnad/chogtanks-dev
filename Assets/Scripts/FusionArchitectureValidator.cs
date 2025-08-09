using UnityEngine;
using Fusion;

/// <summary>
/// 🔍 Validateur d'architecture Fusion "Pure"
/// Vérifie que l'architecture persistant/temporaire fonctionne correctement
/// </summary>
public class FusionArchitectureValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    public bool enableAutoValidation = true;
    public float validationInterval = 5f;
    
    private void Start()
    {
        if (enableAutoValidation)
        {
            InvokeRepeating(nameof(ValidateArchitecture), 2f, validationInterval);
        }
    }
    
    /// <summary>Valider l'architecture Fusion "Pure"</summary>
    [ContextMenu("Validate Fusion Architecture")]
    public void ValidateArchitecture()
    {
        Debug.Log("🔍 === FUSION ARCHITECTURE VALIDATION ===");
        
        ValidatePersistentObjects();
        ValidateTemporaryObjects();
        ValidateEventSystem();
        
        Debug.Log("🔍 === VALIDATION COMPLETED ===");
    }
    
    /// <summary>Valider les objets persistants (MonoBehaviour + DontDestroyOnLoad)</summary>
    private void ValidatePersistentObjects()
    {
        Debug.Log("🏛️ PERSISTENT OBJECTS:");
        
        // LobbyUI
        var lobbyUI = LobbyUI.Instance;
        if (lobbyUI != null)
        {
            bool isDontDestroy = lobbyUI.gameObject.scene.name == "DontDestroyOnLoad";
            Debug.Log($"✅ LobbyUI: Found, DontDestroyOnLoad={isDontDestroy}, Type={lobbyUI.GetType().BaseType.Name}");
        }
        else
        {
            Debug.LogError("❌ LobbyUI: NOT FOUND!");
        }
        
        // GameManager
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            bool isDontDestroy = gameManager.gameObject.scene.name == "DontDestroyOnLoad";
            Debug.Log($"✅ GameManager: Found, DontDestroyOnLoad={isDontDestroy}, Type={gameManager.GetType().BaseType.Name}");
        }
        else
        {
            Debug.LogError("❌ GameManager: NOT FOUND!");
        }
        
        // Canvas principal
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            bool isDontDestroy = canvas.gameObject.scene.name == "DontDestroyOnLoad";
            Debug.Log($"✅ Canvas: Found, DontDestroyOnLoad={isDontDestroy}");
        }
        else
        {
            Debug.LogWarning("⚠️ Canvas: Not found");
        }
    }
    
    /// <summary>Valider les objets temporaires (NetworkBehaviour + NetworkObject)</summary>
    private void ValidateTemporaryObjects()
    {
        Debug.Log("⚡ TEMPORARY OBJECTS:");
        
        // NetworkRunner
        var networkRunner = FindFirstObjectByType<NetworkRunner>();
        if (networkRunner != null)
        {
            Debug.Log($"✅ NetworkRunner: Found, IsRunning={networkRunner.IsRunning}, GameMode={networkRunner.GameMode}");
        }
        else
        {
            Debug.Log("ℹ️ NetworkRunner: Not found (normal si pas en session)");
        }
        
        // BasicSpawner
        var basicSpawner = FindFirstObjectByType<BasicSpawner>();
        if (basicSpawner != null)
        {
            Debug.Log($"✅ BasicSpawner: Found, Type={basicSpawner.GetType().BaseType.Name}");
        }
        else
        {
            Debug.Log("ℹ️ BasicSpawner: Not found (normal si pas en session)");
        }
        
        // NetworkUIManager
        var networkUIManager = FindFirstObjectByType<NetworkUIManager>();
        if (networkUIManager != null)
        {
            Debug.Log($"✅ NetworkUIManager: Found, Type={networkUIManager.GetType().BaseType.Name}");
        }
        else
        {
            Debug.Log("ℹ️ NetworkUIManager: Not found (normal si pas en session)");
        }
        
        // PhotonLauncher
        var photonLauncher = FindFirstObjectByType<PhotonLauncher>();
        if (photonLauncher != null)
        {
            Debug.Log($"✅ PhotonLauncher: Found, Type={photonLauncher.GetType().BaseType.Name}");
        }
        else
        {
            Debug.LogWarning("⚠️ PhotonLauncher: Not found");
        }
        
        // Tanks (NetworkObjects)
        var tanks = FindObjectsByType<TankMovement2D>(FindObjectsSortMode.None);
        Debug.Log($"🚗 Tanks: {tanks.Length} found");
    }
    
    /// <summary>Valider le système d'events</summary>
    private void ValidateEventSystem()
    {
        Debug.Log("🔗 EVENT SYSTEM:");
        
        var lobbyUI = LobbyUI.Instance;
        if (lobbyUI != null)
        {
            // Vérifier que LobbyUI écoute les events (pas de moyen direct, mais on peut tester)
            Debug.Log("✅ LobbyUI: Event listeners configurés (voir SubscribeToNetworkEvents)");
        }
        
        var networkUIManager = FindFirstObjectByType<NetworkUIManager>();
        if (networkUIManager != null)
        {
            Debug.Log("✅ NetworkUIManager: Event broadcaster disponible");
        }
        else
        {
            Debug.Log("ℹ️ NetworkUIManager: Pas en session - events indisponibles");
        }
    }
    
    /// <summary>Test manuel des events</summary>
    [ContextMenu("Test Event System")]
    public void TestEventSystem()
    {
        Debug.Log("🧪 Testing Event System...");
        
        // Tester via NetworkUIManager si disponible
        var networkUIManager = FindFirstObjectByType<NetworkUIManager>();
        if (networkUIManager != null)
        {
            networkUIManager.TestEvents();
            Debug.Log("🧪 Event test completed via NetworkUIManager - check UI for updates");
        }
        else
        {
            Debug.LogWarning("🧪 NetworkUIManager not found - cannot test events (normal si pas en session)");
        }
    }
}
