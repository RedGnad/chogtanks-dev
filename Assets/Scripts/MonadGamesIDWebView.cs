using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Gestionnaire WebView pour Cross App Monad Games ID
/// Ouvre page React avec SDK Privy complet et reçoit les résultats
/// </summary>
public class MonadGamesIDWebView : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string webViewUrl = "http://localhost:8000"; // Dev: React app
    [SerializeField] private string productionUrl = "https://redgnad.github.io/CHOGTANKS/monad-react-webview/"; // Production: GitHub Pages
    
    // Events pour notifier les autres scripts
    public static event System.Action<MonadGamesIDResult> OnMonadGamesIDResultEvent;
    
    private static MonadGamesIDWebView _instance;
    public static MonadGamesIDWebView Instance => _instance;

    [System.Serializable]
    public class MonadGamesIDResult
    {
        public bool success;
        public string walletAddress;
        public string username;
        public string userId;
        public string error;
        public string registrationUrl;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Enregistrer callback pour communication JavaScript → Unity
        RegisterMessageCallback();
    }

    /// <summary>
    /// Ouvre WebView pour login Monad Games ID
    /// </summary>
    public void OpenMonadGamesIDLogin()
    {
        Debug.Log("[MONAD WEBVIEW] 🚀 Opening Monad Games ID WebView...");
        
        // Choisir URL selon environnement
        string targetUrl = Application.isEditor ? webViewUrl : productionUrl;
        
        // Ajouter cache-busting pour forcer le rechargement
        string timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        string urlWithCacheBusting = $"{targetUrl}?v={timestamp}";
        
        Debug.Log($"[MONAD WEBVIEW] 📍 URL: {urlWithCacheBusting}");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            // En WebGL, ouvrir dans nouvelle fenêtre/onglet
            Application.ExternalEval($"window.monadGamesWindow = window.open('{urlWithCacheBusting}', 'MonadGamesID', 'width=500,height=700,scrollbars=yes,resizable=yes');");
        #else
            // En Editor/Standalone, ouvrir dans navigateur par défaut
            Application.OpenURL(urlWithCacheBusting);
        #endif
    }

    /// <summary>
    /// Méthode appelée par JavaScript pour retourner résultats
    /// </summary>
    public void OnMonadGamesIDResult(string jsonResult)
    {
        try
        {
            Debug.Log($"[MONAD WEBVIEW] 📨 Received result: {jsonResult}");
            
            MonadGamesIDResult result = JsonUtility.FromJson<MonadGamesIDResult>(jsonResult);
            
            if (result.success)
            {
                Debug.Log($"[MONAD WEBVIEW] ✅ Success! Username: {result.username}, Wallet: {result.walletAddress}");
                
                // Sauvegarder les données utilisateur
                PlayerPrefs.SetString("monad_wallet_address", result.walletAddress);
                PlayerPrefs.SetString("monad_username", result.username);
                PlayerPrefs.SetString("monad_user_id", result.userId);
                PlayerPrefs.Save();
                
                // Fermer la popup WebView
                CloseWebView();
            }
            else
            {
                Debug.LogError($"[MONAD WEBVIEW] ❌ Error: {result.error}");
            }
            
            // Notifier les autres scripts
            OnMonadGamesIDResultEvent?.Invoke(result);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MONAD WEBVIEW] ❌ Error parsing result: {e.Message}");
            
            // Fallback: essayer de lire depuis localStorage
            TryReadFromLocalStorage();
        }
    }
    
    /// <summary>
    /// Fallback: lire résultat depuis localStorage
    /// </summary>
    private void TryReadFromLocalStorage()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string result = ReadMonadWalletResult();
            if (!string.IsNullOrEmpty(result))
            {
                Debug.Log($"[MONAD WEBVIEW] 📦 Fallback: Reading from localStorage: {result}");
                OnMonadGamesIDResult(result);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MONAD WEBVIEW] ❌ Fallback failed: {e.Message}");
        }
        #endif
    }

    /// <summary>
    /// Enregistre callback pour communication JavaScript
    /// </summary>
    private void RegisterMessageCallback()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            RegisterMessageCallbackWebGL(gameObject.name, "OnMonadGamesIDResult");
        #endif
    }

    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RegisterMessageCallbackWebGL(string gameObjectName, string methodName);
    
    [DllImport("__Internal")]
    private static extern string ReadMonadWalletResult();
    
    [DllImport("__Internal")]
    private static extern int IsUnityReady();
    #endif

    /// <summary>
    /// Ferme WebView (si applicable)
    /// </summary>
    public void CloseWebView()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            Application.ExternalEval("if(window.monadGamesWindow) { window.monadGamesWindow.close(); }");
        #endif
    }

    private void OnDestroy()
    {
        CloseWebView();
    }
}
