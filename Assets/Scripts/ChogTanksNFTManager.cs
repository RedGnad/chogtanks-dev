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
    public int newBalance;
    public string error;
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

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void GetNFTStateJS(string walletAddress);
    
    [DllImport("__Internal")]
    private static extern void CheckEvolutionEligibilityJS(string walletAddress);
    
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
#else
    private static void GetNFTStateJS(string walletAddress) { }
    private static void CheckEvolutionEligibilityJS(string walletAddress) { }
    private static void CanMintNFTJS(string walletAddress, string callbackMethod) { }
    
    private static void MarkMintSuccessJS(string walletAddress) { }
    private static void CheckHasMintedNFTJS(string walletAddress) { }
    private static void UpdateNFTLevelJS(string walletAddress, int newLevel) { }
    private static void ReadNFTFromBlockchainJS(string walletAddress, string callbackMethod) { }
    private static void SyncNFTLevelWithFirebaseJS(string walletAddress, int blockchainLevel, int tokenId) { }
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
        
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        OnNFTStateChanged -= HandleNFTStateChanged;
    }
    
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
        }
        else
        {
            Debug.LogError("[NFT] Aucun wallet connecté détecté");
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
            statusText.text = "Connect your wallet to continue";
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
        
        StartCoroutine(ReadNFTFromBlockchain());
    }
    
    System.Collections.IEnumerator ReadNFTFromBlockchain()
    {
        Debug.Log($"[BLOCKCHAIN] 🔗 Starting DIRECT blockchain verification for wallet: {currentPlayerWallet}");
        
        yield return StartCoroutine(GetNFTsDirectlyFromBlockchain());
    }
    
    System.Collections.IEnumerator GetNFTsDirectlyFromBlockchain()
    {
        Debug.Log($"[BLOCKCHAIN] 🔍 Calling balanceOf directly via AppKit...");
        
        string normalizedWallet = currentPlayerWallet.ToLowerInvariant();
        Debug.Log($"[BLOCKCHAIN] 🔧 Normalized wallet: {currentPlayerWallet} → {normalizedWallet}");
        
        bool balanceReceived = false;
        uint nftBalance = 0;
        
        System.Threading.Tasks.Task<uint> balanceTask = null;
        
        try
        {
            balanceTask = Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<uint>(
                "0x68c582651d709f6e2b6113c01d69443f8d27e30d",
                "function balanceOf(address) view returns (uint256)",
                "balanceOf",
                new object[] { currentPlayerWallet }
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BLOCKCHAIN] ❌ Error calling balanceOf: {e.Message}");
        }
        
        if (balanceTask != null)
        {
            while (!balanceTask.IsCompleted)
            {
                yield return null;
            }
            
            if (balanceTask.Exception == null)
            {
                nftBalance = balanceTask.Result;
                balanceReceived = true;
                Debug.Log($"[BLOCKCHAIN] ✅ Direct balanceOf result: {nftBalance} NFTs");
            }
            else
            {
                Debug.LogError($"[BLOCKCHAIN] ❌ balanceOf failed: {balanceTask.Exception.Message}");
            }
        }
        
        if (!balanceReceived || nftBalance == 0)
        {
            Debug.Log($"[BLOCKCHAIN] ❌ No NFTs found, sending empty state");
            var noNFTState = new NFTStateData
            {
                hasNFT = false,
                level = 0,
                tokenId = 0,
                walletAddress = normalizedWallet,
                score = 0
            };
            OnNFTStateLoaded(JsonUtility.ToJson(noNFTState));
            
            Debug.Log($"[BLOCKCHAIN] 🔄 Reading Firebase score for no-NFT wallet: {normalizedWallet}");
            LoadNFTStateFromFirebase();
            yield break;
        }
        
        Debug.Log($"[BLOCKCHAIN] 🎯 Found {nftBalance} NFTs, getting first token details...");
        
        var nftState = new NFTStateData
        {
            hasNFT = true,
            level = 1, // Default level, will be updated if we can read it
            tokenId = 1, // Default token ID
            walletAddress = normalizedWallet,
            score = 0,
            nftCount = (int)nftBalance
        };
        
        Debug.Log($"[BLOCKCHAIN] 📤 Sending state: {nftBalance} NFTs found, Level {nftState.level}");
        OnNFTStateLoaded(JsonUtility.ToJson(nftState));
        
        Debug.Log($"[BLOCKCHAIN] 🔄 Now reading Firebase score for normalized wallet: {normalizedWallet}");
        LoadNFTStateFromFirebase();
        
        yield return null;
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
                    string levelMessage = $"NFT Level: {level} (Blockchain)";
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
                    int nextLevelThreshold = GetNextLevelThreshold(level + 1); // Next level threshold
                    string scoreMessage = $"XP: {currentScore}/{nextLevelThreshold}";
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
            
            UpdateNFTLevelInFirebase(newLevel);
            
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
            
            UpdateLevelUI(level);
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
        UpdateStatusUI($"Transaction error: {error}");
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
    
    public void RequestEvolutionForSelectedNFT()
    {
        Debug.Log($"[EVOLUTION] ===== DÉBUT DEMANDE ÉVOLUTION =====");
        Debug.Log($"[EVOLUTION] Selected token ID: {currentNFTState.tokenId}");
        Debug.Log($"[EVOLUTION] Current NFT state - TokenId: {currentNFTState.tokenId}, Level: {currentNFTState.level}");
        Debug.Log($"[EVOLUTION] Processing evolution: {isProcessingEvolution}");
        
        if (currentNFTState.tokenId <= 0)
        {
            Debug.LogWarning($"[EVOLUTION] No valid NFT selected (tokenId: {currentNFTState.tokenId})");
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
        
        int currentLevel = currentNFTState.level;
        int targetLevel = currentLevel + 1;
        
        Debug.Log($"[EVOLUTION] Current level: {currentLevel}, Target level: {targetLevel}");
        
        if (targetLevel > 10)
        {
            Debug.LogWarning($"[EVOLUTION] NFT already at max level ({currentLevel})");
            UpdateStatusUI("NFT already at max level");
            isProcessingEvolution = false;
            return;
        }
        
        Debug.Log($"[EVOLUTION] Requesting evolution authorization for NFT #{currentNFTState.tokenId} from level {currentLevel} to {targetLevel}");
        UpdateStatusUI($"Requesting evolution authorization for NFT #{currentNFTState.tokenId}...");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[EVOLUTION] Calling CheckEvolutionEligibilityJS for wallet: {currentPlayerWallet}");
        CheckEvolutionEligibilityJS(currentPlayerWallet);
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
            {2, 100}, {3, 200}, {4, 300}, {5, 400}, {6, 500},
            {7, 600}, {8, 700}, {9, 800}, {10, 900}
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
            
            string abi = "function evolveNFT(uint256,uint256,uint256,bytes)";
            
            var result = await Reown.AppKit.Unity.AppKit.Evm.WriteContractAsync(
                CONTRACT_ADDRESS,
                abi,
                "evolveNFT",
                new object[] { 
                    authData.tokenId,
                    authData.currentPoints,
                    authData.nonce,
                    authData.signature
                }
            );
            
            Debug.Log($"[NFT] Evolution transaction sent: {result}");
            UpdateStatusUI("Evolution transaction confirmed!");
            
            ConsumePointsAfterTransaction(authData.evolutionCost, result);
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFT] Evolution transaction failed: {ex.Message}");
            UpdateStatusUI($"Evolution failed: {ex.Message}");
            isProcessingEvolution = false;
        }
    }
    
    private void ConsumePointsAfterTransaction(int pointsConsumed, string transactionHash)
    {
        UpdateStatusUI("Consuming points...");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[NFT] Points consumed: {pointsConsumed} for transaction: {transactionHash}");
        OnPointsConsumed(JsonUtility.ToJson(new { success = true, newBalance = 500 }));
#else
        OnPointsConsumed(JsonUtility.ToJson(new { success = true, newBalance = 500 }));
#endif
    }
    
    public void OnPointsConsumed(string responseJson)
    {
        try
        {
            var response = JsonUtility.FromJson<PointConsumptionResponse>(responseJson);
            
            if (response.success)
            {
                UpdateStatusUI($"Evolution completed! New balance: {response.newBalance} points");
                RefreshNFTData();
            }
            else
            {
                UpdateStatusUI($"Points consumption failed: {response.error}");
            }
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NFT] Error parsing points consumption response: {ex.Message}");
            UpdateStatusUI("Error updating points");
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
}