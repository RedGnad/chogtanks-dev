using UnityEngine;

namespace Privy
{
    public class WebViewHandlerForUnsupportedPlatform : IWebViewHandler
    {
        public void LoadUrl(string url)
        {
            Debug.LogWarning($"IWebViewHandler::LoadUrl called on unsupported platform: {Application.platform}.");
        }

        public void SendMessage(string message)
        {
            Debug.LogWarning($"IWebViewHandler::SendMessage called on unsupported platform: {Application.platform}.");
        }
    }
}