using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Numerics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

[System.Serializable]
public class EvolutionData
{
    public bool authorized;
    public string walletAddress;
    public int score;
    public int currentLevel;
    public int targetLevel;    // Target level for evolution (current + 1)
    public int requiredScore;
    public int evolutionCost;  // Cost to consume
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
    public int tokenId;
    public int nftCount;
}

[System.Serializable]
public class CanMintResponse
{
    public bool canMint;
    public string error;
}

[System.Serializable]
public class EvolutionAuthorizationData
{
    public bool authorized;
    public string walletAddress;
    public int tokenId;
    public int currentPoints;
    public int evolutionCost;
    public int targetLevel;
    public long nonce;
    public string signature;
    public string error;
}

[System.Serializable]
public class PointConsumptionResponse
{
    public bool success;
    public int newScore; // ✅ Corrected to match JavaScript response
    public string error;
    public UpdatedNFTInfo updatedNFT;
}

[System.Serializable]
public class UpdatedNFTInfo
{
    public int tokenId;
    public int newLevel;
}

[System.Serializable]
public class PreEvolutionResponse
{
    public bool success;
    public bool authorized;
    public int pointsConsumed;
    public int newScore;
    public int tokenId;
    public int targetLevel;
    public string error;
    public int currentScore;
    public int pointsRequired;
}

[System.Serializable]
public class MintAuthorizationData
{
    public bool authorized;
    public string walletAddress;
    public long mintPrice;
    public long nonce;
    public string signature;
    public string error;
}

[System.Serializable]
public class PointsConsumptionResponse
{
    public bool success;
    public int consumedPoints;
    public int newScore;
    public string walletAddress;
    public string error;
}

public class ChogTanksNFTManager : MonoBehaviour
{
    [Header("Contract Settings")]
    private const string CONTRACT_ADDRESS = "0x68c582651d709f6e2b6113c01d69443f8d27e30d";
    
    // Events for UI updates
    public static System.Action<bool, int> OnNFTStateChanged; 
    private const string MINT_NFT_SELECTOR = "0xd46c2811";
    private const string EVOLVE_NFT_SELECTOR = "0x3365a3b6";
    private const string GET_LEVEL_SELECTOR = "0x86481d40";
    private const string CAN_MINT_NFT_SELECTOR = "0x13d0a65a";
    private const string TOTAL_SUPPLY_SELECTOR = "0x18160ddd";
    private const string REMAINING_SUPPLY_SELECTOR = "0xda0239a6";
    private const string IS_MAX_SUPPLY_REACHED_SELECTOR = "0xf931377b";
    private const string GET_WALLET_NFTS_SELECTOR = "0xbc116540";
    private const string GET_WALLET_NFTS_DETAILS_SELECTOR = "0x60e4f45b"; 
    
    [Header("UI References")]
    public UnityEngine.UI.Button evolutionButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI scoreProgressText;
    
    [Header("Warm-Up System")]
    [Tooltip("Button that triggers the warm-up (e.g., settings button)")]
    public UnityEngine.UI.Button warmUpTriggerButton;
    [Tooltip("Button to simulate clicking (e.g., evolution button)")]
    public UnityEngine.UI.Button warmUpTargetButton;
    private bool hasWarmedUp = false;
    private bool isWarmingUp = false; // Track if warm-up is currently in progress
    
    [Header("Simple NFT Buttons (Coexist with Panel)")]
    [Tooltip("Container for simple NFT buttons - should be positioned to not conflict with NFTDisplayPanel")]
    public Transform nftButtonContainer;
    [Tooltip("Optional: Prefab template for NFT buttons")]
    public GameObject nftButtonPrefab;
    private List<UnityEngine.UI.Button> nftButtons = new List<UnityEngine.UI.Button>();
    
    private string currentPlayerWallet = "";
    private bool isProcessingEvolution = false;
    public NFTStateData currentNFTState = new NFTStateData();
    public int selectedTokenId = 0;
    private int lastConsumedPoints = 0; // Track points that should be consumed after transaction success
    private int pendingEvolutionCost = 0; // Track evolution cost for current attempt

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void GetNFTStateJS(string walletAddress);
    
    [DllImport("__Internal")]
    private static extern void CheckEvolutionEligibilityJS(string walletAddress);
    
    [DllImport("__Internal")]
    private static extern void SetUnityNFTStateJS(string nftStateJson);
    
    [DllImport("__Internal")]
    private static extern void CanMintNFTJS(string walletAddress, string callbackMethod);
    
    [DllImport("__Internal")]
    private static extern void DirectMintNFTJS(string walletAddress);
    
    [DllImport("__Internal")]
    private static extern void MarkMintSuccessJS(string walletAddress);
    
    [DllImport("__Internal")]
    public static extern void CheckHasMintedNFTJS(string walletAddress);
    
    [DllImport("__Internal")]
    private static extern void UpdateNFTLevelJS(string walletAddress, int newLevel);
    
    [DllImport("__Internal")]
    private static extern void ReadNFTFromBlockchainJS(string walletAddress, string callbackMethod);
    
    [DllImport("__Internal")]
    private static extern void SyncNFTLevelWithFirebaseJS(string walletAddress, int blockchainLevel, int tokenId);
    
    [DllImport("__Internal")]
    private static extern void CheckEvolutionEligibilityOnlyJS(string walletAddress, int pointsRequired, int tokenId, int targetLevel);
    
    [DllImport("__Internal")]
    private static extern void ConsumePointsAfterSuccessJS(string walletAddress, int pointsToConsume, int tokenId, int newLevel);
    
    [DllImport("__Internal")]
    private static extern void SetupRealTransactionDetection();
    
    [DllImport("__Internal")]
    private static extern void RequestEvolutionSignatureJS(string walletAddress, int tokenId, int playerPoints, int targetLevel);
#else
    private static void GetNFTStateJS(string walletAddress) { }
    private static void CheckEvolutionEligibilityJS(string walletAddress) { }
    private static void CanMintNFTJS(string walletAddress, string callbackMethod) { }
    
    private static void MarkMintSuccessJS(string walletAddress) { }
    private static void CheckHasMintedNFTJS(string walletAddress) { }
    private static void UpdateNFTLevelJS(string walletAddress, int newLevel) { }
    private static void ReadNFTFromBlockchainJS(string walletAddress, string callbackMethod) { }
    private static void SyncNFTLevelWithFirebaseJS(string walletAddress, int blockchainLevel, int tokenId) { }
    private static void CheckEvolutionEligibilityOnlyJS(string walletAddress, int pointsRequired, int tokenId, int targetLevel) { }
    private static void ConsumePointsAfterSuccessJS(string walletAddress, int pointsToConsume, int tokenId, int newLevel) { }
    private static void RequestEvolutionSignatureJS(string walletAddress, int tokenId, int playerPoints, int targetLevel) { }
#endif

    void Start()
    {
        HideLevelUI();
        
        // Subscribe to NFT state change events
        OnNFTStateChanged += HandleNFTStateChanged;
        
        if (evolutionButton != null)
        {
            Debug.Log("[NFT-DEBUG] Evolution button found and listener added");
            evolutionButton.onClick.AddListener(OnEvolutionButtonClicked);
        }
        else
        {
            Debug.LogError("[NFT-DEBUG] Evolution button is NULL in Start()!");
        }
        
        UpdateStatusUI(" ");
        
        currentPlayerWallet = PlayerPrefs.GetString("walletAddress", "");
        
        if (!string.IsNullOrEmpty(currentPlayerWallet))
        {
            Debug.Log($"[NFT-DEBUG] Wallet found in PlayerPrefs: {currentPlayerWallet}");
            Debug.Log($"[NFT-DEBUG] Starting delayed reconnection process...");
            StartCoroutine(DelayedReconnection());
        }
        
        var connect = FindObjectOfType<Sample.ConnectWalletButton>();
        if (connect != null)
        {
            connect.OnPersonalSignCompleted += OnPersonalSignApproved;
        }
        
        // Setup warm-up system
        SetupWarmUpSystem();
        
        // 🎯 Setup real transaction detection
        InitializeRealTransactionDetection();
        
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        OnNFTStateChanged -= HandleNFTStateChanged;
    }
    
    // ===== WARM-UP SYSTEM =====
    private void SetupWarmUpSystem()
    {
        if (warmUpTriggerButton != null)
        {
            Debug.Log("[WARM-UP] 🎯 Setting up warm-up trigger button");
            warmUpTriggerButton.onClick.AddListener(OnWarmUpTriggerClicked);
        }
        else
        {
            Debug.LogWarning("[WARM-UP] ⚠️ Warm-up trigger button not assigned in Inspector");
        }
    }
    
    private void OnWarmUpTriggerClicked()
    {
        Debug.Log("[WARM-UP] 🎯 Warm-up trigger activated");
        
        if (!hasWarmedUp)
        {
            Debug.Log("[WARM-UP] 🚀 First time trigger - starting warm-up simulation");
            hasWarmedUp = true;
            isWarmingUp = true; // Mark warm-up as in progress
            StartCoroutine(SimulateButtonClickSilently());
        }
        else
        {
            Debug.Log("[WARM-UP] ✅ Already warmed up this session");
        }
    }
    
    private System.Collections.IEnumerator SimulateButtonClickSilently()
    {
        Debug.Log("[WARM-UP] 🤫 Simulating button click silently...");
        
        if (warmUpTargetButton != null)
        {
            // Wait one frame to ensure clean execution
            yield return null;
            
            Debug.Log("[WARM-UP] 🖱️ Invoking target button click silently");
            
            // Simulate the actual button click by invoking all its listeners
            warmUpTargetButton.onClick.Invoke();
            
            Debug.Log("[WARM-UP] ✅ Silent button click simulation completed");
            
            // Wait a moment then end warm-up state
            yield return new WaitForSeconds(0.1f);
            isWarmingUp = false;
            Debug.Log("[WARM-UP] 🏁 Warm-up state ended - normal flow resumed");
        }
        else
        {
            Debug.LogWarning("[WARM-UP] ⚠️ Warm-up target button not assigned in Inspector");
            isWarmingUp = false;
        }
    }
    // ===== END WARM-UP SYSTEM =====
    
    private void HandleNFTStateChanged(bool hasNFT, int nftCount)
    {
        Debug.Log($"[NFT-UI] ===== HANDLING NFT STATE CHANGE =====");
        Debug.Log($"[NFT-UI] hasNFT={hasNFT}, count={nftCount}");
        Debug.Log($"[NFT-UI] Current UI components - statusText: {(statusText != null ? "OK" : "NULL")}, levelText: {(levelText != null ? "OK" : "NULL")}");
        
        if (hasNFT && nftCount > 0)
        {
            Debug.Log($"[NFT-UI] Updating UI to show {nftCount} NFT(s)");
            
            // Force UI update
            UpdateStatusUI($"{nftCount} NFT FOUND - MINTED SUCCESSFULLY!");
            UpdateLevelUI(1); // Assume level 1 for new mint
            
            // Update internal state
            currentNFTState.hasNFT = true;
            currentNFTState.level = 1;
            currentNFTState.tokenId = 1;
            
            // Force UI visibility
            ShowLevelUI();
            
            Debug.Log($"[NFT-UI] ✅ UI FORCEFULLY UPDATED: Status='{nftCount} NFT FOUND', Level=1");
        }
        else
        {
            Debug.Log($"[NFT-UI] No NFT to display (hasNFT={hasNFT}, count={nftCount})");
        }
    }
    
    void OnPersonalSignApproved()
    {
        Debug.Log("[NFTManager] Personal sign completed - refreshing wallet and UI");
        currentPlayerWallet = PlayerPrefs.GetString("walletAddress", "");
        if (!string.IsNullOrEmpty(currentPlayerWallet))
        {
            Debug.Log($"[NFT-UI] Wallet connected: {currentPlayerWallet} - forcing UI refresh");
            
            // Force immediate UI reset to clear old Firebase data
            UpdateStatusUI("Checking blockchain state...");
            HideLevelUI();
            
            // Load fresh blockchain state
            LoadNFTStateFromBlockchain();
            
            Debug.Log("[NFT-UI] Blockchain state refresh initiated after wallet connection");
        }
    }
    
    public void HideLevelUI()
    {
        if (levelText != null)
        {
            levelText.gameObject.SetActive(false);
        }
        
        if (scoreProgressText != null)
        {
            scoreProgressText.gameObject.SetActive(false);
        }
        
        // Réinitialiser le texte d'état NFT seulement s'il contient un message NFT
        if (statusText != null && statusText.text.Contains("Level"))
        {
            statusText.text = " "; // Caractère vide par défaut
        }
    }
    
    public void ShowLevelUI()
    {
        string walletAddress = PlayerPrefs.GetString("walletAddress", "");
        bool hasWallet = !string.IsNullOrEmpty(walletAddress);
        bool signApproved = PlayerPrefs.GetInt("personalSignApproved", 0) == 1;
        
        Debug.Log($"[UI-LEVEL] ShowLevelUI check: hasWallet={hasWallet}, signApproved={signApproved}");
        
        // ✅ CONDITION STRICTE: Wallet ET signature requis
        if (hasWallet && signApproved)
        {
            Debug.Log($"[UI-LEVEL] ✅ Both wallet and signature approved - showing level UI");
            
            if (levelText != null)
            {
                levelText.gameObject.SetActive(true);
            }
            
            if (scoreProgressText != null)
            {
                scoreProgressText.gameObject.SetActive(true);
            }
        }
        else
        {
            Debug.Log($"[UI-LEVEL] ❌ UI hidden - wallet: {hasWallet}, signature: {signApproved}");
            
            // Garder les UI cachées si signature pas faite
            if (levelText != null)
            {
                levelText.gameObject.SetActive(false);
            }
            
            if (scoreProgressText != null)
            {
                scoreProgressText.gameObject.SetActive(false);
            }
        }
    }

    public void DisconnectWallet()
    {
        currentPlayerWallet = "";
        PlayerPrefs.DeleteKey("walletAddress");
        PlayerPrefs.Save();
        HideLevelUI();
        
        // NOTE: Warm-up system is NOT reset here - it's for the entire web session
        // hasWarmedUp stays true until page reload/refresh
        Debug.Log("[WARM-UP] 🔄 Warm-up system preserved during wallet disconnection (web session scope)");
        
        // Reset evolution tracking to prevent orphaned pending costs
        pendingEvolutionCost = 0;
        lastConsumedPoints = 0;
        isProcessingEvolution = false;
        Debug.Log("[EVOLUTION] 🔄 Evolution state reset after wallet disconnection");
        
        // Clean up NFT buttons when wallet disconnects
        var nftPanel = FindObjectOfType<NFTDisplayPanel>();
        if (nftPanel != null)
        {
            nftPanel.CleanupAllSimpleNFTButtons();
            Debug.Log("[NFTManager] 🧹 NFT buttons cleaned up after wallet disconnection");
        }
        
        Debug.Log("[NFTManager] Wallet disconnected - UI hidden");
    }
    
    public void ForceRefreshAfterMatch(int matchScore = 0)
    {
        Debug.Log($"[NFTManager] ForceRefreshAfterMatch called with matchScore={matchScore}");
        RefreshWalletAddress();
        
        bool signApproved = PlayerPrefs.GetInt("personalSignApproved", 0) == 1;
        bool walletInPrefs = !string.IsNullOrEmpty(PlayerPrefs.GetString("walletAddress", ""));
        bool walletConnected = !string.IsNullOrEmpty(currentPlayerWallet);
        
        if ((walletConnected && signApproved) || walletInPrefs)
        {
            if (matchScore > 0)
            {
                Debug.Log($"[NFTManager] 🎯 Match completed with score {matchScore} - refreshing from Firebase (no local update to avoid double counting)");
                // DON'T update local score - let Firebase be the single source of truth
                StartCoroutine(DelayedFirebaseRefresh());
            }
            else
            {
                Debug.Log("[NFTManager] No match score, loading NFT state with delay");
                StartCoroutine(DelayedFirebaseRefresh());
            }
        }
        else
        {
            Debug.Log("[NFTManager] No valid wallet connection, updating UI to level 0");
            UpdateLevelUI(0);
        }
    }
    
    void UpdateLocalScoreAndUI(int matchScore)
    {
        int oldScore = currentNFTState.score;
        int newScore = oldScore + matchScore;
        
        currentNFTState.score = newScore;
        
        Debug.Log($"[NFTManager] Local score updated: {oldScore} -> {newScore}");
        UpdateLevelUI(currentNFTState.level);
    }
    
    System.Collections.IEnumerator DelayedFirebaseRefresh()
    {
        yield return new WaitForSeconds(2f);
        Debug.Log("[NFTManager] Loading NFT state from Firebase after delay");
        LoadNFTStateFromFirebase();
    }
    
    System.Collections.IEnumerator DelayedReconnection()
    {
        Debug.Log($"[NFT-DEBUG] DelayedReconnection started, waiting for AppKit initialization...");
        
        yield return new WaitForSeconds(3f);
        
        if (Reown.AppKit.Unity.AppKit.IsInitialized)
        {
            Debug.Log($"[NFT-DEBUG] AppKit initialized, proceeding with reconnection for wallet: {currentPlayerWallet}");
            LoadNFTStateFromBlockchain();
        }
        else
        {
            Debug.LogWarning($"[NFT-DEBUG] AppKit not initialized after delay, skipping automatic reconnection");
        }
    }
    
    // REMOVED: DelayedFirebaseConfirmation() - was causing double counting
    // Now using only DelayedFirebaseRefresh() as single source of truth
    
    System.Collections.IEnumerator DelayedBlockchainRefresh()
    {
        Debug.Log("[NFT] Waiting 3 seconds for evolution transaction confirmation...");
        yield return new WaitForSeconds(3f);
        
        Debug.Log("[NFT] 🎯 SKIPPING immediate blockchain refresh to avoid level desync");
        Debug.Log("[NFT] Level synchronization will be handled by OnPointsConsumedAfterSuccess");
        
        // 🎯 MODIFICATION : Ne pas faire LoadNFTStateFromBlockchain() immédiatement
        // car ça écrase le bon niveau avec l'ancien niveau blockchain
        // La synchronisation se fait maintenant dans OnPointsConsumedAfterSuccess
        
        isProcessingEvolution = false; // Reset evolution flag
        
        Debug.Log("[NFT] 🎉 Evolution flow completed - level sync in progress!");
    }
    


    public void RefreshWalletAddress()
    {
        string walletAddress = string.Empty;
        
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
                    PlayerPrefs.SetString("walletAddress", appKitAddress);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[NFT] Erreur AppKit: {ex.Message}");
        }
        
        if (string.IsNullOrEmpty(walletAddress))
        {
            string walletFromPrefs = PlayerPrefs.GetString("walletAddress", "");
            if (!string.IsNullOrEmpty(walletFromPrefs))
            {
                walletAddress = walletFromPrefs;
            }
        }
        
        if (string.IsNullOrEmpty(walletAddress))
        {
            try
            {
                if (PlayerSession.IsConnected && !string.IsNullOrEmpty(PlayerSession.WalletAddress))
                {
                    walletAddress = PlayerSession.WalletAddress;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[NFT] Erreur PlayerSession: {ex.Message}");
            }
        }
        
        currentPlayerWallet = walletAddress;
    
    if (!string.IsNullOrEmpty(currentPlayerWallet))
    {
        Debug.Log($"[NFT-SYNC] Wallet updated to: {currentPlayerWallet}");
    }
    else
    {
        Debug.LogError("[NFT] No Wallet Connected");
    }
}

    private bool IsWalletConnectedAndSigned()
    {
        bool hasWallet = !string.IsNullOrEmpty(currentPlayerWallet);
        bool hasSignature = PlayerPrefs.GetInt("personalSignApproved", 0) == 1;
        return hasWallet && hasSignature;
    }
    
    public void UpdateStatusUI(string message = "")
    {
        bool hasWallet = !string.IsNullOrEmpty(currentPlayerWallet);
        bool isFullyAuthenticated = IsWalletConnectedAndSigned();
        
        if (!hasWallet)
        {
            // During warm-up, show empty space instead of "Connect Your Wallet First"
            if (isWarmingUp)
            {
                statusText.text = " ";
                Debug.Log("[WARM-UP] 🤫 Hiding wallet message during warm-up");
            }
            else
            {
                statusText.text = " ";
            }
            return;
        }
        
        if (!isFullyAuthenticated)
        {
            statusText.text = "Complete personal signature to continue";
            return;
        }
        
        if (!string.IsNullOrEmpty(message))
        {
            statusText.text = message;
        }
    }

    private bool IsFirebaseAllowed()
    {
        bool walletConnected = !string.IsNullOrEmpty(currentPlayerWallet);
        bool signApproved = PlayerPrefs.GetInt("personalSignApproved", 0) == 1;
        return walletConnected && signApproved;
    }

    public void LoadNFTStateFromBlockchain()
    {
        if (string.IsNullOrEmpty(currentPlayerWallet))
        {
            Debug.LogError("[NFT] No wallet address to load NFT state");
            return;
        }
        
        Debug.Log($"[NFT-DEBUG] 🔗 LoadNFTStateFromBlockchain called. Wallet: {currentPlayerWallet}");
        UpdateStatusUI("Reading NFT from blockchain...");
        
        // CRITICAL: Mark that we're actively doing blockchain verification
        isBlockchainVerificationActive = true;
        blockchainStateLoaded = false; // Reset to allow blockchain data
        Debug.Log("[NFT-DEBUG] 🔗 Blockchain verification ACTIVE - ready to receive blockchain data");
        
        StartCoroutine(VerifyNFTDirectlyFromBlockchain());
    }
    
    System.Collections.IEnumerator VerifyNFTDirectlyFromBlockchain()
    {
        Debug.Log($"[BLOCKCHAIN] 🔗 Starting DIRECT blockchain verification for wallet: {currentPlayerWallet}");
        
        // Démarrer la tâche async et attendre qu'elle se termine
        var task = GetNFTsDirectlyFromBlockchainV2();
        
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        if (task.Exception != null)
        {
            Debug.LogError($"[BLOCKCHAIN] ❌ Blockchain verification failed: {task.Exception.Message}");
        }
    }
    
    async System.Threading.Tasks.Task GetNFTsDirectlyFromBlockchainV2()
    {
        Debug.Log($"[BLOCKCHAIN-V2] 🔍 Using NFTDisplayPanel logic that WORKS...");
        
        string normalizedWallet = currentPlayerWallet.ToLowerInvariant();
        Debug.Log($"[BLOCKCHAIN-V2] 🔧 Normalized wallet: {currentPlayerWallet} → {normalizedWallet}");
        
        try
        {
            // ✅ MÊME ABI QUE NFTDisplayPanel qui fonctionne
            string balanceAbi = "function balanceOf(address) view returns (uint256)";
            
            var balance = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
                "0x68c582651d709f6e2b6113c01d69443f8d27e30d",
                balanceAbi,
                "balanceOf",
                new object[] { normalizedWallet }
            );
            
            Debug.Log($"[BLOCKCHAIN-V2] ✅ Balance: {balance} NFTs found");
            
            if (balance == 0)
            {
                Debug.Log($"[BLOCKCHAIN-V2] 📝 No NFTs found, sending empty state");
                var emptyState = new NFTStateData
                {
                    hasNFT = false,
                    level = 0,
                    tokenId = 0,
                    walletAddress = normalizedWallet,
                    score = 0,
                    nftCount = 0
                };
                OnNFTStateLoaded(JsonUtility.ToJson(emptyState));
                return;
            }
            
            // ✅ MÊME LOGIQUE QUE NFTDisplayPanel
            string tokenByIndexAbi = "function tokenOfOwnerByIndex(address owner, uint256 index) view returns (uint256)";
            string getLevelAbi = "function getLevel(uint256 tokenId) view returns (uint256)";
            
            int maxLevel = 0;
            int maxTokenId = 0;
            
            for (int i = 0; i < balance; i++)
            {
                try
                {
                    Debug.Log($"[BLOCKCHAIN-V2] Getting token at index {i}/{balance-1}");
                    
                    var tokenId = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
                        "0x68c582651d709f6e2b6113c01d69443f8d27e30d",
                        tokenByIndexAbi,
                        "tokenOfOwnerByIndex",
                        new object[] { normalizedWallet, i }
                    );
                    
                    Debug.Log($"[BLOCKCHAIN-V2] ✅ TokenId at index {i}: {tokenId}");
                    
                    if (tokenId > 0)
                    {
                        Debug.Log($"[BLOCKCHAIN-V2] Reading level for token #{tokenId}");
                        
                        int level = 1; // Default level
                        
                        try
                        {
                            level = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
                                "0x68c582651d709f6e2b6113c01d69443f8d27e30d",
                                getLevelAbi,
                                "getLevel",
                                new object[] { tokenId }
                            );
                            
                            Debug.Log($"[BLOCKCHAIN-V2] ✅ Token #{tokenId} has level {level}");
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[BLOCKCHAIN-V2] ⚠️ Failed to read level for token #{tokenId}, using default: {ex.Message}");
                        }
                        
                        // Garder le NFT avec le niveau le plus élevé
                        if (level > maxLevel)
                        {
                            maxLevel = level;
                            maxTokenId = tokenId;
                            Debug.Log($"[BLOCKCHAIN-V2] 🏆 New max level: Token #{maxTokenId} level {maxLevel}");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[BLOCKCHAIN-V2] ❌ Failed to read token at index {i}: {ex.Message}");
                }
            }
            
            Debug.Log($"[BLOCKCHAIN-V2] 🎯 FINAL RESULT: {balance} NFTs, Max level {maxLevel} (Token #{maxTokenId})");
            
            var nftState = new NFTStateData
            {
                hasNFT = true,
                level = maxLevel,
                tokenId = maxTokenId,
                walletAddress = normalizedWallet,
                score = 0,
                nftCount = balance
            };
            
            Debug.Log($"[BLOCKCHAIN-V2] 📤 Sending REAL state: {balance} NFTs, Token #{maxTokenId}, Level {maxLevel}");
            OnNFTStateLoaded(JsonUtility.ToJson(nftState));
            
            Debug.Log($"[BLOCKCHAIN-V2] 🔄 Now reading Firebase score for normalized wallet: {normalizedWallet}");
            LoadNFTStateFromFirebase();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[BLOCKCHAIN-V2] ❌ Critical error: {ex.Message}");
            var errorState = new NFTStateData
            {
                hasNFT = false,
                level = 0,
                tokenId = 0,
                walletAddress = normalizedWallet,
                score = 0,
                nftCount = 0
            };
            OnNFTStateLoaded(JsonUtility.ToJson(errorState));
        }
    }
    
    void SyncFirebaseWithBlockchainData(NFTStateData blockchainState)
    {
        Debug.Log($"[FIREBASE-SYNC] 🔄 Synchronizing Firebase with blockchain data");
        Debug.Log($"[FIREBASE-SYNC] 🔗 Blockchain NFT: hasNFT={blockchainState.hasNFT}, level={blockchainState.level}, tokenId={blockchainState.tokenId}");
        
        if (blockchainState.hasNFT)
        {
            // Le NFT existe sur la blockchain, récupérer les points depuis Firebase et synchroniser le niveau
            Debug.Log($"[FIREBASE-SYNC] 📊 NFT exists on blockchain, fetching score from Firebase and syncing level");
            
#if UNITY_WEBGL && !UNITY_EDITOR
            // Appeler Firebase pour récupérer les points ET mettre à jour le niveau
            SyncNFTLevelWithFirebaseJS(currentPlayerWallet, blockchainState.level, blockchainState.tokenId);
#else
            // Mode éditeur : simuler un score et finaliser
            blockchainState.score = 150;
            Debug.Log($"[FIREBASE-SYNC] 🎮 Editor mode: using mock score {blockchainState.score}");
            OnNFTStateLoaded(JsonUtility.ToJson(blockchainState));
#endif
        }
        else
        {
            // Aucun NFT sur la blockchain, retourner l'état vide
            Debug.Log($"[FIREBASE-SYNC] 📝 No NFT on blockchain, returning empty state");
            OnNFTStateLoaded(JsonUtility.ToJson(blockchainState));
        }
    }
    
    void OnFirebaseSyncCompleted(string firebaseDataJson)
    {
        try
        {
            Debug.Log($"[FIREBASE-SYNC] ✅ Firebase sync completed: {firebaseDataJson}");
            
            var firebaseData = JsonUtility.FromJson<NFTStateData>(firebaseDataJson);
            Debug.Log($"[FIREBASE-SYNC] 📊 Final state: hasNFT={firebaseData.hasNFT}, level={firebaseData.level}, score={firebaseData.score}");
            
            // IMPORTANT : Les données blockchain (niveau, tokenId) sont prioritaires
            // Firebase ne fournit que le score
            OnNFTStateLoaded(firebaseDataJson);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FIREBASE-SYNC] ❌ Error processing Firebase sync result: {ex.Message}");
            
            // En cas d'erreur, utiliser les données blockchain avec un score par défaut
            var fallbackState = new NFTStateData
            {
                hasNFT = false,
                level = 0,
                tokenId = 0,
                walletAddress = currentPlayerWallet,
                score = 0
            };
            OnNFTStateLoaded(JsonUtility.ToJson(fallbackState));
        }
    }
    void LoadScoreFromFirebase(NFTStateData blockchainState)
    {
        Debug.Log($"[BLOCKCHAIN] 📊 Loading score from Firebase for verified NFT (blockchain state preserved)");
        Debug.Log($"[BLOCKCHAIN] 📊 Blockchain NFT: hasNFT={blockchainState.hasNFT}, level={blockchainState.level}, tokenId={blockchainState.tokenId}");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        StartCoroutine(WaitForFirebaseScore(blockchainState));
#else
        blockchainState.score = 150;
        Debug.Log($"[BLOCKCHAIN] 📊 Mock score added: {blockchainState.score}");
        OnNFTStateLoaded(JsonUtility.ToJson(blockchainState));
#endif
    }
    
    System.Collections.IEnumerator WaitForFirebaseScore(NFTStateData blockchainState)
    {
        Debug.Log($"[BLOCKCHAIN] 📊 Waiting for Firebase score - preserving blockchain NFT data");
        Debug.Log($"[BLOCKCHAIN] 📊 Blockchain state to preserve: hasNFT={blockchainState.hasNFT}, level={blockchainState.level}, tokenId={blockchainState.tokenId}");
        
        // Get Firebase score but preserve blockchain NFT data
        GetNFTStateJS(currentPlayerWallet);
        
        float timeout = 3f; // Reduced timeout
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        // CRITICAL: Always use blockchain state for NFT data, only get score from Firebase
        Debug.Log($"[BLOCKCHAIN] 📊 Timeout reached - proceeding with blockchain state and default score");
        if (blockchainState.score <= 0)
        {
            blockchainState.score = 100; // Default score if Firebase doesn't respond
            Debug.Log($"[BLOCKCHAIN] 📊 Using default score: {blockchainState.score}");
        }
        
        Debug.Log($"[BLOCKCHAIN] 📊 Final state: hasNFT={blockchainState.hasNFT}, level={blockchainState.level}, score={blockchainState.score}");
        OnNFTStateLoaded(JsonUtility.ToJson(blockchainState));
    }
    
    public void OnBlockchainNFTVerified(string blockchainDataJson)
    {
        try
        {
            var blockchainState = JsonUtility.FromJson<NFTStateData>(blockchainDataJson);
            Debug.Log($"[BLOCKCHAIN] ✅ Verification result: {blockchainDataJson}");
            Debug.Log($"[BLOCKCHAIN] ✅ Parsed: hasNFT={blockchainState.hasNFT}, level={blockchainState.level}, tokenId={blockchainState.tokenId}");
            
            // ALWAYS process blockchain data immediately - it's the source of truth
            if (blockchainState.hasNFT)
            {
                Debug.Log($"[BLOCKCHAIN] ✅ NFT found on-chain - loading score from Firebase as secondary data");
                LoadScoreFromFirebase(blockchainState);
            }
            else
            {
                Debug.Log($"[BLOCKCHAIN] ✅ No NFT found on-chain - updating UI directly");
                OnNFTStateLoaded(blockchainDataJson);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[BLOCKCHAIN] Error parsing verification result: {ex.Message}");
            var fallbackState = new NFTStateData
            {
                hasNFT = false,
                level = 0,
                tokenId = 0,
                walletAddress = currentPlayerWallet,
                score = 0
            };
            OnNFTStateLoaded(JsonUtility.ToJson(fallbackState));
        }
    }

    public void LoadNFTStateFromFirebase()
    {
        if (!IsFirebaseAllowed())
        {
            Debug.LogWarning("[NFT] Accès Firebase refusé : signature manquante");
            UpdateStatusUI("Connect and sign to access");
            return;
        }
        Debug.Log($"[NFT-DEBUG] LoadNFTStateFromFirebase called. Wallet: {currentPlayerWallet}, FirebaseAllowed: true");
        if (string.IsNullOrEmpty(currentPlayerWallet))
        {
            Debug.LogError("[NFT] No wallet address to load NFT state");
            return;
        }
        UpdateStatusUI("Loading NFT state...");
#if UNITY_WEBGL && !UNITY_EDITOR
        string normalizedWallet = currentPlayerWallet.ToLowerInvariant();
        Debug.Log($"[FIREBASE-SCORE] 🔍 Loading score from Firebase for normalized wallet: {normalizedWallet}");
        Debug.Log($"[FIREBASE-SCORE] 🔧 Original: {currentPlayerWallet} → Normalized: {normalizedWallet}");
        
        GetNFTStateJS(normalizedWallet);
#else
        var mockNFTState = new NFTStateData
        {
            hasNFT = false,
            level = 0,
            walletAddress = currentPlayerWallet,
            score = 150
        };
        Debug.Log($"[NFT-DEBUG] Mock NFT state: {JsonUtility.ToJson(mockNFTState)}");
        OnNFTStateLoaded(JsonUtility.ToJson(mockNFTState));
#endif
    }

    private bool blockchainStateLoaded = false;
    private bool isBlockchainVerificationActive = false;
    
    public void OnNFTStateLoaded(string json)
    {
        try
        {
            Debug.Log($"[NFT-DEBUG] OnNFTStateLoaded json={json}");
            Debug.Log($"[NFT-DEBUG] Current flags: blockchainStateLoaded={blockchainStateLoaded}, isBlockchainVerificationActive={isBlockchainVerificationActive}");
            
            var nftState = JsonUtility.FromJson<NFTStateData>(json);
            
            Debug.Log($"[NFT-DEBUG] Parsed - hasNFT={nftState.hasNFT}, level={nftState.level}, score={nftState.score}, wallet={nftState.walletAddress}");
            
            // CRITICAL: Detect if this is Firebase data trying to override blockchain state
            bool isFirebaseData = json.Contains("walletAddress") && !isBlockchainVerificationActive;
            
            if (blockchainStateLoaded && isFirebaseData)
            {
                Debug.Log("[NFT-DEBUG] 🔄 Firebase data received - preserving blockchain NFT data, updating score only");
                Debug.Log($"[NFT-DEBUG] 📊 Firebase score: {nftState.score}, Blockchain NFT: level={currentNFTState.level}, hasNFT={currentNFTState.hasNFT}");
                
                // PRESERVE blockchain NFT data, UPDATE score only
                currentNFTState.score = nftState.score;
                
                Debug.Log($"[NFT-DEBUG] ✅ Score updated to {nftState.score}, blockchain NFT data preserved");
                
                // Update UI with preserved blockchain data + new score
                if (currentNFTState.hasNFT && currentNFTState.level > 0)
                {
                    int nftCount = currentNFTState.nftCount > 0 ? currentNFTState.nftCount : 1;
                string statusMessage = $"{nftCount} NFT{(nftCount > 1 ? "S" : "")} FOUND - Max Level {currentNFTState.level}";
                    UpdateStatusUI(statusMessage);
                    UpdateLevelUI(currentNFTState.level);
                }
                else
                {
                    UpdateStatusUI("Ready to mint your first NFT!");
                    UpdateLevelUI(0);
                }
                
                ShowLevelUI();
                return;
            }
            
            // If this is blockchain verification data, mark it as such
            if (isBlockchainVerificationActive)
            {
                Debug.Log("[NFT-DEBUG] ✅ Processing BLOCKCHAIN verification data");
                blockchainStateLoaded = true;
                isBlockchainVerificationActive = false;
            }
            
            // Update state only if not blocked
            currentNFTState = nftState;
            
            Debug.Log($"[NFT-DEBUG] ✅ State updated: hasNFT={currentNFTState.hasNFT}, level={currentNFTState.level}");
            
            Debug.Log($"[UI-UPDATE] 🎯 About to update UI with: hasNFT={nftState.hasNFT}, level={nftState.level}");
        
            if (nftState.hasNFT && nftState.level > 0)
            {
                int nftCount = nftState.nftCount > 0 ? nftState.nftCount : 1;
                string statusMessage = $"{nftCount} NFT{(nftCount > 1 ? "S" : "")} FOUND - Max Level {nftState.level}";
                Debug.Log($"[UI-UPDATE] ✅ Setting status: {statusMessage}");
                Debug.Log($"[UI-UPDATE] ✅ Setting level: {nftState.level}");
                
                UpdateStatusUI(statusMessage);
                UpdateLevelUI(nftState.level);
                
                // CRÉER LES BOUTONS NFT SIMPLES (COEXISTENT AVEC LE PANEL)
                // DÉSACTIVÉ : Boutons NFT maintenant créés uniquement dans NFTDisplayPanel après refresh
                // CreateSimpleNFTButtons(nftCount);
                
                // SYNCHRONISER FIREBASE AVEC LA RÉALITÉ BLOCKCHAIN
                Debug.Log($"[FIREBASE-SYNC] 🔄 Starting Firebase sync for wallet: {nftState.walletAddress}");
                Debug.Log($"[FIREBASE-SYNC] 🔄 Syncing NFT Token #{nftState.tokenId} Level {nftState.level} to Firebase...");
                SyncNFTLevelWithFirebaseJS(nftState.walletAddress, nftState.level, nftState.tokenId);
            }
            else
            {
                string statusMessage = "Ready to mint your first NFT!";
                Debug.Log($"[UI-UPDATE] ✅ Setting status: {statusMessage}");
                Debug.Log($"[UI-UPDATE] ✅ Setting level: 0");
                
                UpdateStatusUI(statusMessage);
                UpdateLevelUI(0);
            }
            
            // Force UI visibility after loading data
            Debug.Log($"[UI-UPDATE] 🔄 Calling ShowLevelUI() to force visibility...");
            ShowLevelUI();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFT] Error parsing NFT state: {ex.Message}");
            UpdateStatusUI("Error loading NFT state");
            currentNFTState = new NFTStateData
            {
                hasNFT = false,
                level = 0,
                walletAddress = currentPlayerWallet,
                score = 0
            };
        }
    }

    void UpdateLevelUI(int level)
    {
        Debug.Log($"[UI-LEVEL] ===== UpdateLevelUI called with BLOCKCHAIN level={level} =====");
        Debug.Log($"[UI-LEVEL] UI Components - levelText: {(levelText != null ? "ASSIGNED" : "NULL")}, scoreProgressText: {(scoreProgressText != null ? "ASSIGNED" : "NULL")}");
        
        string walletAddress = PlayerPrefs.GetString("walletAddress", "");
        bool hasWallet = !string.IsNullOrEmpty(walletAddress);
        Debug.Log($"[UI-LEVEL] Wallet state: hasWallet={hasWallet}, address={walletAddress}");
        
        // UTILISER LE LEVEL BLOCKCHAIN PASSÉ EN PARAMÈTRE (PAS currentNFTState !)
        Debug.Log($"[UI-LEVEL] 🎯 USING BLOCKCHAIN LEVEL: {level} (ignoring any other data sources)");
        
        if (levelText != null)
        {
            levelText.gameObject.SetActive(hasWallet);
            if (hasWallet)
            {
                if (level > 0)
                {
                    string levelMessage = $"NFT Level: {level} ";
                    levelText.text = levelMessage;
                    Debug.Log($"[UI-LEVEL] ✅ levelText set to: '{levelMessage}'");
                }
                else
                {
                    string levelMessage = "Ready to mint NFT";
                    levelText.text = levelMessage;
                    Debug.Log($"[UI-LEVEL] ✅ levelText set to: '{levelMessage}'");
                }
            }
        }
        else
        {
            Debug.LogError($"[UI-LEVEL] ❌ levelText is NULL! Cannot update level display!");
        }
        
        if (scoreProgressText != null)
        {
            scoreProgressText.gameObject.SetActive(hasWallet);
            if (hasWallet)
            {
                // UTILISER LE SCORE ACTUEL DE currentNFTState (points du joueur)
                int currentScore = currentNFTState.score;
                Debug.Log($"[UI-LEVEL] Current player score from Firebase: {currentScore}");
                
                if (level >= 10)
                {
                    string scoreMessage = "MAX LEVEL";
                    scoreProgressText.text = scoreMessage;
                    Debug.Log($"[UI-LEVEL] ✅ scoreProgressText set to: '{scoreMessage}'");
                }
                else if (level == 0)
                {
                    string scoreMessage = $"XP: {currentScore}/0 (Ready to mint!)";
                    scoreProgressText.text = scoreMessage;
                    Debug.Log($"[UI-LEVEL] ✅ scoreProgressText set to: '{scoreMessage}'");
                }
                else
                {
                    int nextLevelCost = GetEvolutionCost(level + 1); // Cost to evolve to next level
                    string scoreMessage = $"XP: {currentScore}/{nextLevelCost}";
                    scoreProgressText.text = scoreMessage;
                    Debug.Log($"[UI-LEVEL] ✅ scoreProgressText set to: '{scoreMessage}'");
                }
            }
        }
        else
        {
            Debug.LogError($"[UI-LEVEL] ❌ scoreProgressText is NULL! Cannot update score display!");
        }
        
        Debug.Log($"[UI-LEVEL] ✅ UpdateLevelUI completed: hasWallet={hasWallet}, blockchainLevel={level}");
    }

    public void OnEvolutionButtonClicked()
    {
        Debug.Log("[NFT-DEBUG] OnEvolutionButtonClicked() called!");
        
        if (string.IsNullOrEmpty(currentPlayerWallet))
        {
            Debug.LogWarning("[NFT] No wallet detected - checking PlayerPrefs...");
            
            string savedWallet = PlayerPrefs.GetString("walletAddress", "");
            if (!string.IsNullOrEmpty(savedWallet))
            {
                Debug.Log($"[NFT-DEBUG] Found wallet in PlayerPrefs: {savedWallet}, updating currentPlayerWallet");
                currentPlayerWallet = savedWallet;
            }
            else
            {
                UpdateStatusUI("Connect your wallet first");
                return;
            }
        }
        
        // Verify AppKit is connected
        if (!Reown.AppKit.Unity.AppKit.IsAccountConnected)
        {
            Debug.LogError($"[NFT-DEBUG] ❌ AppKit not connected! Cannot open NFT panel.");
            UpdateStatusUI("Wallet connection lost - please reconnect");
            return;
        }
        
        bool signApproved = PlayerPrefs.GetInt("personalSignApproved", 0) == 1;
        bool isReconnection = !string.IsNullOrEmpty(PlayerPrefs.GetString("walletAddress", "")) && !signApproved;
        if (!signApproved && !isReconnection)
        {
            UpdateStatusUI("Please sign in");
            return;
        }
        
        // Open NFT display panel to show all NFTs owned by the wallet
        Debug.Log($"[NFT-DEBUG] Opening NFT display panel for wallet: {currentPlayerWallet}");
        
        // Find NFT panel (simple approach)
        var nftPanel = FindObjectOfType<NFTDisplayPanel>();
        if (nftPanel != null)
        {
            // Simple protection: if panel is already active, don't open again
            if (nftPanel.gameObject.activeInHierarchy)
            {
                Debug.Log($"[NFT-DEBUG] Panel already open, skipping duplicate opening");
                return;
            }
            
            Debug.Log($"[NFT-DEBUG] NFT display panel found, calling ShowPanel");
            
            if (string.IsNullOrEmpty(currentPlayerWallet))
            {
                Debug.LogError($"[NFT-DEBUG] ❌ currentPlayerWallet is empty! Cannot open panel.");
                UpdateStatusUI("Wallet not connected - please connect wallet first");
                return;
            }
            
            nftPanel.ShowPanel(currentPlayerWallet);
        }
        else
        {
            Debug.LogError("[NFT-DEBUG] ❌ NFT display panel not found in scene!");
            UpdateStatusUI("NFT panel not found - check Unity scene setup");
        }
    }

    public void RequestEvolution()
    {
        isProcessingEvolution = true;
        UpdateStatusUI("Requesting evolution authorization...");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        CheckEvolutionEligibilityJS(currentPlayerWallet);
#else
        var mockData = new EvolutionData
        {
            authorized = true,
            walletAddress = currentPlayerWallet,
            score = 250,
            currentLevel = currentNFTState.level,
            requiredScore = 100,
            nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            signature = "0xmocksignature123"
        };
        OnEvolutionCheckComplete(JsonUtility.ToJson(mockData));
#endif
    }

    public void OnEvolutionCheckComplete(string evolutionDataJson)
    {
        try
        {
            var evolutionData = JsonUtility.FromJson<EvolutionData>(evolutionDataJson);
            if (evolutionData.authorized)
            {
                // Use the target level from the authorization data (already validated by server)
                int targetLevel = evolutionData.targetLevel;
                int authorizedCurrentLevel = evolutionData.currentLevel;
                Debug.Log($"[EVOLUTION] ✅ Server authorized evolution to level {targetLevel}");
                Debug.Log($"[EVOLUTION] Server current level: {authorizedCurrentLevel}, Target level: {targetLevel}");
                
                // Use server-validated current level instead of currentNFTState.level
                if (targetLevel > authorizedCurrentLevel)
                {
                    UpdateStatusUI($"Evolution authorized to Level {targetLevel}! Score: {evolutionData.score}");
                    
                    // Convert EvolutionData to EvolutionAuthorizationData and use SendEvolveTransactionV2
                    var authData = new EvolutionAuthorizationData
                    {
                        authorized = true,
                        walletAddress = evolutionData.walletAddress,
                        tokenId = selectedTokenId,
                        currentPoints = evolutionData.score,
                        evolutionCost = evolutionData.evolutionCost,
                        targetLevel = targetLevel,
                        nonce = evolutionData.nonce,
                        signature = evolutionData.signature
                    };
                    
                    Debug.Log($"[EVOLUTION] 🚀 Using V2 transaction with tokenId: {selectedTokenId}");
                    SendEvolveTransactionV2(authData);
                }
                else
                {
                    UpdateStatusUI($"Target level {targetLevel} not higher than server current level {authorizedCurrentLevel}");
                    isProcessingEvolution = false;
                }
            }
            else
            {
                string errorMsg = !string.IsNullOrEmpty(evolutionData.error) ? 
                    evolutionData.error : 
                    $"Insufficient Score: {evolutionData.score}";
                UpdateStatusUI($"Git Gud. {errorMsg}"); 
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

    private int CalculateTargetLevel(int score, int currentLevel = 1)
    {
        
        if (score >= 2 && currentLevel == 1)
        {
            return 2;
        }
        
        int maxLevel = 2;
        int threshold = 100; 
        
        while (score >= threshold)
        {
            maxLevel++;
            threshold += 100;
        }
        
        return Mathf.Max(currentLevel, maxLevel);
    }
    
    private int GetNextLevelThreshold(int currentLevel)
    {
        if (currentLevel == 1)
        {
            return 2;
        }
        
        return (currentLevel - 1) * 100;
    }
    
    private System.Collections.IEnumerator RefreshBlockchainStateAfterMint()
    {
        Debug.Log("[NFT] Waiting 3 seconds for transaction confirmation...");
        yield return new WaitForSeconds(3f);
        
        Debug.Log("[NFT] Refreshing blockchain state after mint");
        
        // Force fresh blockchain state check
        if (!string.IsNullOrEmpty(currentPlayerWallet))
        {
            LoadNFTStateFromBlockchain();
            
            // Also trigger NFT panel refresh if it exists
            var nftPanel = FindObjectOfType<NFTDisplayPanel>();
            if (nftPanel != null)
            {
                Debug.Log("[NFT] Triggering NFT panel refresh after mint");
                nftPanel.RefreshNFTList();
            }
        }
    }

    private async void SendMintTransaction()
    {
        try
        {
            UpdateStatusUI("Sending mint transaction...");
            
            if (Reown.AppKit.Unity.AppKit.IsInitialized && 
                Reown.AppKit.Unity.AppKit.IsAccountConnected)
            {
                string functionSelector = MINT_NFT_SELECTOR;
                string data = functionSelector;
                
                try 
                {
                    BigInteger mintPrice = BigInteger.Parse("1000000000000000"); // 0.001 ETH
                    
                    var result = await Reown.AppKit.Unity.AppKit.Evm.SendTransactionAsync(
                        CONTRACT_ADDRESS,  // to address
                        mintPrice,         // value (0.001 ETH sent)
                        data               // transaction data
                    );
                    
                    if (!string.IsNullOrEmpty(result))
                    {
                        // 🎯 NE PAS marquer le mint comme réussi immédiatement !
                        // Attendre la vraie confirmation blockchain
                        Debug.Log($"[MINT] Transaction sent with hash: {result}. Starting REAL blockchain monitoring...");
                        
                        // Démarrer le monitoring de la vraie transaction
#if UNITY_WEBGL && !UNITY_EDITOR
                        StartRealMintMonitoring(result);
#else
                        // En mode éditeur, simuler le succès après un délai
                        StartCoroutine(SimulateRealMintSuccess(result));
#endif
                        
                        UpdateStatusUI($"Mint transaction sent! Waiting for blockchain confirmation...");
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
                string functionSelector = EVOLVE_NFT_SELECTOR;
                string paddedLevel = targetLevel.ToString("X").PadLeft(64, '0');
                string data = functionSelector + paddedLevel;
                
                try 
                {
                    var result = await Reown.AppKit.Unity.AppKit.Evm.SendTransactionAsync(
                        CONTRACT_ADDRESS,  
                        BigInteger.Zero,   
                        data               
                    );
                    
                    if (!string.IsNullOrEmpty(result))
                    {
                        // 🎯 NE PAS consommer les points immédiatement !
                        // Au lieu de ça, démarrer le monitoring de la vraie transaction blockchain
                        Debug.Log($"[EVOLUTION] Transaction sent with hash: {result}. Starting REAL blockchain monitoring...");
                        
                        // Démarrer le monitoring de la vraie transaction
#if UNITY_WEBGL && !UNITY_EDITOR
                        StartRealTransactionMonitoring(result, targetLevel);
#else
                        // En mode éditeur, simuler le succès après un délai
                        StartCoroutine(SimulateRealTransactionSuccess(result, targetLevel));
#endif
                        
                        UpdateStatusUI($"Transaction sent! Waiting for blockchain confirmation...");
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
            string displayHash = string.IsNullOrEmpty(transactionHash) ? 
                "unknown" : 
                (transactionHash.Length > 10 ? transactionHash.Substring(0, 10) + "..." : transactionHash);
            
            UpdateNFTLevelInFirebase(1);
        
        // MARQUER LE MINT COMME RÉUSSI DANS FIREBASE
#if UNITY_WEBGL && !UNITY_EDITOR
        MarkMintSuccessJS(currentPlayerWallet);
        Debug.Log($"[MINT-SUCCESS] 🎆 Marked mint as successful in Firebase for wallet: {currentPlayerWallet}");
#else
        Debug.Log($"[MINT-SUCCESS] 🎮 Editor mode: skipping Firebase mint success marking");
#endif
        
        currentNFTState.hasNFT = true;
        currentNFTState.level = 1;
        
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
            string displayHash = string.IsNullOrEmpty(transactionHash) ? 
                "unknown" : 
                (transactionHash.Length > 10 ? transactionHash.Substring(0, 10) + "..." : transactionHash);
            
            Debug.Log($"[EVOLUTION] ✅ Evolution transaction successful! Now consuming points safely.");
            
            // CONSUME POINTS ONLY AFTER BLOCKCHAIN SUCCESS
            if (pendingEvolutionCost > 0)
            {
                Debug.Log($"[EVOLUTION] 💰 Consuming {pendingEvolutionCost} points after confirmed blockchain success for token #{selectedTokenId}");
                
#if UNITY_WEBGL && !UNITY_EDITOR
                ConsumePointsAfterSuccessJS(currentPlayerWallet, pendingEvolutionCost, selectedTokenId, newLevel);
#endif
                
                // Update local score to reflect consumption
                currentNFTState.score = Mathf.Max(0, currentNFTState.score - pendingEvolutionCost);
                
                pendingEvolutionCost = 0; // Reset pending cost
            }
            
            UpdateNFTLevelInFirebase(newLevel);
            
            currentNFTState.level = newLevel;
            
            // Reset tracking variables since transaction succeeded
            lastConsumedPoints = 0;
            
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

    public async void OnMintSuccess(string transactionHash)
    {
        try
        {
            UpdateStatusUI("NFT créé avec succès! Récupération du tokenId...");
            
            await Task.Delay(3000); 
            
            int actualTokenId = await GetPlayerNFTTokenId(currentPlayerWallet);
            
            if (actualTokenId > 0)
            {
                currentNFTState.tokenId = actualTokenId;
                currentNFTState.hasNFT = true;
                currentNFTState.level = 1; 
                
                string updateData = JsonUtility.ToJson(currentNFTState);
                UpdateNFTDataInFirebase(updateData);
                
                UpdateLevelUI(1);
                UpdateStatusUI($"NFT #{actualTokenId} créé avec succès!");
                
                ReadNFTLevelFromBlockchain();
            }
            else
            {
                Debug.LogWarning("[NFT] Failed to retrieve tokenId after mint");
                UpdateStatusUI("NFT créé, mais impossible de récupérer le tokenId");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NFT] Error handling mint success: {ex.Message}");
            UpdateStatusUI("Erreur lors de la récupération des informations du NFT");
        }
    }

    private void UpdateNFTLevelInFirebase(int newLevel)
    {
        if (!IsFirebaseAllowed())
        {
            Debug.LogWarning("[NFT] Écriture Firebase refusée : signature manquante");
            UpdateStatusUI("Connectez votre wallet et signez pour mettre à jour votre NFT.");
            return;
        }
        if (string.IsNullOrEmpty(currentPlayerWallet))
        {
            Debug.LogError("[NFT] Cannot update NFT level: currentPlayerWallet is empty!");
            return;
        }
        
#if UNITY_WEBGL && !UNITY_EDITOR
        UpdateNFTLevelJS(currentPlayerWallet, newLevel);
#else
        OnNFTLevelUpdated($"{newLevel}");
#endif
    }

    private void UpdateNFTDataInFirebase(string data)
    {
        if (!IsFirebaseAllowed())
        {
            Debug.LogWarning("[NFT] Écriture Firebase refusée : signature manquante");
            UpdateStatusUI("Connectez votre wallet et signez pour mettre à jour votre NFT.");
            return;
        }
        if (string.IsNullOrEmpty(currentPlayerWallet))
        {
            return;
        }
        
        
#if UNITY_WEBGL && !UNITY_EDITOR
#else
#endif
    }

    public void OnNFTLevelUpdated(string levelStr)
    {
        try
        {
            if (string.IsNullOrEmpty(levelStr) || !int.TryParse(levelStr, out int level))
            {
                Debug.LogError($"[NFT] Invalid level value received: {levelStr}");
                level = 0; // Default to 0 if invalid
            }
            
            currentNFTState.level = level;
            currentNFTState.hasNFT = level > 0;
            
            Debug.Log($"[NFT] OnNFTLevelUpdated called: level={level}, updating UI...");
            UpdateLevelUI(level);
            
            // 🎯 Mettre à jour le statut pour confirmer la synchronisation
            if (level > 0)
            {
                UpdateStatusUI($"Level synchronized! NFT Level: {level}");
                Debug.Log($"[NFT] ✅ Level synchronization completed: {level}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NFT] Error in OnNFTLevelUpdated: {ex.Message}");
        }
    }
    
    public void OnNFTStateReceived(string levelStr) => OnNFTLevelUpdated(levelStr);
    
    public void OnEvolutionEligibilityChecked(string evolutionDataJson) => OnEvolutionCheckComplete(evolutionDataJson);
    
    public void OnCanMintChecked(string jsonResponse)
    {
        try
        {
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
                SendMintTransaction();
            }
            else
            {
                string errorMsg = !string.IsNullOrEmpty(response.error) ? 
                    response.error : 
                    "This wallet already has an NFT";
                    
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

    public async Task<int> GetPlayerNFTTokenId(string walletAddress)
    {
        try
        {
            if (string.IsNullOrEmpty(walletAddress))
            {
                Debug.LogWarning("[NFT] Cannot get tokenId: Wallet address is empty");
                return 0;
            }
            
            if (Reown.AppKit.Unity.AppKit.IsInitialized && 
                Reown.AppKit.Unity.AppKit.IsAccountConnected)
            {
                try
                {
                    string abi = "function playerNFT(address) view returns (uint256)";
                    
                    var tokenId = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
                        CONTRACT_ADDRESS,
                        abi,
                        "playerNFT",
                        new object[] { walletAddress }
                    );
                    
                    return tokenId;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NFT] Error calling playerNFT: {ex.Message}");
                }
            }
            
            return 0; 
        }
        catch (Exception ex)
        {
            return 0;
        }
    }

    public async void ReadNFTLevelFromBlockchain()
    {
        try
        {
            UpdateStatusUI("Vérification du NFT sur la blockchain...");
            
            if (string.IsNullOrEmpty(currentPlayerWallet))
            {
                UpdateStatusUI("Wallet non connecté");
                return;
            }
            
            if (Reown.AppKit.Unity.AppKit.IsInitialized && 
                Reown.AppKit.Unity.AppKit.IsAccountConnected)
            {
                int tokenId = await GetPlayerNFTTokenId(currentPlayerWallet);
                
                if (tokenId <= 0)
                {
                    UpdateStatusUI("Aucun NFT détecté");
                    UpdateLevelUI(0);
                    return;
                }
                
                currentNFTState.tokenId = tokenId;
                
                
                try 
                {
                    string abi = "function getLevel(uint256) view returns (uint256)";
                    
                    var level = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
                        CONTRACT_ADDRESS,
                        abi,
                        "getLevel",
                        new object[] { tokenId }
                    );
                    
                    
                    currentNFTState.level = level;
                    currentNFTState.hasNFT = level > 0;
                    
                    UpdateLevelUI(level);
                    
                    if (level > 0) {
                        UpdateStatusUI($"NFT #{tokenId}, niveau {level} confirmé");
                    } else {
                        UpdateStatusUI("Aucun NFT trouvé on-chain");
                    }
                    
                    UpdateNFTLevelInFirebase(level);
                }
                catch (Exception ex)
                {
                    UpdateStatusUI("Erreur lors de la lecture du niveau");
                }
            }
            else
            {
                UpdateStatusUI("Wallet non connecté");
            }
        }
        catch (Exception ex)
        {
            UpdateStatusUI("Erreur lors de la lecture du niveau");
        }
    }

    public void OnNFTLevelUpdateError(string error)
    {
        Debug.LogError($"[NFT] Error updating NFT level in Firebase: {error}");
    }

    private void OnTransactionError(string error)
    {
        Debug.LogError($"[EVOLUTION] ❌ Transaction failed: {error}");
        
        // No points were consumed yet, so no restoration needed
        if (pendingEvolutionCost > 0)
        {
            Debug.Log($"[EVOLUTION] ✅ Transaction failed but no points were consumed. {pendingEvolutionCost} points remain safe.");
            UpdateStatusUI($"Transaction failed - your {pendingEvolutionCost} points are safe: {error}");
            pendingEvolutionCost = 0; // Reset pending cost
        }
        else
        {
            UpdateStatusUI($"Transaction error: {error}");
        }
        
        isProcessingEvolution = false;
    }





    public void ForceLevelTextDisplay()
    {
        Debug.Log("[NFT-DEBUG] ForceLevelTextDisplay called after personal sign");
        UpdateLevelUI(currentNFTState.level);
    }

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
            LoadNFTStateFromBlockchain();
        }
        else
        {
            Debug.LogWarning("[NFT] No wallet connected");
        }
    }

    public void RequestMintNFT()
    {
        Debug.Log($"[MINT] ===== DÉBUT DEMANDE MINT =====");
        Debug.Log($"[MINT] Wallet: {currentPlayerWallet}");
        
        if (string.IsNullOrEmpty(currentPlayerWallet))
        {
            Debug.LogWarning($"[MINT] No wallet connected");
            UpdateStatusUI("Connect wallet first");
            return;
        }
        
        if (isProcessingEvolution)
        {
            Debug.LogWarning($"[MINT] Already processing a transaction");
            UpdateStatusUI("Transaction in progress...");
            return;
        }
        
        isProcessingEvolution = true;
        UpdateStatusUI("Requesting mint authorization...");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        // Call simplified JavaScript function for direct mint
        DirectMintNFTJS(currentPlayerWallet);
#else
        // Mock authorization for editor testing
        var mockAuth = new MintAuthorizationData
        {
            authorized = true,
            walletAddress = currentPlayerWallet,
            mintPrice = 1000000000000000, // 0.001 ETH in wei
            nonce = System.DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            signature = "0x1234567890abcdef"
        };
        OnMintAuthorized(JsonUtility.ToJson(mockAuth));
#endif
    }
    
    [System.Serializable]
    public class UnityNFTState
    {
        public bool hasNFT;
        public int level;
        public int tokenId;
    }
    
    private void ShareNFTStateWithJS()
    {
        var nftState = new UnityNFTState
        {
            hasNFT = currentNFTState.tokenId > 0,
            level = currentNFTState.level,
            tokenId = currentNFTState.tokenId
        };
        
        string nftStateJson = JsonUtility.ToJson(nftState);
        Debug.Log($"[EVOLUTION] Sharing NFT state with JS: {nftStateJson}");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        SetUnityNFTStateJS(nftStateJson);
#endif
    }
    
    // ✅ NOUVELLE MÉTHODE : Partager les données d'un NFT spécifique
    private void ShareSpecificNFTStateWithJS(NFTStateData specificNFTData)
    {
        var nftState = new UnityNFTState
        {
            hasNFT = specificNFTData.hasNFT,
            level = specificNFTData.level,
            tokenId = specificNFTData.tokenId
        };
        
        string nftStateJson = JsonUtility.ToJson(nftState);
        Debug.Log($"[EVOLUTION] Sharing SPECIFIC NFT state with JS: {nftStateJson}");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        SetUnityNFTStateJS(nftStateJson);
#endif
    }
    
    public void RequestEvolutionForSelectedNFT()
    {
        Debug.Log($"[EVOLUTION] ===== DÉBUT DEMANDE ÉVOLUTION =====");
        Debug.Log($"[EVOLUTION] Selected token ID: {selectedTokenId}");
        Debug.Log($"[EVOLUTION] Current NFT state - TokenId: {currentNFTState.tokenId}, Level: {currentNFTState.level}");
        Debug.Log($"[EVOLUTION] Processing evolution: {isProcessingEvolution}");
        
        if (selectedTokenId <= 0)
        {
            Debug.LogWarning($"[EVOLUTION] No valid NFT selected (selectedTokenId: {selectedTokenId})");
            UpdateStatusUI("No NFT selected");
            return;
        }
        
        if (isProcessingEvolution)
        {
            Debug.LogWarning($"[EVOLUTION] Evolution already in progress, ignoring request");
            return;
        }
        
        Debug.Log($"[EVOLUTION] Setting processing flag to true");
        isProcessingEvolution = true;
        
        // ✅ CORRECTION : Récupérer le niveau du NFT sélectionné spécifiquement
        Debug.Log($"[EVOLUTION] Getting level for selected NFT #{selectedTokenId}...");
        StartCoroutine(GetSelectedNFTLevelAndEvolve());
    }
    
    System.Collections.IEnumerator GetSelectedNFTLevelAndEvolve()
    {
        // Récupérer le niveau du NFT sélectionné depuis la blockchain
        var levelTask = Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
            "0x68c582651d709f6e2b6113c01d69443f8d27e30d",
            "function getLevel(uint256 tokenId) view returns (uint256)",
            "getLevel",
            new object[] { selectedTokenId }
        );
        
        while (!levelTask.IsCompleted)
        {
            yield return null;
        }
        
        if (levelTask.Exception != null)
        {
            Debug.LogError($"[EVOLUTION] ❌ Failed to get level for NFT #{selectedTokenId}: {levelTask.Exception.Message}");
            UpdateStatusUI("Error reading NFT level");
            isProcessingEvolution = false;
            yield break;
        }
        
        int currentLevel = levelTask.Result;
        int targetLevel = currentLevel + 1;
        
        Debug.Log($"[EVOLUTION] ✅ NFT #{selectedTokenId} current level: {currentLevel}, Target level: {targetLevel}");
        
        if (targetLevel > 10)
        {
            Debug.LogWarning($"[EVOLUTION] NFT already at max level ({currentLevel})");
            UpdateStatusUI("NFT already at max level");
            isProcessingEvolution = false;
            yield break;
        }
        
        Debug.Log($"[EVOLUTION] Requesting evolution authorization for NFT #{selectedTokenId} from level {currentLevel} to {targetLevel}");
        UpdateStatusUI($"Requesting evolution authorization for NFT #{selectedTokenId}...");
        
#if UNITY_WEBGL && !UNITY_EDITOR
    Debug.Log($"[EVOLUTION] ✅ Sharing SELECTED NFT data with JavaScript");
    
    // ✅ CORRECTION : Partager les données du NFT sélectionné au lieu de currentNFTState
    var selectedNFTData = new NFTStateData
    {
        hasNFT = true,
        level = currentLevel,
        tokenId = selectedTokenId,
        walletAddress = currentPlayerWallet,
        score = currentNFTState.score // Garder le score Firebase
    };
    
    Debug.Log($"[EVOLUTION] 📤 Sending CORRECT data: TokenId={selectedTokenId}, Level={currentLevel}");
    ShareSpecificNFTStateWithJS(selectedNFTData);
    
    // 🔄 NOUVEAU FLUX : Vérifier et consommer les points AVANT l'évolution blockchain
    int evolutionCost = GetEvolutionCost(currentLevel + 1);
    Debug.Log($"[EVOLUTION] Evolution cost for level {currentLevel} -> {currentLevel + 1}: {evolutionCost} points");
    
    Debug.Log($"[EVOLUTION] 🔍 Checking evolution eligibility WITHOUT consuming points yet");
    Debug.Log($"[EVOLUTION] Wallet: {currentPlayerWallet}, TokenId: {selectedTokenId}, Cost: {evolutionCost}, Target: {currentLevel + 1}");
    
    // Track evolution cost for consumption AFTER successful transaction
    pendingEvolutionCost = evolutionCost;
    Debug.Log($"[EVOLUTION] 📝 Pending evolution cost: {pendingEvolutionCost} points (will consume only after blockchain success)");
    
    // ✅ CHECK eligibility only, don't consume points yet
    CheckEvolutionEligibilityOnlyJS(currentPlayerWallet, evolutionCost, selectedTokenId, currentLevel + 1);
#else
        var mockAuth = new EvolutionAuthorizationData
        {
            authorized = true,
            walletAddress = currentPlayerWallet,
            tokenId = currentNFTState.tokenId,
            currentPoints = 100,
            evolutionCost = GetEvolutionCost(targetLevel),
            targetLevel = targetLevel,
            nonce = System.DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            signature = "0x1234567890abcdef"
        };
        OnEvolutionAuthorized(JsonUtility.ToJson(mockAuth));
#endif
    }
    
    private int GetNFTLevel(int tokenId)
    {
        return currentNFTState.level;
    }
    
    private int GetEvolutionCost(int targetLevel)
    {
        // Coûts d'évolution conformes au contrat ChogTanksNFTv2_Final.sol
        var costs = new Dictionary<int, int>
        {
            {2, 2},     // Level 1→2 = 2 points
            {3, 200},   // Level 2→3 = 200 points  
            {4, 300},   // Level 3→4 = 300 points
            {5, 400},   // Level 4→5 = 400 points
            {6, 500},   // Level 5→6 = 500 points
            {7, 600},   // Level 6→7 = 600 points
            {8, 700},   // Level 7→8 = 700 points
            {9, 800},   // Level 8→9 = 800 points
            {10, 900}   // Level 9→10 = 900 points
        };
        
        return costs.ContainsKey(targetLevel) ? costs[targetLevel] : 0;
    }
    
    public void OnEvolutionAuthorized(string authDataJson)
    {
        try
        {
            var authData = JsonUtility.FromJson<EvolutionAuthorizationData>(authDataJson);
            
            if (authData.authorized)
            {
                UpdateStatusUI($"Evolution authorized! Cost: {authData.evolutionCost} points");
                SendEvolveTransactionV2(authData);
            }
            else
            {
                string errorMsg = !string.IsNullOrEmpty(authData.error) ? 
                    authData.error : 
                    "Evolution not authorized";
                UpdateStatusUI($"Cannot evolve: {errorMsg}");
                isProcessingEvolution = false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFT] Error parsing evolution authorization: {ex.Message}");
            UpdateStatusUI("Error checking evolution eligibility");
            isProcessingEvolution = false;
        }
    }
    
    public void OnMintAuthorized(string authDataJson)
    {
        try
        {
            var authData = JsonUtility.FromJson<MintAuthorizationData>(authDataJson);
            
            if (authData.authorized)
            {
                UpdateStatusUI("Mint authorized! Sending transaction...");
                SendMintTransactionWithSignature(authData);
            }
            else
            {
                string errorMsg = !string.IsNullOrEmpty(authData.error) ? 
                    authData.error : 
                    "Mint not authorized";
                UpdateStatusUI($"Cannot mint: {errorMsg}");
                isProcessingEvolution = false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFT] Error parsing mint authorization: {ex.Message}");
            UpdateStatusUI("Error checking mint authorization");
            isProcessingEvolution = false;
        }
    }
    
    private async void SendMintTransactionWithSignature(MintAuthorizationData authData)
    {
        try
        {
            UpdateStatusUI("Sending mint transaction...");
            
            if (!Reown.AppKit.Unity.AppKit.IsInitialized || !Reown.AppKit.Unity.AppKit.IsAccountConnected)
            {
                UpdateStatusUI("Wallet not connected");
                isProcessingEvolution = false;
                return;
            }
            
            // Use SendTransactionAsync with manual parameter encoding to bypass ABI issues
            string functionSelector = MINT_NFT_SELECTOR; // "0xd46c2811"
            
            // Manual encoding of parameters for mintNFT(uint256 playerPoints, uint256 nonce, bytes signature)
            string playerPointsHex = "0000000000000000000000000000000000000000000000000000000000000000"; // 0 padded to 32 bytes
            string nonceHex = authData.nonce.ToString("X").PadLeft(64, '0'); // nonce padded to 32 bytes
            
            // For bytes parameter, we need offset + length + data
            string signatureWithoutPrefix = authData.signature.StartsWith("0x") ? authData.signature.Substring(2) : authData.signature;
            string signatureOffsetHex = "0000000000000000000000000000000000000000000000000000000000000060"; // offset to signature data (3*32 = 96 = 0x60)
            string signatureLengthHex = (signatureWithoutPrefix.Length / 2).ToString("X").PadLeft(64, '0'); // length of signature in bytes
            string signatureDataHex = signatureWithoutPrefix.PadRight((int)Math.Ceiling(signatureWithoutPrefix.Length / 64.0) * 64, '0'); // signature data padded
            
            string encodedData = functionSelector + playerPointsHex + nonceHex + signatureOffsetHex + signatureLengthHex + signatureDataHex;
            
            Debug.Log($"[NFT] Sending mint transaction with manual encoding");
            Debug.Log($"[NFT] Signature: {authData.signature.Substring(0, 10)}..., Nonce: {authData.nonce}");
            Debug.Log($"[NFT] Encoded data: {encodedData.Substring(0, 50)}...");
            
            var result = await Reown.AppKit.Unity.AppKit.Evm.SendTransactionAsync(
                CONTRACT_ADDRESS,
                System.Numerics.BigInteger.Parse(authData.mintPrice.ToString()),
                encodedData
            );
            
            Debug.Log($"[NFT] Mint transaction sent: {result}");
            UpdateStatusUI("Mint transaction confirmed!");
            
            currentNFTState.hasNFT = true;
            currentNFTState.level = 1;
            currentNFTState.tokenId = 1;
            
            // Notify UI that NFT state has changed
            OnNFTStateChanged?.Invoke(true, 1);
            Debug.Log("[NFT] NFT state changed event fired: hasNFT=true, count=1");
            
            // Force blockchain state refresh after successful mint
            Debug.Log("[NFT] Forcing blockchain state refresh after mint success");
            StartCoroutine(RefreshBlockchainStateAfterMint());
            
            isProcessingEvolution = false;
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFT] Mint transaction failed: {ex.Message}");
            UpdateStatusUI($"Mint failed: {ex.Message}");
            isProcessingEvolution = false;
        }
    }
    
    private async void SendEvolveTransactionV2(EvolutionAuthorizationData authData)
    {
        try
        {
            UpdateStatusUI("Sending evolution transaction...");
            
            if (!Reown.AppKit.Unity.AppKit.IsInitialized || !Reown.AppKit.Unity.AppKit.IsAccountConnected)
            {
                UpdateStatusUI("Wallet not connected");
                isProcessingEvolution = false;
                return;
            }
            
            // ABI complet pour AppKit (pas juste la signature)
            string abi = @"[
                {
                    ""name"": ""evolveNFT"",
                    ""type"": ""function"",
                    ""inputs"": [
                        {""name"": ""tokenId"", ""type"": ""uint256""},
                        {""name"": ""playerPoints"", ""type"": ""uint256""},
                        {""name"": ""nonce"", ""type"": ""uint256""},
                        {""name"": ""signature"", ""type"": ""bytes""}
                    ],
                    ""outputs"": []
                }
            ]";
            
            // Convertir la signature string hex en byte[] pour AppKit
            byte[] signatureBytes;
            try
            {
                string hexSignature = authData.signature.StartsWith("0x") ? authData.signature.Substring(2) : authData.signature;
                
                // Méthode compatible Unity pour convertir hex string en byte[]
                signatureBytes = new byte[hexSignature.Length / 2];
                for (int i = 0; i < signatureBytes.Length; i++)
                {
                    signatureBytes[i] = System.Convert.ToByte(hexSignature.Substring(i * 2, 2), 16);
                }
                
                Debug.Log($"[NFT] Converted signature '{authData.signature}' to {signatureBytes.Length} bytes");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NFT] Failed to convert signature '{authData.signature}' to bytes: {ex.Message}");
                // Fallback: créer une signature mock de 65 bytes (standard Ethereum)
                signatureBytes = new byte[65];
                for (int i = 0; i < 65; i++) signatureBytes[i] = (byte)(i % 256);
                Debug.Log($"[NFT] Using fallback signature of {signatureBytes.Length} bytes");
            }
            
            var result = await Reown.AppKit.Unity.AppKit.Evm.WriteContractAsync(
                CONTRACT_ADDRESS,
                abi,
                "evolveNFT",
                new object[] { 
                    authData.tokenId,
                    authData.currentPoints,
                    authData.nonce,
                    signatureBytes  // Utiliser byte[] au lieu de string
                }
            );
            
            Debug.Log($"[NFT] Evolution transaction sent: {result}");
            UpdateStatusUI("Evolution transaction confirmed!");
            
            // ✅ CONSUME POINTS AFTER SUCCESSFUL BLOCKCHAIN TRANSACTION
            if (!string.IsNullOrEmpty(result))
            {
                OnEvolveTransactionSuccess(result, authData.targetLevel);
            }
            
            // ✅ Points consommés - Maintenant rafraîchir les données
            Debug.Log("[NFT] Refreshing blockchain data after successful evolution...");
            
            // ✅ SOLUTION SIMPLE: Refresh complet blockchain (comme page reload)
            uint newLevel = (uint)(authData.targetLevel);
            UpdateStatusUI($"Evolution completed! Refreshing NFT state...");
            
            // Attendre un peu que la transaction soit confirmée
            StartCoroutine(DelayedBlockchainRefresh());
            
            Debug.Log($"[NFT] 🎉 Evolution flow completed - blockchain refresh initiated!");
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFT] Evolution transaction failed: {ex.Message}");
            UpdateStatusUI($"Evolution failed: {ex.Message}");
            isProcessingEvolution = false;
        }
    }
    
    // ✅ MÉTHODE SUPPRIMÉE : ConsumePointsAfterTransaction
    // Les points sont maintenant consommés APRÈS l'évolution via ConsumePointsAfterSuccessJS
    
    // 💰 NOUVELLE MÉTHODE : Callback pour la consommation de points APRÈS succès blockchain
    public void OnPointsConsumedAfterSuccess(string responseJson)
    {
        try
        {
            var response = JsonUtility.FromJson<PointsConsumptionResponse>(responseJson);
            
            if (response.success)
            {
                Debug.Log($"[POINTS-CONSUME] ✅ Points consumed successfully: {response.consumedPoints}");
                Debug.Log($"[POINTS-CONSUME] ✅ New score: {response.newScore}");
                
                // Update local state with new score from server
                currentNFTState.score = response.newScore;
                
                // 🎯 SOLUTION SIMPLE : Juste rafraîchir le panel après évolution
                Debug.Log($"[POINTS-CONSUME] 🔄 Refreshing NFT panel after successful evolution...");
                
                var nftPanel = FindObjectOfType<NFTDisplayPanel>(true);
                if (nftPanel != null)
                {
                    Debug.Log($"[POINTS-CONSUME] ✅ Panel found, triggering delayed refresh");
                    nftPanel.RefreshAfterEvolution();
                }
                else
                {
                    Debug.LogWarning($"[POINTS-CONSUME] ⚠️ NFT panel not found");
                }
                
                Debug.Log($"[POINTS-CONSUME] ✅ Local state updated with new score: {response.newScore}");
            }
            else
            {
                Debug.LogError($"[POINTS-CONSUME] ❌ Failed to consume points: {response.error}");
                
                // On failure, we might need to restore the pending cost or handle gracefully
                // For now, just log the error - the blockchain transaction already succeeded
                UpdateStatusUI($"Evolution completed but points may not be properly consumed");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[POINTS-CONSUME] ❌ Error parsing consumption response: {ex.Message}");
        }
    }
    
    //  NOUVELLE MÉTHODE : Callback pour la vérification/consommation des points AVANT évolution
    public void OnPointsPreConsumed(string responseJson)
    {
        try
        {
            var response = JsonUtility.FromJson<PreEvolutionResponse>(responseJson);
            
            if (response.success && response.authorized)
            {
                Debug.Log($"[PRE-EVOLUTION] ✅ Points verified and consumed! New score: {response.newScore}");
                Debug.Log($"[PRE-EVOLUTION] Proceeding with blockchain evolution for NFT #{response.tokenId} to level {response.targetLevel}");
                
                // Mettre à jour le score local
                currentNFTState.score = response.newScore;
                
                UpdateStatusUI($"Points consumed ({response.pointsConsumed}). Proceeding with blockchain evolution...");
                
                // Maintenant procéder à l'évolution blockchain directement
                // Les points ont déjà été vérifiés et consommés, on peut directement autoriser l'évolution
                // Utiliser le score AVANT consommation pour l'autorisation serveur
                int originalScore = response.newScore + response.pointsConsumed; // Recalculer le score original
                RequestEvolutionAuthorizationDirectly(response.tokenId, response.targetLevel, originalScore);
            }
            else
            {
                Debug.LogError($"[PRE-EVOLUTION] ❌ Evolution blocked: {response.error}");
                UpdateStatusUI($"Evolution failed: {response.error}");
                isProcessingEvolution = false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PRE-EVOLUTION] Error parsing pre-evolution response: {ex.Message}");
            UpdateStatusUI("Evolution failed: Invalid response");
            isProcessingEvolution = false;
        }
    }
    
    // Méthode pour demander l'autorisation d'évolution directement après consommation des points
    private void RequestEvolutionAuthorizationDirectly(int tokenId, int targetLevel, int originalScore)
    {
        Debug.Log($"[EVOLUTION-DIRECT] Requesting evolution authorization for NFT #{tokenId} to level {targetLevel}");
        Debug.Log($"[EVOLUTION-DIRECT] Using original score: {originalScore} (before consumption)");
        
        // Appeler directement le serveur de signature avec les données du NFT sélectionné
        var evolutionData = new
        {
            walletAddress = currentPlayerWallet,
            tokenId = tokenId,
            playerPoints = originalScore, // Score AVANT consommation pour l'autorisation serveur
            targetLevel = targetLevel
        };
        
        Debug.Log($"[EVOLUTION-DIRECT] Calling signature server with data: {JsonUtility.ToJson(evolutionData)}");
        
        // Utiliser le score original pour l'autorisation serveur
        RequestEvolutionSignatureJS(currentPlayerWallet, tokenId, originalScore, targetLevel);
    }
    
    public void OnPointsConsumed(string responseJson)
    {
        try
        {
            var response = JsonUtility.FromJson<PointConsumptionResponse>(responseJson);
            
            if (response.success)
            {
                Debug.Log($"[NFT] ✅ Points consumption successful! New score: {response.newScore}");
                
                // Mettre à jour le score local
                currentNFTState.score = response.newScore;
                
                UpdateStatusUI($"Evolution completed! New score: {response.newScore} points");
                
                // 🔄 RAFRAÎCHIR TOUTES LES DONNÉES ET L'UI
                Debug.Log("[NFT] Refreshing all data after successful evolution...");
                
                // 1. Relire les données blockchain (niveaux NFT)
                RefreshNFTData();
                
                // 2. Synchroniser le nouveau niveau avec Firebase
                if (response.updatedNFT != null)
                {
                    Debug.Log($"[NFT] Syncing new level {response.updatedNFT.newLevel} for NFT #{response.updatedNFT.tokenId} with Firebase");
                    SyncNFTLevelWithFirebaseJS(currentPlayerWallet, (int)response.updatedNFT.newLevel, (int)response.updatedNFT.tokenId);
                }
                
                // 3. Relire les données Firebase (score, niveaux)
                GetNFTStateJS(currentPlayerWallet);
                
                // 4. Mettre à jour l'UI principale (status, level)
                UpdateLevelUI(currentNFTState.level);
                
                Debug.Log($"[NFT] 🎉 Evolution flow completed successfully!");
            }
            else
            {
                Debug.LogError($"[NFT] Points consumption failed: {response.error}");
                UpdateStatusUI($"Points consumption failed: {response.error}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFT] Error parsing points consumption response: {ex.Message}");
            UpdateStatusUI("Points consumption failed");
        }
        finally
        {
            isProcessingEvolution = false;
        }
    }
    
    private void RefreshNFTData()
    {
        if (!string.IsNullOrEmpty(currentPlayerWallet))
        {
            LoadNFTStateFromBlockchain();
            ReadNFTLevelFromBlockchain();
        }
    }
    
    public async void ReadNFTLevelFromBlockchain(int tokenId)
    {
        try
        {
            if (!Reown.AppKit.Unity.AppKit.IsInitialized)
            {
                Debug.LogWarning("[NFT] AppKit not initialized");
                return;
            }
            
            string abi = "function getLevel(uint256) view returns (uint256)";
            
            var result = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<string>(
                CONTRACT_ADDRESS,
                abi,
                "getLevel",
                new object[] { tokenId }
            );
            
            if (result != null && int.TryParse(result.ToString(), out int level))
            {
                Debug.Log($"[NFT] Token #{tokenId} level from blockchain: {level}");
                UpdateNFTLevelDisplay(tokenId, level);
            }
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFT] Error reading NFT level from blockchain: {ex.Message}");
        }
    }
    
    private void UpdateNFTLevelDisplay(int tokenId, int level)
    {
        var nftPanel = FindObjectOfType<NFTDisplayPanel>();
        if (nftPanel != null)
        {
            nftPanel.UpdateNFTLevel(tokenId, level);
        }
        
        if (currentNFTState.tokenId == tokenId)
        {
            currentNFTState.level = level;
            UpdateStatusUI($"NFT #{tokenId} is Level {level}");
        }
    }
    
    // ========== BOUTONS NFT SIMPLES (COEXISTENCE AVEC PANEL) ==========
    
    private void CreateSimpleNFTButtons(int nftCount)
    {
        Debug.Log($"[NFT-BUTTONS] 🎯 Creating {nftCount} simple NFT buttons (coexist with panel)");
        
        ClearNFTButtons();
        
        if (nftButtonContainer == null)
        {
            Debug.LogWarning("[NFT-BUTTONS] ⚠️ nftButtonContainer is null - assign it in Inspector for simple NFT buttons");
            return;
        }
        
        for (int i = 0; i < nftCount; i++)
        {
            CreateSingleNFTButton(i + 1);
        }
        
        Debug.Log($"[NFT-BUTTONS] ✅ Created {nftButtons.Count} simple NFT buttons successfully");
    }

    private void CreateSingleNFTButton(int nftIndex)
    {
        GameObject buttonObj = null;
        
        // Utiliser le prefab si disponible, sinon créer un bouton basique
        if (nftButtonPrefab != null)
        {
            Debug.Log($"[NFT-BUTTONS] 🎨 Using prefab for NFT #{nftIndex}");
            buttonObj = Instantiate(nftButtonPrefab, nftButtonContainer);
            buttonObj.name = $"SimpleNFT_Button_{nftIndex}";
        }
        else
        {
            Debug.Log($"[NFT-BUTTONS] 🔧 Creating basic button for NFT #{nftIndex}");
            buttonObj = CreateBasicNFTButton(nftIndex);
        }
        
        // Configurer le bouton
        var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
        if (button == null)
        {
            button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        }
        
        // Personnaliser le texte
        CustomizeButtonText(buttonObj, nftIndex);
        
        // Positionner le bouton (non-conflictuel avec le panel)
        PositionButton(buttonObj, nftIndex);
        
        // Ajouter l'action de clic
        int tokenIndex = nftIndex;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSimpleNFTButtonClicked(tokenIndex));
        
        nftButtons.Add(button);
        
        Debug.Log($"[NFT-BUTTONS] ✅ Simple NFT button #{nftIndex} created and configured");
    }
    
    private GameObject CreateBasicNFTButton(int nftIndex)
    {
        GameObject buttonObj = new GameObject($"SimpleNFT_Button_{nftIndex}");
        buttonObj.transform.SetParent(nftButtonContainer, false);
        
        // Ajouter les composants de base
        var button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        var image = buttonObj.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.1f, 0.7f, 0.3f, 0.9f); // Vert pour distinguer des autres boutons
        
        // Créer le texte
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = $"NFT #{nftIndex}";
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        // Configurer le RectTransform du texte
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = UnityEngine.Vector2.zero;
        textRect.anchorMax = UnityEngine.Vector2.one;
        textRect.offsetMin = UnityEngine.Vector2.zero;
        textRect.offsetMax = UnityEngine.Vector2.zero;
        
        return buttonObj;
    }
    
    private void CustomizeButtonText(GameObject buttonObj, int nftIndex)
    {
        // Chercher TextMeshProUGUI dans le bouton
        var textComponents = buttonObj.GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents.Length > 0)
        {
            textComponents[0].text = $"NFT #{nftIndex}";
            Debug.Log($"[NFT-BUTTONS] 📝 Updated text to 'NFT #{nftIndex}'");
        }
        else
        {
            // Fallback pour Text legacy
            var legacyText = buttonObj.GetComponentsInChildren<UnityEngine.UI.Text>();
            if (legacyText.Length > 0)
            {
                legacyText[0].text = $"NFT #{nftIndex}";
                Debug.Log($"[NFT-BUTTONS] 📝 Updated legacy text to 'NFT #{nftIndex}'");
            }
        }
    }
    
    private void PositionButton(GameObject buttonObj, int nftIndex)
    {
        var rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Position compacte pour ne pas interférer avec le NFTDisplayPanel
            rectTransform.sizeDelta = new UnityEngine.Vector2(120, 40); // Plus petit
            rectTransform.anchoredPosition = new UnityEngine.Vector2((nftIndex - 1) * 130, 0); // Horizontal
            
            Debug.Log($"[NFT-BUTTONS] 📍 Positioned NFT #{nftIndex} at {rectTransform.anchoredPosition} (horizontal layout)");
        }
    }
    
    private void ClearNFTButtons()
    {
        Debug.Log($"[NFT-BUTTONS] 🧹 Clearing {nftButtons.Count} existing simple NFT buttons");
        
        foreach (var button in nftButtons)
        {
            if (button != null && button.gameObject != null)
            {
                DestroyImmediate(button.gameObject);
            }
        }
        
        nftButtons.Clear();
    }
    
    private void OnSimpleNFTButtonClicked(int nftIndex)
    {
        Debug.Log($"[NFT-BUTTONS] 🖱️ Simple NFT #{nftIndex} button clicked");
        
        // Action simple : mettre à jour le statut et sélectionner le NFT
        UpdateStatusUI($"Selected NFT #{nftIndex} - Level {currentNFTState.level}");
        selectedTokenId = nftIndex;
        
        // Optionnel : ouvrir directement le NFTDisplayPanel pour ce NFT spécifique
        Debug.Log($"[NFT-BUTTONS] 🎯 Opening detailed view for NFT #{nftIndex}");
        OnEvolutionButtonClicked(); // Ouvre le panel détaillé
    }

    // 🎯 NOUVELLES MÉTHODES : Recevoir les événements de VRAIES transactions blockchain
    
    // Appelée par JavaScript quand une vraie transaction mint réussit sur la blockchain
    public void OnRealMintSuccess(string transactionHash)
    {
        Debug.Log($"[REAL-TX] 🎆 REAL mint transaction succeeded on blockchain: {transactionHash}");
        
        // Maintenant on peut marquer le mint comme réussi et déclencher les actions
        OnMintTransactionSuccess(transactionHash);
    }
    
    // Appelée par JavaScript quand une vraie transaction evolve réussit sur la blockchain
    public void OnRealEvolveSuccess(string evolveDataJson)
    {
        try
        {
            var evolveData = JsonUtility.FromJson<RealEvolveSuccess>(evolveDataJson);
            Debug.Log($"[REAL-TX] 🚀 REAL evolve transaction succeeded on blockchain: {evolveData.hash} to level {evolveData.level}");
            
            // Maintenant on peut consommer les points car la transaction blockchain a vraiment réussi
            OnEvolveTransactionSuccess(evolveData.hash, evolveData.level);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[REAL-TX] Error parsing real evolve success data: {ex.Message}");
        }
    }

    [System.Serializable]
    public class RealEvolveSuccess
    {
        public string hash;
        public int level;
    }

    // 🎯 Démarrer le monitoring d'une vraie transaction
    private void StartRealTransactionMonitoring(string txHash, int targetLevel)
    {
        Debug.Log($"[REAL-TX] 👀 Starting real transaction monitoring for {txHash} (level {targetLevel})");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        // Appeler JavaScript pour démarrer le monitoring avec les données d'évolution
        Application.ExternalEval($@"
            if (window.monitorTransaction) {{
                window.monitorTransaction('{txHash}', 'evolve', {{ targetLevel: {targetLevel} }});
            }} else {{
                console.error('[REAL-TX] monitorTransaction function not available');
            }}
        ");
#endif
    }

    // � Démarrer le monitoring d'une vraie transaction mint
    private void StartRealMintMonitoring(string txHash)
    {
        Debug.Log($"[REAL-TX] 👀 Starting real mint transaction monitoring for {txHash}");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        // Appeler JavaScript pour démarrer le monitoring du mint
        Application.ExternalEval($@"
            if (window.monitorTransaction) {{
                window.monitorTransaction('{txHash}', 'mint');
            }} else {{
                console.error('[REAL-TX] monitorTransaction function not available');
            }}
        ");
#endif
    }

    // �🎮 Simulation pour l'éditeur Unity
    private System.Collections.IEnumerator SimulateRealTransactionSuccess(string txHash, int targetLevel)
    {
        Debug.Log($"[REAL-TX] 🎮 Simulating real transaction success in editor after 3 seconds...");
        yield return new WaitForSeconds(3f);
        
        Debug.Log($"[REAL-TX] 🎮 Simulated blockchain confirmation for {txHash}");
        OnEvolveTransactionSuccess(txHash, targetLevel);
    }

    // 🎮 Simulation mint pour l'éditeur Unity
    private System.Collections.IEnumerator SimulateRealMintSuccess(string txHash)
    {
        Debug.Log($"[REAL-TX] 🎮 Simulating real mint success in editor after 3 seconds...");
        yield return new WaitForSeconds(3f);
        
        Debug.Log($"[REAL-TX] 🎮 Simulated mint blockchain confirmation for {txHash}");
        OnMintTransactionSuccess(txHash);
    }

    // 🎯 Initialiser le système de détection des vraies transactions
    private void InitializeRealTransactionDetection()
    {
        Debug.Log("[REAL-TX] 🎯 Setting up real transaction detection system...");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            SetupRealTransactionDetection(); // Appel à la méthode JavaScript
            Debug.Log("[REAL-TX] ✅ Real transaction detection initialized");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[REAL-TX] ❌ Failed to setup real transaction detection: {ex.Message}");
        }
#else
        Debug.Log("[REAL-TX] 🎮 Editor mode - real transaction detection will be simulated");
#endif
    }
}