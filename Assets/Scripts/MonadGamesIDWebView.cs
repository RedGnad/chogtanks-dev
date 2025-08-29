using System;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine;

public class MonadGamesIDWebView : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string webViewUrl = "http://localhost:8000"; 
    [SerializeField] private string productionUrl = "https://redgnad.github.io/CHOGTANKS/";
    
    [Header("Polling Configuration")]
    [SerializeField] [Range(1, 10)] private int pollingInterval = 2; 
    [SerializeField] [Range(5, 60)] private int maxPollingDuration = 30; 
    [SerializeField] [Range(1, 10)] private int pollingBackoffMultiplier = 2; 
    
    private bool isResultReceived = false;
    
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
        RegisterMessageCallback();
        
        InjectJavaScriptBridge();
        
        InjectMessageListener();
    }

    public void OpenMonadGamesIDLogin()
    {
        
        string targetUrl = Application.isEditor ? webViewUrl : productionUrl;
        
        Debug.Log($"[MONAD WEBVIEW] 📍 URL: {targetUrl}");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval("localStorage.removeItem('MONAD_WALLET_RESULT');");
        #endif
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            Application.ExternalEval($"window.monadGamesWindow = window.open('{targetUrl}', 'MonadGamesID', 'width=500,height=700,scrollbars=yes,resizable=yes');");
            
            StartCoroutine(SmartPollingCoroutine());
        #else
            Application.OpenURL(targetUrl);
        #endif
    }

    public void OnMonadGamesIDResult(string jsonResult)
    {
        try
        {
            
            MonadGamesIDResult result = JsonUtility.FromJson<MonadGamesIDResult>(jsonResult);
            
            if (result.success)
            {
                
                PlayerPrefs.SetString("monad_wallet_address", result.walletAddress);
                PlayerPrefs.SetString("monad_username", result.username);
                PlayerPrefs.SetString("monad_user_id", result.userId);
                PlayerPrefs.Save();
                
                CloseWebView();
                
                isResultReceived = true;
            }
            else
            {
                Debug.LogError($"[MONAD WEBVIEW] ❌ Error: {result.error}");
            }
            
            OnMonadGamesIDResultEvent?.Invoke(result);
        }
        catch (Exception e)
        {
            
            TryReadFromLocalStorage();
        }
    }
    
    private bool TryReadFromLocalStorage()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string result = ReadMonadWalletResult();
            if (!string.IsNullOrEmpty(result))
            {
                OnMonadGamesIDResult(result);
                
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

    private void RegisterMessageCallback()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            RegisterMessageCallbackWebGL(gameObject.name, "OnMonadGamesIDResult");
        #endif
    }

    private void InjectJavaScriptBridge()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        string bridgeScript = @"
            window.MonadGamesIDWebViewBridge = {
                sendToUnity: function(data) {
                    try {
                        var jsonData = typeof data === 'object' ? JSON.stringify(data) : data;
                        
                        if (typeof window.unityInstance !== 'undefined' && window.unityInstance) {
                            window.unityInstance.SendMessage('MonadGamesIDWebView', 'OnMonadGamesIDResult', jsonData);
                            console.log('[UNITY BRIDGE]  Sent via unityInstance');
                            return true;
                        }
                        
                        localStorage.setItem('MONAD_WALLET_RESULT', jsonData);
                        console.log('[UNITY BRIDGE]  Saved to localStorage');
                        
                        localStorage.setItem('MONAD_WALLET_TIMESTAMP', Date.now().toString());
                        
                        return true;
                    } catch (err) {
                        console.error('[UNITY BRIDGE]  Error:', err);
                        return false;
                    }
                }
            };
            
            window.OnMonadGamesIDResult = function(jsonData) {
                window.MonadGamesIDWebViewBridge.sendToUnity(jsonData);
            };
            
            console.log('[UNITY BRIDGE]  Bridge initialized and ready');
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
    
    private void InjectMessageListener()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        string listenerScript = @"
            window.ReadMonadWalletResult = function() {
                return localStorage.getItem('MONAD_WALLET_RESULT') || '';
            };
            
            
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
        #endif
    }
    
    private IEnumerator SmartPollingCoroutine()
    {
        if (isResultReceived)
        {
            yield break;
        }
        
        
        int currentInterval = pollingInterval;
        float elapsedTime = 0;
        bool resultFound = false;
        
        yield return new WaitForSeconds(0.5f);
        resultFound = TryReadFromLocalStorage();
        
        if (resultFound)
        {
            yield break;
        }
        
        while (elapsedTime < maxPollingDuration)
        {
            yield return new WaitForSeconds(currentInterval);
            elapsedTime += currentInterval;
            
            resultFound = TryReadFromLocalStorage();
            
            if (resultFound)
            {
                break;
            }
            
            currentInterval = Mathf.Min(currentInterval * pollingBackoffMultiplier, 10);
        }
        
        if (!resultFound)
        {
            Debug.LogWarning("[MONAD WEBVIEW] ⚠️ Aucun résultat trouvé après " + maxPollingDuration + " secondes");
        }
    }
}