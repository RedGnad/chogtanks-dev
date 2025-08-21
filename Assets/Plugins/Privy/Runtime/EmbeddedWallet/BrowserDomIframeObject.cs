using UnityEngine;

namespace Privy
{
    /// <summary>
    /// A <see cref="MonoBehaviour"/> for enabling communication between an iframe element in the browser's dom and Unity.
    /// It also instantiates a singleton <see cref="GameObject"/> on scene load (marked as <see cref="Object.DontDestroyOnLoad"/>).
    /// </summary>
    /// <remarks>
    /// The Unity instance in a WebGL build can only communicate with a game object, it can't send messages to a static script.
    /// This class is used to receive messages from the DOM (via <c>unityInstance.SendMessage</c>),
    /// and then send those messages to the <see cref="WebViewManager"/> class to handle.
    /// </remarks>
    /// <seealso cref="SingletonGameObjectName"/>
    internal class BrowserDomIframeObject : MonoBehaviour
    {
        private WebViewManager _webViewManager;

        public void Initialize(WebViewManager webViewManager)
        {
            _webViewManager = webViewManager;
        }

        public void OnMessageReceived(string message)
        {
            //Unity sends message to this function, as this is the game object it can talk to
            //Then we trigger our actual message handler
            _webViewManager?.OnMessageReceived(message);
        }

        public void OnWebViewReady()
        {
            _webViewManager?.OnWebViewReady();
        }

        /// <summary>
        /// The name of the singleton <see cref="GameObject"/> instantiated by <see cref="BrowserDomIframeObject"/>
        /// </summary>
        public const string SingletonGameObjectName = "BrowserDomIframeObject";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBridge()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var browserDomIframeGameObject = new GameObject(SingletonGameObjectName);
            browserDomIframeGameObject.AddComponent<BrowserDomIframeObject>();
            DontDestroyOnLoad(browserDomIframeGameObject);
#endif
        }
    }
}