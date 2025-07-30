using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SimpleWalletWhitelist : MonoBehaviour
{
    [SerializeField] private List<string> whitelistedWallets = new List<string>();
    
    [SerializeField] private List<Button> restrictedButtons = new List<Button>();
    
    void Start()
    {
        UpdateButtonsState(false);
        
        InvokeRepeating(nameof(CheckWalletAccess), 0.5f, 2f);
    }
    
    public void CheckWalletAccess()
    {
        string currentWallet = GetConnectedWallet();
        bool isWhitelisted = IsWalletWhitelisted(currentWallet);
        
        UpdateButtonsState(isWhitelisted);
    }
    
    private void UpdateButtonsState(bool enabled)
    {
        foreach (Button button in restrictedButtons)
        {
            if (button != null)
            {
                button.interactable = enabled;
            }
        }
    }
    
    private string GetConnectedWallet()
    {
        string wallet = string.Empty;
        
        try 
        {
            if (Reown.AppKit.Unity.AppKit.IsInitialized && 
                Reown.AppKit.Unity.AppKit.IsAccountConnected && 
                Reown.AppKit.Unity.AppKit.Account != null)
            {
                wallet = Reown.AppKit.Unity.AppKit.Account.Address;
                if (!string.IsNullOrEmpty(wallet))
                {
                    return wallet;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Whitelist] Erreur AppKit: {ex.Message}");
        }
        
        wallet = PlayerPrefs.GetString("walletAddress", "");
        if (!string.IsNullOrEmpty(wallet))
        {
            return wallet;
        }
        
        try
        {
            if (PlayerSession.IsConnected && !string.IsNullOrEmpty(PlayerSession.WalletAddress))
            {
                return PlayerSession.WalletAddress;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Whitelist] Erreur PlayerSession: {ex.Message}");
        }
        
        return "";
    }
    
    private bool IsWalletWhitelisted(string walletAddress)
    {
        if (string.IsNullOrEmpty(walletAddress)) return false;
        
        return whitelistedWallets.Contains(walletAddress);
    }
    
    public void AddWalletToWhitelist(string walletAddress)
    {
        if (!string.IsNullOrEmpty(walletAddress) && !whitelistedWallets.Contains(walletAddress))
        {
            whitelistedWallets.Add(walletAddress);
            CheckWalletAccess();
        }
    }
    
    public void RemoveWalletFromWhitelist(string walletAddress)
    {
        if (whitelistedWallets.Remove(walletAddress))
        {
            CheckWalletAccess();
        }
    }
    
    void OnDestroy()
    {
        CancelInvoke();
    }
}
