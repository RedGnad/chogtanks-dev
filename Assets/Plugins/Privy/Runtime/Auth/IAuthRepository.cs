using System.Threading.Tasks;

namespace Privy
{
    internal interface IAuthRepository
    {
        Task<bool> SendEmailCode(string email);
        Task<InternalAuthSession> LoginWithEmailCode(string email, string code);
        Task<InternalAuthSession> RefreshSession(string accessToken, string refreshToken);

        Task<InitiateOAuthFlowResponse> InitiateOAuthFlow(OAuthProvider provider, string codeChallenge,
            string redirectUri, string stateCode);

        Task<InternalAuthSession> AuthenticateOAuthFlow(string authorizationCode, string codeVerifier, string stateCode,
            bool isRawFlow = false);
    }
}