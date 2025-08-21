using System;
using System.Threading.Tasks;

namespace Privy
{
    internal class OAuthIOSWebAuthenticationFlow : OAuthFlow
    {
        public async Task<OAuthResultData> PerformOAuthFlow(string oAuthUrl, string redirectUri)
        {
            var oauthFlowTaskSource = new TaskCompletionSource<OAuthResultData>();

            var redirectScheme = new Uri(redirectUri).Scheme;

            using var authSession = new ASWebAuthenticationSession(oAuthUrl, redirectScheme,
                (uri, error) =>
                {
                    if (error != null)
                    {
                        oauthFlowTaskSource.SetException(new PrivyException.AuthenticationException(error.Message,
                            AuthenticationError.OAuthVerificationFailed));
                        return;
                    }

                    var result = OAuthResultData.parseFromUri(uri);
                    oauthFlowTaskSource.SetResult(result);
                });

            authSession.Start();
            return await oauthFlowTaskSource.Task;
        }
    }
}