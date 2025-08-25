using System;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gestionnaire WebView pour Cross App Monad Games ID
/// Ouvre page React avec SDK Privy complet et reçoit les résultats
/// </summary>
public class MonadGamesIDWebView : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string webViewUrl = "http://localhost:8000"; 
    [SerializeField] private string productionUrl = "https://redgnad.github.io/CHOGTANKS/";
    
    [Header("Polling Configuration")]
    [SerializeField] [Range(1, 10)] private int pollingInterval = 2; // Intervalle initial en secondes
    [SerializeField] [Range(5, 60)] private int maxPollingDuration = 30; // Durée maximale en secondes
    [SerializeField] [Range(1, 10)] private int pollingBackoffMultiplier = 2; // Multiplicateur pour espacer les vérifications
    
    private bool isResultReceived = false; // Flag pour éviter les redémarrages
    
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
        
        // Injecter script JavaScript pour faciliter la communication
        InjectJavaScriptBridge();
        
        // Injecter le script d'écoute des messages dans la page principale
        InjectMessageListener();
    }

    /// <summary>
    /// Ouvre WebView pour login Monad Games ID
    /// </summary>
    public void OpenMonadGamesIDLogin()
    {
        Debug.Log("[MONAD WEBVIEW] 🚀 Opening Monad Games ID WebView...");
        
        // Choisir URL selon environnement
        string targetUrl = Application.isEditor ? webViewUrl : productionUrl;
        
        Debug.Log($"[MONAD WEBVIEW] 📍 URL: {targetUrl}");
        
        // Réinitialiser le localStorage avant d'ouvrir la WebView
        #if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval("localStorage.removeItem('MONAD_WALLET_RESULT');");
        #endif
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            // En WebGL, ouvrir dans nouvelle fenêtre/onglet
            Application.ExternalEval($"window.monadGamesWindow = window.open('{targetUrl}', 'MonadGamesID', 'width=500,height=700,scrollbars=yes,resizable=yes');");
            
            // Démarrer le polling intelligent
            StartCoroutine(SmartPollingCoroutine());
        #else
            // En Editor/Standalone, ouvrir dans navigateur par défaut
            Application.OpenURL(targetUrl);
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
                Debug.Log($"[MONAD WEBVIEW] Success! Username: {result.username}, Wallet: {result.walletAddress}");
                
                // Sauvegarder les données utilisateur
                PlayerPrefs.SetString("monad_wallet_address", result.walletAddress);
                PlayerPrefs.SetString("monad_username", result.username);
                PlayerPrefs.SetString("monad_user_id", result.userId);
                PlayerPrefs.Save();
                
                // Fermer la popup WebView
                CloseWebView();
                
                isResultReceived = true;
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
    private bool TryReadFromLocalStorage()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string result = ReadMonadWalletResult();
            if (!string.IsNullOrEmpty(result))
            {
                Debug.Log($"[MONAD WEBVIEW] 📦 Résultat trouvé dans localStorage: {result}");
                OnMonadGamesIDResult(result);
                
                // Nettoyer localStorage après traitement
                #if UNITY_WEBGL && !UNITY_EDITOR
                Application.ExternalEval("localStorage.removeItem('MONAD_WALLET_RESULT');");
                #endif
                
                isResultReceived = true;
                
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MONAD WEBVIEW] ❌ Fallback failed: {e.Message}");
        }
        #endif
        return false;
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

    /// <summary>
    /// Injecte un script JavaScript pour faciliter la communication WebView → Unity
    /// </summary>
    private void InjectJavaScriptBridge()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        string bridgeScript = @"
            window.MonadGamesIDWebViewBridge = {
                sendToUnity: function(data) {
                    try {
                        // Convertir en string si c'est un objet
                        var jsonData = typeof data === 'object' ? JSON.stringify(data) : data;
                        
                        // Envoyer à Unity via unityInstance (nom correct dans le build)
                        if (typeof window.unityInstance !== 'undefined' && window.unityInstance) {
                            window.unityInstance.SendMessage('MonadGamesIDWebView', 'OnMonadGamesIDResult', jsonData);
                            console.log('[UNITY BRIDGE] ✅ Sent via unityInstance');
                            return true;
                        }
                        
                        // Fallback: localStorage
                        localStorage.setItem('MONAD_WALLET_RESULT', jsonData);
                        console.log('[UNITY BRIDGE] ✅ Saved to localStorage');
                        
                        // Marquer l'heure de la dernière mise à jour
                        localStorage.setItem('MONAD_WALLET_TIMESTAMP', Date.now().toString());
                        
                        return true;
                    } catch (err) {
                        console.error('[UNITY BRIDGE] ❌ Error:', err);
                        return false;
                    }
                }
            };
            
            // Exposer la fonction globalement pour la WebView
            window.OnMonadGamesIDResult = function(jsonData) {
                window.MonadGamesIDWebViewBridge.sendToUnity(jsonData);
            };
            
            console.log('[UNITY BRIDGE] 🔄 Bridge initialized and ready');
        ";
        
        Application.ExternalEval(bridgeScript);
        Debug.Log("[MONAD WEBVIEW] 🔄 JavaScript bridge injected");
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
        StopAllCoroutines();
        CloseWebView();
    }
    
    /// <summary>
    /// Injecte un script d'écoute des messages dans la page principale
    /// </summary>
    private void InjectMessageListener()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        string listenerScript = @"
            // Fonction pour lire depuis localStorage
            window.ReadMonadWalletResult = function() {
                return localStorage.getItem('MONAD_WALLET_RESULT') || '';
            };
            
            
            // Écouter les messages de la WebView
            window.addEventListener('message', function(event) {
                if (event.data && event.data.type === 'MONAD_GAMES_ID_RESULT') {
                    console.log('[UNITY MAIN] Received message from WebView:', event.data);
                    if (window.unityInstance) {
                        window.unityInstance.SendMessage('MonadGamesIDWebView', 'OnMonadGamesIDResult', 
                            JSON.stringify(event.data.data));
                    }
                }
            }, false);
            
            console.log('[UNITY MAIN] 🔄 Message listener initialized');
        ";
        
        Application.ExternalEval(listenerScript);
        Debug.Log("[MONAD WEBVIEW] 🔄 Message listener injected");
        #endif
    }
    
    /// <summary>
    /// Coroutine pour un polling intelligent avec backoff exponentiel
    /// </summary>
    private IEnumerator SmartPollingCoroutine()
    {
        if (isResultReceived)
        {
            yield break;
        }
        
        Debug.Log("[MONAD WEBVIEW] 🔄 Démarrage du polling intelligent");
        
        int currentInterval = pollingInterval;
        float elapsedTime = 0;
        bool resultFound = false;
        
        // Première vérification immédiate
        yield return new WaitForSeconds(0.5f);
        resultFound = TryReadFromLocalStorage();
        
        if (resultFound)
        {
            Debug.Log("[MONAD WEBVIEW] ✅ Résultat trouvé immédiatement");
            yield break;
        }
        
        // Polling avec backoff exponentiel
        while (elapsedTime < maxPollingDuration)
        {
            yield return new WaitForSeconds(currentInterval);
            elapsedTime += currentInterval;
            
            Debug.Log($"[MONAD WEBVIEW] 🔄 Vérification périodique ({elapsedTime}s/{maxPollingDuration}s)");
            resultFound = TryReadFromLocalStorage();
            
            if (resultFound)
            {
                Debug.Log("[MONAD WEBVIEW] ✅ Résultat trouvé après " + elapsedTime + " secondes");
                break;
            }
            
            // Augmenter l'intervalle pour réduire la fréquence (backoff exponentiel)
            currentInterval = Mathf.Min(currentInterval * pollingBackoffMultiplier, 10);
        }
        
        if (!resultFound)
        {
            Debug.LogWarning("[MONAD WEBVIEW] ⚠️ Aucun résultat trouvé après " + maxPollingDuration + " secondes");
        }
    }
}