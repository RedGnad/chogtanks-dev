using System;
using System.Threading.Tasks;

namespace Privy
{
    internal class EmbeddedWalletManager
    {
        private WebViewManager _webViewManager;
        private AuthDelegator _authDelegator;

        private EmbeddedWalletState _embeddedWalletState = new EmbeddedWalletState.Disconnected();

        private Task<string> _connectWalletTask;


        public EmbeddedWalletManager(WebViewManager webViewManager, AuthDelegator authDelegator)
        {
            _webViewManager = webViewManager;
            _authDelegator = authDelegator;

            _authDelegator.OnAuthStateChanged += HandleAuthStateChanged;
        }

        private async void HandleAuthStateChanged(AuthState newState)
        {
            if (newState == AuthState.Authenticated)
            {
                //IMPORTANT, we do not throw errors from here to the developer. They can't catch these errors, as the state change and connect wallet is happening internally
                //Instead, we throw the errors when the user executes an operation that requires a connected wallet
                try
                {
                    await AttemptConnectingToWallet(_authDelegator
                        .GetAuthSession()); //Await so we actually get the error
                }
                catch (Exception ex)
                {
                    PrivyLogger.Error("Could not connect wallet after authenticating", ex);
                }
            }
            else
            {
                PrivyLogger.Debug("Wallet Disconnected");
                _embeddedWalletState = new EmbeddedWalletState.Disconnected();
            }
        }

        // unsubscribe from the event when the manager is disposed of to avoid memory leaks
        public void Dispose()
        {
            _authDelegator.OnAuthStateChanged -= HandleAuthStateChanged;
        }

        internal async Task AttemptConnectingToWallet(InternalAuthSession authSession)
        {
            var primaryWallet = authSession.User.LinkedAccounts.PrimaryEmbeddedWalletAccountOrNull();
            var isOnDeviceWallet = primaryWallet?.IsOnDevice ?? false;
            if (!isOnDeviceWallet)
            {
                // Connection is not necessary for wallets that are not on-device
                return;
            }

            PrivyLogger.Debug("Attempting connect");
            var walletEntropy = authSession.User.LinkedAccounts.WalletEntropyOrNull() ??
                                throw new PrivyException.EmbeddedWalletException(
                                    $"Failed to connect wallet: wallet does not exist",
                                    EmbeddedWalletError
                                        .WalletDoesNotExist); //Let this bubble up to await connected or handle auth state change, as they are caught there

            if (_embeddedWalletState is EmbeddedWalletState.Connected connectedState)
            {
                //No need to connect
                return;
            }

            string token = await _authDelegator.GetAccessToken();
            await ConnectWallet(token, walletEntropy, true);
        }

        private async Task AwaitConnected()
        {
            if (_authDelegator.CurrentAuthState != AuthState.Authenticated)
            {
                throw new Exception("User is not authenticated."); //TBD change this to another exception
            }

            await AttemptConnectingToWallet(_authDelegator.GetAuthSession());
        }


        internal async Task<string> CreateEthereumWallet(string accessToken, string solanaAddress = null)
        {
            var result = await _webViewManager.CreateEthereumWallet(accessToken, solanaAddress);

            if (result is IframeResponseError errorResponse)
            {
                throw new PrivyException.EmbeddedWalletException(
                    $"Failed to create wallet: {errorResponse.Error.Message}",
                    EmbeddedWalletError.CreateFailed); //Let this bubble up to Create
            }
            else if (result is IframeResponseSuccess<CreateEthereumWalletResponseData> walletResponse)
            {
                string connectedWalletAddress = walletResponse.Data.Address;
                return connectedWalletAddress;
            }
            else
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to create wallet",
                    EmbeddedWalletError.CreateFailed); //Let this bubble up to HandleAuthStateChanged and AwaitConnected
            }
        }

        internal async Task<string> CreateSolanaWallet(string accessToken, string ethereumAddress = null)
        {
            var result = await _webViewManager.CreateSolanaWallet(accessToken);

            if (result is IframeResponseError errorResponse)
            {
                throw new PrivyException.EmbeddedWalletException(
                    $"Failed to create wallet: {errorResponse.Error.Message}",
                    EmbeddedWalletError.CreateFailed); //Let this bubble up to Create
            }
            else if (result is IframeResponseSuccess<CreateSolanaWalletResponseData> walletResponse)
            {
                return walletResponse.Data.PublicKey;
            }
            else
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to create wallet",
                    EmbeddedWalletError.CreateFailed); //Let this bubble up to HandleAuthStateChanged and AwaitConnected
            }
        }

        internal async Task<string> CreateAdditionalWallet(string accessToken, WalletEntropy walletEntropy,
            ChainType chainType, int hdWalletIndex)
        {
            var result =
                await _webViewManager.CreateAdditionalWallet(accessToken, walletEntropy, chainType, hdWalletIndex);

            if (result is IframeResponseError errorResponse)
            {
                throw new PrivyException.EmbeddedWalletException(
                    $"Failed to create additional wallet: {errorResponse.Error.Message}",
                    EmbeddedWalletError.CreateAdditionalFailed); //Let this bubble up to Create
            }
            else if (result is IframeResponseSuccess<CreateAdditionalWalletResponseData> walletResponse)
            {
                string connectedWalletAddress = walletResponse.Data.Address;
                return connectedWalletAddress;
            }
            else
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to create additional wallet",
                    EmbeddedWalletError
                        .CreateAdditionalFailed); //Let this bubble up to HandleAuthStateChanged and AwaitConnected
            }
        }

        internal async Task<string> ConnectWallet(string accessToken, WalletEntropy walletEntropy,
            bool attemptRecovery = false)
        {
            if (_embeddedWalletState is EmbeddedWalletState.Connected connectedState)
            {
                return connectedState.WalletAddress;
            }

            // If a connection task is already in progress, return it
            if (_connectWalletTask != null)
            {
                return await _connectWalletTask;
            }

            // Otherwise, start a new connection task
            _connectWalletTask = ConnectWalletWithoutLock(accessToken, walletEntropy, attemptRecovery);

            try
            {
                // Await the connection task and return the result
                return await _connectWalletTask;
            }
            finally
            {
                // Clear the task when it's done, regardless of success or failure
                _connectWalletTask = null;
            }
        }

        private async Task<string> ConnectWalletWithoutLock(string accessToken, WalletEntropy walletEntropy,
            bool attemptRecovery)
        {
            if (_embeddedWalletState is EmbeddedWalletState.Connected connectedState)
            {
                return connectedState.WalletAddress;
            }

            var result = await _webViewManager.ConnectWallet(accessToken, walletEntropy);

            if (result is IframeResponseError errorResponse)
            {
                PrivyLogger.Debug("Error Type: " + errorResponse.Error.Type);
                PrivyLogger.Debug("Error Message: " + errorResponse.Error.Message);

                // Check if we should attempt recovery and if the error type is "wallet_not_on_device"
                if (attemptRecovery && errorResponse.Error.Type == "wallet_not_on_device")
                {
                    PrivyLogger.Debug("Attempting Recovery due to wallet not being on device.");
                    return
                        await RecoverWalletThenTryConnecting(accessToken,
                            walletEntropy); //This could throw an error, which would be bubbled to connect, and caught
                }
                else
                {
                    // Handle other errors or return null
                    throw new PrivyException.EmbeddedWalletException(
                        $"Failed to connect wallet: {errorResponse.Error.Message}",
                        EmbeddedWalletError
                            .ConnectionFailed); //Let this bubble up to HandleAuthStateChanged and AwaitConnected
                }
            }

            else if (result is IframeResponseSuccess<ConnectWalletResponseData> walletResponse)
            {
                // EntropyId equals the wallet address in this case
                string connectedWalletAddress = walletResponse.Data.EntropyId;
                _embeddedWalletState = new EmbeddedWalletState.Connected(connectedWalletAddress);
                return connectedWalletAddress;
            }
            else
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to connect wallet",
                    EmbeddedWalletError
                        .ConnectionFailed); //Let this bubble up to HandleAuthStateChanged and AwaitConnected
            }
        }


        internal async Task<string> RecoverWalletThenTryConnecting(string accessToken, WalletEntropy walletEntropy)
        {
            //This method will be used when there's an error in connecting wallet
            //Makes a request to recover the wallet
            //If recovery is successful, then we can connect without an error
            var result = await _webViewManager.RecoverWallet(accessToken, walletEntropy);

            if (result is IframeResponseSuccess<RecoverWalletResponseData> walletResponse)
            {
                // After recovery, call ConnectWalletWithoutLock to avoid re-locking
                return await ConnectWalletWithoutLock(accessToken, walletEntropy,
                    false); // No recovery on second attempt
            }
            else if (result is IframeResponseError errorResponse)
            {
                throw new PrivyException.EmbeddedWalletException(
                    $"Failed to recover wallet: {errorResponse.Error.Message}",
                    EmbeddedWalletError.RecoverFailed); //Let this bubble up to connect wallet without lock
            }
            else
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to recover wallet",
                    EmbeddedWalletError.RecoverFailed); //Let this bubble up to connect wallet without lock
            }
        }

        internal async Task<RpcResponseData.IRpcResponseDetails> Request(WalletEntropy walletEntropy,
            ChainType chainType, int hdWalletIndex, RpcRequestData.IRpcRequestDetails request)
        {
            if (_embeddedWalletState is EmbeddedWalletState.Disconnected)
            {
                //This could throw an EmbeddedWalletException, due to a failure with connecting the wallet
                //Developer needs to catch this
                await AwaitConnected();
            }

            string token = await _authDelegator.GetAccessToken();
            var result = await _webViewManager.Request(token, walletEntropy, chainType, hdWalletIndex, request);

            if (result is IframeResponseSuccess<RpcResponseData> rpcResponseData)
            {
                return rpcResponseData.Data.Response;
            }
            else if (result is IframeResponseError errorResponse)
            {
                throw new PrivyException.EmbeddedWalletException(
                    $"Failed to execute RPC Request: {errorResponse.Error.Message}",
                    EmbeddedWalletError.RpcRequestFailed); //Let this bubble up to developer RPC Request
            }
            else
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to execute RPC Request",
                    EmbeddedWalletError.RpcRequestFailed); //Let this bubble up to developer RPC Request
            }
        }

        internal async Task<byte[]> SignWithUserSigner(string accessToken, byte[] message)
        {
            var result = await _webViewManager.SignWithUserSigner(accessToken, message);

            if (result is IframeResponseError errorResponse)
            {
                throw new PrivyException.EmbeddedWalletException(
                    $"Failed to sign with the user's authorization key: {errorResponse.Error.Message}",
                    EmbeddedWalletError.UserSignerRequestFailed);
            }

            if (result is IframeResponseSuccess<UserSignerSignResponseData> walletResponse)
            {
                string signatureAsBase64 = walletResponse.Data.Signature;
                return Convert.FromBase64String(signatureAsBase64);
            }

            throw new PrivyException.EmbeddedWalletException($"Failed to create additional wallet",
                EmbeddedWalletError
                    .CreateAdditionalFailed); //Let this bubble up to HandleAuthStateChanged and AwaitConnected
        }
    }
}