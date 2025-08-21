using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Privy
{
    internal class AuthRepository : IAuthRepository
    {
        private IHttpRequestHandler _httpRequestHandler;

        public AuthRepository(IHttpRequestHandler httpRequestHandler)
        {
            _httpRequestHandler = httpRequestHandler;
        }

        public async Task<bool> SendEmailCode(string email)
        {
            var requestData = new SendCodeRequestData
            {
                Email = email
            };


            string serializedRequest = JsonConvert.SerializeObject(requestData);

            string path = "passwordless/init";

            // Execute the request

            try
            {
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest);
                var response = JsonConvert.DeserializeObject<SendCodeResponseData>(jsonResponse);
                return response.Success; //this response could be failure, which is why return type is a bool
            }
            catch (Exception ex)
            {
                throw new PrivyException.AuthenticationException($"Failed to send email code: {ex.Message}",
                    AuthenticationError.SendCodeFailed);
            }
        }

        public async Task<InternalAuthSession> LoginWithEmailCode(string email, string code)
        {
            //Construct Request Data + Path
            var requestData = new LogInRequestData
            {
                Email = email,
                Code = code
            };

            string path = "passwordless/authenticate";

            //Serialize the request data
            string serializedRequest = JsonConvert.SerializeObject(requestData);


            //TODO: Catch errors here, or return null
            try
            {
                // Execute the request
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest);

                //Deserialize Response
                ValidSessionResponse authResponse = DeserializeSessionResponse(jsonResponse);

                //Mapping To Internal
                InternalAuthSession _internalAuthSession = AuthSessionResponseMapper.MapToInternalSession(authResponse);

                return _internalAuthSession;
            }
            catch (Exception ex)
            {
                //This catches request failures
                throw new PrivyException.AuthenticationException($"Failed to login with email code: {ex.Message}",
                    AuthenticationError.WrongOtpCode);
            }
        }

        public async Task<InitiateOAuthFlowResponse> InitiateOAuthFlow(OAuthProvider provider, string codeChallenge,
            string redirectUri, string stateCode)
        {
            var requestData = new InitiateOAuthFlowRequestData
            {
                ProviderName = provider,
                CodeChallenge = codeChallenge,
                RedirectUri = redirectUri,
                StateCode = stateCode
            };

            string path = "oauth/init";

            string serializedRequest = JsonConvert.SerializeObject(requestData);

            try
            {
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest);

                InitiateOAuthFlowResponse initOauthResponse =
                    JsonConvert.DeserializeObject<InitiateOAuthFlowResponse>(jsonResponse);

                return initOauthResponse;
            }
            catch (Exception ex)
            {
                throw new PrivyException.AuthenticationException($"Failed to initiate OAuth: {ex.Message}",
                    AuthenticationError.OAuthInitFailed);
            }
        }

        public async Task<InternalAuthSession> AuthenticateOAuthFlow(string authorizationCode, string codeVerifier,
            string stateCode, bool isRawFlow = false)
        {
            var requestData = new AuthenticateOAuthFlowRequestData
            {
                AuthorizationCode = authorizationCode,
                CodeVerifier = codeVerifier,
                StateCode = stateCode,
                CodeType = isRawFlow ? AuthenticateOAuthFlowCodeType.Raw : null
            };

            string path = "oauth/authenticate";

            string serializedRequest = JsonConvert.SerializeObject(requestData);

            try
            {
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest);

                ValidSessionResponse authResponse = DeserializeSessionResponse(jsonResponse);

                InternalAuthSession _internalAuthSession = AuthSessionResponseMapper.MapToInternalSession(authResponse);
                return _internalAuthSession;
            }
            catch (Exception ex)
            {
                throw new PrivyException.AuthenticationException($"Failed to authenticate with OAuth: {ex.Message}",
                    AuthenticationError.OAuthAuthenticateFailed);
            }
        }

        public async Task<InternalAuthSession> RefreshSession(string accessToken, string refreshToken)
        {
            //This Refresh Session is called by multiple methods
            //It's called on initialize by the Restore, but also called by CreateWallet, to initially get a valid access token + to refresh after wallet creation
            string path = "sessions";


            SendRefreshRequestData requestData = new SendRefreshRequestData
            {
                RefreshToken = refreshToken
            };


            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer " + accessToken }
            };

            //Serialize the request data
            string serializedRequest = JsonConvert.SerializeObject(requestData);

            try
            {
                // Execute the request
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest, headers);

                // Deserialize Response
                ValidSessionResponse authResponse = DeserializeSessionResponse(jsonResponse);

                // Mapping To Internal
                InternalAuthSession _internalAuthSession = AuthSessionResponseMapper.MapToInternalSession(authResponse);

                return _internalAuthSession;
            }
            catch (Exception ex)
            {
                throw new PrivyException.AuthenticationException($"Failed to refresh session: {ex.Message}",
                    AuthenticationError.RefreshFailed);
            }
        }

        private ValidSessionResponse DeserializeSessionResponse(string jsonResponse)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Converters = new List<JsonConverter> { new LinkedAccountConverter() }
            };

            ValidSessionResponse authResponse =
                JsonConvert.DeserializeObject<ValidSessionResponse>(jsonResponse, settings);
            return authResponse;
        }
    }
}