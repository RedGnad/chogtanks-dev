using Reown.AppKit.Unity;
using Reown.AppKit.Unity.Model;
using Reown.Core.Common.Logging;
using UnityEngine;
using UnityLogger = Reown.Sign.Unity.UnityLogger;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sample
{
    public class AppKitInit : MonoBehaviour
    {
        public string WalletAddress { get; private set; }
        public static event Action OnAppKitInitialized;

        [Header("UI")]
        public GameObject walletWaitPanel;

        private static bool _isInitializing = false;
        private static bool walletPanelHasBeenHidden = false;
        private static bool gameOverHasBeenTriggered = false;

        [Header("Scene Management")]
        [SerializeField] private bool shouldSwitchScene = false;
        [SerializeField] private string targetSceneName = "";

        [Header("Interaction Management")]
        [SerializeField] private bool disableInteractionsOnModal = true;
        [SerializeField] private string[] interactionScriptNames = { "PlayerController" };
        [SerializeField] private float checkInterval = 0.2f;

        private List<MonoBehaviour> disabledComponents = new List<MonoBehaviour>();
        private bool isModalActive = false;
        private Coroutine modalCheckCoroutine;
        private Coroutine walletCheckCoroutine;
        private float walletDisconnectedTime = -1f;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void ResetAppKitOnEditorReload()
        {
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode ||
                state == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                Debug.Log("[AppKit] Resetting AppKit state for Editor...");
                _isInitializing = false;
                walletPanelHasBeenHidden = false;
                gameOverHasBeenTriggered = false;
                try {
                    var t = typeof(Reown.AppKit.Unity.AppKit);
                    var bf = t.GetField("<IsInitialized>k__BackingField", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    if (bf != null) {
                        bf.SetValue(null, false);
                        Debug.Log("[AppKit] Successfully reset internal state");
                    }
                } catch (Exception e) {
                    Debug.LogWarning($"[AppKit] Could not reset internal state: {e.Message}");
                }
            }
        }
#endif

        private bool IsWebGLMobile()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return Application.isMobilePlatform;
#else
            return false;
#endif
        }

        private void Start()
        {
            ReownLogger.Instance = new UnityLogger();
            if (AppKit.IsInitialized && AppKit.IsAccountConnected && walletWaitPanel != null)
                walletWaitPanel.SetActive(false);

            StartCoroutine(InitializeAppKitWithRetry());
            walletCheckCoroutine = StartCoroutine(WalletPanelCheckRoutine());
            StartCoroutine(AutoGameOverIfPanel());
        }

        private void Update()
        {
            bool panelUp = walletWaitPanel != null && walletWaitPanel.activeSelf;
            foreach (var script in FindObjectsOfType<MonoBehaviour>())
            {
                if (script == null) continue;
                foreach (var target in interactionScriptNames)
                    if (script.GetType().Name == target)
                        script.enabled = !panelUp;
            }
        }

        private IEnumerator AutoGameOverIfPanel()
        {
            yield return new WaitForSeconds(0.1f);
            if (walletWaitPanel != null && walletWaitPanel.activeSelf)
                Debug.Log("[AppKit] Wallet panel is active - waiting for connection");
        }

        private IEnumerator WalletPanelCheckRoutine()
        {
            while (true)
            {
                if (AppKit.IsInitialized && AppKit.IsAccountConnected)
                {
                    var account = AppKit.Account;
                    if (account != null && !string.IsNullOrEmpty(account.Address))
                    {
                        WalletAddress = account.Address;
                        walletDisconnectedTime = -1f;
                        if (walletWaitPanel != null && walletWaitPanel.activeSelf && !walletPanelHasBeenHidden)
                        {
                            walletWaitPanel.SetActive(false);
                            walletPanelHasBeenHidden = true;
                            if (!gameOverHasBeenTriggered)
                            {
                                Debug.Log("[AppKit] Wallet connected - panel hidden");
                                gameOverHasBeenTriggered = true;
                            }
                        }
                        OnAppKitInitialized?.Invoke();
                        yield break;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(WalletAddress))
                    {
                        WalletAddress = "";
                        PlayerPrefs.DeleteKey("walletAddress");
                        Debug.Log("[AppKit] Wallet disconnected - cleared address");
                        var nftUI = FindObjectOfType<NFTVerifyUI>();
                        if (nftUI != null)
                            nftUI.ForceCheckWalletStatus();
                    }
                    if (walletDisconnectedTime < 0f) walletDisconnectedTime = Time.time;
                    if (Time.time - walletDisconnectedTime > 1f && walletWaitPanel != null && !walletWaitPanel.activeSelf)
                    {
                        walletWaitPanel.SetActive(true);
                        walletPanelHasBeenHidden = false;
                        gameOverHasBeenTriggered = false;
                    }
                }
                yield return new WaitForSeconds(checkInterval);
            }
        }

        public static void TryInitialize()
        {
            if (AppKit.IsInitialized || _isInitializing) return;
            var inst = FindObjectOfType<AppKitInit>();
            if (inst != null) inst.StartCoroutine(inst.InitializeAppKitWithRetry());
        }

        public static async System.Threading.Tasks.Task TryInitializeAsync()
        {
            if (AppKit.IsInitialized) return;
            TryInitialize();
            int timeout = 0;
            while (!AppKit.IsInitialized && timeout < 100)
            {
                await System.Threading.Tasks.Task.Delay(100);
                timeout++;
            }
            if (!AppKit.IsInitialized)
                Debug.LogError("[AppKit] Timeout during initialization");
        }

        private IEnumerator InitializeAppKitWithRetry()
        {
            bool isMobile = IsWebGLMobile();
            bool shouldReinit = AppKit.IsInitialized && isMobile;
            if (_isInitializing || (AppKit.IsInitialized && !shouldReinit))
                yield break;

            _isInitializing = true;

            var monadTestnet = new Chain(
                ChainConstants.Namespaces.Evm,
                "10143", "Monad Testnet",
                new Currency("Monad","MON",18),
                new BlockExplorer("Monad Explorer","https://explorer.testnet.monad.xyz"),
                "https://testnet-rpc.monad.xyz/",
                true,
                "https://monad.xyz/logo.svg"
            );

            var cfg = new AppKitConfig
            {
                projectId = "a4ea622af154daef687398cb6c4ce85a",
                metadata = new Metadata(
                    "CHOGTANKS",
                    "CHOGTANKS Unity WebGL",
                    "https://redgnad.github.io/CHOGTANKS-testbuild-2/",
                    "https://raw.githubusercontent.com/reown-com/reown-dotnet/main/media/appkit-icon.png",
                    new RedirectData {
                        Native = "appkit-sample-unity://",
                        Universal = "https://redgnad.github.io/CHOGTANKS-testbuild-2/"
                    }
                ),
                customWallets = GetCustomWallets(),
                connectViewWalletsCountMobile = 4,
                excludedWalletIds = new[]
                {
                    "walletconnect","rainbow","coinbase","safe"
                },
                includedWalletIds = new[]
                {
                    "2bd8c14e035c2d48f184aaa168559e86b0e3433228d3c4075900a221785019b0",
                    "719bd888109f5e8dd23419b20e749900ce4d2fc6858cf588395f19c82fd036b3",
                    "c57ca95b47569778a828d19178114f4db188b89b763c899ba0be274e97267d96",
                    "4622a2b2d6af1c9844944291e5e7351a6aa24cd7b23099efac1b2fd875da31a0",
                    "a797aa35c0fadbfc1a53e7f675162ed5226968b44a19ee3d24385c64d1d3c393",
                    "io.rabby"  // 🟢 Ajout minimal pour Rabby desktop
                },
                supportedChains = new[] { monadTestnet },
                enableEmail = false,
                allWallets = "HIDE"
                socials = new[]
                {
                    SocialLogin.Discord,
                }
            };

            const int MAX = 10;
            int attempts = 0;
            while (!AppKit.IsInitialized && attempts < MAX)
            {
                attempts++;
                Debug.Log($"[AppKitInit] Attempt {attempts}/{MAX}...");
                var initTask = AppKit.InitializeAsync(cfg);

                float timer = 0f;
                while (!initTask.IsCompleted && timer < 5f)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }

                if (initTask.IsCompleted && !initTask.IsFaulted)
                {
                    Debug.Log("[AppKitInit] Initialization succeeded!");
                    AppKit.AccountConnected    += OnWalletEvent;
                    AppKit.AccountDisconnected += OnWalletEvent;
                    if (disableInteractionsOnModal) StartModalCheck();
                    if (shouldSwitchScene && Application.CanStreamedLevelBeLoaded(targetSceneName))
                        UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
                    break;
                }

                yield return new WaitForSeconds(1f);
            }

            if (!AppKit.IsInitialized)
                Debug.LogError($"[AppKitInit] Failed after {MAX} attempts.");

            _isInitializing = false;
        }

        private void OnWalletEvent(object sender, EventArgs e)
        {
            isModalActive = false;
            EnableAllInteractions();
            var nftUI = FindObjectOfType<NFTVerifyUI>();
            if (nftUI != null)
                nftUI.ForceCheckWalletStatus();
        }

        private void StartModalCheck()
        {
            if (modalCheckCoroutine != null) StopCoroutine(modalCheckCoroutine);
            modalCheckCoroutine = StartCoroutine(CheckModalRoutine());
        }

        private IEnumerator CheckModalRoutine()
        {
            while (true)
            {
                bool modalDetected = IsModalVisible();
                if (modalDetected != isModalActive)
                {
                    isModalActive = modalDetected;
                    if (modalDetected) DisableAllInteractions(); else EnableAllInteractions();
                }
                yield return new WaitForSeconds(checkInterval);
            }
        }

        private bool IsModalVisible()
        {
            var modal = GameObject.Find("AppKit_ModalContainer");
            if (modal != null && modal.activeInHierarchy) return true;
            foreach (var c in FindObjectsOfType<Canvas>())
                if ((c.name.ToLower().Contains("modal") || c.name.ToLower().Contains("wallet"))
                    && c.gameObject.activeInHierarchy)
                    return true;
            return false;
        }

        private void DisableAllInteractions()
        {
            disabledComponents.Clear();
            foreach (var script in FindObjectsOfType<MonoBehaviour>())
            {
                if (script == null) continue;
                foreach (var t in interactionScriptNames)
                    if (script.GetType().Name == t && script.enabled)
                    {
                        script.enabled = false;
                        disabledComponents.Add(script);
                        break;
                    }
            }
        }

        private void EnableAllInteractions()
        {
            foreach (var script in disabledComponents)
                if (script != null) script.enabled = true;
            disabledComponents.Clear();
        }

        private void OnDestroy()
        {
            if (AppKit.IsInitialized)
            {
                AppKit.AccountConnected    -= OnWalletEvent;
                AppKit.AccountDisconnected -= OnWalletEvent;
            }
            if (modalCheckCoroutine != null) StopCoroutine(modalCheckCoroutine);
            if (walletCheckCoroutine != null) StopCoroutine(walletCheckCoroutine);
            EnableAllInteractions();
        }

        private Wallet[] GetCustomWallets()
        {
            bool isMobile = Application.isMobilePlatform || IsWebGLMobile();
            if (isMobile)
            {
                return new[]
                {
                    new Wallet { Name="Backpack", ImageUrl="https://backpack.app/favicon.ico", MobileLink="backpack://", WebappLink="https://backpack.app/", Id="2bd8c14e035c2d48f184aaa168559e86b0e3433228d3c4075900a221785019b0" },
                    new Wallet { Name="HAHA",     ImageUrl="https://raw.githubusercontent.com/RedGnad/pokenads/master/pokenads-logo8.png", MobileLink="haha://", WebappLink="https://haha-wallet-url/", Id="719bd888109f5e8dd23419b20e749900ce4d2fc6858cf588395f19c82fd036b3" },
                    new Wallet { Name="MetaMask", ImageUrl="https://metamask.io/images/favicon.ico", MobileLink="metamask://wc", WebappLink="https://metamask.io/", Id="c57ca95b47569778a828d19178114f4db188b89b763c899ba0be274e97267d96" },
                    new Wallet { Name="Trust Wallet", ImageUrl="https://trustwallet.com/assets/images/favicon.ico", MobileLink="trust://wc", Id="4622a2b2d6af1c9844944291e5e7351a6aa24cd7b23099efac1b2fd875da31a0" }
                };
            }
            else
            {
                return new[]
                {
                    new Wallet { Name="Backpack", ImageUrl="https://backpack.app/favicon.ico", MobileLink="backpack://", WebappLink="https://backpack.app/", Id="2bd8c14e035c2d48f184aaa168559e86b0e3433228d3c4075900a221785019b0" },
                    new Wallet { Name="HAHA",     ImageUrl="https://raw.githubusercontent.com/RedGnad/pokenads/master/pokenads-logo8.png", MobileLink="haha://", WebappLink="https://haha-wallet-url/", Id="719bd888109f5e8dd23419b20e749900ce4d2fc6858cf588395f19c82fd036b3" },
                    new Wallet { Name="MetaMask", ImageUrl="https://metamask.io/images/favicon.ico", MobileLink="metamask://wc", WebappLink="https://metamask.io/", Id="c57ca95b47569778a828d19178114f4db188b89b763c899ba0be274e97267d96" },
                    new Wallet { Name="Trust Wallet", ImageUrl="https://trustwallet.com/assets/images/favicon.ico", MobileLink="trust://wc", Id="4622a2b2d6af1c9844944291e5e7351a6aa24cd7b23099efac1b2fd875da31a0" },
                    new Wallet { Name="Phantom",  ImageUrl="https://phantom.app/img/phantom-logo.png", WebappLink="https://phantom.app/ul/browse", Id="a797aa35c0fadbfc1a53e7f675162ed5226968b44a19ee3d24385c64d1d3c393" },
                    new Wallet { Name="Rabby",    ImageUrl="https://rabby.io/assets/images/logo.png", WebappLink="https://rabby.io/", Id="io.rabby" }
                };
            }
        }
    }
}
