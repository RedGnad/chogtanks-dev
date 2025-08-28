using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Photon.Pun;

namespace Sample
{
    public class MonadGamesIDManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button monadSignInButton;
        [SerializeField] private TextMeshProUGUI usernameText;
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Header("Config")]
        [SerializeField] private string gameWalletAddress = "0x8107edd492E8201a286b163f38d896a779AFA6b9";
        [SerializeField] private string monadGamesContractAddress = "0x1234567890123456789012345678901234567890"; // À remplacer par l'adresse réelle
        [SerializeField] private string monadRpcUrl = "https://testnet-rpc.monad.xyz/"; // RPC Monad Testnet
        [SerializeField] private string monadChainId = "10143"; // Chain ID Monad Testnet
        
        [Header("Scoring Strategy - À définir")]
        [SerializeField] private bool useTransactionCount = false;
        [SerializeField] private bool useGameScore = true;
        
        [Header("UI Management")]
        [SerializeField] private TMP_Text mainScreenPlayerNameText; // Référence au Main Screen Player Name
        [SerializeField] private GameObject panelToHide; // Panel à cacher quand username Privy récupéré
        
        private string currentUsername = "";
        private bool isSignedIn = false;
        
        private static MonadGamesIDManager _instance;
        public static MonadGamesIDManager Instance => _instance;
        
        public static event System.Action<string> OnUsernameChanged;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SetupUI();
            LoadSavedState();
            
            // S'abonner aux événements du nouveau système WebView
            MonadGamesIDWebView.OnMonadGamesIDResultEvent += OnMonadWebViewResult;
            
            // Forcer la restauration du username après un délai
            StartCoroutine(ForceRestoreUsernameAfterDelay());
        }
        
        private System.Collections.IEnumerator ForceRestoreUsernameAfterDelay()
        {
            // Attendre que tout soit initialisé
            yield return new WaitForSeconds(1f);
            
            string savedUsername = PlayerPrefs.GetString("MonadGamesID_Username", "");
            if (!string.IsNullOrEmpty(savedUsername))
            {
                Debug.Log($"[MONAD-RESTORE] Forçage restauration username: {savedUsername}");
                SetMonadUsernameAsPlayerName(savedUsername);
                
                // Vérifier encore après 2 secondes si nécessaire
                yield return new WaitForSeconds(2f);
                if (PhotonNetwork.NickName != savedUsername)
                {
                    Debug.Log($"[MONAD-RESTORE] Deuxième tentative de restauration: {savedUsername}");
                    PhotonNetwork.NickName = savedUsername;
                }
            }
        }

        private void SetupUI()
        {
            if (monadSignInButton != null)
            {
                monadSignInButton.onClick.AddListener(OnMonadSignInButtonClicked);
            }
            
            UpdateUI();
        }
        
        private void OnMonadSignInButtonClicked()
        {
            Debug.Log("[MONAD-GAMES] Bouton Sign In cliqué");
            
            // Utiliser le nouveau système WebView
            if (MonadGamesIDWebView.Instance != null)
            {
                UpdateStatus("Ouverture WebView...");
                MonadGamesIDWebView.Instance.OpenMonadGamesIDLogin();
            }
            else
            {
                Debug.LogError("[MONAD-GAMES] MonadGamesIDWebView introuvable");
                UpdateStatus("Erreur système");
            }
        }

        /// <summary>
        /// Callback du nouveau système WebView
        /// </summary>
        private void OnMonadWebViewResult(MonadGamesIDWebView.MonadGamesIDResult result)
        {
            if (result.success)
            {
                Debug.Log($"[MONAD-GAMES] ✅ WebView Success: {result.username}, Wallet: {result.walletAddress}");
                
                currentUsername = result.username;
                isSignedIn = true;
                SaveState();
                
                UpdateUI();
                UpdateStatus($"Connecté: {result.username}");
                OnUsernameChanged?.Invoke(result.username);
                
                // NOUVEAU: Utiliser le username Monad Games ID comme nom de joueur
                SetMonadUsernameAsPlayerName(result.username);
                
                // Sauvegarder les données
                PlayerPrefs.SetString("MonadGamesID_Username", result.username);
                PlayerPrefs.SetString("MonadGamesID_WalletAddress", result.walletAddress);
                
                // SYNCHRONISATION CRITIQUE: Écrire aussi dans walletAddress pour compatibilité avec tout le système NFT
                PlayerPrefs.SetString("walletAddress", result.walletAddress);
                // IMPORTANT: Ne PAS auto-approuver personalSign pour Privy - l'utilisateur doit le faire manuellement
                // PlayerPrefs.SetInt("personalSignApproved", 1); // SUPPRIMÉ
                PlayerPrefs.Save();
                
                // CRITIQUE: Synchroniser PlayerSession avec l'adresse wallet Privy
                PlayerSession.SetWalletAddress(result.walletAddress);
                
                Debug.Log($"[MONAD-SYNC] Wallet stored: {result.walletAddress}");
                Debug.Log($"[MONAD-SYNC] Personal sign NOT auto-approved - user must sign manually");
                Debug.Log($"[MONAD-SYNC] PlayerSession synchronized with Privy wallet");
                
                // Déclencher OnPersonalSignCompleted pour débloquer les fonctionnalités
                var connect = FindObjectOfType<Sample.ConnectWalletButton>();
                if (connect != null)
                {
                    Debug.Log("[MONAD-SYNC] Triggering OnPersonalSignCompleted for Privy");
                    connect.TriggerPersonalSignCompleted();
                    Debug.Log("[MONAD-SYNC] OnPersonalSignCompleted triggered successfully");
                }
            }
            else
            {
                Debug.LogError($"[MONAD-GAMES] ❌ WebView Error: {result.error}");
                UpdateStatus($"Erreur: {result.error}");
            }
        }

        public async Task CheckMonadGamesUsername(string walletAddress)
        {
            try
            {
                // Cette méthode est maintenant appelée par PrivyManager
                // qui gère directement l'API call et appelle OnMonadGamesIDFound/NotFound
                await Task.Delay(100); // Petit délai pour éviter les race conditions
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MONAD-GAMES] Erreur auto-check: {e.Message}");
            }
        }
        
        /// <summary>
        /// Appelée quand un username Monad Games ID est trouvé
        /// </summary>
        public void OnMonadGamesIDFound(string username, string walletAddress)
        {
            Debug.Log($"[MONAD GAMES ID] ✅ Username trouvé: {username} pour wallet: {walletAddress}");
            
            currentUsername = username;
            isSignedIn = true;
            SaveState();
            
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                UpdateUI();
                UpdateStatus($"Monad Games ID: {username}");
                OnUsernameChanged?.Invoke(username);
                
                // NOUVEAU: Utiliser le username Monad Games ID comme nom de joueur
                SetMonadUsernameAsPlayerName(username);
            });
            
            // Sauvegarder les données
            PlayerPrefs.SetString("MonadGamesID_Username", username);
            PlayerPrefs.SetString("MonadGamesID_WalletAddress", walletAddress);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Appelée quand aucun username Monad Games ID n'est trouvé
        /// </summary>
        public void OnMonadGamesIDNotFound(string walletAddress)
        {
            Debug.Log($"[MONAD GAMES ID] ⚠️ Aucun username pour wallet: {walletAddress}");
            
            currentUsername = "";
            isSignedIn = false;
            SaveState();
            
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                UpdateUI();
                UpdateStatus("Créer un username Monad Games ID");
                // Optionnel: ouvrir URL de création de username
                // Application.OpenURL("https://monad-games-id-site.vercel.app/");
            });
            
            // Sauvegarder la wallet address pour plus tard
            PlayerPrefs.SetString("MonadGamesID_WalletAddress", walletAddress);
            PlayerPrefs.DeleteKey("MonadGamesID_Username");
            PlayerPrefs.Save();
        }

        private async Task<string> GetMonadGamesUsername()
        {
            try
            {
                // Cette méthode n'est plus utilisée avec le nouveau système WebView
                // Le username est maintenant récupéré directement via React + Privy Cross App
                string savedWallet = PlayerPrefs.GetString("MonadGamesID_WalletAddress", "");
                
                if (string.IsNullOrEmpty(savedWallet))
                {
                    return "";
                }

                // Simulation d'appel RPC pour compatibilité
                await Task.Delay(100);
                
                return "";
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MONAD-GAMES] Erreur récupération username: {e.Message}");
                return "";
            }
        }

        private string GetUsernameCallData(string walletAddress)
        {
            string methodId = "0x12345678";
            string paddedAddress = walletAddress.Substring(2).PadLeft(64, '0');
            return methodId + paddedAddress;
        }

        private string DecodeUsernameFromHex(string hexData)
        {
            try
            {
                if (hexData.StartsWith("0x"))
                {
                    hexData = hexData.Substring(2);
                }

                byte[] bytes = new byte[hexData.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = System.Convert.ToByte(hexData.Substring(i * 2, 2), 16);
                }

                return System.Text.Encoding.UTF8.GetString(bytes).Trim('\0');
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MONAD-GAMES] Erreur décodage: {e.Message}");
                return "";
            }
        }

        public async Task SubmitScore(int score, int transactionCount = 0)
        {
            if (!isSignedIn)
            {
                return;
            }

            try
            {
                UpdateStatus("Soumission score Monad Games...");
                
                var transaction = new
                {
                    to = monadGamesContractAddress,
                    data = GetSubmitScoreCallData(score, transactionCount),
                    value = "0x0",
                    gas = "0x7530"
                };

                var request = new
                {
                    method = "eth_sendTransaction",
                    @params = new object[] { transaction }
                };

                // Simulation d'envoi de transaction pour compatibilité
                // Dans le futur, utiliser le wallet address sauvegardé pour les transactions
                string savedWallet = PlayerPrefs.GetString("MonadGamesID_WalletAddress", "");
                if (!string.IsNullOrEmpty(savedWallet))
                {
                    await Task.Delay(1000); // Simulation délai réseau
                    UpdateStatus($"Score {score} soumis!");
                    Debug.Log($"[MONAD-GAMES] Score {score} soumis pour wallet: {savedWallet}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MONAD-GAMES] Erreur soumission: {e.Message}");
                UpdateStatus("Erreur soumission score");
            }
        }

        private string GetSubmitScoreCallData(int score, int transactionCount)
        {
            string methodId = "0x87654321";
            return methodId;
        }

        private void UpdateUI()
        {
            if (usernameText != null)
            {
                if (isSignedIn && !string.IsNullOrEmpty(currentUsername))
                {
                    usernameText.text = $"Monad ID: {currentUsername}";
                    usernameText.gameObject.SetActive(true);
                    
                    // CORRIGÉ: Mettre à jour le Main Screen Player Name avec le username au lieu de le cacher
                    if (mainScreenPlayerNameText != null)
                    {
                        mainScreenPlayerNameText.text = " " + currentUsername;
                        mainScreenPlayerNameText.gameObject.SetActive(true);
                        Debug.Log($"[MONAD-UI] Main Screen Player Name updated to: {currentUsername}");
                    }
                    
                    // Cacher le panel spécifique quand username Privy récupéré
                    if (panelToHide != null)
                    {
                        panelToHide.SetActive(false);
                        Debug.Log("[MONAD-UI] Panel hidden - Privy username retrieved");
                    }
                }
                else
                {
                    usernameText.gameObject.SetActive(false);
                    
                    // Réafficher le Main Screen Player Name quand pas de username Privy
                    if (mainScreenPlayerNameText != null)
                    {
                        mainScreenPlayerNameText.gameObject.SetActive(true);
                        Debug.Log("[MONAD-UI] Main Screen Player Name restored - no Privy username");
                    }
                    
                    // Réafficher le panel spécifique quand pas d'username Privy
                    if (panelToHide != null)
                    {
                        panelToHide.SetActive(true);
                        Debug.Log("[MONAD-UI] Panel restored - no Privy username");
                    }
                }
            }

            if (monadSignInButton != null)
            {
                var buttonText = monadSignInButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = isSignedIn ? "Monad Games Info" : "Sign in with Monad Games ID";
                }
            }
        }

        private void UpdateStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
            Debug.Log($"[MONAD-GAMES] {status}");
        }

        private void ShowMonadInfo()
        {
            UpdateStatus($"Monad Games ID: {currentUsername}");
        }

        private void SaveState()
        {
            PlayerPrefs.SetString("monadGamesUsername", currentUsername);
            PlayerPrefs.SetInt("monadGamesSignedIn", isSignedIn ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadSavedState()
        {
            currentUsername = PlayerPrefs.GetString("monadGamesUsername", "");
            isSignedIn = PlayerPrefs.GetInt("monadGamesSignedIn", 0) == 1;
            
            if (isSignedIn && !string.IsNullOrEmpty(currentUsername))
            {
                UpdateUI();
                OnUsernameChanged?.Invoke(currentUsername);
                
                // NOUVEAU: Restaurer le username comme nom de joueur au démarrage
                SetMonadUsernameAsPlayerName(currentUsername);
            }
        }

        /// <summary>
        /// Définit le username Monad Games ID comme nom de joueur - SIMPLE
        /// </summary>
        private void SetMonadUsernameAsPlayerName(string username)
        {
            if (string.IsNullOrEmpty(username)) return;
            
            Debug.Log($"[MONAD-PLAYER-NAME] Setting '{username}' as PhotonNetwork.NickName");
            
            // SIMPLE: Mettre à jour PhotonNetwork.NickName directement
            PhotonNetwork.NickName = username;
            
            // Forcer la mise à jour du mainScreenPlayerNameText via LobbyUI
            var lobbyUI = FindObjectOfType<LobbyUI>();
            if (lobbyUI != null)
            {
                // Utiliser la méthode privée UpdateMainScreenPlayerName via reflection
                var method = typeof(LobbyUI).GetMethod("UpdateMainScreenPlayerName", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(lobbyUI, null);
                    Debug.Log($"[MONAD-PLAYER-NAME] UpdateMainScreenPlayerName() called - should show '{username}'");
                }
            }
            
            Debug.Log($"[MONAD-PLAYER-NAME] PhotonNetwork.NickName set to '{username}'");
        }

        public string GetCurrentUsername() => currentUsername;
        public bool IsSignedIn() => isSignedIn;
    }

    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private System.Collections.Generic.Queue<System.Action> _executionQueue = new System.Collections.Generic.Queue<System.Action>();

        public static UnityMainThreadDispatcher Instance()
        {
            if (_instance == null)
            {
                var go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }

        public void Enqueue(System.Action action)
        {
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }

        private void Update()
        {
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }
    }
}
