using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Linq;

[Serializable]
public class WhitelistedButtonRule
{
    public Button button;
    
    public TextMeshProUGUI lockedText;
    
    public string lockedMessage = "Access denied";
    
    public Color unlockedColor = Color.white;
    
    public Color lockedColor = Color.gray;
}

public class WalletWhitelistManager : MonoBehaviour
{
    [Header("Whitelist Configuration")]
    [SerializeField] private List<string> whitelistedWallets = new List<string>();
    
    [Header("Button Configuration")]
    [SerializeField] private List<WhitelistedButtonRule> buttonRules = new List<WhitelistedButtonRule>();
    
    [Header("NFT Skin Buttons")]
    [SerializeField] private string notConnectedMessage = "Connect Wallet";
    
    [SerializeField] private List<TextMeshProUGUI> nftButtonTexts = new List<TextMeshProUGUI>();
    
    [SerializeField] private List<string> originalNftButtonTexts = new List<string>();
    
    private string currentWallet = "";
    private bool isWalletConnected = false;
    
    void Start()
    {
        SaveOriginalButtonTexts();
        
        InvokeRepeating(nameof(CheckWalletStatus), 0.5f, 1.5f);
    }
    
    private void SaveOriginalButtonTexts()
    {
        originalNftButtonTexts.Clear();
        foreach (var textComponent in nftButtonTexts)
        {
            if (textComponent != null)
            {
                originalNftButtonTexts.Add(textComponent.text);
            }
            else
            {
                originalNftButtonTexts.Add("");
            }
        }
    }
    
    public void CheckWalletStatus()
    {
        bool wasConnected = isWalletConnected;
        string oldWallet = currentWallet;
        
        currentWallet = GetConnectedWallet();
        isWalletConnected = !string.IsNullOrEmpty(currentWallet);
        
        if (wasConnected != isWalletConnected || oldWallet != currentWallet)
        {
            UpdateButtonStates();
            UpdateNftButtonTexts();
        }
    }
    
    private string GetConnectedWallet()
    {
        string walletAddress = string.Empty;
        
        try
        {
            if (Reown.AppKit.Unity.AppKit.IsInitialized && 
                Reown.AppKit.Unity.AppKit.IsAccountConnected && 
                Reown.AppKit.Unity.AppKit.Account != null)
            {
                walletAddress = Reown.AppKit.Unity.AppKit.Account.Address;
                if (!string.IsNullOrEmpty(walletAddress))
                {
                    return walletAddress;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WalletWhitelist] Erreur AppKit: {ex.Message}");
        }
        
        walletAddress = PlayerPrefs.GetString("walletAddress", "");
        if (!string.IsNullOrEmpty(walletAddress))
        {
            return walletAddress;
        }
        
        try
        {
            if (PlayerSession.IsConnected && !string.IsNullOrEmpty(PlayerSession.WalletAddress))
            {
                return PlayerSession.WalletAddress;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WalletWhitelist] Erreur PlayerSession: {ex.Message}");
        }
        
        return "";
    }
    
    private bool IsWalletWhitelisted(string walletAddress)
    {
        if (string.IsNullOrEmpty(walletAddress)) return false;
        
        return whitelistedWallets.Contains(walletAddress);
    }
    
    public void UpdateButtonStates()
    {
        bool isWhitelisted = IsWalletWhitelisted(currentWallet);
        
        foreach (var rule in buttonRules)
        {
            if (rule.button == null) continue;
            
            bool unlocked = isWalletConnected && isWhitelisted;
            
            rule.button.interactable = unlocked;
            
            Image buttonImage = rule.button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = unlocked ? rule.unlockedColor : rule.lockedColor;
            }
            
            if (rule.lockedText != null)
            {
                rule.lockedText.gameObject.SetActive(!unlocked);
                rule.lockedText.text = rule.lockedMessage;
            }
        }
    }
    
    public void UpdateNftButtonTexts()
    {
        for (int i = 0; i < nftButtonTexts.Count; i++)
        {
            TextMeshProUGUI textComponent = nftButtonTexts[i];
            if (textComponent == null) continue;
            
            if (isWalletConnected)
            {
                textComponent.text = (i < originalNftButtonTexts.Count) ? originalNftButtonTexts[i] : "";
            }
            else
            {
                textComponent.text = notConnectedMessage;
            }
        }
    }
    
    public void AddWalletToWhitelist(string walletAddress)
    {
        if (string.IsNullOrEmpty(walletAddress)) return;
        
        if (!whitelistedWallets.Contains(walletAddress))
        {
            whitelistedWallets.Add(walletAddress);
            UpdateButtonStates();
        }
    }
    
    public void RemoveWalletFromWhitelist(string walletAddress)
    {
        if (whitelistedWallets.Contains(walletAddress))
        {
            whitelistedWallets.Remove(walletAddress);
            UpdateButtonStates();
        }
    }
    
    void OnDestroy()
    {
        CancelInvoke(nameof(CheckWalletStatus));
    }
}


