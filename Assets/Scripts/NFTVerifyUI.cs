using UnityEngine;
using TMPro;
using System.Collections;

public class NFTVerifyUI : MonoBehaviour
{
    [SerializeField] private NFTVerification nftVerification;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
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
                ShowStatus("Erreur: Référence manquante", true); // Disparaît après 3 secondes
            }
            return;
        }

        // Vérifier si un wallet est connecté
        string walletAddress = PlayerPrefs.GetString("walletAddress", "");
        Debug.Log($"[NFT] Adresse du wallet depuis PlayerPrefs: {walletAddress}");
        
        // Vérifier si la clé existe
        bool hasKey = PlayerPrefs.HasKey("walletAddress");
        Debug.Log($"[NFT] La clé walletAddress existe: {hasKey}");
        
        if (string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogWarning("[NFT] Aucun wallet connecté");
            if (statusText != null)
            {
                ShowStatus("Aucun wallet connecté", true); // Disparaît après 3 secondes
            }
            return;
        }

        // Afficher le statut de vérification
        ShowStatus("Vérification en cours...");

        // Lancer la vérification
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
    
    // Suppression de HideStatusAfterDelay car on garde les messages affichés
}
