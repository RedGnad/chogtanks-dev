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
        Debug.Log($"[NFT-PANEL] DisplayNFTItems called with {playerNFTs.Count} NFTs");
        Debug.Log($"[NFT-PANEL] Container children before creation: {nftContainer.childCount}");
        
        int itemsCreated = 0;
        foreach (var nft in playerNFTs)
        {
            Debug.Log($"[NFT-PANEL] Creating UI item #{itemsCreated + 1} for NFT #{nft.tokenId} level {nft.level}");
            CreateNFTItem(nft);
            itemsCreated++;
            Debug.Log($"[NFT-PANEL] Container children after item #{itemsCreated}: {nftContainer.childCount}");
        }
        
        Debug.Log($"[NFT-PANEL] ✅ All {playerNFTs.Count} NFT items created. Final container children: {nftContainer.childCount}");
        
        // Verify each child is properly created and active
        for (int i = 0; i < nftContainer.childCount; i++)
        {
            var child = nftContainer.GetChild(i);
            Debug.Log($"[NFT-PANEL] Child {i}: {child.name}, Active: {child.gameObject.activeInHierarchy}");
        }
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
        
        var nftImage = nftItem.transform.Find("NFTImage")?.GetComponent<Image>();
        var levelText = nftItem.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        var evolveButton = nftItem.transform.Find("EvolveButton")?.GetComponent<Button>();
        
        if (nftImage != null)
        {
            SetNFTImage(nftImage, nft.level);
        }
        
        if (levelText != null)
        {
            levelText.text = $"TANK Level {nft.level}";
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
        
        // Clear all children immediately
        for (int i = nftContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = nftContainer.GetChild(i);
            Debug.Log($"[NFT-PANEL] Destroying: {child.name}");
            Destroy(child.gameObject);
        }
        
        playerNFTs.Clear();
        Debug.Log($"[NFT-PANEL] NFT list cleared");
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
                UpdateStatus("No NFTs found - Click 'Mint NFT' to get your first tank");
                // Note: Auto-mint removed to prevent infinite recursion
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
}
