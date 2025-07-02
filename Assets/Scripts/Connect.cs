using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Reown.AppKit.Unity;

namespace Sample
{
    [RequireComponent(typeof(Button))]
    public class ConnectWalletButton : MonoBehaviour
    {
        [SerializeField] private Button connectButton;

        private void Awake()
        {
            if (connectButton == null)
                connectButton = GetComponent<Button>();

            connectButton.interactable = true;
            connectButton.onClick.AddListener(OnConnectClicked);
        }

        private async void OnConnectClicked()
        {
            Debug.Log("[Connect] Début de la connexion...");
            
            try
            {
                // PROTECTION EDITOR : Vérifications simples sans test modal
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Debug.LogWarning("[Connect] Not in play mode!");
                    return;
                }
                
                // Simple vérification d'état sans ouvrir le modal
                if (AppKit.IsInitialized)
                {
                    Debug.Log("[Connect] AppKit already initialized");
                }
#endif

                if (!AppKit.IsInitialized)
                {
                    Debug.Log("[Connect] AppKit non initialisé, tentative d'initialisation...");
                    await AppKitInit.TryInitializeAsync();
                    await System.Threading.Tasks.Task.Delay(500);
                    Debug.Log("[Connect] AppKit initialisé avec succès");
                }

                // Vérification sécurisée de l'Account
                string initialAddress = "";
                try
                {
                    if (AppKit.IsInitialized && AppKit.IsAccountConnected && AppKit.Account != null)
                    {
                        initialAddress = AppKit.Account.Address ?? "";
                    }
                }
                catch (System.Exception accountEx)
                {
                    Debug.LogWarning($"[Connect] Impossible de récupérer l'adresse actuelle : {accountEx.Message}");
                    initialAddress = "";
                }
                
                Debug.Log($"[Connect] Adresse actuelle : {(string.IsNullOrEmpty(initialAddress) ? "Aucune" : initialAddress)}");

                try
                {
                    Debug.Log("[Connect] Ouverture du modal...");
                    AppKit.OpenModal();
                    Debug.Log("[Connect] Modal ouvert");
                    StartCoroutine(WaitForModalCloseAndSign(initialAddress));
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Connect] Erreur ouverture modal : {e.Message}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Connect] ERREUR CRITIQUE : {e}");
            }
        }

        private IEnumerator WaitForModalCloseAndSign(string initialAddress)
        {
            Debug.Log("[Connect] En attente de l'ouverture du modal...");
            
            float timeout = Time.time + 10f;
            while (!AppKit.IsModalOpen && Time.time < timeout)
            {
                yield return null;
            }
            
            if (!AppKit.IsModalOpen)
            {
                Debug.LogError("[Connect] Le modal ne s'est pas ouvert dans le délai imparti");
                yield break;
            }
            
            Debug.Log("[Connect] Modal détecté comme ouvert");
            Debug.Log("[Connect] En attente de la fermeture du modal...");
            timeout = Time.time + 300f;
            while (AppKit.IsModalOpen && Time.time < timeout)
            {
                yield return null;
            }

            if (AppKit.IsModalOpen)
            {
                Debug.LogError("[Connect] Le modal n'a pas été fermé dans le délai imparti");
                yield break;
            }
            
            Debug.Log("[Connect] Modal fermé, vérification de l'état...");
            yield return new WaitForSeconds(1f);

            // Vérification sécurisée de l'Account final
            string finalAddress = "";
            try
            {
                if (AppKit.IsInitialized && AppKit.IsAccountConnected && AppKit.Account != null)
                {
                    finalAddress = AppKit.Account.Address ?? "";
                }
            }
            catch (System.Exception accountEx)
            {
                Debug.LogWarning($"[Connect] Impossible de récupérer l'adresse finale : {accountEx.Message}");
                finalAddress = "";
            }
            
            Debug.Log($"[Connect] Adresse après fermeture : {(string.IsNullOrEmpty(finalAddress) ? "Aucune" : finalAddress)}");

            if (string.IsNullOrEmpty(finalAddress))
            {
                Debug.LogWarning("[Connect] Aucun portefeuille connecté après fermeture du modal");
                yield break;
            }

            Debug.Log($"[Connect] Adresse finale : {finalAddress}");

            if (finalAddress != initialAddress)
            {
                Debug.Log($"[Connect] Nouvelle connexion détectée : {finalAddress}");

                try
                {
                    // Sauvegarder dans PlayerPrefs pour compatibilité
                    PlayerPrefs.SetString("walletAddress", finalAddress);
                    PlayerPrefs.Save();
                    Debug.Log("[Connect] Adresse sauvegardée dans PlayerPrefs");

                    // Vérification si PlayerSession existe
                    try
                    {
                        PlayerSession.SetWalletAddress(finalAddress);
                        Debug.Log("[Connect] Adresse enregistrée dans PlayerSession");
                    }
                    catch (System.Exception playerEx)
                    {
                        Debug.LogWarning($"[Connect] PlayerSession non disponible : {playerEx.Message}");
                    }

                    var dapp = FindObjectOfType<Dapp>();
                    if (dapp != null)
                    {
                        Debug.Log("[Connect] Déclenchement de la signature...");
                        dapp.OnPersonalSignButton();
                        Debug.Log("[Connect] Signature demandée avec succès");
                    }
                    else
                    {
                        Debug.LogError("[Connect] ERREUR: Aucun composant Dapp trouvé");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Connect] ERREUR lors du traitement : {e}");
                }
            }
            else
            {
                Debug.Log("[Connect] Aucun changement d'adresse détecté");
            }
        }
    }
}