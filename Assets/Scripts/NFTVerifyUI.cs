using UnityEngine;
using TMPro;
using System.Collections;
using Sample;

public class NFTVerifyUI : MonoBehaviour
{
    [SerializeField] private NFTVerification nftVerification;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Panel de saisie de nom")]
    [SerializeField] private GameObject nameInputPanel;

    private void Start()
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
        if (nameInputPanel != null)
        {
            nameInputPanel.SetActive(false); 
        }
        var connect = FindObjectOfType<ConnectWalletButton>();
        if (connect != null)
            connect.OnPersonalSignCompleted += OnPersonalSignApproved;
        
        CheckWalletAndUpdateUI();
        InvokeRepeating(nameof(CheckWalletAndUpdateUI), 1f, 1f);
    }
    
    private void OnPersonalSignApproved()
    {
        if (nameInputPanel != null)
            nameInputPanel.SetActive(true);
    }
    
    private void CheckWalletAndUpdateUI()
    {
        bool isWalletConnected = IsWalletConnected();
        if (!isWalletConnected && nftVerification != null)
        {
            nftVerification.DisconnectWallet();
            if (nameInputPanel != null)
                nameInputPanel.SetActive(false);
        }
    }
    
    private bool IsWalletConnected()
    {
        try
        {
            if (Reown.AppKit.Unity.AppKit.IsInitialized && 
                Reown.AppKit.Unity.AppKit.IsAccountConnected && 
                Reown.AppKit.Unity.AppKit.Account != null)
            {
                string appKitAddress = Reown.AppKit.Unity.AppKit.Account.Address;
                if (!string.IsNullOrEmpty(appKitAddress))
                {
                    PlayerPrefs.SetString("walletAddress", appKitAddress);
                    return true;
                }
            }
            
            if (Reown.AppKit.Unity.AppKit.IsInitialized && !Reown.AppKit.Unity.AppKit.IsAccountConnected)
            {
                string oldPrefsAddress = PlayerPrefs.GetString("walletAddress", "");
                if (!string.IsNullOrEmpty(oldPrefsAddress))
                {
                    PlayerPrefs.DeleteKey("walletAddress");
                }
                
                try
                {
                    if (PlayerSession.IsConnected)
                    {
                        
                        var playerSessionType = typeof(PlayerSession);
                        var walletAddressField = playerSessionType.GetField("_walletAddress", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        var isConnectedField = playerSessionType.GetField("_isConnected", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        
                        if (walletAddressField != null) walletAddressField.SetValue(null, "");
                        if (isConnectedField != null) isConnectedField.SetValue(null, false);
                        
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.Log("[NFTVerifyUI] Ignorant PlayerSession car AppKit est déconnecté");
                }
                
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[NFTVerifyUI] Erreur AppKit: {ex.Message}");
        }
        
        string walletFromPrefs = PlayerPrefs.GetString("walletAddress", "");
        if (!string.IsNullOrEmpty(walletFromPrefs))
        {
            return true;
        }
        
        try
        {
            if (!Reown.AppKit.Unity.AppKit.IsInitialized && PlayerSession.IsConnected && !string.IsNullOrEmpty(PlayerSession.WalletAddress))
            {
                return true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[NFTVerifyUI] Erreur PlayerSession: {ex.Message}");
        }
        
        return false;
    }
    
    private void ShowStatus(string message, bool hideAfterDelay = false)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.gameObject.SetActive(true);
            
            if (hideAfterDelay)
            {
                StartCoroutine(HideStatusAfterDelay(3f));
            }
        }
    }
    
    private IEnumerator HideStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
    }

    public void OnVerifyButtonClick()
    {
        Debug.Log("[NFT-DEBUG] OnVerifyButtonClick appelé");
        
        if (nftVerification == null)
        {
            Debug.LogError("[NFT-DEBUG] ERREUR: NFTVerification not found!");
            if (statusText != null)
            {
                ShowStatus("Erreur: Référence manquante", true);
            }
            return;
        }

        if (!IsWalletConnected())
        {
            Debug.LogError("[NFT-DEBUG] ERREUR: Pas de wallet connecté!");
            if (statusText != null)
            {
                ShowStatus("no wallet connected", true);
            }
            return;
        }

        ShowStatus("loading...");
        
        string wallet = PlayerPrefs.GetString("walletAddress", "");
        
    }
    
    public void ClearStatus()
    {
        if (statusText != null)
        {
            statusText.text = "";
            statusText.gameObject.SetActive(false);
        }
    }
    
    public void ForceCheckWalletStatus()
    {
        CheckWalletAndUpdateUI();
    }
    
    private void OnDestroy()
    {
        var dapp = FindObjectOfType<Sample.Dapp>();
        if (dapp != null)
            dapp.OnPersonalSignCompleted -= OnPersonalSignApproved;
        
        CancelInvoke(nameof(CheckWalletAndUpdateUI));
    }
}