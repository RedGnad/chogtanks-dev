using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Privy
{
    internal class OAuthWebGLPopupFlow : OAuthFlow
    {
        public OAuthWebGLPopupFlow()
        {
            oAuthInit();
        }

        public async Task<OAuthResultData> PerformOAuthFlow(string oAuthUrl, string redirectUri)
        {
            var oauthFlowTaskSource = new TaskCompletionSource<OAuthResultData>();

            var oauthCallbackGameObject = new GameObject("WebGLPopupMessageInterceptor");
            var interceptor =
                oauthCallbackGameObject.AddComponent<WebGLPopupMessageInterceptor>();

            interceptor.OnSignedIn += payload =>
            {
                try
                {
                    var result = JsonConvert.DeserializeObject<OAuthResultData>(payload) ??
                                 throw new NullReferenceException();
                    oauthFlowTaskSource.SetResult(result);
                }
                catch
                {
                    oauthFlowTaskSource.SetException(new PrivyException.AuthenticationException("OAuth failure",
                        AuthenticationError.OAuthVerificationFailed));
                }
            };

            interceptor.OnSignInFailed += () =>
            {
                oauthFlowTaskSource.SetException(new PrivyException.AuthenticationException("OAuth failure",
                    AuthenticationError.OAuthVerificationFailed));
            };

            oAuthSignIn(oAuthUrl);

            var result = await oauthFlowTaskSource.Task;

            Object.Destroy(oauthCallbackGameObject);

            return result;
        }

#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void oAuthInit();

        [DllImport("__Internal")]
        private static extern void oAuthSignIn(string oAuthUrl);
#else
        private static void oAuthInit()
        {
            throw new NotImplementedException("OAuthWebGLPopupFlow is only supported on WebGL builds");
        }

        private static void oAuthSignIn(string oAuthUrl)
        {
            throw new NotImplementedException("OAuthWebGLPopupFlow is only supported on WebGL builds");
        }
#endif
    }
}