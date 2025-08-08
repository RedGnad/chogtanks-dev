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
    public int targetLevel;    
    public int requiredScore;
    public int evolutionCost; 
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
    public int newScore; 
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
    private const string CONTRACT_ADDRESS = "0x07045605a0d70b12f3688a438db706bc1eda7e8c";
    
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
    private bool isWarmingUp = false; 
    
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
    private int lastConsumedPoints = 0; 
    private int pendingEvolutionCost = 0; 

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
    
    // 🚨 DEPRECATED: Use SyncNFTLevelWithFirebaseJS instead to maintain consistency
    [DllImport("__Internal")]
    private static extern void UpdateNFTLevelJS(string walletAddress, int newLevel);
    
    [DllImport("__Internal")]
    private static extern void ReadNFTFromBlockchainJS(string walletAddress, string callbackMethod);
    
    // 🎯 PRIMARY: Use this function for all Firebase NFT level updates
    [DllImport("__Internal")]
    public static extern void SyncNFTLevelWithFirebaseJS(string walletAddress, int blockchainLevel, int tokenId);
    
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
    public static void CheckHasMintedNFTJS(string walletAddress) { }
    private static void UpdateNFTLevelJS(string walletAddress, int newLevel) { }
    private static void ReadNFTFromBlockchainJS(string walletAddress, string callbackMethod) { }
    public static void SyncNFTLevelWithFirebaseJS(string walletAddress, int blockchainLevel, int tokenId) { }
    private static void CheckEvolutionEligibilityOnlyJS(string walletAddress, int pointsRequired, int tokenId, int targetLevel) { }
    private static void ConsumePointsAfterSuccessJS(string walletAddress, int pointsToConsume, int tokenId, int newLevel) { }
    private static void RequestEvolutionSignatureJS(string walletAddress, int tokenId, int playerPoints, int targetLevel) { }
#endif

    void Start()
    {
        HideLevelUI();
        
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
        
        SetupWarmUpSystem();
        
        InitializeRealTransactionDetection();
        
    }
    
    void OnDestroy()
    {
        OnNFTStateChanged -= HandleNFTStateChanged;
    }
    
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
            isWarmingUp = true; 
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
            yield return null;
            
            Debug.Log("[WARM-UP] 🖱️ Invoking target button click silently");
            
            warmUpTargetButton.onClick.Invoke();
            
            Debug.Log("[WARM-UP] ✅ Silent button click simulation completed");
            
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
    
    private void HandleNFTStateChanged(bool hasNFT, int nftCount)
    {
        Debug.Log($"[NFT-UI] ===== HANDLING NFT STATE CHANGE =====");
        Debug.Log($"[NFT-UI] hasNFT={hasNFT}, count={nftCount}");
        Debug.Log($"[NFT-UI] Current UI components - statusText: {(statusText != null ? "OK" : "NULL")}, levelText: {(levelText != null ? "OK" : "NULL")}");
        
        if (hasNFT && nftCount > 0)
        {
            Debug.Log($"[NFT-UI] Updating UI to show {nftCount} NFT(s)");
            
            UpdateStatusUI($"{nftCount} NFT FOUND - MINTED SUCCESSFULLY!");
            UpdateLevelUI(1); 
            
            currentNFTState.hasNFT = true;
            currentNFTState.level = 1;
            currentNFTState.tokenId = 1;
            
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
            
            UpdateStatusUI("Checking blockchain state...");
            HideLevelUI();
            
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
        
        if (statusText != null && statusText.text.Contains("Level"))
        {
            statusText.text = " "; 
        }
    }
    
    public void ShowLevelUI()
    {
        string walletAddress = PlayerPrefs.GetString("walletAddress", "");
        bool walletInPrefs = !string.IsNullOrEmpty(walletAddress);
        bool signApproved = PlayerPrefs.GetInt("personalSignApproved", 0) == 1;
        
        bool appKitConnected = false;
        string appKitAddress = "";
        
        try
        {
            appKitConnected = Reown.AppKit.Unity.AppKit.IsAccountConnected;
            if (appKitConnected && Reown.AppKit.Unity.AppKit.Account != null)
            {
                appKitAddress = Reown.AppKit.Unity.AppKit.Account.Address;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UI-LEVEL] Error checking AppKit state: {ex.Message}");
        }
        
        if (appKitConnected && !string.IsNullOrEmpty(appKitAddress) && string.IsNullOrEmpty(walletAddress))
        {
            Debug.Log($"[UI-LEVEL] 🔄 Session reconnection detected - syncing PlayerPrefs with AppKit");
            PlayerPrefs.SetString("walletAddress", appKitAddress);
            PlayerPrefs.Save();
            walletAddress = appKitAddress;
            walletInPrefs = true;
            
            currentPlayerWallet = appKitAddress;
        }
        
        bool hasWallet = walletInPrefs || appKitConnected;
        
        Debug.Log($"[UI-LEVEL] ShowLevelUI check:");
        Debug.Log($"[UI-LEVEL] - PlayerPrefs wallet: '{walletAddress}' (hasValue: {walletInPrefs})");
        Debug.Log($"[UI-LEVEL] - AppKit connected: {appKitConnected} (address: '{appKitAddress}')");
        Debug.Log($"[UI-LEVEL] - Combined hasWallet: {hasWallet}");
        Debug.Log($"[UI-LEVEL] - Sign approved: {signApproved}");
        
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
            
            if (hasWallet && !signApproved)
            {
                Debug.Log($"[UI-LEVEL] 🔔 Wallet connected but no signature - could trigger auto-sign flow");
            }
            
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

    private System.Collections.IEnumerator DelayedAutoSignRequest()
    {
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"[UI-LEVEL] 🔄 Auto-requesting signature for reconnected wallet");
        
        var nftVerifyUI = FindObjectOfType<NFTVerifyUI>();
        if (nftVerifyUI != null)
        {
            Debug.Log($"[UI-LEVEL] ✅ Found NFTVerifyUI, triggering auto-verification");
        }
        else
        {
            Debug.LogWarning($"[UI-LEVEL] ⚠️ NFTVerifyUI not found for auto-signature");
        }
    }

    public void DisconnectWallet()
    {
        currentPlayerWallet = "";
        PlayerPrefs.DeleteKey("walletAddress");
        PlayerPrefs.Save();
        HideLevelUI();
        
        Debug.Log("[WARM-UP] 🔄 Warm-up system preserved during wallet disconnection (web session scope)");
        
        pendingEvolutionCost = 0;
        lastConsumedPoints = 0;
        isProcessingEvolution = false;
        Debug.Log("[EVOLUTION] 🔄 Evolution state reset after wallet disconnection");
        
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
            
            if (Reown.AppKit.Unity.AppKit.IsAccountConnected && Reown.AppKit.Unity.AppKit.Account != null)
            {
                string appKitAddress = Reown.AppKit.Unity.AppKit.Account.Address;
                Debug.Log($"[NFT-DEBUG] 🔄 AppKit reports wallet: {appKitAddress}");
                
                if (!string.IsNullOrEmpty(appKitAddress))
                {
                    PlayerPrefs.SetString("walletAddress", appKitAddress);
                    PlayerPrefs.Save();
                    currentPlayerWallet = appKitAddress;
                    
                    Debug.Log($"[NFT-DEBUG] ✅ PlayerPrefs synchronized with AppKit address: {appKitAddress}");
                    
                    ShowLevelUI();
                }
            }
            else
            {
                Debug.LogWarning($"[NFT-DEBUG] AppKit initialized but no account connected, clearing PlayerPrefs");
                PlayerPrefs.DeleteKey("walletAddress");
                PlayerPrefs.Save();
                currentPlayerWallet = "";
            }
            
            LoadNFTStateFromBlockchain();
        }
        else
        {
            Debug.LogWarning($"[NFT-DEBUG] AppKit not initialized after delay, skipping automatic reconnection");
        }
    }
    
    System.Collections.IEnumerator DelayedBlockchainRefresh()
    {
        Debug.Log("[NFT] Waiting 3 seconds for evolution transaction confirmation...");
        yield return new WaitForSeconds(3f);
        
        Debug.Log("[NFT] 🎯 SKIPPING immediate blockchain refresh to avoid level desync");
        Debug.Log("[NFT] Level synchronization will be handled by OnPointsConsumedAfterSuccess");
        
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
        
        isBlockchainVerificationActive = true;
        blockchainStateLoaded = false;
        Debug.Log("[NFT-DEBUG] 🔗 Blockchain verification ACTIVE - ready to receive blockchain data");
        
        StartCoroutine(VerifyNFTDirectlyFromBlockchain());
    }
    
    System.Collections.IEnumerator VerifyNFTDirectlyFromBlockchain()
    {
        Debug.Log($"[BLOCKCHAIN] 🔗 Starting DIRECT blockchain verification for wallet: {currentPlayerWallet}");
        
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
            string balanceAbi = "function balanceOf(address) view returns (uint256)";
            
            var balance = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
                CONTRACT_ADDRESS,
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
                        CONTRACT_ADDRESS,
                        tokenByIndexAbi,
                        "tokenOfOwnerByIndex",
                        new object[] { normalizedWallet, i }
                    );
                    
                    Debug.Log($"[BLOCKCHAIN-V2] ✅ TokenId at index {i}: {tokenId}");
                    
                    if (tokenId > 0)
                    {
                        Debug.Log($"[BLOCKCHAIN-V2] Reading level for token #{tokenId}");
                        
                        int level = 1; 
                        
                        try
                        {
                            level = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
                                CONTRACT_ADDRESS,
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
            Debug.Log($"[FIREBASE-SYNC] 📊 NFT exists on blockchain, fetching score from Firebase and syncing level");
            
#if UNITY_WEBGL && !UNITY_EDITOR
            SyncNFTLevelWithFirebaseJS(currentPlayerWallet, blockchainState.level, blockchainState.tokenId);
#else
            blockchainState.score = 150;
            Debug.Log($"[FIREBASE-SYNC] 🎮 Editor mode: using mock score {blockchainState.score}");
            OnNFTStateLoaded(JsonUtility.ToJson(blockchainState));
#endif
        }
        else
        {
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
            
            OnNFTStateLoaded(firebaseDataJson);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FIREBASE-SYNC] ❌ Error processing Firebase sync result: {ex.Message}");
            
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
        
        GetNFTStateJS(currentPlayerWallet);
        
        float timeout = 3f; // Reduced timeout
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
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
            
            bool isFirebaseData = json.Contains("walletAddress") && !isBlockchainVerificationActive;
            
            if (blockchainStateLoaded && isFirebaseData)
            {
                Debug.Log("[NFT-DEBUG] 🔄 Firebase data received - preserving blockchain NFT data, updating score only");
                Debug.Log($"[NFT-DEBUG] 📊 Firebase score: {nftState.score}, Blockchain NFT: level={currentNFTState.level}, hasNFT={currentNFTState.hasNFT}");
                
                currentNFTState.score = nftState.score;
                
                Debug.Log($"[NFT-DEBUG] ✅ Score updated to {nftState.score}, blockchain NFT data preserved");
                
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
            
            if (isBlockchainVerificationActive)
            {
                Debug.Log("[NFT-DEBUG] ✅ Processing BLOCKCHAIN verification data");
                blockchainStateLoaded = true;
                isBlockchainVerificationActive = false;
            }
            
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
                    string levelMessage = "Ready to mint";
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
                    string scoreMessage = $"XP: {currentScore}/0";
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
        
        Debug.Log($"[NFT-DEBUG] Opening NFT display panel for wallet: {currentPlayerWallet}");
        
        var nftPanel = FindObjectOfType<NFTDisplayPanel>();
        if (nftPanel != null)
        {
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
                int targetLevel = evolutionData.targetLevel;
                int authorizedCurrentLevel = evolutionData.currentLevel;
                Debug.Log($"[EVOLUTION] ✅ Server authorized evolution to level {targetLevel}");
                Debug.Log($"[EVOLUTION] Server current level: {authorizedCurrentLevel}, Target level: {targetLevel}");
                
                if (targetLevel > authorizedCurrentLevel)
                {
                    UpdateStatusUI($"Evolution authorized to Level {targetLevel}! Score: {evolutionData.score}");
                    
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
        
        if (!string.IsNullOrEmpty(currentPlayerWallet))
        {
            LoadNFTStateFromBlockchain();
            
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
                    BigInteger mintPrice = BigInteger.Parse("1000000000000000"); 
                    
                    var result = await Reown.AppKit.Unity.AppKit.Evm.SendTransactionAsync(
                        CONTRACT_ADDRESS,  
                        mintPrice,         
                        data               
                    );
                    
                    if (!string.IsNullOrEmpty(result))
                    {
                        Debug.Log($"[MINT] Transaction sent with hash: {result}. Starting REAL blockchain monitoring...");
                        
#if UNITY_WEBGL && !UNITY_EDITOR
                        StartRealMintMonitoring(result);
#else
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
                        Debug.Log($"[EVOLUTION] Transaction sent with hash: {result}. Starting REAL blockchain monitoring...");
                        
#if UNITY_WEBGL && !UNITY_EDITOR
                        StartRealTransactionMonitoring(result, targetLevel);
#else
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
            
            if (pendingEvolutionCost > 0)
            {
                Debug.Log($"[EVOLUTION] 💰 Consuming {pendingEvolutionCost} points after confirmed blockchain success for token #{selectedTokenId}");
                
#if UNITY_WEBGL && !UNITY_EDITOR
                ConsumePointsAfterSuccessJS(currentPlayerWallet, pendingEvolutionCost, selectedTokenId, newLevel);
#endif
                
                currentNFTState.score = Mathf.Max(0, currentNFTState.score - pendingEvolutionCost);
                
                pendingEvolutionCost = 0; 
            }
            
            UpdateNFTLevelInFirebase(newLevel);
            
            currentNFTState.level = newLevel;
            
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
        
        Debug.Log($"[NFT-FIREBASE] 🔄 UpdateNFTLevelInFirebase called with level {newLevel}");
        Debug.Log($"[NFT-FIREBASE] 🔄 Using selectedTokenId: {selectedTokenId}");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        SyncNFTLevelWithFirebaseJS(currentPlayerWallet, newLevel, selectedTokenId);
        Debug.Log($"[NFT-FIREBASE] ✅ Called SyncNFTLevelWithFirebaseJS for wallet {currentPlayerWallet}, level {newLevel}, token {selectedTokenId}");
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
                level = 0; 
            }
            
            currentNFTState.level = level;
            currentNFTState.hasNFT = level > 0;
            
            Debug.Log($"[NFT] OnNFTLevelUpdated called: level={level}, updating UI...");
            UpdateLevelUI(level);
            
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
        
        if (pendingEvolutionCost > 0)
        {
            Debug.Log($"[EVOLUTION] ✅ Transaction failed but no points were consumed. {pendingEvolutionCost} points remain safe.");
            UpdateStatusUI($"Transaction failed - your {pendingEvolutionCost} points are safe: {error}");
            pendingEvolutionCost = 0;
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
        DirectMintNFTJS(currentPlayerWallet);
#else
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
        
        Debug.Log($"[EVOLUTION] Getting level for selected NFT #{selectedTokenId}...");
        StartCoroutine(GetSelectedNFTLevelAndEvolve());
    }
    
    System.Collections.IEnumerator GetSelectedNFTLevelAndEvolve()
    {
        var levelTask = Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
            CONTRACT_ADDRESS,
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
    
    var selectedNFTData = new NFTStateData
    {
        hasNFT = true,
        level = currentLevel,
        tokenId = selectedTokenId,
        walletAddress = currentPlayerWallet,
        score = currentNFTState.score 
    };
    
    Debug.Log($"[EVOLUTION] 📤 Sending CORRECT data: TokenId={selectedTokenId}, Level={currentLevel}");
    ShareSpecificNFTStateWithJS(selectedNFTData);
    
    int evolutionCost = GetEvolutionCost(currentLevel + 1);
    Debug.Log($"[EVOLUTION] Evolution cost for level {currentLevel} -> {currentLevel + 1}: {evolutionCost} points");
    
    Debug.Log($"[EVOLUTION] 🔍 Checking evolution eligibility WITHOUT consuming points yet");
    Debug.Log($"[EVOLUTION] Wallet: {currentPlayerWallet}, TokenId: {selectedTokenId}, Cost: {evolutionCost}, Target: {currentLevel + 1}");
    
    pendingEvolutionCost = evolutionCost;
    Debug.Log($"[EVOLUTION] 📝 Pending evolution cost: {pendingEvolutionCost} points (will consume only after blockchain success)");
    
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
        var costs = new Dictionary<int, int>
        {
            {2, 2},     
            {3, 100},  
            {4, 200}, 
            {5, 300}, 
            {6, 400}, 
            {7, 500}, 
            {8, 600}, 
            {9, 700}, 
            {10, 800} 
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
            
            string functionSelector = MINT_NFT_SELECTOR; 
            
            string playerPointsHex = "0000000000000000000000000000000000000000000000000000000000000000"; 
            string nonceHex = authData.nonce.ToString("X").PadLeft(64, '0'); 
            
            string signatureWithoutPrefix = authData.signature.StartsWith("0x") ? authData.signature.Substring(2) : authData.signature;
            string signatureOffsetHex = "0000000000000000000000000000000000000000000000000000000000000060"; 
            string signatureLengthHex = (signatureWithoutPrefix.Length / 2).ToString("X").PadLeft(64, '0');
            string signatureDataHex = signatureWithoutPrefix.PadRight((int)Math.Ceiling(signatureWithoutPrefix.Length / 64.0) * 64, '0');
            
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
            
            OnNFTStateChanged?.Invoke(true, 1);
            Debug.Log("[NFT] NFT state changed event fired: hasNFT=true, count=1");
            
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
            
            byte[] signatureBytes;
            try
            {
                string hexSignature = authData.signature.StartsWith("0x") ? authData.signature.Substring(2) : authData.signature;
                
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
                    signatureBytes 
                }
            );
            
            Debug.Log($"[NFT] Evolution transaction sent: {result}");
            UpdateStatusUI("Evolution transaction confirmed!");
            
            if (!string.IsNullOrEmpty(result))
            {
                OnEvolveTransactionSuccess(result, authData.targetLevel);
            }
            
            Debug.Log("[NFT] Refreshing blockchain data after successful evolution...");
            
            uint newLevel = (uint)(authData.targetLevel);
            UpdateStatusUI($"Evolution completed! Refreshing NFT state...");
            
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
    
    public void OnPointsConsumedAfterSuccess(string responseJson)
    {
        try
        {
            var response = JsonUtility.FromJson<PointsConsumptionResponse>(responseJson);
            
            if (response.success)
            {
                Debug.Log($"[POINTS-CONSUME] ✅ Points consumed successfully: {response.consumedPoints}");
                Debug.Log($"[POINTS-CONSUME] ✅ New score: {response.newScore}");
                
                currentNFTState.score = response.newScore;
                
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
                
                UpdateStatusUI($"Evolution completed but points may not be properly consumed");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[POINTS-CONSUME] ❌ Error parsing consumption response: {ex.Message}");
        }
    }
    
    public void OnPointsPreConsumed(string responseJson)
    {
        try
        {
            var response = JsonUtility.FromJson<PreEvolutionResponse>(responseJson);
            
            if (response.success && response.authorized)
            {
                Debug.Log($"[PRE-EVOLUTION] ✅ Points verified and consumed! New score: {response.newScore}");
                Debug.Log($"[PRE-EVOLUTION] Proceeding with blockchain evolution for NFT #{response.tokenId} to level {response.targetLevel}");
                
                currentNFTState.score = response.newScore;
                
                UpdateStatusUI($"Points consumed ({response.pointsConsumed}). Proceeding with blockchain evolution...");
                
                int originalScore = response.newScore + response.pointsConsumed;
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
    
    private void RequestEvolutionAuthorizationDirectly(int tokenId, int targetLevel, int originalScore)
    {
        Debug.Log($"[EVOLUTION-DIRECT] Requesting evolution authorization for NFT #{tokenId} to level {targetLevel}");
        Debug.Log($"[EVOLUTION-DIRECT] Using original score: {originalScore} (before consumption)");
        
        var evolutionData = new
        {
            walletAddress = currentPlayerWallet,
            tokenId = tokenId,
            playerPoints = originalScore, 
            targetLevel = targetLevel
        };
        
        Debug.Log($"[EVOLUTION-DIRECT] Calling signature server with data: {JsonUtility.ToJson(evolutionData)}");
        
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
                
                currentNFTState.score = response.newScore;
                
                UpdateStatusUI($"Evolution completed! New score: {response.newScore} points");
                
                Debug.Log("[NFT] Refreshing all data after successful evolution...");
                
                RefreshNFTData();
                
                if (response.updatedNFT != null)
                {
                    Debug.Log($"[NFT] Syncing new level {response.updatedNFT.newLevel} for NFT #{response.updatedNFT.tokenId} with Firebase");
                    SyncNFTLevelWithFirebaseJS(currentPlayerWallet, (int)response.updatedNFT.newLevel, (int)response.updatedNFT.tokenId);
                }
                
                GetNFTStateJS(currentPlayerWallet);
                
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
        
        var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
        if (button == null)
        {
            button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        }
        
        int nftLevel = GetNFTLevelForToken(nftIndex);
        CustomizeButtonText(buttonObj, nftIndex, nftLevel);
        
        PositionButton(buttonObj, nftIndex);
        
        int tokenIndex = nftIndex;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSimpleNFTButtonClicked(tokenIndex));
        
        nftButtons.Add(button);
        
        Debug.Log($"[NFT-BUTTONS] ✅ Simple NFT button #{nftIndex} created with level {nftLevel}");
    }
    
    private GameObject CreateBasicNFTButton(int nftIndex)
    {
        GameObject buttonObj = new GameObject($"SimpleNFT_Button_{nftIndex}");
        buttonObj.transform.SetParent(nftButtonContainer, false);
        
        var button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        var image = buttonObj.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.1f, 0.7f, 0.3f, 0.9f); 
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = $"NFT #{nftIndex}";
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = UnityEngine.Vector2.zero;
        textRect.anchorMax = UnityEngine.Vector2.one;
        textRect.offsetMin = UnityEngine.Vector2.zero;
        textRect.offsetMax = UnityEngine.Vector2.zero;
        
        return buttonObj;
    }
    
    private void CustomizeButtonText(GameObject buttonObj, int nftIndex)
    {
        var textComponents = buttonObj.GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents.Length > 0)
        {
            textComponents[0].text = $"NFT #{nftIndex}";
            Debug.Log($"[NFT-BUTTONS] 📝 Updated text to 'NFT #{nftIndex}'");
        }
        else
        {
            var legacyText = buttonObj.GetComponentsInChildren<UnityEngine.UI.Text>();
            if (legacyText.Length > 0)
            {
                legacyText[0].text = $"NFT #{nftIndex}";
                Debug.Log($"[NFT-BUTTONS] 📝 Updated legacy text to 'NFT #{nftIndex}'");
            }
        }
    }
    
    private void CustomizeButtonText(GameObject buttonObj, int nftIndex, int nftLevel)
    {
        var textComponents = buttonObj.GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents.Length > 0)
        {
            textComponents[0].text = $"NFT #{nftIndex}\nLevel {nftLevel}";
        }
        else
        {
            var legacyText = buttonObj.GetComponentsInChildren<UnityEngine.UI.Text>();
            if (legacyText.Length > 0)
            {
                legacyText[0].text = $"NFT #{nftIndex}\nLevel {nftLevel}";
            }
        }
    }
    
    private int GetNFTLevelForToken(int tokenId)
    {
        return Mathf.Max(1, currentNFTState.level);
    }
    

    private void PositionButton(GameObject buttonObj, int nftIndex)
    {
        var rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new UnityEngine.Vector2(120, 40); 
            rectTransform.anchoredPosition = new UnityEngine.Vector2((nftIndex - 1) * 130, 0); 
            
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
        
        UpdateStatusUI($"Selected NFT #{nftIndex} - Level {currentNFTState.level}");
        selectedTokenId = nftIndex;
        
        Debug.Log($"[NFT-BUTTONS] 🎯 Opening detailed view for NFT #{nftIndex}");
        OnEvolutionButtonClicked(); 
    }

    public void OnRealMintSuccess(string transactionHash)
    {
        Debug.Log($"[REAL-TX] 🎆 REAL mint transaction succeeded on blockchain: {transactionHash}");
        
        OnMintTransactionSuccess(transactionHash);
    }
    
    public void OnRealEvolveSuccess(string evolveDataJson)
    {
        try
        {
            var evolveData = JsonUtility.FromJson<RealEvolveSuccess>(evolveDataJson);
            Debug.Log($"[REAL-TX] 🚀 REAL evolve transaction succeeded on blockchain: {evolveData.hash} to level {evolveData.level}");
            
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

    private void StartRealTransactionMonitoring(string txHash, int targetLevel)
    {
        Debug.Log($"[REAL-TX] 👀 Starting real transaction monitoring for {txHash} (level {targetLevel})");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval($@"
            if (window.monitorTransaction) {{
                window.monitorTransaction('{txHash}', 'evolve', {{ targetLevel: {targetLevel} }});
            }} else {{
                console.error('[REAL-TX] monitorTransaction function not available');
            }}
        ");
#endif
    }

    private void StartRealMintMonitoring(string txHash)
    {
        Debug.Log($"[REAL-TX] 👀 Starting real mint transaction monitoring for {txHash}");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval($@"
            if (window.monitorTransaction) {{
                window.monitorTransaction('{txHash}', 'mint');
            }} else {{
                console.error('[REAL-TX] monitorTransaction function not available');
            }}
        ");
#endif
    }

    private System.Collections.IEnumerator SimulateRealTransactionSuccess(string txHash, int targetLevel)
    {
        Debug.Log($"[REAL-TX] 🎮 Simulating real transaction success in editor after 3 seconds...");
        yield return new WaitForSeconds(3f);
        
        Debug.Log($"[REAL-TX] 🎮 Simulated blockchain confirmation for {txHash}");
        OnEvolveTransactionSuccess(txHash, targetLevel);
    }

    private System.Collections.IEnumerator SimulateRealMintSuccess(string txHash)
    {
        Debug.Log($"[REAL-TX] 🎮 Simulating real mint success in editor after 3 seconds...");
        yield return new WaitForSeconds(3f);
        
        Debug.Log($"[REAL-TX] 🎮 Simulated mint blockchain confirmation for {txHash}");
        OnMintTransactionSuccess(txHash);
    }

    private void InitializeRealTransactionDetection()
    {
        Debug.Log("[REAL-TX] 🎯 Setting up real transaction detection system...");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            SetupRealTransactionDetection(); 
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