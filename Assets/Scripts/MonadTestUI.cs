using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interface de test pour Monad Games ID WebView
/// </summary>
public class MonadTestUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI walletText;
    [SerializeField] private TextMeshProUGUI usernameText;
    
    private void Start()
    {
        // Setup UI
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }
        
        UpdateUI("Ready to connect", "", "");
        
        // S'abonner aux événements
        MonadGamesIDWebView.OnMonadGamesIDResultEvent += OnMonadResult;
        
        // Vérifier s'il y a déjà des données sauvegardées
        CheckExistingData();
    }
    
    private void OnDestroy()
    {
        // Se désabonner des événements
        MonadGamesIDWebView.OnMonadGamesIDResultEvent -= OnMonadResult;
    }
    
    private void OnLoginButtonClicked()
    {
        Debug.Log("[MONAD TEST] Login button clicked");
        UpdateUI("Opening WebView...", "", "");
        
        if (MonadGamesIDWebView.Instance != null)
        {
            MonadGamesIDWebView.Instance.OpenMonadGamesIDLogin();
        }
        else
        {
            Debug.LogError("[MONAD TEST] MonadGamesIDWebView instance not found!");
            UpdateUI("Error: WebView not found", "", "");
        }
    }
    
    private void OnMonadResult(MonadGamesIDWebView.MonadGamesIDResult result)
    {
        Debug.Log($"[MONAD TEST] Received result: Success={result.success}");
        
        if (result.success)
        {
            UpdateUI("Connected successfully!", result.walletAddress, result.username);
        }
        else
        {
            UpdateUI($"Error: {result.error}", "", "");
        }
    }
    
    private void CheckExistingData()
    {
        string savedWallet = PlayerPrefs.GetString("monad_wallet_address", "");
        string savedUsername = PlayerPrefs.GetString("monad_username", "");
        
        if (!string.IsNullOrEmpty(savedWallet))
        {
            UpdateUI("Previously connected", savedWallet, savedUsername);
        }
    }
    
    private void UpdateUI(string status, string wallet, string username)
    {
        if (statusText != null)
            statusText.text = status;
            
        if (walletText != null)
            walletText.text = string.IsNullOrEmpty(wallet) ? "No wallet" : $"Wallet: {wallet}";
            
        if (usernameText != null)
            usernameText.text = string.IsNullOrEmpty(username) ? "No username" : $"User: {username}";
    }
}
