using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Reown.AppKit.Unity;
using System;

namespace Sample
{
    [RequireComponent(typeof(Button))]
    public class ConnectWalletButton : MonoBehaviour
    {
        [SerializeField] private Button connectButton;

        public event Action OnPersonalSignCompleted;

        private void Awake()
        {
            if (connectButton == null)
                connectButton = GetComponent<Button>();

            connectButton.interactable = true;
            connectButton.onClick.AddListener(OnConnectClicked);
            OnPersonalSignCompleted += OnPersonalSignApproved;
        }

        private void OnPersonalSignApproved()
        {
            var nftManager = FindObjectOfType<ChogTanksNFTManager>();
            if (nftManager != null)
            {
                nftManager.RefreshWalletAddress(); 
                nftManager.LoadNFTStateFromBlockchain(); 
                nftManager.ForceLevelTextDisplay();   
            }
            var nftVerification = FindObjectOfType<NFTVerification>();
            if (nftVerification != null)
            {
                nftVerification.ForceNFTCheck();
            }
        }

        private async void OnConnectClicked()
        {
            
            try
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return;
                }
                
                if (AppKit.IsInitialized)
                {
                    Debug.Log("[Connect] AppKit already initialized");
                }
#endif

                if (!AppKit.IsInitialized)
                {
                    await AppKitInit.TryInitializeAsync();
                    await System.Threading.Tasks.Task.Delay(500);
                }

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
                    initialAddress = "";
                }
                
                
                try
                {
                    AppKit.OpenModal();
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
            
            float timeout = Time.time + 10f;
            while (!AppKit.IsModalOpen && Time.time < timeout)
            {
                yield return null;
            }
            
            if (!AppKit.IsModalOpen)
            {
                yield break;
            }
            
            timeout = Time.time + 300f;
            while (AppKit.IsModalOpen && Time.time < timeout)
            {
                yield return null;
            }

            if (AppKit.IsModalOpen)
            {
                yield break;
            }
            
            yield return new WaitForSeconds(0.2f);

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
                finalAddress = "";
            }
            
            
            if (string.IsNullOrEmpty(finalAddress))
            {
                yield break;
            }

            
            if (finalAddress != initialAddress)
            {
                
                try
                {
                    PlayerPrefs.SetString("walletAddress", finalAddress);
                    PlayerPrefs.Save();
                    
                    try
                    {
                        PlayerSession.SetWalletAddress(finalAddress);
                    }
                    catch (System.Exception playerEx)
                    {
                        Debug.LogWarning($"[Connect] PlayerSession non disponible : {playerEx.Message}");
                    }

                    StartCoroutine(TriggerPersonalSignAfterDelay());
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
        
        private IEnumerator TriggerPersonalSignAfterDelay()
        {
            yield return new WaitForSeconds(1f);
            for (int i = 0; i < 5; i++)
                yield return null;
#if UNITY_WEBGL && !UNITY_EDITOR
            string message = "Hello Choggie! (Request #1)";
            var signatureTask = AppKit.Evm.SignMessageAsync(message);
            yield return new WaitUntil(() => signatureTask.IsCompleted);
            
            if (signatureTask.IsCompletedSuccessfully && !string.IsNullOrEmpty(signatureTask.Result))
            {
                try
                {
                    PlayerPrefs.SetInt("personalSignApproved", 1);
                    PlayerPrefs.Save();
                    OnPersonalSignCompleted?.Invoke(); 
                    var nftVerification = FindObjectOfType<NFTVerification>();
                    if (nftVerification != null)
                    {
                        nftVerification.ForceNFTCheck();
                    }
                    var nftManager = FindObjectOfType<ChogTanksNFTManager>();
                    if (nftManager != null)
                    {
                        nftManager.LoadNFTStateFromBlockchain(); 
                        nftManager.ForceLevelTextDisplay();
                    }
                }
                catch (System.Exception signEx)
                {
                    Debug.LogWarning($"[Connect] Exception lors de la signature, mais continuons : {signEx.Message}");
                }
            }
            else
            {
                Debug.LogWarning("[Connect] Signature personnelle REFUSÉE ou FERMÉE - wallet non autorisé");
            }
#else
            // dapp.OnPersonalSignButton(); // Désactivated
#endif
        }
        
        public void TriggerPersonalSignCompleted()
        {
            OnPersonalSignCompleted?.Invoke();
        }
    }
}