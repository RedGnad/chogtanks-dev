using System;
using System.Threading.Tasks;

namespace Privy
{
    internal class AuthDelegator : IAuthDelegator
    {
        private IAuthRepository _authRepository;
        private InternalAuthSessionStorage _internalAuthSessionStorage;
        private InternalAuthSession _internalAuthSession;
        public AuthState CurrentAuthState; //Public property for privy class to access
        private Task _refreshSessionTask;

        private void UpdateAuthState(AuthState newState)
        {
            if (CurrentAuthState != newState)
            {
                CurrentAuthState = newState;
                OnAuthStateChanged?.Invoke(CurrentAuthState); // Trigger the callback
            }
        }


        public delegate void AuthStateChangedHandler(AuthState newState);

        public event AuthStateChangedHandler OnAuthStateChanged;

        public AuthDelegator(IAuthRepository authRepository, InternalAuthSessionStorage internalAuthSessionStorage)
        {
            _authRepository = authRepository;
            _internalAuthSessionStorage = internalAuthSessionStorage;
            UpdateAuthState(AuthState.NotReady);
        }

        public void SetAuthStateChangeCallback(Action<AuthState> callback)
        {
            OnAuthStateChanged += new AuthStateChangedHandler(callback);
            callback(CurrentAuthState);
        }


        //Methods to trigger Auth Repository
        public async Task<bool> SendEmailCode(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                //This check saves us from making a request we know will faill
                throw new PrivyException.AuthenticationException("Email cannot be null or empty",
                    AuthenticationError.EmailEmpty);
            }

            bool successfullySentCode = await _authRepository.SendEmailCode(email);

            PrivyLogger.Debug($"Successfully sent OTP code to {email}: {successfullySentCode}");

            return successfullySentCode;
        }

        public async Task<AuthState> LoginWithEmailCode(string email, string code)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new PrivyException.AuthenticationException("Email cannot be null or empty",
                    AuthenticationError.EmailEmpty);
            }

            if (string.IsNullOrEmpty(code))
            {
                throw new PrivyException.AuthenticationException("Code cannot be null or empty",
                    AuthenticationError.OtpEmpty);
            }

            InternalAuthSession authSession = await _authRepository.LoginWithEmailCode(email, code);

            //Check if auth session was successful
            if (authSession == null || authSession.User == null)
            {
                UpdateAuthState(AuthState.Unauthenticated);
                //This gets thrown if request is successful, but the internal auth session does not have accurate data
                throw new PrivyException.AuthenticationException("Could not sign in, invalid OTP",
                    AuthenticationError.WrongOtpCode);
            }

            SetInternalAuthSession(authSession);
            return AuthState.Authenticated;
        }

        public async Task<string> InitiateOAuthFlow(OAuthProvider provider, string codeChallenge, string redirectUri,
            string stateCode)
        {
            if (string.IsNullOrEmpty(codeChallenge))
            {
                throw new PrivyException.AuthenticationException("Code Challenge cannot be null or empty",
                    AuthenticationError.CodeChallengeEmpty);
            }

            if (string.IsNullOrEmpty(stateCode))
            {
                throw new PrivyException.AuthenticationException("State Code cannot be null or empty",
                    AuthenticationError.StateCodeEmpty);
            }

            if (string.IsNullOrEmpty(redirectUri))
            {
                throw new PrivyException.AuthenticationException("Redirect URI cannot be null or empty",
                    AuthenticationError.RedirectUriEmpty);
            }

            var response = await _authRepository.InitiateOAuthFlow(provider, codeChallenge, redirectUri, stateCode);
            return response.OAuthUrl;
        }

        public async Task<AuthState> AuthenticateOAuthFlow(string authorizationCode, string codeVerifier,
            string stateCode, bool isRawFlow = false)
        {
            if (string.IsNullOrEmpty(authorizationCode))
            {
                throw new PrivyException.AuthenticationException("Authorization Code cannot be null or empty",
                    AuthenticationError.AuthorizationCodeEmpty);
            }

            if (string.IsNullOrEmpty(codeVerifier))
            {
                throw new PrivyException.AuthenticationException("Code Verifier cannot be null or empty",
                    AuthenticationError.CodeVerifierEmpty);
            }

            if (string.IsNullOrEmpty(stateCode))
            {
                throw new PrivyException.AuthenticationException("State Code cannot be null or empty",
                    AuthenticationError.StateCodeEmpty);
            }

            var authSession =
                await _authRepository.AuthenticateOAuthFlow(authorizationCode, codeVerifier, stateCode, isRawFlow);

            //Check if auth session was successful
            if (authSession == null || authSession.User == null)
            {
                UpdateAuthState(AuthState.Unauthenticated);
                throw new PrivyException.AuthenticationException("Could not sign in, invalid OAuth result",
                    AuthenticationError.InvalidOAuthResult);
            }

            SetInternalAuthSession(authSession);
            return AuthState.Authenticated;
        }

        //Methods for persisting storage
        public async Task RestoreSession()
        {
            //To handle errors here, we should simply log out
            //Wrapping it all in a try catch is the simple solution, although we could get more granular and see what's failing
            //For example the retrieval from storage, or the token parsing could be failing
            //Regardless, any errors should just lead to a logout, as we don't want to throw an exception for the dev to catch here, given that this is called on initialize

            try
            {
                InternalAuthSession persistedSession =
                    _internalAuthSessionStorage.RetrieveInternalAuthSessionFromStorage();


                if (persistedSession != null)
                {
                    Token jwt = Token.Parse(persistedSession.AccessToken);
                    InternalAuthSession newSession = persistedSession;


                    if (jwt != null && jwt.IsExpired(Constants.DEFAULT_EXPIRATION_PADDING_IN_SECONDS))
                    {
                        newSession = await _authRepository.RefreshSession(persistedSession.AccessToken,
                            persistedSession.RefreshToken); //could be null if request fails
                    }

                    SetInternalAuthSession(newSession, persistedSession);
                }
                else
                {
                    Logout();
                }
            }
            catch
            {
                Logout();
            }
        }

        internal async Task RefreshSession(bool forceRefresh = false)
        {
            //Here we are mimicking a lock, as we may be potentially calling RefreshSession in multiple places
            //If GetAccessToken() and RefreshSession are called at the same time (currently not possible)
            //If an RPC Method is spammed, and multiple refreshes are triggered to get access token
            if (_refreshSessionTask != null)
            {
                // If a refresh is already in progress, await the existing task
                await _refreshSessionTask;
                return;
            }

            _refreshSessionTask = RefreshSessionWithoutLock(forceRefresh);

            try
            {
                // Await the refresh operation
                await _refreshSessionTask;
            }
            finally
            {
                // Clear the task so future refreshes can start a new operation
                _refreshSessionTask = null;
            }
        }

        private async Task RefreshSessionWithoutLock(bool forceRefresh)
        {
            // Check if a refresh is needed based on token validity or forceRefresh flag
            Token jwt = Token.Parse(_internalAuthSession.AccessToken);
            bool needsRefresh = forceRefresh ||
                                (jwt != null && jwt.IsExpired(Constants.DEFAULT_EXPIRATION_PADDING_IN_SECONDS));

            if (!needsRefresh)
            {
                // If no refresh is needed, return early to prevent new tokens being fetched
                return;
            }

            // Proceed with the refresh
            var newSession =
                await _authRepository.RefreshSession(_internalAuthSession.AccessToken,
                    _internalAuthSession.RefreshToken);
            SetInternalAuthSession(newSession);
        }

        internal async Task RefreshSessionIfNeeded()
        {
            if (_internalAuthSession == null || CurrentAuthState != AuthState.Authenticated)
            {
                throw new PrivyException.AuthenticationException($"User is not authenticated",
                    AuthenticationError.NotAuthenticated);
            }

            Token jwt = Token.Parse(_internalAuthSession.AccessToken);

            if (jwt != null && jwt.IsExpired(Constants.DEFAULT_EXPIRATION_PADDING_IN_SECONDS))
            {
                await RefreshSession(); //will trigger refresh due to jwt being expired
            }
        }

        //Methods for in-memory storage
        private void SetInternalAuthSession(InternalAuthSession authSession,
            InternalAuthSession persistedSession = null)
        {
            //Either a new session, or a refreshed session


            string sessionUpdateAction = authSession.SessionUpdateAction; //make this an enum

            switch (sessionUpdateAction)
            {
                case "set":
                    // Save tokens in keychain and update auth state to authenticated
                    _internalAuthSessionStorage.SaveInternalAuthSessionInStorage(authSession);
                    _internalAuthSession = authSession;
                    UpdateAuthState(AuthState.Authenticated);
                    break;

                case "clear":
                    // Log user out, which handles clearing session state
                    Logout();
                    break;

                case "ignore":
                    //Handle ignore
                    _internalAuthSessionStorage.SaveInternalAuthSessionInStorage(authSession);
                    _internalAuthSession = authSession;
                    UpdateAuthState(AuthState.Authenticated);
                    break;
                // NOTE: the above logic is still not ideal management. Ideal checks are outlined in the Slack thread:
                // https://privyio.slack.com/archives/C06QTV3RQDS/p1718735684071109?thread_ts=1718690205.889179&cid=C06QTV3RQDS
                // "We should only ever ignore as we're not expecting anything. Realistically, my ideal logic is for ignore:
                // If res.token && !previousToken - set to do the auto-heal
                // If res.token && previousToken: set the one with the higher iss (issuedAt) aka the newest one
                // if user && !previousUser - set
                // if user && previousUser - set the one with the newer updatedAt"                    
            }
        }


        internal async Task<string> GetAccessToken()
        {
            await RefreshSessionIfNeeded(); //This could throw an error, which would bubble up to create/connect wallet
            //Session will be refreshed by now, new access token
            return _internalAuthSession.AccessToken;
        }

        internal InternalAuthSession GetAuthSession()
        {
            return _internalAuthSession;
        }

        public void Logout()
        {
            // Placeholder implementation
            UpdateAuthState(AuthState.Unauthenticated);
            _internalAuthSessionStorage.ClearInternalAuthSessionInStorage();
            _internalAuthSession = null;
        }
    }
}