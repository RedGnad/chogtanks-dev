using UnityEngine;

// 🌐 FUSION: GameManager est maintenant un MonoBehaviour persistant (pas NetworkBehaviour)
// La synchronisation réseau sera gérée par NetworkUIManager via events
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [HideInInspector]
    public bool isGameOver = false;
    
    private void Awake()
    {
        Debug.Log($"[GAMEMANAGER] 🔍 GameManager.Awake called on GameObject: {gameObject.name}");
        
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"[GAMEMANAGER] ✅ GameManager Instance set to: {gameObject.name}");
            
            // 🔧 FUSION: Marquer cet objet comme persistant entre les sessions
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GAMEMANAGER] GameManager marqué comme persistant avec DontDestroyOnLoad");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"[GAMEMANAGER] ❌ DUPLICATE GameManager detected! Destroying: {gameObject.name} (keeping: {Instance.gameObject.name})");
            Destroy(gameObject);
        }
        
        isGameOver = false;
    }
    
    public void SetGameOver()
    {
        isGameOver = true;
    }
    
    // OnShutdown removed for Fusion
    public void OnShutdownFusion()
    {
        isGameOver = false;
    }
    
    public bool ShouldEndMatch()
    {
        return isGameOver;
    }
}
