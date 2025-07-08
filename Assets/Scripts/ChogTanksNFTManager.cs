using System;
using System.Collections;
using System.Numerics; 
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

[System.Serializable]
public class NFTStateData
{
    public bool hasNFT;
    public int level;
    public string walletAddress;
    public int score;
}

[System.Serializable]
public class CanMintResponse
{
    public bool canMint;
    public string error;
}

public class ChogTanksNFTManager : MonoBehaviour
{
    [Header("Contract Settings")]
    // Adresse du contrat NFT sur Monad Testnet
    private const string CONTRACT_ADDRESS = "0xa19a0b4d8c6f842ac81c265050cf0c187018a5e7";
    private const string MINT_NFT_SELECTOR = "0x6a627842"; // mintNFT()
    private const string EVOLVE_NFT_SELECTOR = "0x7e3ba4a8"; // evolveNFT(uint256)
    private const string UPDATE_SCORE_SELECTOR = "0x24bbd84c"; // updateScore(address,uint256)
    
    [Header("UI References")]
    public UnityEngine.UI.Button evolutionButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI levelText;
    
    private string currentPlayerWallet = "";
    private bool isProcessingEvolution = false;
    private NFTStateData currentNFTState = new NFTStateData();

    // Import des fonctions JavaScript depuis ton FirebasePlugin.jslib
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void CheckEvolutionEligibilityJS(string walletAddress);
    
    [DllImport("__Internal")]
    private static extern void GetNFTStateJS(string walletAddress);
    
    [DllImport("__Internal")]
    private static extern void UpdateNFTLevelJS(string walletAddress, int newLevel);
    
    [DllImport("__Internal")]
    private static extern void CanMintNFTJS(string walletAddress, string callbackMethod);
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
        
        // Charger l'état NFT depuis Firebase
        if (!string.IsNullOrEmpty(currentPlayerWallet))
        {
            LoadNFTStateFromFirebase();
        }
    }

    void RefreshWalletAddress()
    {
        string walletAddress = "";
        
        // COPIE EXACTE de NFTVerifyUI.IsWalletConnected()
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

    // Charger l'état NFT depuis Firebase (source de vérité)
    private void LoadNFTStateFromFirebase()
    {
        if (string.IsNullOrEmpty(currentPlayerWallet))
        {
            Debug.LogError("[NFT] No wallet address to load NFT state");
            return;
        }

        UpdateStatusUI("Loading NFT state...");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        GetNFTStateJS(currentPlayerWallet);
#else
        // Simulation pour l'éditeur
        var mockNFTState = new NFTStateData
        {
            hasNFT = false, // Simuler aucun NFT pour tester le mint
            level = 0,
            walletAddress = currentPlayerWallet,
            score = 150
        };
        OnNFTStateLoaded(JsonUtility.ToJson(mockNFTState));
#endif
    }

    // Callback pour l'état NFT chargé depuis Firebase
    public void OnNFTStateLoaded(string nftStateJson)
    {
        try
        {
            currentNFTState = JsonUtility.FromJson<NFTStateData>(nftStateJson);
            Debug.Log($"[NFT] NFT state loaded: hasNFT={currentNFTState.hasNFT}, level={currentNFTState.level}, score={currentNFTState.score}");
            
            if (currentNFTState.hasNFT && currentNFTState.level > 0)
            {
                UpdateStatusUI($"You have a Level {currentNFTState.level} NFT");
                UpdateLevelUI(currentNFTState.level);
            }
            else
            {
                UpdateStatusUI("Ready to mint your first NFT!");
                UpdateLevelUI(0);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NFT] Error parsing NFT state: {ex.Message}");
            UpdateStatusUI("Error loading NFT state");
            
            // Fallback: état par défaut
            currentNFTState = new NFTStateData
            {
                hasNFT = false,
                level = 0,
                walletAddress = currentPlayerWallet,
                score = 0
            };
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
        
        // Vérifier l'état NFT depuis Firebase (source de vérité)
        if (!currentNFTState.hasNFT || currentNFTState.level == 0)
        {
            // Double-vérification avec Firebase avant le mint
            isProcessingEvolution = true;
            UpdateStatusUI("Checking mint eligibility...");
            
            // Vérifier l'éligibilité au mint via Firebase (source de vérité)
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log($"[NFT] Checking mint eligibility for {currentPlayerWallet}");
            CanMintNFTJS(currentPlayerWallet, "OnCanMintChecked");
#else
            // En mode éditeur, simuler la vérification
            Debug.Log("[NFT] [EDITOR] Simulating mint eligibility check");
            OnCanMintChecked(JsonUtility.ToJson(new CanMintResponse { canMint = true }));
#endif
        }
        else
        {
            // A déjà un NFT = vérifier les points pour évoluer
            RequestEvolution();
        }
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
            score = 250, // Score simulé
            currentLevel = currentNFTState.level,
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
            
            Debug.Log($"[NFT] Evolution check result: Authorized={evolutionData.authorized}, Score={evolutionData.score}");
            
            if (evolutionData.authorized)
            {
                // Calculer le niveau cible basé sur le score et le niveau actuel
                int targetLevel = CalculateTargetLevel(evolutionData.score, evolutionData.currentLevel);
                
                if (targetLevel > currentNFTState.level)
                {
                    UpdateStatusUI($"Evolution authorized to Level {targetLevel}! Score: {evolutionData.score}");
                    SendEvolveTransaction(targetLevel);
                }
                else
                {
                    UpdateStatusUI($"Already at maximum level for your score ({evolutionData.score} points)");
                    isProcessingEvolution = false;
                }
            }
            else
            {
                string errorMsg = !string.IsNullOrEmpty(evolutionData.error) ? 
                    evolutionData.error : 
                    $"Insufficient Score: {evolutionData.score}";
                    
                UpdateStatusUI($"Git Gud. {errorMsg}");  // Espace ajouté après Git Gud.
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

    // Calculer le niveau cible basé sur le score
    private int CalculateTargetLevel(int score, int currentLevel = 1)
    {
        // Logique adaptée pour correspondre à celle du JavaScript:
        // - Niveau 1->2 : seulement 2 points requis
        // - Niveau 2->3 : 100 points requis
        // - Niveau 3->4 : 200 points requis (100*(currentLevel-1))
        // - etc.
        
        if (score >= 2 && currentLevel == 1)
        {
            // Cas spécial: niveau 1 à 2 avec 2 points minimum
            return 2;
        }
        
        // Pour les niveaux supérieurs, reprendre la formule générale
        // Calcul basé sur le score disponible
        int maxLevel = 2; // Démarre à 2 car niveau 1->2 est un cas spécial
        int threshold = 100; // Seuil de base pour le niveau 3
        
        while (score >= threshold)
        {
            maxLevel++;
            threshold += 100; // Augmentation de 100 par niveau
        }
        
        // S'assurer que le niveau retourné est au moins le niveau actuel
        return Mathf.Max(currentLevel, maxLevel);
    }

    private async void SendMintTransaction()
    {
        try
        {
            UpdateStatusUI("Sending mint transaction...");
            
            if (Reown.AppKit.Unity.AppKit.IsInitialized && 
                Reown.AppKit.Unity.AppKit.IsAccountConnected)
            {
                Debug.Log("[NFT] Preparing mint transaction");
                
                // Pour le mint, on utilise la fonction mintNFT() ou safeMint()
                string functionSelector = MINT_NFT_SELECTOR;
                string data = functionSelector;
                
                try 
                {
                    Debug.Log($"[NFT] Sending mint transaction to {CONTRACT_ADDRESS}");
                    
                    // Calculer la valeur 0.001 ETH en wei (1 ETH = 10^18 wei)
                    BigInteger mintPrice = BigInteger.Parse("1000000000000000"); // 0.001 ETH
                    
                    Debug.Log($"[NFT] Sending mint transaction with {mintPrice} wei (0.001 ETH)");
                    
                    // Utiliser la méthode correcte pour envoyer la transaction avec la valeur
                    var result = await Reown.AppKit.Unity.AppKit.Evm.SendTransactionAsync(
                        CONTRACT_ADDRESS,  // to address
                        mintPrice,         // value (0.001 ETH sent)
                        data               // transaction data
                    );
                    
                    Debug.Log($"[NFT] Mint transaction result: {result}");
                    if (!string.IsNullOrEmpty(result))
                    {
                        OnMintTransactionSuccess(result);
                    }
                    else
                    {
                        OnTransactionError("Empty transaction result");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NFT] Mint transaction failed: {ex.Message}");
                    OnTransactionError(ex.Message);
                }
            }
            else
            {
                Debug.LogError("[NFT] AppKit not initialized or wallet not connected");
                UpdateStatusUI("Connect your wallet first");
                isProcessingEvolution = false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFT] Error sending mint transaction: {ex.Message}");
            UpdateStatusUI("Error sending mint transaction");
            isProcessingEvolution = false;
        }
    }

    private async void SendEvolveTransaction(int targetLevel)
    {
        try
        {
            UpdateStatusUI("Sending evolution transaction...");
            
            if (Reown.AppKit.Unity.AppKit.IsInitialized && 
                Reown.AppKit.Unity.AppKit.IsAccountConnected)
            {
                Debug.Log($"[NFT] Preparing evolution transaction for level {targetLevel}");
                
                // Encodage pour evolveNFT(uint256)
                string functionSelector = EVOLVE_NFT_SELECTOR;
                string paddedLevel = targetLevel.ToString("X").PadLeft(64, '0');
                string data = functionSelector + paddedLevel;
                
                Debug.Log($"[NFT] Transaction data: {data}");
                
                try 
                {
                    Debug.Log($"[NFT] Sending evolution transaction to {CONTRACT_ADDRESS}");
                    // Utiliser la méthode correcte pour envoyer la transaction
                    var result = await Reown.AppKit.Unity.AppKit.Evm.SendTransactionAsync(
                        CONTRACT_ADDRESS,  // to address
                        BigInteger.Zero,   // value (no ETH sent)
                        data               // transaction data
                    );
                    
                    Debug.Log($"[NFT] Evolution transaction result: {result}");
                    if (!string.IsNullOrEmpty(result))
                    {
                        OnEvolveTransactionSuccess(result, targetLevel);
                    }
                    else
                    {
                        OnTransactionError("Empty transaction result");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NFT] Evolution transaction failed: {ex.Message}");
                    OnTransactionError(ex.Message);
                }
            }
            else
            {
                Debug.LogError("[NFT] AppKit not initialized or wallet not connected");
                UpdateStatusUI("Connect your wallet first");
                isProcessingEvolution = false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFT] Error sending evolution transaction: {ex.Message}");
            UpdateStatusUI("Error sending transaction");
            isProcessingEvolution = false;
        }
    }

    private void OnMintTransactionSuccess(string transactionHash)
    {
        try
        {
            Debug.Log($"[NFT] Mint transaction sent: {transactionHash}");
            Debug.Log($"[NFT] Current wallet address: {currentPlayerWallet}");
            
            string displayHash = string.IsNullOrEmpty(transactionHash) ? 
                "unknown" : 
                (transactionHash.Length > 10 ? transactionHash.Substring(0, 10) + "..." : transactionHash);
            
            // Mettre à jour l'état dans Firebase (source de vérité)
            Debug.Log($"[NFT] Calling UpdateNFTLevelInFirebase with level 1 for wallet: {currentPlayerWallet}");
            UpdateNFTLevelInFirebase(1);
            
            // Mettre à jour l'état local
            currentNFTState.hasNFT = true;
            currentNFTState.level = 1;
            Debug.Log($"[NFT] Local state updated: hasNFT=true, level=1");
            
            UpdateStatusUI($"NFT minted successfully! TX: {displayHash}");
            UpdateLevelUI(1);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NFT] Error in OnMintTransactionSuccess: {ex.Message}");
            UpdateStatusUI("Error processing mint result");
        }
        finally
        {
            isProcessingEvolution = false;
        }
    }

    private void OnEvolveTransactionSuccess(string transactionHash, int newLevel)
    {
        try
        {
            Debug.Log($"[NFT] Evolution transaction sent: {transactionHash}");
            
            string displayHash = string.IsNullOrEmpty(transactionHash) ? 
                "unknown" : 
                (transactionHash.Length > 10 ? transactionHash.Substring(0, 10) + "..." : transactionHash);
            
            // Mettre à jour l'état dans Firebase (source de vérité)
            UpdateNFTLevelInFirebase(newLevel);
            
            // Mettre à jour l'état local
            currentNFTState.level = newLevel;
            
            UpdateStatusUI($"NFT evolved to Level {newLevel}! TX: {displayHash}");
            UpdateLevelUI(newLevel);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NFT] Error in OnEvolveTransactionSuccess: {ex.Message}");
            UpdateStatusUI("Error processing evolution result");
        }
        finally
        {
            isProcessingEvolution = false;
        }
    }

    // Mettre à jour le niveau NFT dans Firebase
    private void UpdateNFTLevelInFirebase(int newLevel)
    {
        if (string.IsNullOrEmpty(currentPlayerWallet))
        {
            Debug.LogError("[NFT] Cannot update NFT level: currentPlayerWallet is empty!");
            return;
        }
        
        Debug.Log($"[NFT] Updating NFT level to {newLevel} in Firebase for wallet: {currentPlayerWallet}");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[NFT] Calling UpdateNFTLevelJS({currentPlayerWallet}, {newLevel})");
        UpdateNFTLevelJS(currentPlayerWallet, newLevel);
        Debug.Log($"[NFT] UpdateNFTLevelJS call completed");
#else
        Debug.Log($"[NFT] [Editor] Would update Firebase NFT level to {newLevel}");
#endif
    }

    // Callback pour la mise à jour réussie dans Firebase
    public void OnNFTLevelUpdated(string levelStr)
    {
        try
        {
            if (string.IsNullOrEmpty(levelStr) || !int.TryParse(levelStr, out int level))
            {
                Debug.LogError($"[NFT] Invalid level value received: {levelStr}");
                level = 0; // Default to 0 if invalid
            }
            
            Debug.Log($"[NFT] NFT level updated in Firebase: {level}");
            
            // Synchroniser l'état local
            currentNFTState.level = level;
            currentNFTState.hasNFT = level > 0;
            
            UpdateLevelUI(level);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NFT] Error in OnNFTLevelUpdated: {ex.Message}");
        }
    }
    
    // Alias for backward compatibility
    public void OnNFTStateReceived(string levelStr) => OnNFTLevelUpdated(levelStr);
    
    // Alias for backward compatibility
    public void OnEvolutionEligibilityChecked(string evolutionDataJson) => OnEvolutionCheckComplete(evolutionDataJson);
    
    // Callback pour la vérification d'éligibilité au mint
    public void OnCanMintChecked(string jsonResponse)
    {
        try
        {
            Debug.Log($"[NFT] OnCanMintChecked received: {jsonResponse}");
            CanMintResponse response = JsonUtility.FromJson<CanMintResponse>(jsonResponse);
            
            if (response == null)
            {
                Debug.LogError("[NFT] Failed to parse CanMintResponse JSON");
                UpdateStatusUI("Error checking mint eligibility");
                isProcessingEvolution = false;
                return;
            }
            
            if (response.canMint)
            {
                // Eligible pour mint, continuer avec la transaction
                Debug.Log("[NFT] Wallet is eligible for minting NFT");
                SendMintTransaction();
            }
            else
            {
                // Déjà minté ou erreur
                string errorMsg = !string.IsNullOrEmpty(response.error) ? 
                    response.error : 
                    "This wallet already has an NFT";
                    
                Debug.LogWarning($"[NFT] Cannot mint NFT: {errorMsg}");
                UpdateStatusUI($"Cannot mint: {errorMsg}");
                isProcessingEvolution = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NFT] Error in OnCanMintChecked: {ex.Message}");
            UpdateStatusUI("Error checking mint eligibility");
            isProcessingEvolution = false;
        }
    }

    // Callback pour les erreurs de mise à jour Firebase
    public void OnNFTLevelUpdateError(string error)
    {
        Debug.LogError($"[NFT] Error updating NFT level in Firebase: {error}");
        // L'état local reste, mais Firebase pourrait être désynchronisé
    }

    private void OnTransactionError(string error)
    {
        Debug.LogError($"[NFT] Transaction error: {error}");
        UpdateStatusUI($"Transaction error: {error}");
        isProcessingEvolution = false;
    }

    void UpdateLevelUI(int level)
    {
        if (levelText != null)
        {
            levelText.text = level > 0 ? $"NFT Level: {level}" : "No NFT";
        }
    }

    void UpdateStatusUI(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[NFT] Status: {message}");
    }

    // Fonctions de test disponibles dans l'inspecteur
    [ContextMenu("Test Evolution")]
    public void TestEvolution()
    {
        if (!string.IsNullOrEmpty(currentPlayerWallet))
        {
            OnEvolutionButtonClicked();
        }
        else
        {
            Debug.LogWarning("[NFT] No wallet connected for test");
        }
    }

    [ContextMenu("Reload NFT State")]
    public void ReloadNFTState()
    {
        if (!string.IsNullOrEmpty(currentPlayerWallet))
        {
            LoadNFTStateFromFirebase();
        }
        else
        {
            Debug.LogWarning("[NFT] No wallet connected");
        }
    }
}
