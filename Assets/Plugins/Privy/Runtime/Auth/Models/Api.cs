using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;

namespace Privy
{
    //Request Models

    //ideally would use record structs here but c# version limitation, still potentially room for improvement
    //immutable req/resp body data types, ex usage: new SendCodeRequestData('moiz@privy.io')
    internal class SendCodeRequestData
    {
        [JsonProperty("email")]
        public string Email;
    }

    internal class LogInRequestData
    {
        [JsonProperty("email")]
        public string Email;

        [JsonProperty("code")]
        public string Code;
    }

    internal class SendRefreshRequestData
    {
        [JsonProperty("refresh_token")]
        public string RefreshToken;
    }

    internal class InitiateOAuthFlowRequestData
    {
        [JsonProperty("provider")]
        public OAuthProvider ProviderName;

        [JsonProperty("redirect_to")]
        public string RedirectUri;

        [JsonProperty("code_challenge")]
        public string CodeChallenge;

        [JsonProperty("state_code")]
        public string StateCode;
    }

    internal class AuthenticateOAuthFlowRequestData
    {
        [JsonProperty("authorization_code")]
        public string AuthorizationCode;

        [JsonProperty("code_verifier")]
        public string CodeVerifier;

        [JsonProperty("state_code")]
        public string StateCode;

        [JsonProperty("code_type", NullValueHandling = NullValueHandling.Ignore)]
        public AuthenticateOAuthFlowCodeType? CodeType;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum AuthenticateOAuthFlowCodeType
    {
        [EnumMember(Value = "raw")]
        Raw
    }

    //Response Models
    internal class SendCodeResponseData
    {
        [JsonProperty("success")]
        public bool Success;
    }

    internal class UserResponse
    {
        [JsonProperty("id")]
        public string Id;

        // TODO: convert these to dates
        [JsonProperty("created_at")]
        public long CreatedAt;

        [JsonProperty("has_accepted_terms")]
        public bool HasAcceptedTerms;

        [JsonProperty("linked_accounts")]
        public LinkedAccountResponse[] LinkedAccounts;

        [JsonProperty("custom_metadata")]
        public Dictionary<string, string> CustomMetadata = new Dictionary<string, string>();

        // TODO: implement mfa_methods
    }

    internal class ValidSessionResponse
    {
        [JsonProperty("user")]
        public UserResponse User;

        [JsonProperty("is_new_user")]
        public bool IsNewUser;

        public string Token;

        [JsonProperty("refresh_token")]
        public string RefreshToken;

        [JsonProperty("identity_token")]
        public string IdentityToken;

        [JsonProperty("session_update_action")]
        public string SessionUpdateAction;
    }

    internal class InitiateOAuthFlowResponse
    {
        [JsonProperty("url")]
        public string OAuthUrl;
    }
}