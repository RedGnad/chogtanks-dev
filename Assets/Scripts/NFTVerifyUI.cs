using UnityEngine;
using TMPro;
using System.Collections;

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
        
        CheckWalletAndUpdateUI();
        InvokeRepeating(nameof(CheckWalletAndUpdateUI), 1f, 1f);
    }
    
    private void CheckWalletAndUpdateUI()
    {
        bool isWalletConnected = IsWalletConnected();
        
        if (nameInputPanel != null)
        {
            nameInputPanel.SetActive(isWalletConnected);
            Debug.Log($"[NFTVerifyUI] Panel de nom: {(isWalletConnected ? "AFFICHÉ" : "CACHÉ")} - Wallet connecté: {isWalletConnected}");
        }
    }
    
    // ✅ MODIFIÉ : Nettoyer PlayerSession via réflexion ou ignorer
    private bool IsWalletConnected()
    {
        // Méthode 1 : Vérifier AppKit EN PREMIER (source de vérité)
        try
        {
            if (Reown.AppKit.Unity.AppKit.IsInitialized && 
                Reown.AppKit.Unity.AppKit.IsAccountConnected && 
                Reown.AppKit.Unity.AppKit.Account != null)
            {
                string appKitAddress = Reown.AppKit.Unity.AppKit.Account.Address;
                if (!string.IsNullOrEmpty(appKitAddress))
                {
                    Debug.Log($"[NFTVerifyUI] Wallet trouvé dans AppKit: {appKitAddress}");
                    PlayerPrefs.SetString("walletAddress", appKitAddress);
                    return true;
                }
            }
            
            // ✅ NOUVEAU : Si AppKit est initialisé mais PAS connecté, nettoyer tout
            if (Reown.AppKit.Unity.AppKit.IsInitialized && !Reown.AppKit.Unity.AppKit.IsAccountConnected)
            {
                // Nettoyer PlayerPrefs
                string oldPrefsAddress = PlayerPrefs.GetString("walletAddress", "");
                if (!string.IsNullOrEmpty(oldPrefsAddress))
                {
                    Debug.Log($"[NFTVerifyUI] AppKit déconnecté, nettoyage PlayerPrefs: {oldPrefsAddress}");
                    PlayerPrefs.DeleteKey("walletAddress");
                }
                
                // ✅ NOUVEAU : Nettoyer PlayerSession via réflexion
                try
                {
                    if (PlayerSession.IsConnected)
                    {
                        Debug.Log($"[NFTVerifyUI] AppKit déconnecté, tentative nettoyage PlayerSession: {PlayerSession.WalletAddress}");
                        
                        // Essayer de forcer la déconnexion via réflexion
                        var playerSessionType = typeof(PlayerSession);
                        var walletAddressField = playerSessionType.GetField("_walletAddress", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        var isConnectedField = playerSessionType.GetField("_isConnected", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        
                        if (walletAddressField != null) walletAddressField.SetValue(null, "");
                        if (isConnectedField != null) isConnectedField.SetValue(null, false);
                        
                        Debug.Log("[NFTVerifyUI] PlayerSession nettoyé par réflexion");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[NFTVerifyUI] Impossible de nettoyer PlayerSession: {ex.Message}");
                    // ✅ FALLBACK : Ignorer PlayerSession si AppKit dit déconnecté
                    Debug.Log("[NFTVerifyUI] Ignorant PlayerSession car AppKit est déconnecté");
                }
                
                Debug.Log("[NFTVerifyUI] Déconnexion détectée - aucun wallet connecté");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[NFTVerifyUI] Erreur AppKit: {ex.Message}");
        }
        
        // Méthode 2 : Vérifier PlayerPrefs (seulement si AppKit pas disponible)
        string walletFromPrefs = PlayerPrefs.GetString("walletAddress", "");
        if (!string.IsNullOrEmpty(walletFromPrefs))
        {
            Debug.Log($"[NFTVerifyUI] Wallet trouvé dans PlayerPrefs: {walletFromPrefs}");
            return true;
        }
        
        // Méthode 3 : Vérifier PlayerSession (en dernier recours, SEULEMENT si AppKit pas initialisé)
        try
        {
            if (!Reown.AppKit.Unity.AppKit.IsInitialized && PlayerSession.IsConnected && !string.IsNullOrEmpty(PlayerSession.WalletAddress))
            {
                Debug.Log($"[NFTVerifyUI] Wallet trouvé dans PlayerSession: {PlayerSession.WalletAddress}");
                return true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[NFTVerifyUI] Erreur PlayerSession: {ex.Message}");
        }
        
        Debug.Log("[NFTVerifyUI] Aucun wallet connecté détecté");
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
        Debug.Log("[NFT] Bouton de vérification cliqué");
        
        if (nftVerification == null)
        {
            Debug.LogError("[NFT] NFTVerification non assigné dans l'inspecteur");
            if (statusText != null)
            {
                ShowStatus("Erreur: Référence manquante", true);
            }
            return;
        }

        if (!IsWalletConnected())
        {
            Debug.LogWarning("[NFT] Aucun wallet connecté");
            if (statusText != null)
            {
                ShowStatus("no wallet connected", true);
            }
            return;
        }

        ShowStatus("Vérification en cours...");
        nftVerification.ForceNFTCheck();
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
        CancelInvoke(nameof(CheckWalletAndUpdateUI));
    }
}