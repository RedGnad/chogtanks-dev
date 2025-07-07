using System;
using System.Collections;
using UnityEngine;
using System.Runtime.InteropServices;
using TMPro;

[System.Serializable]
public class EvolutionData
{
    public bool authorized;
    public string walletAddress;
    public int score;
    public int currentLevel;
    public int requiredScore;
    public long nonce;
    public long timestamp;
    public string signature;
    public string error;
}

public class ChogTanksNFTManager : MonoBehaviour
{
    [Header("Contract Settings")]
    // Adresse du contrat NFT sur Monad Testnet
    private const string CONTRACT_ADDRESS = "0x7EF2e0048f5bAeDe046f6BF797943daF4ED8CB47";
    
    [Header("UI References")]
    public UnityEngine.UI.Button evolutionButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI levelText;
    
    private string currentPlayerWallet = "";
    private bool isProcessingEvolution = false;

    // Import des fonctions JavaScript depuis ton FirebasePlugin.jslib
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool CheckEvolutionEligibilityJS(string walletAddress);
    
    [DllImport("__Internal")]
    private static extern bool UpdateNFTLevelJS(string walletAddress, string newLevel);
#endif

    void Start()
    {
        // Récupérer l'adresse wallet depuis ton système existant
        RefreshWalletAddress();
        
        // Setup UI
        if (evolutionButton != null)
        {
            evolutionButton.onClick.AddListener(OnEvolutionButtonClicked);
        }
        
        UpdateStatusUI("Initializing...");
    }

    void RefreshWalletAddress()
    {
        string walletAddress = "";
        
        // ✅ COPIE EXACTE de NFTVerifyUI.IsWalletConnected()
        // Méthode 1 : Vérifier AppKit EN PREMIER (source de vérité)
        try
        {
            if (Reown.AppKit.Unity.AppKit.IsInitialized && 
                Reown.AppKit.Unity.AppKit.IsAccountConnected && 
                Reown.AppKit.Unity.AppKit.Account != null)
            {
                string appKitAddress = Reown.AppKit.Unity.AppKit.Account.Address;
                if (!string.IsNullOrEmpty(appKitAddress))
                {
                    walletAddress = appKitAddress;
                    Debug.Log($"[NFT] Wallet trouvé dans AppKit: {appKitAddress}");
                    PlayerPrefs.SetString("walletAddress", appKitAddress);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[NFT] Erreur AppKit: {ex.Message}");
        }
        
        // Méthode 2 : Vérifier PlayerPrefs (seulement si AppKit pas disponible)
        if (string.IsNullOrEmpty(walletAddress))
        {
            string walletFromPrefs = PlayerPrefs.GetString("walletAddress", "");
            if (!string.IsNullOrEmpty(walletFromPrefs))
            {
                walletAddress = walletFromPrefs;
                Debug.Log($"[NFT] Wallet trouvé dans PlayerPrefs: {walletFromPrefs}");
            }
        }
        
        // Méthode 3 : Vérifier PlayerSession (en dernier recours)
        if (string.IsNullOrEmpty(walletAddress))
        {
            try
            {
                if (PlayerSession.IsConnected && !string.IsNullOrEmpty(PlayerSession.WalletAddress))
                {
                    walletAddress = PlayerSession.WalletAddress;
                    Debug.Log($"[NFT] Wallet trouvé dans PlayerSession: {PlayerSession.WalletAddress}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[NFT] Erreur PlayerSession: {ex.Message}");
            }
        }
        
        if (!string.IsNullOrEmpty(walletAddress))
        {
            currentPlayerWallet = walletAddress;
            Debug.Log($"[NFT] Wallet final détecté: {currentPlayerWallet}");
            UpdateStatusUI($"Wallet connected: {walletAddress.Substring(0, 6)}...");
        }
        else
        {
            Debug.LogError("[NFT] Aucun wallet connecté détecté");
            UpdateStatusUI("No wallet connected");
        }
    }

    public void OnEvolutionButtonClicked()
    {
        Debug.Log("[NFT] OnEvolutionButtonClicked called!");
        
        if (isProcessingEvolution)
        {
            Debug.Log("[NFT] Evolution already in progress...");
            return;
        }
        
        // DETECTION APPKIT DIRECTE
        string walletAddress = "";
        try
        {
            if (Reown.AppKit.Unity.AppKit.IsInitialized && 
                Reown.AppKit.Unity.AppKit.IsAccountConnected && 
                Reown.AppKit.Unity.AppKit.Account != null)
            {
                walletAddress = Reown.AppKit.Unity.AppKit.Account.Address;
                Debug.Log($"[NFT] Wallet found: {walletAddress}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFT] AppKit error: {ex.Message}");
        }
        
        if (string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogWarning("[NFT] No wallet detected!");
            UpdateStatusUI("Please connect your wallet first");
            return;
        }
        
        currentPlayerWallet = walletAddress;
        Debug.Log($"[NFT] Starting evolution for: {walletAddress}");
        RequestEvolution();
    }

    public void RequestEvolution()
    {
        Debug.Log($"[NFT] Requesting evolution for wallet: {currentPlayerWallet}");
        isProcessingEvolution = true;
        UpdateStatusUI("Checking evolution eligibility...");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        CheckEvolutionEligibilityJS(currentPlayerWallet);
#else
        // Simulation pour l'éditeur
        var mockData = new EvolutionData
        {
            authorized = true,
            walletAddress = currentPlayerWallet,
            score = 150,
            currentLevel = 1,
            requiredScore = 100,
            nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            signature = "0xmocksignature123"
        };
        OnEvolutionCheckComplete(JsonUtility.ToJson(mockData));
#endif
    }

    // Callback appelé depuis JavaScript
    public void OnEvolutionCheckComplete(string evolutionDataJson)
    {
        try
        {
            var evolutionData = JsonUtility.FromJson<EvolutionData>(evolutionDataJson);
            
            Debug.Log($"[NFT] Evolution check result: Authorized={evolutionData.authorized}, Score={evolutionData.score}, Level={evolutionData.currentLevel}");
            
            if (evolutionData.authorized)
            {
                UpdateStatusUI($"Evolution authorized! Score: {evolutionData.score}/{evolutionData.requiredScore}");
                
                // Maintenant appeler le smart contract
                CallSmartContractEvolution(evolutionData);
            }
            else
            {
                string errorMsg = !string.IsNullOrEmpty(evolutionData.error) ? 
                    evolutionData.error : 
                    $"Insufficient score: {evolutionData.score}/{evolutionData.requiredScore}";
                    
                UpdateStatusUI($"Evolution denied: {errorMsg}");
                isProcessingEvolution = false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFT] Error parsing evolution data: {ex.Message}");
            UpdateStatusUI("Error checking evolution eligibility");
            isProcessingEvolution = false;
        }
    }

    public void OnEvolutionCheckError(string error)
    {
        Debug.LogError($"[NFT] Evolution check error: {error}");
        UpdateStatusUI($"Error: {error}");
        isProcessingEvolution = false;
    }

    void CallSmartContractEvolution(EvolutionData evolutionData)
    {
        UpdateStatusUI("Calling smart contract...");
        
        // Ici tu peux utiliser ton système Web3 existant ou ajouter du JavaScript
        Debug.Log($"[NFT] Calling smart contract evolution:");
        Debug.Log($"  - Contract: {CONTRACT_ADDRESS}");
        Debug.Log($"  - Wallet: {evolutionData.walletAddress}");
        Debug.Log($"  - Score: {evolutionData.score}");
        Debug.Log($"  - Nonce: {evolutionData.nonce}");
        Debug.Log($"  - Signature: {evolutionData.signature}");
        
        // Pour l'instant, simulation d'évolution réussie
        StartCoroutine(SimulateEvolutionSuccess(evolutionData));
    }

    IEnumerator SimulateEvolutionSuccess(EvolutionData evolutionData)
    {
        yield return new WaitForSeconds(2f); // Simulation transaction
        
        int newLevel = evolutionData.currentLevel + 1;
        
        // Mettre à jour le niveau dans Firebase
#if UNITY_WEBGL && !UNITY_EDITOR
        UpdateNFTLevelJS(currentPlayerWallet, newLevel.ToString());
#endif
        
        UpdateStatusUI($"Evolution successful! New level: {newLevel}");
        UpdateLevelUI(newLevel);
        
        isProcessingEvolution = false;
        
        Debug.Log($"[NFT] Evolution completed! New level: {newLevel}");
    }

    // Callbacks pour la mise à jour du niveau
    public void OnNFTLevelUpdated(string newLevel)
    {
        Debug.Log($"[NFT] NFT level updated in database: {newLevel}");
        UpdateLevelUI(int.Parse(newLevel));
    }

    public void OnNFTLevelUpdateError(string error)
    {
        Debug.LogError($"[NFT] Error updating NFT level: {error}");
    }

    void UpdateStatusUI(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[NFT] Status: {message}");
    }

    void UpdateLevelUI(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"NFT Level: {level}";
        }
    }

    // Fonctions de test disponibles dans l'inspecteur
    [ContextMenu("Test Evolution")]
    public void TestEvolution()
    {
        if (!string.IsNullOrEmpty(currentPlayerWallet))
        {
            RequestEvolution();
        }
        else
        {
            Debug.LogWarning("[NFT] No wallet connected for test");
        }
    }
}
