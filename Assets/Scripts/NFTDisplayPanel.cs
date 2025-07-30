using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.InteropServices;

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
    [Header("UI References")]
    public Transform nftContainer;
    public TextMeshProUGUI statusText;
    public Button refreshButton;
    
    [Header("NFT Item Prefab (Simple)")]
    public GameObject nftItemPrefab;
    
    private string currentWalletAddress;
    private List<NFTDisplayItem> playerNFTs = new List<NFTDisplayItem>();
    private ChogTanksNFTManager nftManager;
    private bool isRefreshing = false; // Protection contre les appels multiples

    void Start()
    {
        nftManager = FindObjectOfType<ChogTanksNFTManager>();
        
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshNFTList);
            
        gameObject.SetActive(false);
    }
    
    public void ShowPanel(string walletAddress)
    {
        Debug.Log($"[NFT-PANEL] ShowPanel called with wallet: {walletAddress}");
        currentWalletAddress = walletAddress;
        gameObject.SetActive(true);
        RefreshNFTList();
    }
    
    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
    
    public async void RefreshNFTList()
    {
        Debug.Log($"[NFT-PANEL] RefreshNFTList started for wallet: {currentWalletAddress}");
        
        // Protection contre les appels multiples simultanés
        if (isRefreshing)
        {
            Debug.Log("[NFT-PANEL] RefreshNFTList already in progress, skipping duplicate call");
            return;
        }
        
        if (string.IsNullOrEmpty(currentWalletAddress))
        {
            Debug.LogWarning("[NFT-PANEL] No wallet address provided - checking PlayerPrefs fallback");
            string savedWallet = PlayerPrefs.GetString("walletAddress", "");
            if (!string.IsNullOrEmpty(savedWallet))
            {
                Debug.Log($"[NFT-PANEL] Using fallback wallet from PlayerPrefs: {savedWallet}");
                currentWalletAddress = savedWallet;
            }
            else
            {
                Debug.LogError("[NFT-PANEL] No wallet available in PlayerPrefs either");
                UpdateStatus("No wallet connected");
                return;
            }
        }
        
        isRefreshing = true;
        
        try
        {
            Debug.Log("[NFT-PANEL] Clearing existing NFT list and loading new data");
            UpdateStatus("Loading NFTs...");
            ClearNFTList();
            
            await GetAllNFTsFromBlockchain(currentWalletAddress);
            Debug.Log("[NFT-PANEL] RefreshNFTList completed");
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
            Debug.LogError($"[NFTPanel] Error parsing NFT data: {ex.Message}");
            UpdateStatus("Error loading NFTs");
        }
    }
    
    private void DisplayNFTItems()
    {
        Debug.Log($"[NFT-PANEL] 🎯 DisplayNFTItems called with {playerNFTs.Count} NFTs");
        
        // 🔍 DEBUG CRITIQUE: Vérifier l'état de playerNFTs
        if (playerNFTs == null)
        {
            Debug.LogError("[NFT-PANEL] ❌ CRITICAL: playerNFTs is NULL!");
            return;
        }
        
        Debug.Log($"[NFT-PANEL] 🔍 playerNFTs.Count = {playerNFTs.Count}");
        for (int i = 0; i < playerNFTs.Count; i++)
        {
            Debug.Log($"[NFT-PANEL] 🔍 playerNFTs[{i}]: Token #{playerNFTs[i].tokenId}, Level {playerNFTs[i].level}");
        }
        
        // 🔍 DIAGNOSTIC COMPLET
        if (!DiagnoseDisplaySetup())
        {
            Debug.LogError("[NFT-PANEL] ❌ Display setup invalid, using fallback method");
            DisplayNFTItemsFallback();
            return;
        }
        
        Debug.Log($"[NFT-PANEL] Container children before creation: {nftContainer.childCount}");
        
        // 🧹 NETTOYER LES ANCIENS ÉLÉMENTS
        ClearNFTList();
        
        int itemsCreated = 0;
        foreach (var nft in playerNFTs)
        {
            Debug.Log($"[NFT-PANEL] 📦 Creating UI item #{itemsCreated + 1} for NFT #{nft.tokenId} level {nft.level}");
            
            try
            {
                CreateNFTItem(nft);
                itemsCreated++;
                Debug.Log($"[NFT-PANEL] ✅ Item #{itemsCreated} created successfully. Container children: {nftContainer.childCount}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NFT-PANEL] ❌ Failed to create item for NFT #{nft.tokenId}: {ex.Message}");
            }
        }
        
        Debug.Log($"[NFT-PANEL] 🎉 FINAL RESULT: {itemsCreated}/{playerNFTs.Count} NFT items created. Container children: {nftContainer.childCount}");
        
        // 🔍 VÉRIFICATION FINALE
        VerifyCreatedItems();
    }
    
    private void CreateNFTItem(NFTDisplayItem nft)
    {
        if (nftItemPrefab == null || nftContainer == null)
        {
            Debug.LogError("[NFT-PANEL] Missing prefab or container references");
            return;
        }
        
        Debug.Log($"[NFT-PANEL] Creating UI item for NFT #{nft.tokenId} level {nft.level}");
        GameObject nftItem = Instantiate(nftItemPrefab, nftContainer);
        nftItem.name = $"NFTItem_Token{nft.tokenId}_Level{nft.level}";
        Debug.Log($"[NFT-PANEL] Created GameObject: {nftItem.name}, Active: {nftItem.activeInHierarchy}");
        
        // 🔍 DEBUG: Afficher la structure réelle du prefab
        Debug.Log($"[NFT-PANEL] 🔍 PREFAB STRUCTURE DEBUG:");
        Debug.Log($"[NFT-PANEL] Prefab has {nftItem.transform.childCount} children:");
        for (int i = 0; i < nftItem.transform.childCount; i++)
        {
            var child = nftItem.transform.GetChild(i);
            Debug.Log($"[NFT-PANEL] Child {i}: '{child.name}' (Type: {child.GetType().Name})");
            
            // Vérifier les composants sur chaque enfant
            var image = child.GetComponent<Image>();
            var text = child.GetComponent<TextMeshProUGUI>();
            var button = child.GetComponent<Button>();
            
            if (image != null) Debug.Log($"[NFT-PANEL]   → Has Image component");
            if (text != null) Debug.Log($"[NFT-PANEL]   → Has TextMeshProUGUI component");
            if (button != null) Debug.Log($"[NFT-PANEL]   → Has Button component");
        }
        
        var nftImage = nftItem.transform.Find("NFTImage")?.GetComponent<Image>();
        var levelText = nftItem.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        var evolveButton = nftItem.transform.Find("EvolveButton")?.GetComponent<Button>();
        
        Debug.Log($"[NFT-PANEL] 🔍 SEARCH RESULTS:");
        Debug.Log($"[NFT-PANEL] NFTImage found: {(nftImage != null ? "YES" : "NO")}");
        Debug.Log($"[NFT-PANEL] LevelText found: {(levelText != null ? "YES" : "NO")}");
        Debug.Log($"[NFT-PANEL] EvolveButton found: {(evolveButton != null ? "YES" : "NO")}");
        
        if (nftImage != null)
        {
            SetNFTImage(nftImage, nft.level);
        }
        
        if (levelText != null)
        {
            levelText.text = $"TANK Level {nft.level}";
            Debug.Log($"[NFT-PANEL] ✅ Level text set to: 'TANK Level {nft.level}'");
        }
        else
        {
            Debug.LogWarning("[NFT-PANEL] ⚠️ Could not find LevelText - element will be blank!");
        }
        
        if (evolveButton != null)
        {
            evolveButton.interactable = nft.canEvolve;
            evolveButton.onClick.AddListener(() => EvolveNFT(nft.tokenId, nft.level + 1));
            
            var buttonText = evolveButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (nft.canEvolve)
                {
                    buttonText.text = $"EVOLVE → Level {nft.level + 1}\n({nft.evolutionCost} pts)";
                }
                else
                {
                    buttonText.text = "MAX LEVEL";
                }
            }
        }
        else
        {
            Debug.LogWarning("[NFT-PANEL] ⚠️ Could not find EvolveButton - no button will work!");
        }
        
        // 🔧 FORCER LA VISIBILITÉ ET LA TAILLE
        nftItem.SetActive(true);
        var rectTransform = nftItem.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // FORCER une taille visible
            rectTransform.sizeDelta = new Vector2(200, 150);
            Debug.Log($"[NFT-PANEL] ✅ Forced size to 200x150");
            
            // FORCER l'ancrage pour éviter la superposition
            rectTransform.anchoredPosition = new Vector2(0, -160 * (nft.tokenId - 1)); // Espacement vertical
            Debug.Log($"[NFT-PANEL] ✅ Forced position to (0, {-160 * (nft.tokenId - 1)})");
        }
        
        // 🔧 ALTERNATIVE: Si le prefab a un LayoutElement, le configurer
        var layoutElement = nftItem.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = 200;
            layoutElement.preferredHeight = 150;
            Debug.Log($"[NFT-PANEL] ✅ Set LayoutElement preferred size to 200x150");
        }
        else
        {
            Debug.LogWarning("[NFT-PANEL] ⚠️ No LayoutElement found - adding one");
            layoutElement = nftItem.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 200;
            layoutElement.preferredHeight = 150;
        }
    }
    
    private void EvolveNFT(uint tokenId, uint targetLevel)
    {
        Debug.Log($"[NFT-PANEL] EvolveNFT called for token #{tokenId} to level {targetLevel}");
        
        if (nftManager != null)
        {
            Debug.Log($"[NFT-PANEL] NFTManager found, setting selectedTokenId to {tokenId}");
            nftManager.selectedTokenId = (int)tokenId;
            
            Debug.Log($"[NFT-PANEL] Calling RequestEvolutionForSelectedNFT for token #{tokenId}");
            nftManager.RequestEvolutionForSelectedNFT();
            
            Debug.Log($"[NFT-PANEL] Closing panel after evolution request");
            ClosePanel();
        }
        else
        {
            Debug.LogError($"[NFT-PANEL] NFTManager is null, cannot evolve NFT #{tokenId}");
        }
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
            {1, 100}, {2, 200}, {3, 300}, {4, 400}, {5, 500},
            {6, 600}, {7, 700}, {8, 800}, {9, 900}
        };
        
        return costs.ContainsKey(currentLevel) ? costs[currentLevel] : 0;
    }
    
    private void ClearNFTList()
    {
        Debug.Log($"[NFT-PANEL] Clearing {nftContainer.childCount} existing NFT items");
        
        for (int i = nftContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = nftContainer.GetChild(i);
            Debug.Log($"[NFT-PANEL] Destroying: {child.name}");
            Destroy(child.gameObject);
        }
        
        Debug.Log($"[NFT-PANEL] UI elements cleared (playerNFTs preserved)");
    }
    
    private void UpdateStatus(string message)
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
            
            RefreshNFTList();
        }
    }
    
    private async System.Threading.Tasks.Task GetAllNFTsFromBlockchain(string walletAddress)
    {
        try
        {
            Debug.Log($"[NFT-LIST] ===== DÉBUT RÉCUPÉRATION NFTs =====" );
            Debug.Log($"[NFT-LIST] Wallet address: {walletAddress}");
            Debug.Log($"[NFT-LIST] AppKit initialized: {Reown.AppKit.Unity.AppKit.IsInitialized}");
            Debug.Log($"[NFT-LIST] Account connected: {Reown.AppKit.Unity.AppKit.IsAccountConnected}");
            
            if (!Reown.AppKit.Unity.AppKit.IsInitialized || !Reown.AppKit.Unity.AppKit.IsAccountConnected)
            {
                Debug.LogError("[NFT-LIST] AppKit not initialized or account not connected");
                UpdateStatus("Wallet not connected");
                return;
            }
            
            var contractAddresses = new string[]
            {
                "0x68c582651d709f6e2b6113c01d69443f8d27e30d"
            };
            
            Debug.Log($"[NFT-LIST] Checking {contractAddresses.Length} contracts for NFTs");
            
            playerNFTs.Clear();
            Debug.Log($"[NFT-LIST] Cleared previous NFT data");
            
            var allNFTs = new List<NFTDisplayItem>();
            
            foreach (var contractAddr in contractAddresses)
            {
                try
                {
                    Debug.Log($"[NFT-LIST] ----- Checking contract: {contractAddr} -----");
                
                string balanceAbi = "function balanceOf(address) view returns (uint256)";
                Debug.Log($"[NFT-LIST] Calling balanceOf for wallet {walletAddress}");
                    
                    var balance = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
                        contractAddr,
                        balanceAbi,
                        "balanceOf",
                        new object[] { walletAddress }
                    );
                    
                    Debug.Log($"[NFT-LIST] ✓ Balance result: {balance} NFTs found in contract {contractAddr}");
                    
                    if (balance > 0)
                    {
                        Debug.Log($"[NFT-LIST] Found {balance} NFTs in contract, enumerating tokens...");
                        
                        string tokenByIndexAbi = "function tokenOfOwnerByIndex(address owner, uint256 index) view returns (uint256)";
                        string getLevelAbi = "function getLevel(uint256 tokenId) view returns (uint256)";
                        
                        for (int i = 0; i < balance; i++)
                        {
                            try
                            {
                                Debug.Log($"[NFT-LIST] Getting token at index {i}/{balance-1}");
                                
                                var tokenId = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
                                    contractAddr,
                                    tokenByIndexAbi,
                                    "tokenOfOwnerByIndex",
                                    new object[] { walletAddress, i }
                                );
                                
                                Debug.Log($"[NFT-LIST] ✓ TokenId at index {i}: {tokenId}");
                                
                                if (tokenId > 0)
                                {
                                    Debug.Log($"[NFT-LIST] Reading level for token #{tokenId}");
                                    
                                    int level = 1; // Default level for NFTs without getLevel function
                                    
                                    try
                                    {
                                        level = await Reown.AppKit.Unity.AppKit.Evm.ReadContractAsync<int>(
                                            contractAddr,
                                            getLevelAbi,
                                            "getLevel",
                                            new object[] { tokenId }
                                        );
                                        
                                        Debug.Log($"[NFT-LIST] ✓ NFT #{tokenId} has level {level}");
                                    }
                                    catch (System.Exception levelError)
                                    {
                                        Debug.LogWarning($"[NFT-LIST] ⚠️ Contract {contractAddr} doesn't have getLevel function, assuming level 1 for token #{tokenId}");
                                        Debug.LogWarning($"[NFT-LIST] getLevel error: {levelError.Message}");
                                    }
                                    
                                    var evolutionCost = GetEvolutionCost((uint)level);
                                    Debug.Log($"[NFT-LIST] Evolution cost for level {level}: {evolutionCost} points");
                                    
                                    allNFTs.Add(new NFTDisplayItem
                                    {
                                        tokenId = (uint)tokenId,
                                        level = (uint)level,
                                        canEvolve = level < 10,
                                        evolutionCost = evolutionCost
                                    });
                                    
                                    Debug.Log($"[NFT-LIST] ✓ Added NFT #{tokenId} to collection (level: {level}, canEvolve: {level < 10})");
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
            
            Debug.Log($"[NFT-LIST] ===== RÉSUMÉ FINAL =====");
            Debug.Log($"[NFT-LIST] Total NFTs found: {allNFTs.Count}");
            
            for (int i = 0; i < allNFTs.Count; i++)
            {
                var nft = allNFTs[i];
                Debug.Log($"[NFT-LIST] NFT {i+1}: Token #{nft.tokenId}, Level {nft.level}, Can evolve: {nft.canEvolve}");
            }
            
            playerNFTs = allNFTs;
            
            if (allNFTs.Count == 0)
            {
                Debug.Log($"[NFT-LIST] No NFTs found for wallet {walletAddress}");
                
                // 🎯 AUTO-MINT LOGIC: Si 0 NFT + jamais minté → mint automatique
                Debug.Log($"[NFT-LIST] 🎆 AUTO-MINT: No NFTs found, checking Firebase for mint history...");
                UpdateStatus("No NFTs found - Checking mint history...");
                CheckAutoMintEligibility(walletAddress);
            }
            else
            {
                Debug.Log($"[NFT-LIST] Displaying {allNFTs.Count} NFT items in UI");
                DisplayNFTItems();
                UpdateStatus($"Found {allNFTs.Count} NFTs");
            }
            
            Debug.Log($"[NFT-LIST] ===== FIN RÉCUPÉRATION NFTs =====");
        }
        catch (System.Exception error)
        {
            Debug.LogError($"[NFT-LIST] ❌ ERREUR CRITIQUE: {error.Message}");
            Debug.LogError($"[NFT-LIST] Stack trace: {error.StackTrace}");
            UpdateStatus("Error loading NFTs");
        }
    }
    
    /// <summary>
    /// Détermine si le wallet n'a jamais minté de NFT (première fois)
    /// Utilise Firebase hasMintedNFT au lieu de PlayerPrefs
    /// </summary>
    private void CheckAutoMintEligibility(string walletAddress)
    {
        if (string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogWarning("[AUTO-MINT] ⚠️ No wallet address provided for auto-mint check");
            return;
        }
            
        Debug.Log($"[AUTO-MINT] 🔍 Checking mint history for {walletAddress} via Firebase");
        
        // ✅ NOUVEAU : Utiliser la fonction Firebase pour vérifier hasMintedNFT
#if UNITY_WEBGL && !UNITY_EDITOR
        ChogTanksNFTManager.CheckHasMintedNFTJS(walletAddress);
#else
        // Pour l'éditeur, simuler une réponse
        var simulatedResult = new {
            walletAddress = walletAddress.ToLowerInvariant(),
            hasMintedNFT = false,
            shouldAutoMint = true
        };
        OnHasMintedNFTChecked(JsonUtility.ToJson(simulatedResult));
#endif
    }
    
    /// <summary>
    /// Callback pour recevoir la réponse de CheckHasMintedNFTJS
    /// </summary>
    public void OnHasMintedNFTChecked(string jsonResponse)
    {
        try
        {
            Debug.Log($"[AUTO-MINT] 📨 Received Firebase response: {jsonResponse}");
            
            var response = JsonUtility.FromJson<AutoMintCheckResponse>(jsonResponse);
            
            Debug.Log($"[AUTO-MINT] 📊 Wallet: {response.walletAddress}");
            Debug.Log($"[AUTO-MINT] 📊 Has minted before: {response.hasMintedNFT}");
            Debug.Log($"[AUTO-MINT] 📊 Should auto-mint: {response.shouldAutoMint}");
            
            if (response.shouldAutoMint && playerNFTs.Count == 0)
            {
                Debug.Log($"[AUTO-MINT] ✅ Conditions met: No NFTs found + Never minted before = AUTO-MINT!");
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
            Debug.LogError($"[AUTO-MINT] ❌ Error parsing Firebase response: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Déclenche le mint automatique via ChogTanksNFTManager
    /// </summary>
    private void TriggerAutoMint()
    {
        if (nftManager == null)
        {
            Debug.LogError("[AUTO-MINT] ❌ NFTManager not found, cannot trigger auto-mint");
            UpdateStatus("Error: NFT Manager not available");
            return;
        }
        
        Debug.Log($"[AUTO-MINT] 🚀 Triggering automatic mint via NFTManager.RequestMintNFT()");
        
        // 📝 NOTE: Le champ hasMintedNFT sera automatiquement mis à true dans Firebase
        // par MarkMintSuccessJS() quand le mint réussira (dans OnMintTransactionSuccess)
        Debug.Log($"[AUTO-MINT] 📝 hasMintedNFT will be set to true in Firebase upon successful mint");
        
        // Utiliser la fonction existante et fonctionnelle
        nftManager.RequestMintNFT();
    }
    
    /// <summary>
    /// Diagnostic complet de la configuration d'affichage
    /// </summary>
    private bool DiagnoseDisplaySetup()
    {
        Debug.Log($"[NFT-PANEL] 🔍 === DIAGNOSTIC DISPLAY SETUP ===");
        
        bool isValid = true;
        
        // Vérifier le container
        if (nftContainer == null)
        {
            Debug.LogError("[NFT-PANEL] ❌ nftContainer is NULL - assign it in Inspector!");
            isValid = false;
        }
        else
        {
            Debug.Log($"[NFT-PANEL] ✅ nftContainer found: {nftContainer.name}");
            Debug.Log($"[NFT-PANEL] Container type: {nftContainer.GetType().Name}");
            Debug.Log($"[NFT-PANEL] Container active: {nftContainer.gameObject.activeInHierarchy}");
        }
        
        // Vérifier le prefab
        if (nftItemPrefab == null)
        {
            Debug.LogError("[NFT-PANEL] ❌ nftItemPrefab is NULL - assign it in Inspector!");
            isValid = false;
        }
        else
        {
            Debug.Log($"[NFT-PANEL] ✅ nftItemPrefab found: {nftItemPrefab.name}");
            Debug.Log($"[NFT-PANEL] Prefab active: {nftItemPrefab.activeInHierarchy}");
            
            // Vérifier les composants du prefab
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
        
        Debug.Log($"[NFT-PANEL] 🎯 Diagnostic result: {(isValid ? "VALID" : "INVALID")}");
        return isValid;
    }
    
    /// <summary>
    /// Méthode de fallback si l'affichage normal échoue
    /// </summary>
    private void DisplayNFTItemsFallback()
    {
        Debug.Log($"[NFT-PANEL] 🆘 FALLBACK: Creating simple text display for {playerNFTs.Count} NFTs");
        
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
    
    /// <summary>
    /// Vérification finale des éléments créés
    /// </summary>
    private void VerifyCreatedItems()
    {
        Debug.Log($"[NFT-PANEL] 🔍 === VERIFICATION FINALE ===");
        
        if (nftContainer == null)
        {
            Debug.LogError("[NFT-PANEL] ❌ Cannot verify: nftContainer is null");
            return;
        }
        
        int childCount = nftContainer.childCount;
        Debug.Log($"[NFT-PANEL] Container has {childCount} children (expected: {playerNFTs.Count})");
        
        for (int i = 0; i < childCount; i++)
        {
            var child = nftContainer.GetChild(i);
            if (child != null)
            {
                Debug.Log($"[NFT-PANEL] Child {i}: {child.name}, Active: {child.gameObject.activeInHierarchy}, Position: {child.localPosition}");
                
                // Vérifier la visibilité
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
}
