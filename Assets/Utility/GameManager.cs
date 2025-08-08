using UnityEngine;

using Fusion;

public class GameManager : NetworkBehaviour // Fusion scene loading - UnityEngine.SceneManagement.SceneManager.LoadScene
{
    public static GameManager Instance { get; private set; }
    
    [HideInInspector]
    public bool isGameOver = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
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
