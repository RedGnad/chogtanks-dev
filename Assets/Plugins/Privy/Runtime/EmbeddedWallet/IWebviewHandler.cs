// This interface defines the common functionality shared by both WebView and Iframe handlers.
// 
// The purpose of this interface is to abstract the differences between WebView and Iframe implementations,
// allowing the WebViewManager to interact with both through a consistent interface.
// 
// While WebView and Iframe handle certain tasks differently, such as loading URLs and sending messages,
// this interface ensures that the WebViewManager can manage these components in a unified manner, 
// without needing to worry about the underlying implementation details.

namespace Privy
{
    public interface IWebViewHandler
    {
        void LoadUrl(string url);
        void SendMessage(string message);

        internal static IWebViewHandler GetPlatformWebViewHandler(WebViewManager webViewManager,
            PrivyConfig privyConfig)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new BrowserDomIframeHandler(webViewManager);
#elif UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            return new WebViewHandler(webViewManager);
#else
            return new WebViewHandlerForUnsupportedPlatform();
#endif
        }
    }
}