// This class implements the functionality of loading URLs, sending messages, and handling message reception 
// specifically within the context of a native WebView in Unity.

// The class is also responsible for managing a WebViewObject, which is a native implementation of a webview in Unity. 

#if UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using UnityEngine;

namespace Privy
{
    internal class WebViewHandler : IWebViewHandler
    {
        private readonly NativeWebViewObject webViewObject;
        private readonly WebViewManager _webViewManager;

        internal WebViewHandler(WebViewManager webViewManager)
        {
            _webViewManager = webViewManager;

            // Create a GameObject to host the WebViewObject
            webViewObject = GameObject.FindObjectOfType<NativeWebViewObject>();

            // Initialize the WebViewObject
            webViewObject.Wrapped.Init(
                cb: (msg) => _webViewManager.OnMessageReceived(msg),
                err: (msg) => PrivyLogger.Error($"Error from WebView: {msg}"),
                httpErr: (msg) => PrivyLogger.Error($"HTTP error from WebView: {msg}"),
                ld: (msg) => OnPageLoaded(msg),
                enableWKWebView: true
            );
        }

        public void LoadUrl(string url)
        {
            if (webViewObject != null)
            {
                webViewObject.Wrapped.LoadURL(url);
            }
        }

        internal void OnPageLoaded(string url)
        {
            PrivyLogger.Debug($"Loaded URL: {url}");

            // Inject JavaScript into the page (non-WebGL)
            var js = @"
            window.PRIVY_UNITY = true; //Tells the privy page what the client is

            //This is mimicking Android's AddJavascriptInterface
            //This is essentially injecting an object into the webpage, and forwarding messages to the platform (Android, or Unity in this case)
            //What this does is allow the embedded wallet handler to call window.UnityProxy.postMessage, and for the message to then be received in the platform
            window.UnityProxy = {
                postMessage: function(message) {
                    window.location = `unity:${encodeURIComponent(message)}`; //This is what triggers the webview message handler
                }
            };
        ";


            webViewObject.Wrapped.EvaluateJS(js); //Initialize Handlers
            PrivyLogger.Debug("Injected JavaScript into WebView.");

            _webViewManager.PingReadyUntilSuccessful();
        }

        public void SendMessage(string message)
        {
            string jsDispatchEvent = $@"
            window.dispatchEvent(new MessageEvent('message', {{ data: {message} }}));
        ";
            webViewObject.Wrapped.EvaluateJS(jsDispatchEvent);
        }
    }
}
#endif