using UnityEngine;

namespace Privy
{
    /// <summary>
    /// An <see href="https://refactoring.guru/design-patterns/adapter">adapter</see> class for the <see cref="WebViewObject"/>
    /// class in <c>unity-webview</c>.
    /// It also instantiates a singleton <see cref="GameObject"/> on scene load (marked as <see cref="Object.DontDestroyOnLoad"/>).
    /// </summary>
    /// <remarks>
    /// The behavior class provided by the library does not include instantiation, so that is covered here.
    /// </remarks>
    /// <seealso cref="SingletonGameObjectName"/>
    internal class NativeWebViewObject : MonoBehaviour
    {
        private WebViewObject wrapped;
        private WebViewManager _webViewManager;

        // TODO: Use an interface to redirect calls instead of exposing the wrapped instance
        internal WebViewObject Wrapped => wrapped;

        /// <summary>
        /// The name of the singleton <see cref="GameObject"/> instantiated by <see cref="BrowserDomIframeObject"/>
        /// </summary>
        public const string SingletonGameObjectName = "WebViewObject";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBridge()
        {
#if UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            var webViewGameObject = new GameObject(SingletonGameObjectName);
            var nativeWebViewObject = webViewGameObject.AddComponent<NativeWebViewObject>();
            nativeWebViewObject.wrapped = webViewGameObject.AddComponent<WebViewObject>();
            DontDestroyOnLoad(webViewGameObject);
#endif
        }
    }
}