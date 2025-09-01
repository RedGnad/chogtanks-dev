using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.InteropServices;
using System.Linq;



[System.Serializable]
public class PlayerNFTData
{
    public uint[] tokenIds;
    public uint[] levels;
    public int count;
}

[System.Serializable]
public class NFTDisplayItem
{
    public uint tokenId;
    public uint level;
    public bool canEvolve;
    public uint evolutionCost;
}

[System.Serializable]
public class AutoMintCheckResponse
{
    public string walletAddress;
    public bool hasMintedNFT;
    public bool shouldAutoMint;
    public string error;
}

public class NFTDisplayPanel : MonoBehaviour
{
    
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DirectMintNFTJS(string walletAddress);
#else
    private static void DirectMintNFTJS(string walletAddress) { }
#endif

    [Header("UI References")]
    public Transform nftContainer;
    public TextMeshProUGUI statusText;
    public Button refreshButton;
    
    [Header("Simple NFT Buttons (Inside Panel)")]
    public Transform simpleButtonContainer;
    public GameObject simpleButtonPrefab;
    private List<UnityEngine.UI.Button> simpleNFTButtons = new List<UnityEngine.UI.Button>();
    
    [Header("NFT Level Images")]
    public Sprite[] nftLevelSprites = new Sprite[10];
    public Vector2 levelImageSize = new Vector2(40, 40);
    public float levelImageOffset = 50f;
    
    [Header("NFT Item Prefab (Simple)")]
    public GameObject nftItemPrefab;
    
    private string currentWalletAddress;
    private List<NFTDisplayItem> playerNFTs = new List<NFTDisplayItem>();
    
    public void UpdateWalletAddress(string newWalletAddress)
    {
        Debug.Log($"[NFT-PANEL] UpdateWalletAddress: {currentWalletAddress} → {newWalletAddress}");
        currentWalletAddress = newWalletAddress;
    }
    private ChogTanksNFTManager nftManager;
    private bool isRefreshing = false; 
    private float lastRefreshTime = 0f; 
    private const float MIN_REFRESH_INTERVAL = 2f; 

    private void Start()
    {
        Debug.Log("[NFT-PANEL] NFTDisplayPanel Start() called");
        
        nftManager = FindObjectOfType<ChogTanksNFTManager>();
        if (nftManager != null)
        {
            Debug.Log("[NFT-PANEL] ✅ NFTManager trouvé et connecté");
        }
        else
        {
            Debug.LogWarning("[NFT-PANEL] ⚠️ NFTManager non trouvé dans la scène");
        }
        
        CleanupAllSimpleNFTButtons();
        
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshNFTList);
        }
        else
        {
            Debug.LogWarning("[NFT-PANEL] Refresh button is null!");
        }
        
        gameObject.SetActive(false);
    }
    
    public void ShowPanel(string walletAddress)
    {
        bool signApproved = PlayerPrefs.GetInt("personalSignApproved", 0) == 1;
        if (!signApproved)
        {
            Debug.LogWarning("[NFT-PANEL] Personal sign required - panel blocked");
            UpdateStatus("Complete personal signature to access NFT panel");
            return;
        }
        
        currentWalletAddress = walletAddress;
        gameObject.SetActive(true);
        
        CleanupAllSimpleNFTButtons();
        
        RefreshNFTList();
    }
    
    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
    
    public async void RefreshNFTList()
    {
        bool signApproved = PlayerPrefs.GetInt("personalSignApproved", 0) == 1;
        if (!signApproved)
        {
            Debug.LogWarning("[NFT-PANEL] Personal sign required - refresh blocked");
            UpdateStatus("Complete personal signature to access NFTs");
            return;
        }
        
        float currentTime = Time.time;
        if (currentTime - lastRefreshTime < MIN_REFRESH_INTERVAL)
        {
            Debug.LogWarning($"[NFT-PANEL] RefreshNFTList called too soon (last: {currentTime - lastRefreshTime:F1}s ago), skipping to prevent spam");
            return;
        }
        
        string latestWallet = PlayerPrefs.GetString("walletAddress", "");
        if (!string.IsNullOrEmpty(latestWallet))
        {
            currentWalletAddress = latestWallet;
            Debug.Log($"[NFT-PANEL] RefreshNFTList using LATEST wallet: {currentWalletAddress}");
        }
        else
        {
            Debug.Log("[NFT-PANEL] No wallet connected - clearing NFT buttons");
            currentWalletAddress = "";
            UpdateStatus("No wallet connected");
            ClearSimpleNFTButtons();
            return;
        }
        
        if (isRefreshing)
        {
            Debug.Log("[NFT-PANEL] RefreshNFTList already in progress, skipping duplicate call");
            return;
        }
        
        isRefreshing = true;
        lastRefreshTime = currentTime; 
        
        try
        {
            
            UpdateStatus("Loading NFTs...");
            
            ClearSimpleNFTButtons();
            
            ClearNFTList();
            
            await GetAllNFTsFromBlockchain(currentWalletAddress);
        }
        finally
        {
            isRefreshing = false;
        }
    }
    
    public void OnNFTListReceived(string jsonData)
    {
        try
        {
            var nftData = JsonUtility.FromJson<PlayerNFTData>(jsonData);
            
            if (nftData.count == 0)
            {
                UpdateStatus("No NFTs found");
                return;
            }
            
            playerNFTs.Clear();
            
            for (int i = 0; i < nftData.count; i++)
            {
                var nftItem = new NFTDisplayItem
                {
                    tokenId = nftData.tokenIds[i],
                    level = nftData.levels[i],
                    canEvolve = nftData.levels[i] < 10,
                    evolutionCost = GetEvolutionCost(nftData.levels[i])
                };
                
                playerNFTs.Add(nftItem);
            }
            
            DisplayNFTItems();
            UpdateStatus($"Found {nftData.count} NFTs");
            
        }
        catch (System.Exception ex)
        {
            UpdateStatus("Error loading NFTs");
        }
    }
    
    private void DisplayNFTItems()
    {
        
        if (playerNFTs == null)
        {
            return;
        }
        
        for (int i = 0; i < playerNFTs.Count; i++)
        {
            Debug.Log($"[NFT-PANEL] 🔍 playerNFTs[{i}]: Token #{playerNFTs[i].tokenId}, Level {playerNFTs[i].level}");
        }
        
        for (int i = 0; i < nftContainer.childCount; i++)
        {
            var child = nftContainer.GetChild(i);
        }
        
        if (!DiagnoseDisplaySetup())
        {
            DisplayNFTItemsFallback();
            return;
        }
        
        
        ClearNFTList();
        
        if (nftContainer.childCount > 0)
        {
            for (int i = nftContainer.childCount - 1; i >= 0; i--)
            {
                var child = nftContainer.GetChild(i);
                DestroyImmediate(child.gameObject);
            }
        }
        
        int itemsCreated = 0;
        foreach (var nft in playerNFTs)
        {
            
            try
            {
                CreateNFTItem(nft);
                itemsCreated++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NFT-PANEL] ❌ Failed to create item for NFT #{nft.tokenId}: {ex.Message}");
            }
        }
        
        
        VerifyCreatedItems();
        
        for (int i = 0; i < nftContainer.childCount; i++)
        {
            var child = nftContainer.GetChild(i);
        }
        
        if (nftContainer.childCount != playerNFTs.Count)
        {
            Debug.LogError($"[NFT-PANEL] ❌ MISMATCH: Expected {playerNFTs.Count} children, got {nftContainer.childCount}!");
        }
        else
        {
            Debug.Log($"[NFT-PANEL] ✅ PERFECT: {nftContainer.childCount} dynamic elements created as expected!");
        }
    }
    
    private void CreateNFTItem(NFTDisplayItem nft)
    {
        if (nftItemPrefab == null || nftContainer == null)
        {
            Debug.LogError("[NFT-PANEL] Missing prefab or container references");
            return;
        }
        
        GameObject nftItem = Instantiate(nftItemPrefab, nftContainer);
        nftItem.name = $"NFTItem_Token{nft.tokenId}_Level{nft.level}";
        
        nftItem.SetActive(true);
        nftItem.transform.SetAsLastSibling(); 
        
        //var nftImage = nftItem.transform.Find("NFTImage")?.GetComponent<Image>();
        var nftImage = nftItem.transform.Find("NFTImage")?.GetComponent<Image>();
        var levelText = nftItem.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        var evolveButton = nftItem.transform.Find("EvolveButton")?.GetComponent<Button>();
        
        
        if (nftImage != null)
        {
            SetNFTImage(nftImage, nft.level);
            nftImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[NFT-PANEL] ⚠️ NFTImage not found - element will have no image");
        }
        
        if (levelText != null)
        {
            levelText.text = $"TANK #{nft.tokenId}\nLevel {nft.level}";
            levelText.gameObject.SetActive(true);
            levelText.color = Color.white;
            levelText.fontSize = 16;
        }
        else
        {
            Debug.LogWarning($"[NFT-PANEL] ⚠️ LevelText not found - element will have no text");
        }
        
        if (evolveButton != null)
        {
            evolveButton.gameObject.SetActive(true);
            evolveButton.interactable = nft.canEvolve;
            evolveButton.onClick.RemoveAllListeners();
            evolveButton.onClick.AddListener(() => {
                EvolveNFT(nft.tokenId, nft.level + 1);
            });
            
            var buttonText = evolveButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (nft.canEvolve)
                {
                    buttonText.text = $"EVOLVE → Lv.{nft.level + 1}\n({nft.evolutionCost} pts)";
                }
                else
                {
                    buttonText.text = "MAX LEVEL";
                }
                buttonText.gameObject.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning($"[NFT-PANEL] ⚠️ EvolveButton not found - element will have no button");
        }
        
        var rectTransform = nftItem.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            Vector2 webglPosition = new Vector2(50, 400 - (nftContainer.childCount * 100)); // Position absolue visible
            Vector2 webglSize = new Vector2(350, 80); 
            
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.anchoredPosition = webglPosition;
            rectTransform.sizeDelta = webglSize;
            
            #else
            rectTransform.anchoredPosition = new Vector2(0, -(nftContainer.childCount * 160));
            rectTransform.sizeDelta = new Vector2(200, 150);
            #endif
        }
        
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        StartCoroutine(ForceWebGLCanvasRefresh());
        #endif
    }
    
    private void EvolveNFT(uint tokenId, uint targetLevel)
    {
        
        if (nftManager != null)
        {
            nftManager.selectedTokenId = (int)tokenId;
            
            nftManager.RequestEvolutionForSelectedNFT();
            
        }
        else
        {
            Debug.LogError($"[NFT-PANEL] NFTManager is null, cannot evolve NFT #{tokenId}");
        }
    }
    
    public void RefreshAfterEvolution()
    {
        
        if (isRefreshing)
        {
            return;
        }
        
        UpdateStatus("Evolution completed! Updating display...");
        
        StartCoroutine(DelayedAutoRefresh());
    }
    
    private System.Collections.IEnumerator DelayedAutoRefresh()
    {
        yield return new WaitForSeconds(2f);
        
        RefreshNFTList();
        
        yield return new WaitForSeconds(1f);
        UpdateStatus("NFT display updated!");
        
        yield return new WaitForSeconds(3f);
        UpdateStatus("");
    }
    
    private void SetNFTImage(Image nftImage, uint level)
    {
        string imagePath = $"NFT_Level_{level}";
        Sprite nftSprite = Resources.Load<Sprite>(imagePath);
        
        if (nftSprite != null)
        {
            nftImage.sprite = nftSprite;
        }
        else
        {
            Debug.LogWarning($"[NFTPanel] NFT image not found: {imagePath}");
        }
    }
    
    private uint GetEvolutionCost(uint currentLevel)
    {
        var costs = new Dictionary<uint, uint>
        {
            {1, 2},
            {2, 100},
            {3, 200},
            {4, 300},
            {5, 400},
            {6, 500},
            {7, 600},
            {8, 700},
            {9, 800}
        };
        
        return costs.ContainsKey(currentLevel) ? costs[currentLevel] : 0;
    }
    
    private void ClearNFTList()
    {
        
        for (int i = nftContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = nftContainer.GetChild(i);
            
            if (child.name.StartsWith("SimpleNFT_Button_"))
            {
                Debug.Log($"[NFT-PANEL] 🔒 PROTECTING simple NFT button: {child.name}");
                continue; 
            }
            
            Destroy(child.gameObject);
        }
        
    }
    
    public void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
    
    public void UpdateNFTLevel(int tokenId, int newLevel)
    {
        
        var nftToUpdate = playerNFTs.Find(nft => nft.tokenId == tokenId);
        if (nftToUpdate != null)
        {
            nftToUpdate.level = (uint)newLevel;
            nftToUpdate.canEvolve = newLevel < 10;
            nftToUpdate.evolutionCost = GetEvolutionCost((uint)newLevel);
            
            
            UpdateStatus($"NFT #{tokenId} evolved to level {newLevel}");
        }
        else
        {
            Debug.LogWarning($"[NFT-PANEL] NFT #{tokenId} not found in playerNFTs list");
        }
    }
    
    private async System.Threading.Tasks.Task GetAllNFTsFromBlockchain(string walletAddress)
    {
        try
        {
            // Vérification de la signature personnelle avant de charger depuis la blockchain
            bool signApproved = PlayerPrefs.GetInt("personalSignApproved", 0) == 1;
            if (!signApproved)
            {
                Debug.LogWarning("[NFT-PANEL] Personal signature not approved - blockchain loading blocked");
                UpdateStatus("Complete personal signature to access NFTs");
                return;
            }
            
            if (!Reown.AppKit.Unity.AppKit.IsInitialized || !Reown.AppKit.Unity.AppKit.IsAccountConnected)
            {
                UpdateStatus("Wallet not connected");
                return;
            }
            
            var contractAddresses = new string[]
            {
                "0x04223adab3a0c1a2e8aade678bebd3fddd580a38"
            };
            
            Debug.Log($"[NFT-LIST] Checking {contractAddresses.Length} contracts for NFTs");
            
            playerNFTs.Clear();
            Debug.Log($"[NFT-LIST] Cleared previous NFT data");
            
            var allNFTs = new List<NFTDisplayItem>();
            
            foreach (var contractAddr in contractAddresses)
            {
                try
                {
                    
                string balanceAbi = "function balanceOf(address) view returns (uint256)";
                    
                    var balance = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
                        contractAddr,
                        balanceAbi,
                        "balanceOf",
                        new object[] { walletAddress }
                    );
                    
                    
                    if (balance > 0)
                    {
                        
                        string tokenByIndexAbi = "function tokenOfOwnerByIndex(address owner, uint256 index) view returns (uint256)";
                        string getLevelAbi = "function getLevel(uint256 tokenId) view returns (uint256)";
                        
                        for (int i = 0; i < balance; i++)
                        {
                            try
                            {
                                
                                var tokenId = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
                                    contractAddr,
                                    tokenByIndexAbi,
                                    "tokenOfOwnerByIndex",
                                    new object[] { walletAddress, i }
                                );
                                
                                
                                if (tokenId > 0)
                                {
                                    
                                    int level = 1; 
                                    
                                    try
                                    {
                                        level = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
                                            contractAddr,
                                            getLevelAbi,
                                            "getLevel",
                                            new object[] { tokenId }
                                        );
                                        
                                    }
                                    catch (System.Exception levelError)
                                    {
                                        Debug.LogWarning($"[NFT-LIST] ⚠️ Contract {contractAddr} doesn't have getLevel function, assuming level 1 for token #{tokenId}");
                                        Debug.LogWarning($"[NFT-LIST] getLevel error: {levelError.Message}");
                                    }
                                    
                                    var evolutionCost = GetEvolutionCost((uint)level);
                                    
                                    allNFTs.Add(new NFTDisplayItem
                                    {
                                        tokenId = (uint)tokenId,
                                        level = (uint)level,
                                        canEvolve = level < 10,
                                        evolutionCost = evolutionCost
                                    });
                                    
                                }
                                else
                                {
                                    Debug.LogWarning($"[NFT-LIST] Invalid tokenId {tokenId} at index {i}");
                                }
                            }
                            catch (System.Exception tokenError)
                            {
                                Debug.LogError($"[NFT-LIST] ❌ Error getting token at index {i}: {tokenError.Message}");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"[NFT-LIST] No NFTs found in contract {contractAddr}");
                    }
                }
                catch (System.Exception contractError)
                {
                    Debug.LogError($"[NFT-LIST] ❌ Contract error for {contractAddr}: {contractError.Message}");
                    Debug.LogError($"[NFT-LIST] Stack trace: {contractError.StackTrace}");
                }
            }
            
            
            for (int i = 0; i < allNFTs.Count; i++)
            {
                var nft = allNFTs[i];
            }
            
            playerNFTs = allNFTs;
            
            if (allNFTs.Count == 0)
            {
                
                UpdateStatus("No NFTs found - Checking mint history...");
                CheckAutoMintEligibility(walletAddress);
            }
            else
            {
                
                SyncFirebaseWithBlockchainData(walletAddress, allNFTs);
                
                DisplayNFTItems();
                UpdateStatus($"Found {allNFTs.Count} NFTs");
                
                CreateSimpleNFTButtonsInPanel(allNFTs.Count);
            }
            
        }
        catch (System.Exception error)
        {
            UpdateStatus("Error loading NFTs");
        }
    }
    
    private void SyncFirebaseWithBlockchainData(string walletAddress, List<NFTDisplayItem> blockchainNFTs)
    {
        if (blockchainNFTs == null || blockchainNFTs.Count == 0)
        {
            Debug.Log($"[FIREBASE-SYNC] No NFTs to sync for wallet {walletAddress}");
            return;
        }
        
        var highestNFT = blockchainNFTs.OrderByDescending(nft => nft.level).First();
        
        
        if (nftManager != null)
        {
            
#if UNITY_WEBGL && !UNITY_EDITOR
            ChogTanksNFTManager.SyncNFTLevelWithFirebaseJS(walletAddress, (int)highestNFT.level, (int)highestNFT.tokenId);
#else
            Debug.Log($"[FIREBASE-SYNC] Editor mode: would sync Level {highestNFT.level}, Token {highestNFT.tokenId}");
#endif
            
            try
            {
                nftManager.currentNFTState.level = (int)highestNFT.level;
                nftManager.currentNFTState.tokenId = (int)highestNFT.tokenId;
                nftManager.currentNFTState.hasNFT = true;
                
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[FIREBASE-SYNC] Could not update local NFTManager state: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[FIREBASE-SYNC] NFTManager not found, cannot sync Firebase");
        }
    }
    
    private void CheckAutoMintEligibility(string walletAddress)
    {
        if (string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogWarning("[AUTO-MINT] ⚠️ No wallet address provided for auto-mint check");
            return;
        }
            
        
#if UNITY_WEBGL && !UNITY_EDITOR
        ChogTanksNFTManager.CheckHasMintedNFTJS(walletAddress);
#else
        var simulatedResult = new {
            walletAddress = walletAddress.ToLowerInvariant(),
            hasMintedNFT = false,
            shouldAutoMint = true
        };
        OnHasMintedNFTChecked(JsonUtility.ToJson(simulatedResult));
#endif
    }
    
    public void OnHasMintedNFTChecked(string jsonResponse)
    {
        try
        {
            
            var response = JsonUtility.FromJson<AutoMintCheckResponse>(jsonResponse);
            
            
            if (response.shouldAutoMint && playerNFTs.Count == 0)
            {
                TriggerAutoMint();
            }
            else if (!response.shouldAutoMint)
            {
                Debug.Log($"[AUTO-MINT] ℹ️ User has minted before, no auto-mint needed");
            }
            else if (playerNFTs.Count > 0)
            {
                Debug.Log($"[AUTO-MINT] ℹ️ User already has {playerNFTs.Count} NFTs, no auto-mint needed");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AUTO-MINT] Error parsing Firebase response: {ex.Message}");
        }
    }
    
    private void TriggerAutoMint()
    {
        string walletAddress = PlayerPrefs.GetString("walletAddress", "");
        if (string.IsNullOrEmpty(walletAddress))
        {
            UpdateStatus("Error: No wallet connected");
            return;
        }
        
        
        
#if UNITY_WEBGL && !UNITY_EDITOR
        DirectMintNFTJS(walletAddress);
#else
        Debug.Log("[AUTO-MINT] Direct mint call (Editor mode)");
#endif
    }
    
    private bool DiagnoseDisplaySetup()
    {
        
        bool isValid = true;
        
        if (nftContainer == null)
        {
            isValid = false;
        }
        else
        {
            Debug.Log($"[NFT-PANEL] Container active: {nftContainer.gameObject.activeInHierarchy}");
        }
        
        if (nftItemPrefab == null)
        {
            isValid = false;
        }
        else
        {
            
            var rectTransform = nftItemPrefab.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Debug.Log($"[NFT-PANEL] ✅ Prefab has RectTransform: {rectTransform.sizeDelta}");
            }
            else
            {
                Debug.LogWarning("[NFT-PANEL] ⚠️ Prefab missing RectTransform");
            }
        }
        
        return isValid;
    }
    
    private void DisplayNFTItemsFallback()
    {
        
        if (statusText != null)
        {
            string fallbackText = $"NFTs Found: {playerNFTs.Count}\n";
            for (int i = 0; i < playerNFTs.Count; i++)
            {
                var nft = playerNFTs[i];
                fallbackText += $"• Tank #{nft.tokenId} - Level {nft.level}\n";
            }
            
            statusText.text = fallbackText;
            Debug.Log($"[NFT-PANEL] 📝 Fallback text set: {fallbackText}");
        }
        else
        {
            Debug.LogError("[NFT-PANEL] ❌ Even statusText is null, cannot display fallback!");
        }
    }
    
    private void VerifyCreatedItems()
    {
        
        if (nftContainer == null)
        {
            return;
        }
        
        int childCount = nftContainer.childCount;
        
        for (int i = 0; i < childCount; i++)
        {
            var child = nftContainer.GetChild(i);
            if (child != null)
            {
                
                var rectTransform = child.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Debug.Log($"[NFT-PANEL] Child {i} RectTransform: size={rectTransform.sizeDelta}, anchored={rectTransform.anchoredPosition}");
                }
            }
            else
            {
                Debug.LogWarning($"[NFT-PANEL] ⚠️ Child {i} is null!");
            }
        }
        
        if (childCount != playerNFTs.Count)
        {
            Debug.LogWarning($"[NFT-PANEL] ⚠️ MISMATCH: Expected {playerNFTs.Count} items, but container has {childCount} children");
        }
        else
        {
            Debug.Log($"[NFT-PANEL] ✅ SUCCESS: {childCount} items created as expected");
        }
    }
    
    private IEnumerator ForceWebGLCanvasRefresh()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
    }
    
    private void CreateSimpleNFTButtonsInPanel(int nftCount)
    {
        
        ClearSimpleNFTButtons();
        
        if (simpleButtonContainer == null)
        {
            if (nftContainer != null)
            {
                CreateSimpleButtonsInContainer(nftContainer, nftCount);
            }
            return;
        }
        
        CreateSimpleButtonsInContainer(simpleButtonContainer, nftCount);
        
    }
    
    private void CreateSimpleButtonsInContainer(Transform container, int nftCount)
    {
        for (int i = 0; i < nftCount; i++)
        {
            CreateSingleSimpleButton(container, i + 1);
        }
    }
    
    private void CreateSingleSimpleButton(Transform container, int nftIndex)
    {
        GameObject buttonObj = null;
        
        if (simpleButtonPrefab != null)
        {
            buttonObj = Instantiate(simpleButtonPrefab, container);
            buttonObj.name = $"SimpleNFT_Button_{nftIndex}";
        }
        else
        {
            buttonObj = CreateBasicSimpleButton(container, nftIndex);
        }
        
        var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
        if (button == null)
        {
            button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        }
        
        CustomizeSimpleButtonText(buttonObj, nftIndex);
        
        if (nftIndex <= playerNFTs.Count)
        {
            var nft = playerNFTs[nftIndex - 1];
            int nftLevel = (int)nft.level;
            CreateLevelImageForButton(buttonObj, nftLevel);
        }
        else
        {
            Debug.LogWarning($"[NFT-PANEL-DEBUG] Cannot create level image: nftIndex {nftIndex} > playerNFTs.Count {playerNFTs.Count}");
        }
        
        PositionSimpleButton(buttonObj, nftIndex);
        
        int tokenIndex = nftIndex;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSimpleNFTButtonClickedInPanel(tokenIndex));
        
        simpleNFTButtons.Add(button);
        
    }
    
    private GameObject CreateBasicSimpleButton(Transform container, int nftIndex)
    {
        GameObject buttonObj = new GameObject($"SimpleNFT_Button_{nftIndex}");
        buttonObj.transform.SetParent(container, false);
        
        var button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        var image = buttonObj.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.2f, 0.8f, 0.2f, 0.9f); 
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = $"NFT #{nftIndex}";
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        return buttonObj;
    }
    
    private void CustomizeSimpleButtonText(GameObject buttonObj, int nftIndex)
    {
        if (nftIndex <= 0 || nftIndex > playerNFTs.Count)
        {
            Debug.LogError($"[NFT-PANEL] ❌ Invalid nftIndex {nftIndex} for {playerNFTs.Count} NFTs");
            return;
        }
        
        var nft = playerNFTs[nftIndex - 1];
        uint realTokenId = nft.tokenId;
        int nftLevel = (int)nft.level;
        
        string buttonText = $"NFT #{realTokenId}\nLvl {nftLevel}";
        
        var textComponents = buttonObj.GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents.Length > 0)
        {
            textComponents[0].text = buttonText;
            Debug.Log($"[NFT-PANEL] 📝 Updated simple button text to '{buttonText}' (tokenId + level)");
        }
        else
        {
            var legacyText = buttonObj.GetComponentsInChildren<UnityEngine.UI.Text>();
            if (legacyText.Length > 0)
            {
                legacyText[0].text = buttonText;
            }
        }
    }
    
    private void PositionSimpleButton(GameObject buttonObj, int nftIndex)
    {
        var rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(120, 40); 
            rectTransform.anchoredPosition = new Vector2((nftIndex - 1) * 280, -50);
            
        }
    }
    
    private void CreateLevelImageForButton(GameObject buttonObj, int nftLevel)
    {
        
        if (nftLevel < 1 || nftLevel > 10) 
        {
            nftLevel = 1;
        }
        
        
        if (nftLevelSprites == null || nftLevelSprites.Length < nftLevel || nftLevelSprites[nftLevel - 1] == null) 
        {
            return;
        }
        
        GameObject levelImageObj = new GameObject($"LevelImage_Level{nftLevel}");
        levelImageObj.transform.SetParent(buttonObj.transform, false);
        
        var levelImage = levelImageObj.AddComponent<UnityEngine.UI.Image>();
        levelImage.sprite = nftLevelSprites[nftLevel - 1];
        levelImage.preserveAspect = true;
        levelImage.color = Color.white;
        
        var levelImageRect = levelImageObj.GetComponent<RectTransform>();
        levelImageRect.sizeDelta = levelImageSize;
        levelImageRect.anchoredPosition = new Vector2(0, levelImageOffset);
        levelImageRect.anchorMin = new Vector2(0.5f, 0.5f);
        levelImageRect.anchorMax = new Vector2(0.5f, 0.5f);
        
        levelImageObj.transform.SetAsLastSibling();
        
    }
    
    private void ClearSimpleNFTButtons()
    {
        
        foreach (var button in simpleNFTButtons)
        {
            if (button != null && button.gameObject != null)
            {
                DestroyImmediate(button.gameObject);
            }
        }
        
        simpleNFTButtons.Clear();
    }
    
    private void OnSimpleNFTButtonClickedInPanel(int nftIndex)
    {
        if (nftIndex <= 0 || nftIndex > playerNFTs.Count)
        {
            Debug.LogError($"[NFT-PANEL] ❌ Invalid nftIndex {nftIndex} for {playerNFTs.Count} NFTs");
            return;
        }
        
        var selectedNFT = playerNFTs[nftIndex - 1];
        uint realTokenId = selectedNFT.tokenId;
        
        
        UpdateStatus($"Selected NFT #{realTokenId} (Level {selectedNFT.level}) for evolution");
        
        EvolveNFT(realTokenId, selectedNFT.level + 1);
    }
    
    public void CleanupAllSimpleNFTButtons()
    {
        
        ClearSimpleNFTButtons();
        
        var allButtons = FindObjectsOfType<UnityEngine.UI.Button>(true);
        int cleanedCount = 0;
        
        foreach (var button in allButtons)
        {
            if (button != null && button.gameObject != null && 
                (button.name.StartsWith("SimpleNFT_Button_") || 
                 button.name.StartsWith("NFT_Button_") ||
                 button.name.Contains("NFTButton")))
            {
                DestroyImmediate(button.gameObject);
                cleanedCount++;
            }
        }
        
    }
    
    public void HidePanel()
    {   
        ClearSimpleNFTButtons();
        
        gameObject.SetActive(false);
    }
}
