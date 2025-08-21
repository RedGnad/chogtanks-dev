using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Privy
{
    internal class OAuthExternalBrowserFlow : OAuthFlow
    {
        private TaskCompletionSource<OAuthResultData> oauthFlowTaskSource;

        public async Task<OAuthResultData> PerformOAuthFlow(string oAuthUrl, string redirectUri)
        {
            PrivyLogger.Debug("Performing OAuth flow");

            oauthFlowTaskSource = new TaskCompletionSource<OAuthResultData>();

            Application.deepLinkActivated += OnDeepLinkActivated;

            try
            {
                PrivyLogger.Debug($"Attempting to open url: {oAuthUrl}");
                Application.OpenURL(oAuthUrl);
                return await oauthFlowTaskSource.Task;
            }
            finally
            {
                Application.deepLinkActivated -= OnDeepLinkActivated;
            }
        }

        private void OnDeepLinkActivated(string url)
        {
            PrivyLogger.Debug($"Deeplink activated w/ url: {url}");
            var uri = new Uri(url);
            var result = OAuthResultData.parseFromUri(uri);
            oauthFlowTaskSource.SetResult(result);
        }
    }
}