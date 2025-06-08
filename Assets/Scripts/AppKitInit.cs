using Reown.AppKit.Unity;
using Reown.AppKit.Unity.Model;
using Reown.Core.Common.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityLogger = Reown.Sign.Unity.UnityLogger;

namespace Sample
{
    public class AppKitInit : MonoBehaviour
    {
        private static bool _isInitialized = false;
        [SerializeField] private string _menuSceneName;
        private static AppKitInit _instance;

        private void Awake()
        {
            // S'assurer que le GameObject est à la racine
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            // Implémentation du pattern Singleton
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[AppKit] Multiple AppKitInit instances detected. Destroying the new one.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _isInitialized = true;
            
            // Nettoyer les doublons
            CleanupDuplicates();
        }
        
        private void CleanupDuplicates()
        {
            // Désactiver les composants en double
            var duplicateTrackers = FindObjectsOfType<Reown.AppKit.Unity.Utils.OrientationTracker>();
            if (duplicateTrackers.Length > 1)
            {
                for (int i = 1; i < duplicateTrackers.Length; i++)
                {
                    if (duplicateTrackers[i] != null && duplicateTrackers[i].gameObject != gameObject)
                    {
                        Destroy(duplicateTrackers[i].gameObject);
                    }
                }
            }
            
            var duplicateCores = FindObjectsOfType<Reown.AppKit.Unity.AppKitCore>();
            if (duplicateCores.Length > 1)
            {
                for (int i = 1; i < duplicateCores.Length; i++)
                {
                    if (duplicateCores[i] != null && duplicateCores[i].gameObject != gameObject)
                    {
                        Destroy(duplicateCores[i].gameObject);
                    }
                }
            }
        }

        private async void Start()
        {
            // Vérifier si AppKit est déjà initialisé
            if (AppKit.IsInitialized)
            {
                Debug.Log("[AppKit] Already initialized, skipping initialization...");
                LoadMenuScene();
                return;
            }
            // Set up Reown logger to collect logs from AppKit
            ReownLogger.Instance = new UnityLogger();

            // The very basic configuration of SIWE
            // Uncomment it and pass into AppKitConfig below to enable 1-Click Auth and SIWE
            
            // var siweConfig = new SiweConfig
            // {
            //     GetMessageParams = () => new SiweMessageParams
            //     {
            //         Domain = "example.com",
            //         Uri = "https://example.com/login"
            //     },
            //     SignOutOnChainChange = false
            // };
            //
            // // Subscribe to SIWE events
            // siweConfig.SignInSuccess += _ => Debug.Log("[Dapp] SIWE Sign In Success!");
            // siweConfig.SignOutSuccess += () => Debug.Log("[Dapp] SIWE Sign Out Success!");

            
            // AppKit configuration
            var appKitConfig = new AppKitConfig
            {
                // Project ID from https://cloud.reown.com/
                projectId = "884a108399b5e7c9bc00bd9be4ccb2cc",
                metadata = new Metadata(
                    "AppKit Unity",
                    "AppKit Unity Sample",
                    "https://reown.com",
                    "https://raw.githubusercontent.com/reown-com/reown-dotnet/main/media/appkit-icon.png",
                    new RedirectData
                    {
                        // Used by native wallets to redirect back to the app after approving requests
                        Native = "appkit-sample-unity://"
                    }
                ),
                customWallets = GetCustomWallets(),
                // On mobile show 5 wallets on the Connect view (the first AppKit modal screen)
                connectViewWalletsCountMobile = 5,
                supportedChains = new[]
                {
                    new Chain(
                        ChainConstants.Namespaces.Evm, // Correction ici : plus de "namespaceId:"
                        "10143",
                        "Monad Testnet",
                        new Currency("Monad", "MON", 18),
                        new BlockExplorer("Monad Explorer", "https://explorer.testnet.monad.xyz"),
                        "https://rpc.testnet.monad.xyz/",
                        true,
                        "https://monad.xyz/logo.svg"
                    )
                },
                socials = new[]
                {
                    SocialLogin.Google,
                    SocialLogin.X,
                    SocialLogin.Discord,
                    SocialLogin.Apple,
                    SocialLogin.GitHub
                }
            };

            Debug.Log("[AppKit Init] Initializing AppKit...");
            try 
            {
                await AppKit.InitializeAsync(appKitConfig);
                Debug.Log("[AppKit] Initialization successful");
                LoadMenuScene();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AppKit] Initialization failed: {ex.Message}");
                // Optionnel: Détruire cet objet si l'initialisation échoue
                _isInitialized = false;
                _instance = null;
                Destroy(gameObject);
                return;
            }

            // --- Ajout : déclenchement auto de la signature et redirection ---
            AppKit.AccountConnected += async (_, _) =>
            {
                var account = await AppKit.GetAccountAsync();
                if (account != null && !string.IsNullOrEmpty(account.Address))
                {
                    PlayerPrefs.SetString("walletAddress", account.Address);
                    PlayerPrefs.Save();
                    Debug.Log($"[AppKit] Wallet address saved in PlayerPrefs: {account.Address}");
                }

                var dapp = FindObjectOfType<Dapp>();
                if (dapp != null)
                {
                    void OnSignDone()
                    {
                        dapp.OnPersonalSignCompleted -= OnSignDone;
                        UnityEngine.SceneManagement.SceneManager.LoadScene("GameSceneSA");
                    }
                    dapp.OnPersonalSignCompleted += OnSignDone;
                    dapp.OnPersonalSignButton();
                }
            };

#if !UNITY_WEBGL
            // The Mixpanel are Sentry are used by the sample project to collect telemetry
            var clientId = await AppKit.Instance.SignClient.CoreClient.Crypto.GetClientId();
            Mixpanel.Identify(clientId);

            SentrySdk.ConfigureScope(scope =>
            {
                scope.User = new SentryUser
                {
                    Id = clientId
                };
            });
#endif
        }

        private void LoadMenuScene()
        {
            if (string.IsNullOrEmpty(_menuSceneName))
            {
                _menuSceneName = "AppKit Menu"; // Valeur par défaut
                Debug.LogWarning($"[AppKit] Menu scene name not set, using default: {_menuSceneName}");
            }

            // Vérifier si la scène est dans le build settings
            bool sceneInBuild = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneName == _menuSceneName)
                {
                    sceneInBuild = true;
                    break;
                }
            }

            if (!sceneInBuild)
            {
                Debug.LogError($"[AppKit] Scene '{_menuSceneName}' is not in Build Settings!");
                #if UNITY_EDITOR
                if (UnityEditor.EditorUtility.DisplayDialog("Scene Not in Build", 
                    $"The scene '{_menuSceneName}' is not in the build settings.\n\nDo you want to add it now?", 
                    "Yes", "No"))
                {
                    var scenes = new UnityEditor.EditorBuildSettingsScene[SceneManager.sceneCountInBuildSettings + 1];
                    for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                    {
                        scenes[i] = new UnityEditor.EditorBuildSettingsScene(SceneUtility.GetScenePathByBuildIndex(i), true);
                    }
                    var newScene = new UnityEditor.EditorBuildSettingsScene("Assets/Scenes/" + _menuSceneName + ".unity", true);
                    scenes[scenes.Length - 1] = newScene;
                    UnityEditor.EditorBuildSettings.scenes = scenes;
                    UnityEditor.EditorUtility.DisplayDialog("Scene Added", 
                        $"Scene '{_menuSceneName}' has been added to build settings.\nPlease restart the game.", "OK");
                }
                #endif
                return;
            }

            Debug.Log($"[AppKit] Loading menu scene: {_menuSceneName}");
            SceneManager.LoadScene(_menuSceneName);
        }

        /// <summary>
        ///     This method returns a list of Reown sample wallets on iOS and Android.
        ///     These wallets are used for testing and are not included in the default list of wallets returned by AppKit's REST API.
        ///     On other platforms, this method returns null, so only the default list of wallets is used.
        /// </summary>
        private Wallet[] GetCustomWallets()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return new[]
            {
                new Wallet
                {
                    Name = "Swift Wallet",
                    ImageUrl = "https://github.com/reown-com/reown-dotnet/blob/develop/media/wallet-swift.png?raw=true",
                    MobileLink = "walletapp://"
                },
                new Wallet
                {
                    Name = "React Native Wallet",
                    ImageUrl = "https://github.com/reown-com/reown-dotnet/blob/develop/media/wallet-rn.png?raw=true",
                    MobileLink = "rn-web3wallet://"
                },
                new Wallet
                {
                    Name = "Flutter Wallet Prod",
                    ImageUrl = "https://github.com/reown-com/reown-dotnet/blob/develop/media/wallet-flutter.png?raw=true",
                    MobileLink = "wcflutterwallet://"
                }
            };
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            return new[]
            {
                new Wallet
                {
                    Name = "Kotlin Wallet",
                    ImageUrl = "https://github.com/reown-com/reown-dotnet/blob/develop/media/wallet-kotlin.png?raw=true",
                    MobileLink = "kotlin-web3wallet://"
                },
                new Wallet
                {
                    Name = "React Native Wallet",
                    ImageUrl = "https://github.com/reown-com/reown-dotnet/blob/develop/media/wallet-rn.png?raw=true",
                    MobileLink = "rn-web3wallet://"
                },
                new Wallet
                {
                    Name = "Flutter Wallet Prod",
                    ImageUrl = "https://github.com/reown-com/reown-dotnet/blob/develop/media/wallet-flutter.png?raw=true",
                    MobileLink = "wcflutterwallet://"
                }
            };
#endif
            return null;
        }
    }
}