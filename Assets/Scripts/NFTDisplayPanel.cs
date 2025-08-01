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
    // External JavaScript functions
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
    [Tooltip("Container for simple NFT buttons inside the panel")]
    public Transform simpleButtonContainer;
    [Tooltip("Optional: Prefab for simple NFT buttons")]
    public GameObject simpleButtonPrefab;
    private List<UnityEngine.UI.Button> simpleNFTButtons = new List<UnityEngine.UI.Button>();
    
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
    private bool isRefreshing = false; // Protection contre les appels multiples

    private void Start()
    {
        Debug.Log("[NFT-PANEL] NFTDisplayPanel Start() called");
        
        // Connecter au NFTManager existant dans la scène
        nftManager = FindObjectOfType<ChogTanksNFTManager>();
        if (nftManager != null)
        {
            Debug.Log("[NFT-PANEL] ✅ NFTManager trouvé et connecté");
        }
        else
        {
            Debug.LogWarning("[NFT-PANEL] ⚠️ NFTManager non trouvé dans la scène");
        }
        
        // NETTOYER LES BOUTONS NFT EXISTANTS AU DÉMARRAGE
        CleanupAllSimpleNFTButtons();
        
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshNFTList);
            Debug.Log("[NFT-PANEL] Refresh button listener added");
        }
        else
        {
            Debug.LogWarning("[NFT-PANEL] Refresh button is null!");
        }
        
        gameObject.SetActive(false);
    }
    
    public void ShowPanel(string walletAddress)
    {
        Debug.Log($"[NFT-PANEL] ShowPanel called with wallet: {walletAddress}");
        
        currentWalletAddress = walletAddress;
        gameObject.SetActive(true);
        
        // NETTOYER LES ANCIENS BOUTONS NFT AVANT D'AFFICHER LE PANEL
        CleanupAllSimpleNFTButtons();
        
        // Auto-refresh when panel is shown
        RefreshNFTList();
    }
    
    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
    
    public async void RefreshNFTList()
    {
        // TOUJOURS récupérer la dernière adresse wallet (comme un refresh de page)
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
        
        // Protection contre les appels multiples simultanés
        if (isRefreshing)
        {
            Debug.Log("[NFT-PANEL] RefreshNFTList already in progress, skipping duplicate call");
            return;
        }
        
        isRefreshing = true;
        
        try
        {
            Debug.Log("[NFT-PANEL] Clearing existing NFT list and loading new data");
            
            // Réinitialiser le status text pour le nouveau wallet
            UpdateStatus("Loading NFTs...");
            Debug.Log($"[NFT-PANEL] Status reset for wallet: {currentWalletAddress}");
            
            // Nettoyer les boutons NFT simples pour assurer la cohérence avec le nouveau wallet
            ClearSimpleNFTButtons();
            
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
        
        // � VALIDATION CRITIQUE DU CONTAINER
        Debug.Log($"[NFT-PANEL] 🔍 === CONTAINER VALIDATION ===");
        Debug.Log($"[NFT-PANEL] Container name: {nftContainer.name}");
        Debug.Log($"[NFT-PANEL] Container children BEFORE clear: {nftContainer.childCount}");
        
        // Lister tous les enfants existants
        for (int i = 0; i < nftContainer.childCount; i++)
        {
            var child = nftContainer.GetChild(i);
            Debug.LogWarning($"[NFT-PANEL] ⚠️ BLOCKING ELEMENT FOUND: '{child.name}' - This will be REMOVED!");
        }
        
        // �🔍 DIAGNOSTIC COMPLET
        if (!DiagnoseDisplaySetup())
        {
            Debug.LogError("[NFT-PANEL] ❌ Display setup invalid, using fallback method");
            DisplayNFTItemsFallback();
            return;
        }
        
        Debug.Log($"[NFT-PANEL] Container children before creation: {nftContainer.childCount}");
        
        // 🧹 NETTOYER LES ANCIENS ÉLÉMENTS (CRITIQUE!)
        Debug.Log($"[NFT-PANEL] 🧹 === CLEARING CONTAINER COMPLETELY ===");
        ClearNFTList();
        
        // Vérification après nettoyage
        Debug.Log($"[NFT-PANEL] Container children AFTER clear: {nftContainer.childCount}");
        if (nftContainer.childCount > 0)
        {
            Debug.LogError($"[NFT-PANEL] ❌ CRITICAL: Container still has {nftContainer.childCount} children after clear!");
            Debug.LogError($"[NFT-PANEL] ❌ Manual elements in Inspector are BLOCKING dynamic content!");
            
            // Force clear tout
            for (int i = nftContainer.childCount - 1; i >= 0; i--)
            {
                var child = nftContainer.GetChild(i);
                Debug.LogWarning($"[NFT-PANEL] 🗑️ FORCE DESTROYING: {child.name}");
                DestroyImmediate(child.gameObject);
            }
        }
        
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
        
        // 🚨 DIAGNOSTIC FINAL CRITIQUE
        Debug.Log($"[NFT-PANEL] 🔍 === FINAL CONTAINER STATE ===");
        for (int i = 0; i < nftContainer.childCount; i++)
        {
            var child = nftContainer.GetChild(i);
            Debug.Log($"[NFT-PANEL] Final child {i}: {child.name}, Active: {child.gameObject.activeInHierarchy}");
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
        
        Debug.Log($"[NFT-PANEL] 🎯 Creating UI item for NFT #{nft.tokenId} level {nft.level}");
        GameObject nftItem = Instantiate(nftItemPrefab, nftContainer);
        nftItem.name = $"NFTItem_Token{nft.tokenId}_Level{nft.level}";
        
        // � FORCER L'AFFICHAGE AU PREMIER PLAN IMMÉDIATEMENT
        nftItem.SetActive(true);
        nftItem.transform.SetAsLastSibling(); // Premier plan dans le container
        
        Debug.Log($"[NFT-PANEL] ✅ GameObject created: {nftItem.name}, Active: {nftItem.activeInHierarchy}");
        
        // 🎯 CHOISIR CLAIREMENT LES ÉLÉMENTS À AFFICHER
        Debug.Log($"[NFT-PANEL] 🔍 Configuring display elements...");
        
        // Chercher les composants principaux
        var nftImage = nftItem.transform.Find("NFTImage")?.GetComponent<Image>();
        var levelText = nftItem.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        var evolveButton = nftItem.transform.Find("EvolveButton")?.GetComponent<Button>();
        
        Debug.Log($"[NFT-PANEL] � Elements found - Image: {(nftImage != null ? "✅" : "❌")}, Text: {(levelText != null ? "✅" : "❌")}, Button: {(evolveButton != null ? "✅" : "❌")}");
        
        // 🎨 CONFIGURER L'IMAGE NFT
        if (nftImage != null)
        {
            SetNFTImage(nftImage, nft.level);
            nftImage.gameObject.SetActive(true);
            Debug.Log($"[NFT-PANEL] ✅ NFT Image configured and activated");
        }
        else
        {
            Debug.LogWarning($"[NFT-PANEL] ⚠️ NFTImage not found - element will have no image");
        }
        
        // 📝 CONFIGURER LE TEXTE DE NIVEAU
        if (levelText != null)
        {
            levelText.text = $"TANK #{nft.tokenId}\nLevel {nft.level}";
            levelText.gameObject.SetActive(true);
            levelText.color = Color.white;
            levelText.fontSize = 16;
            Debug.Log($"[NFT-PANEL] ✅ Level text configured: '{levelText.text}'");
        }
        else
        {
            Debug.LogWarning($"[NFT-PANEL] ⚠️ LevelText not found - element will have no text");
        }
        
        // 🔘 CONFIGURER LE BOUTON D'ÉVOLUTION
        if (evolveButton != null)
        {
            evolveButton.gameObject.SetActive(true);
            evolveButton.interactable = nft.canEvolve;
            evolveButton.onClick.RemoveAllListeners();
            evolveButton.onClick.AddListener(() => {
                Debug.Log($"[NFT-PANEL] 🎯 Evolution button clicked for NFT #{nft.tokenId}!");
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
                Debug.Log($"[NFT-PANEL] ✅ Button configured: '{buttonText.text}'");
            }
        }
        else
        {
            Debug.LogWarning($"[NFT-PANEL] ⚠️ EvolveButton not found - element will have no button");
        }
        
        // 🎯 FORCER LA TAILLE ET POSITION POUR VISIBILITÉ
        var rectTransform = nftItem.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 🚨 WEBGL FIX: Position absolue et taille forcée
            #if UNITY_WEBGL && !UNITY_EDITOR
            Vector2 webglPosition = new Vector2(50, 400 - (nftContainer.childCount * 100)); // Position absolue visible
            Vector2 webglSize = new Vector2(350, 80); // Taille plus grande pour WebGL
            
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.anchoredPosition = webglPosition;
            rectTransform.sizeDelta = webglSize;
            
            Debug.Log($"[NFT-PANEL] 🚨 WEBGL FIX: Element positioned at ABSOLUTE {webglPosition} with size {webglSize}");
            #else
            // Position normale pour Editor
            rectTransform.anchoredPosition = new Vector2(0, -(nftContainer.childCount * 160));
            rectTransform.sizeDelta = new Vector2(200, 150);
            Debug.Log($"[NFT-PANEL] ✅ Element positioned at (0, {-(nftContainer.childCount * 160)}) with size 200x150");
            #endif
        }
        
        Debug.Log($"[NFT-PANEL] 🎉 NFT #{nft.tokenId} FULLY CONFIGURED AND VISIBLE!");
        Debug.Log($"[NFT-PANEL] 📍 Position: {rectTransform.anchoredPosition}, Size: {rectTransform.sizeDelta}, Active: {nftItem.activeInHierarchy}");
        Debug.Log($"[NFT-PANEL] 🔢 Container now has {nftContainer.childCount} children");
        
        // 🚨 WEBGL FIX: Forcer le refresh du Canvas après chaque création
        #if UNITY_WEBGL && !UNITY_EDITOR
        StartCoroutine(ForceWebGLCanvasRefresh());
        #endif
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
        // ✅ CORRECTION : Coûts alignés sur le contrat ChogTanksNFTv2
        var costs = new Dictionary<uint, uint>
        {
            {1, 2},   // Level 1→2 = 2 points
            {2, 100}, // Level 2→3 = 100 points
            {3, 200}, // Level 3→4 = 200 points
            {4, 300}, // Level 4→5 = 300 points
            {5, 400}, // Level 5→6 = 400 points
            {6, 500}, // Level 6→7 = 500 points
            {7, 600}, // Level 7→8 = 600 points
            {8, 700}, // Level 8→9 = 700 points
            {9, 800}  // Level 9→10 = 800 points
        };
        
        return costs.ContainsKey(currentLevel) ? costs[currentLevel] : 0;
    }
    
    private void ClearNFTList()
    {
        Debug.Log($"[NFT-PANEL] Clearing {nftContainer.childCount} existing NFT items (protecting simple buttons)");
        
        for (int i = nftContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = nftContainer.GetChild(i);
            
            // PROTECTION: Ne pas détruire les boutons NFT simples
            if (child.name.StartsWith("SimpleNFT_Button_"))
            {
                Debug.Log($"[NFT-PANEL] 🔒 PROTECTING simple NFT button: {child.name}");
                continue; // Garder ce bouton
            }
            
            Debug.Log($"[NFT-PANEL] Destroying: {child.name}");
            Destroy(child.gameObject);
        }
        
        Debug.Log($"[NFT-PANEL] UI elements cleared (playerNFTs and simple buttons preserved)");
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
                
                // CRÉER LES BOUTONS NFT SIMPLES DANS LE PANEL
                CreateSimpleNFTButtonsInPanel(allNFTs.Count);
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
            Debug.LogError($"[AUTO-MINT] Error parsing Firebase response: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Déclenche le mint automatique via appel direct
    /// </summary>
    private void TriggerAutoMint()
    {
        string walletAddress = PlayerPrefs.GetString("walletAddress", "");
        if (string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogError("[AUTO-MINT] No wallet address found");
            UpdateStatus("Error: No wallet connected");
            return;
        }
        
        Debug.Log($"[AUTO-MINT] Triggering automatic mint for wallet: {walletAddress}");
        
        // NOTE: Le champ hasMintedNFT sera automatiquement mis à true dans Firebase
        // par MarkMintSuccessJS() quand le mint réussira
        Debug.Log($"[AUTO-MINT] hasMintedNFT will be set to true in Firebase upon successful mint");
        
        // Appel direct sans dépendance au NFTManager
#if UNITY_WEBGL && !UNITY_EDITOR
        DirectMintNFTJS(walletAddress);
#else
        Debug.Log("[AUTO-MINT] Direct mint call (Editor mode)");
#endif
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
    
    private IEnumerator ForceWebGLCanvasRefresh()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
    }
    
    // ========== BOUTONS NFT SIMPLES DANS LE PANEL ==========
    
    private void CreateSimpleNFTButtonsInPanel(int nftCount)
    {
        Debug.Log($"[NFT-PANEL] 🎯 Creating {nftCount} simple NFT buttons inside panel");
        
        ClearSimpleNFTButtons();
        
        if (simpleButtonContainer == null)
        {
            Debug.LogWarning("[NFT-PANEL] ⚠️ simpleButtonContainer is null - using nftContainer as fallback");
            // Utiliser nftContainer comme fallback si simpleButtonContainer n'est pas assigné
            if (nftContainer != null)
            {
                CreateSimpleButtonsInContainer(nftContainer, nftCount);
            }
            return;
        }
        
        CreateSimpleButtonsInContainer(simpleButtonContainer, nftCount);
        
        Debug.Log($"[NFT-PANEL] ✅ Created {simpleNFTButtons.Count} simple NFT buttons in panel");
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
        
        // Utiliser le prefab si disponible, sinon créer un bouton basique
        if (simpleButtonPrefab != null)
        {
            Debug.Log($"[NFT-PANEL] 🎨 Using prefab for simple NFT #{nftIndex}");
            buttonObj = Instantiate(simpleButtonPrefab, container);
            buttonObj.name = $"SimpleNFT_Button_{nftIndex}"; // NOM IMPORTANT pour la protection
        }
        else
        {
            Debug.Log($"[NFT-PANEL] 🔧 Creating basic simple button for NFT #{nftIndex}");
            buttonObj = CreateBasicSimpleButton(container, nftIndex);
        }
        
        // Configurer le bouton
        var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
        if (button == null)
        {
            button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        }
        
        // Personnaliser le texte
        CustomizeSimpleButtonText(buttonObj, nftIndex);
        
        // Positionner le bouton dans le panel
        PositionSimpleButton(buttonObj, nftIndex);
        
        // Ajouter l'action de clic
        int tokenIndex = nftIndex;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSimpleNFTButtonClickedInPanel(tokenIndex));
        
        simpleNFTButtons.Add(button);
        
        Debug.Log($"[NFT-PANEL] ✅ Simple NFT button #{nftIndex} created in panel");
    }
    
    private GameObject CreateBasicSimpleButton(Transform container, int nftIndex)
    {
        GameObject buttonObj = new GameObject($"SimpleNFT_Button_{nftIndex}"); // NOM IMPORTANT
        buttonObj.transform.SetParent(container, false);
        
        // Ajouter les composants de base
        var button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        var image = buttonObj.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.2f, 0.8f, 0.2f, 0.9f); // Vert vif pour distinguer
        
        // Créer le texte
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = $"NFT #{nftIndex}";
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        // Configurer le RectTransform du texte
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        return buttonObj;
    }
    
    private void CustomizeSimpleButtonText(GameObject buttonObj, int nftIndex)
    {
        // Récupérer le vrai tokenId du NFT au lieu d'utiliser l'index séquentiel
        if (nftIndex <= 0 || nftIndex > playerNFTs.Count)
        {
            Debug.LogError($"[NFT-PANEL] ❌ Invalid nftIndex {nftIndex} for {playerNFTs.Count} NFTs");
            return;
        }
        
        var nft = playerNFTs[nftIndex - 1]; // Convertir index 1-based vers 0-based
        uint realTokenId = nft.tokenId;
        int nftLevel = (int)nft.level; // Cast explicite uint → int
        
        // Texte avec tokenId ET niveau pour faciliter le debug
        string buttonText = $"NFT #{realTokenId}\nLvl {nftLevel}";
        
        // Chercher TextMeshProUGUI dans le bouton
        var textComponents = buttonObj.GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents.Length > 0)
        {
            textComponents[0].text = buttonText;
            Debug.Log($"[NFT-PANEL] 📝 Updated simple button text to '{buttonText}' (tokenId + level)");
        }
        else
        {
            // Fallback pour Text legacy
            var legacyText = buttonObj.GetComponentsInChildren<UnityEngine.UI.Text>();
            if (legacyText.Length > 0)
            {
                legacyText[0].text = buttonText;
                Debug.Log($"[NFT-PANEL] 📝 Updated simple button legacy text to '{buttonText}' (tokenId + level)");
            }
        }
    }
    
    private void PositionSimpleButton(GameObject buttonObj, int nftIndex)
    {
        var rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Position dans le panel - layout horizontal avec espacement amélioré
            rectTransform.sizeDelta = new Vector2(120, 40); // Taille normale
            rectTransform.anchoredPosition = new Vector2((nftIndex - 1) * 280, -50); // Espacement augmenté
            
            Debug.Log($"[NFT-PANEL] 📍 Positioned simple NFT #{nftIndex} at {rectTransform.anchoredPosition} in panel");
        }
    }
    
    private void ClearSimpleNFTButtons()
    {
        Debug.Log($"[NFT-PANEL] 🧹 Clearing {simpleNFTButtons.Count} existing simple NFT buttons");
        
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
        // Récupérer le vrai tokenId du NFT cliqué
        if (nftIndex <= 0 || nftIndex > playerNFTs.Count)
        {
            Debug.LogError($"[NFT-PANEL] ❌ Invalid nftIndex {nftIndex} for {playerNFTs.Count} NFTs");
            return;
        }
        
        var selectedNFT = playerNFTs[nftIndex - 1]; // Convertir index 1-based vers 0-based
        uint realTokenId = selectedNFT.tokenId;
        
        Debug.Log($"[NFT-PANEL] 🖱️ Simple NFT #{realTokenId} button clicked in panel (tokenId={realTokenId}, level={selectedNFT.level})");
        
        // Action : sélectionner le NFT et mettre à jour le statut
        UpdateStatus($"Selected NFT #{realTokenId} (Level {selectedNFT.level}) for evolution");
        
        // Déclencher l'évolution directement pour CE tokenId spécifique
        Debug.Log($"[NFT-PANEL] 🎯 Triggering evolution for NFT #{realTokenId} (Level {selectedNFT.level} → {selectedNFT.level + 1})");
        EvolveNFT(realTokenId, selectedNFT.level + 1);
    }
    
    // ========== NETTOYAGE GLOBAL DES BOUTONS NFT ==========
    
    private void CleanupAllSimpleNFTButtons()
    {
        Debug.Log($"[NFT-PANEL] 🧹 CLEANUP: Searching for ALL simple NFT buttons in scene to clean up");
        
        // Nettoyer les boutons dans notre liste
        ClearSimpleNFTButtons();
        
        // Chercher et nettoyer TOUS les boutons NFT dans la scène (déchets)
        var allButtons = FindObjectsOfType<UnityEngine.UI.Button>(true);
        int cleanedCount = 0;
        
        foreach (var button in allButtons)
        {
            if (button != null && button.gameObject != null && 
                (button.name.StartsWith("SimpleNFT_Button_") || 
                 button.name.StartsWith("NFT_Button_") ||
                 button.name.Contains("NFTButton")))
            {
                Debug.Log($"[NFT-PANEL] 🗑️ CLEANUP: Destroying leftover NFT button: {button.name}");
                DestroyImmediate(button.gameObject);
                cleanedCount++;
            }
        }
        
        Debug.Log($"[NFT-PANEL] ✅ CLEANUP: Removed {cleanedCount} leftover NFT buttons from scene");
    }
    
    public void HidePanel()
    {
        Debug.Log($"[NFT-PANEL] HidePanel called - cleaning up NFT buttons");
        
        // Nettoyer les boutons quand le panel se ferme
        ClearSimpleNFTButtons();
        
        gameObject.SetActive(false);
    }
}
