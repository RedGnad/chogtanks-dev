using System;
using System.Web;
using Newtonsoft.Json;

namespace Privy
{
    internal class OAuthResultData
    {
        [JsonProperty("authorizationCode")]
        public string OAuthCode;

        [JsonProperty("stateCode")]
        public string OAuthState;

        public static OAuthResultData parseFromUri(Uri uri)
        {
            var queryParams = HttpUtility.ParseQueryString(uri.Query);

            var oauthState = queryParams["privy_oauth_state"];
            var oauthCode = queryParams["privy_oauth_code"];

            return new OAuthResultData
            {
                OAuthCode = oauthCode,
                OAuthState = oauthState
            };
        }
    }
}