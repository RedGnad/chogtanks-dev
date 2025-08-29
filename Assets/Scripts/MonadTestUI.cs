using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonadTestUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI walletText;
    [SerializeField] private TextMeshProUGUI usernameText;
    
    private void Start()
    {
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }
        
        UpdateUI("Ready to connect", "", "");
        
        MonadGamesIDWebView.OnMonadGamesIDResultEvent += OnMonadResult;
        
        CheckExistingData();
    }
    
    private void OnDestroy()
    {
        MonadGamesIDWebView.OnMonadGamesIDResultEvent -= OnMonadResult;
    }
    
    private void OnLoginButtonClicked()
    {
        UpdateUI("Opening WebView...", "", "");
        
        if (MonadGamesIDWebView.Instance != null)
        {
            MonadGamesIDWebView.Instance.OpenMonadGamesIDLogin();
        }
        else
        {
            UpdateUI("Error: WebView not found", "", "");
        }
    }
    
    private void OnMonadResult(MonadGamesIDWebView.MonadGamesIDResult result)
    {
        
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
