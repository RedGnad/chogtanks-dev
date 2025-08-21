using System.Threading.Tasks;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace Privy
{
    internal interface OAuthFlow
    {
        Task<OAuthResultData> PerformOAuthFlow(string oAuthUrl, string redirectUri);

        string TransformRedirectUrl(string redirectUrl) => redirectUrl;

        internal static OAuthFlow GetPlatformOAuthFlow()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return new OAuthIOSWebAuthenticationFlow();
                case RuntimePlatform.WebGLPlayer:
                    return new OAuthWebGLPopupFlow();
                case RuntimePlatform.OSXEditor:
                    return new OAuthInEditorFlow();
                default:
                    return new OAuthExternalBrowserFlow();
            }
        }
    }
}