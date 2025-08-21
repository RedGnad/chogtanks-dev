// This class handles all logic related to managing the iframe, including injection and message communication.
// 
// Why we're using an iframe:
// In WebGL builds, there is no native support for WebView functionality. To simulate WebView behavior,
// webview packages typically inject an iframe under the hood. However, using these iframes presents certain limitations.
// 
// Key reasons for our custom iframe implementation:
// 1. Control Limitations: The webview package used in this SDK injects an iframe, but it does not support 
//    the use of `postMessage` for communication. Our only alternative with these packages is to use `eval`, 
//    which is blocked when running cross-origin, limiting functionality.
// 
// 2. Origin Security: When dealing with iframes, especially in cross-origin contexts, it’s critical to perform 
//    security checks, such as verifying the origin of messages before processing them. This is necessary to 
//    prevent potential security risks like accepting messages from untrusted sources.
// 
// 3. Message Control: With the custom iframe implementation, we have full control over when and how messages 
//    are sent and received, ensuring that communication between the iframe and Unity is secure and reliable.
//    For example, the endpoint sends us JSON objects, which we need to stringify to send to unity
// 
// Due to these limitations and the need for greater control, we opted for a custom implementation. 
// By directly injecting the iframe and managing all message communication ourselves, we gain the flexibility 
// and security required for this SDK.

#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using UnityEngine;

namespace Privy
{
    internal class BrowserDomIframeHandler : IWebViewHandler
    {
        private readonly BrowserDomIframeObject _browserDomIframeObject;
        private readonly WebViewManager _webViewManager; // Reference to WebViewManager

        internal BrowserDomIframeHandler(WebViewManager webViewManager)
        {
            _webViewManager = webViewManager; // Assign WebViewManager reference

            _browserDomIframeObject = GameObject.FindObjectOfType<BrowserDomIframeObject>();

            _browserDomIframeObject.Initialize(webViewManager);
        }

        public void LoadUrl(string url)
        {
            InjectIframe(url);
        }

        public void SendMessage(string message)
        {
            // Properly escape the message to be a valid JavaScript string literal
            string jsMessage = System.Web.HttpUtility.JavaScriptStringEncode(message);

            string jsCode = $@"
            var iframe = document.getElementById('myIframe');

            // Wrap the message in quotes for JavaScript
            var message = '{jsMessage}';

            if (message.includes('iframe')) {{
                //The ready message should ensure that the iframe is loaded
                iframe.onload = function() {{
                    if (iframe && iframe.contentWindow) {{
                        iframe.contentWindow.postMessage(message, '{PrivyEnvironment.BASE_URL}');
                    }}
                }};
            }} else {{
                if (iframe && iframe.contentWindow) {{
                    iframe.contentWindow.postMessage(message, '{PrivyEnvironment.BASE_URL}');
                }}                
            }}
        ";
            Application.ExternalEval(jsCode);
        }

        private void InjectIframe(string url)
        {
            //This function is executing some javascript into the WebGL build
            //This javascript essentially creating an iframe in the DOM, and making it headless by setting the display value to none
            //The source of the iframe, is the Privy embedded wallet url
            //We also add an event listener on to the page, to listen to events coming from the iframe
            string jsCode = $@"
            var iframe = document.createElement('iframe');
            iframe.id = 'myIframe';
            iframe.style.position = 'absolute';
            iframe.style.display = 'none';
            iframe.src = '{url}';
            document.body.appendChild(iframe);

            window.addEventListener('message', function(event) {{
                // Check that the message is coming from the correct origin
                if (event.origin === new URL(iframe.src).origin) {{
                    if(event.data === 'ready') {{
                        unityInstance.SendMessage('{BrowserDomIframeObject.SingletonGameObjectName}', 'OnWebViewReady', '');
                    }} else {{
                        let data = JSON.stringify(event.data);
                        unityInstance.SendMessage('{BrowserDomIframeObject.SingletonGameObjectName}', 'OnMessageReceived', data);
                    }}
                }} else {{
                    console.warn('Message received from unknown origin:', event.origin);
                }}
            }});
        ";

            Application.ExternalEval(jsCode);

            // Trigger PingReadyUntilSuccessful after injecting the iframe
            _webViewManager.PingReadyUntilSuccessful();        
        }
    }
}
#endif