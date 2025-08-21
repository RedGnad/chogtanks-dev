using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Privy
{
    internal class WebViewManager
    {
        private readonly IWebViewHandler _webViewHandler;
        private TaskCompletionSource<bool> readyTcs = new TaskCompletionSource<bool>();

        private readonly Dictionary<string, TaskCompletionSource<string>> requestResponseMap =
            new Dictionary<string, TaskCompletionSource<string>>();

        public WebViewManager(PrivyConfig privyConfig)
        {
            _webViewHandler = IWebViewHandler.GetPlatformWebViewHandler(this, privyConfig);

            _webViewHandler.LoadUrl(
                $"{PrivyEnvironment.BASE_URL}/apps/{privyConfig.AppId}/embedded-wallets?client_id={privyConfig.ClientId}");
        }

        internal void OnWebViewReady()
        {
            //Receives message from the source
            PrivyLogger.Debug("WebView is ready.");
            if (!readyTcs.Task.IsCompleted)
            {
                readyTcs.TrySetResult(true); //No effect if this is called multiple times, due to the "try"
            }
        }


        // Handle messages received from the WebView
        internal void OnMessageReceived(string message)
        {
            //Here we take a message, deserialize it into an IFrame Response and then parse the relevant fields

            PrivyLogger.Debug("Message from WebView: " + message);

            // Check if the message is URL-encoded (contains '%')
            // For some reason messages are received as an encoded URL on Android
            if (Uri.IsWellFormedUriString(message, UriKind.RelativeOrAbsolute) && message.Contains("%"))
            {
                message = Uri.UnescapeDataString(message);
                PrivyLogger.Debug("Decoded message from WebView: " + message);
            }

            //Generic Response Parsing, for the event and ID
            var messageResponse = JsonConvert.DeserializeObject<IframeResponse>(message);


            string @event = messageResponse.Event;
            string id = messageResponse.Id;

            if (requestResponseMap.TryGetValue(id, out var tcs))
            {
                tcs.TrySetResult(message);
                requestResponseMap.Remove(id); //Task will resolve now, we don't need the id in the map
            }
            else
            {
                //This would happen if the request times out, and the response is received after
                PrivyLogger.Error($"No matching task found for ID: {id}");
            }
        }


        private async Task<IframeResponse> SendRequest<T>(IframeRequest<T> request, double seconds = 30)
        {
            if (request.Event != PrivyEventType.Ready)
            {
                // for all requests except for "ping ready", we need to await until web view is ready
                await AwaitReady();
            }

            var tcs = new TaskCompletionSource<string>();
            requestResponseMap[request.Id] = tcs;

            //POTENTIAL NEW LOGIC
            //Could use a CancellationTokenSource here
            //We could link the task completion source to the cancellation source
            //the cts allows us to acess a "cancelafter" property, which mimics a timeout
            //On cancellation we can throw the error
            //cancellation would only happen if timeout finishes, AND the task isn't currently complete        

            string
                serializedRequest =
                    JsonConvert.SerializeObject(
                        request); //This could throw an error, however is less risky especially since we control the request

            PrivyLogger.Debug("Message to WebView: " + serializedRequest);

            _webViewHandler.SendMessage(serializedRequest); //Sends message based on if it's the iframe or the webview

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(seconds));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                PrivyLogger.Error("Request timed out.");
                requestResponseMap.Remove(request.Id);
                return new IframeResponseError
                {
                    Id = request.Id,
                    Event = request.Event,
                    Error = new ErrorDetails
                    {
                        Type = "timeout_error",
                        Message = "The request timed out."
                    },
                } as IframeResponse;
            }

            string responseString = await tcs.Task;
            var baseResponse =
                JsonConvert.DeserializeObject<IframeResponse>(
                    responseString); //This could throw an error as well, however we may not need a custom exception here

            if (responseString.Contains("error"))
            {
                var errorResponse = JsonConvert.DeserializeObject<IframeResponseError>(responseString);
                return errorResponse;
            }

            //This can potentially be done in a helper
            //Based on the Event field, deserialize into the appropriate response type
            switch (baseResponse.Event)
            {
                case PrivyEventType.Ready:
                    return JsonConvert.DeserializeObject<IframeResponseSuccess<ReadyResponseData>>(responseString);

                case PrivyEventType.CreateEthereumWallet:
                    return JsonConvert.DeserializeObject<IframeResponseSuccess<CreateEthereumWalletResponseData>>(
                        responseString);

                case PrivyEventType.CreateSolanaWallet:
                    return JsonConvert.DeserializeObject<IframeResponseSuccess<CreateSolanaWalletResponseData>>(
                        responseString);

                case PrivyEventType.CreateAdditional:
                    return JsonConvert.DeserializeObject<IframeResponseSuccess<CreateAdditionalWalletResponseData>>(
                        responseString);

                case PrivyEventType.Connect:
                    return JsonConvert.DeserializeObject<IframeResponseSuccess<ConnectWalletResponseData>>(
                        responseString);

                case PrivyEventType.Recover:
                    return JsonConvert.DeserializeObject<IframeResponseSuccess<RecoverWalletResponseData>>(
                        responseString);

                case PrivyEventType.Rpc:
                    return JsonConvert.DeserializeObject<IframeResponseSuccess<RpcResponseData>>(responseString);

                case PrivyEventType.SignWithUserSigner:
                    return JsonConvert.DeserializeObject<IframeResponseSuccess<UserSignerSignResponseData>>(
                        responseString);

                case "error":
                    return JsonConvert.DeserializeObject<IframeResponseError>(responseString);

                default:
                    //Return error on default, and catch in higher level, as opposed to exception
                    return new IframeResponseError
                    {
                        Id = request.Id,
                        Event = request.Event,
                        Error = new ErrorDetails
                        {
                            Type = "unknown_response",
                            Message = "received an unknown response type"
                        },
                    } as IframeResponse;
            }
        }

        internal async void PingReadyUntilSuccessful()
        {
            //NOTE: Semaphore has been removed here, as this is only called once, and we want to prevent WebGL issues, as WebGL does not support multi-threading
            //Essentially, we're using a Task Completion Source to know when the Webview is ready
            //So our await ready is just waiting for the value of the Task Completion source to be set, it doesn't PingReady again
            //However, there is a chance in the future, that we need to call PingReadyUntilSuccessful from multiple places, and in that scenario we may need to re-introduce locking        
            if (readyTcs.Task.IsCompleted)
            {
                PrivyLogger.Debug("Already ready");
                return;
            }

            PrivyLogger.Debug("Ping Ready until success invoked");

            while (!readyTcs.Task.IsCompleted)
            {
                PrivyLogger.Debug("NOT COMPLETED");
                // Send a message to the WebView to check if it's ready

                try
                {
                    //We've added functionality to have PingReady return Error Responses, which we don't need to handle, since the loop will re-trigger
                    //However Ping Ready could potentially throw an error if there is an error in serialization, so we can rap it in a try catch, because this wouldn't be caught
                    //So we wrap in a try catch for that example, although unlikely
                    var result = await PingReady();

                    if (result is IframeResponseSuccess<ReadyResponseData>)
                    {
                        //If this never executes, this loop runs indefinitely
                        // Handle the success response
                        PrivyLogger.Debug("Success Response Received: WebView is ready.");
                        readyTcs.TrySetResult(true); //No effect if this is called multiple times, due to the "try"
                    }
                }
                catch (Exception e)
                {
                    //Fail silently, to have loop continue
                    PrivyLogger.Debug("Success response not received, pinging again");
                }
            }

            PrivyLogger.Debug("Task completed");
        }

        private async Task<IframeResponse> PingReady()
        {
            var readyRequest = new IframeRequest<ReadyRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.Ready,
                Data = new ReadyRequestData { }
            };

            var res = await SendRequest<ReadyRequestData>(readyRequest, 0.15);

            // Ensure the response is of the correct type
            if (res is IframeResponseSuccess<ReadyResponseData> successResponse)
            {
                return successResponse;
            }
            else if (res is IframeResponseError errorResponse)
            {
                //This will get triggered if there's any timeouts, or webview returns an error response
                return errorResponse;
            }
            else
            {
                //Fail silently if we receive an error from webview we don't know how to handle
                //This will trigger another ping
                //Throwing an exception here won't be caught, and there's nothing to send back to the developer, so makes sense to do this
                return null;
            }
        }

        private async Task AwaitReady()
        {
            // after this function executes, it indicates that the webview has sent a message back to unity with the "privy:iframe:ready"
            //usage
            // await WebViewManager.AwaitReady()
            //now ready to send additional messages        


            //Enhancement: Add timeout here, in case the ready state never updates - to prevent indefinite requests
            // You can perform additional async operations here if needed
            await readyTcs.Task;
        }


        //Wallet Methods
        //TBD
        internal async Task<IframeResponse> CreateEthereumWallet(string accessToken, string solanaAddress = null)
        {
            var data = new CreateEthereumWalletRequestData
            {
                AccessToken = accessToken,
                SolanaAddress = solanaAddress
            };

            var createWalletRequest = new IframeRequest<CreateEthereumWalletRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.CreateEthereumWallet,
                Data = data
            };

            var res = await SendRequest<CreateEthereumWalletRequestData>(createWalletRequest, 60);

            if (res is IframeResponseSuccess<CreateEthereumWalletResponseData> successResponse)
            {
                return successResponse;
            }
            else if (res is IframeResponseError errorResponse)
            {
                return errorResponse;
            }
            else
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to create wallet",
                    EmbeddedWalletError.CreateFailed); //This bubbles up to top level
            }
        }

        internal async Task<IframeResponse> CreateSolanaWallet(string accessToken, string ethereumAddress = null)
        {
            var data = new CreateSolanaWalletRequestData
            {
                AccessToken = accessToken,
                EthereumAddress = ethereumAddress
            };

            var createWalletRequest = new IframeRequest<CreateSolanaWalletRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.CreateSolanaWallet,
                Data = data
            };

            var res = await SendRequest(createWalletRequest, 60);

            if (res is IframeResponseSuccess<CreateSolanaWalletResponseData> successResponse)
            {
                return successResponse;
            }
            else if (res is IframeResponseError errorResponse)
            {
                return errorResponse;
            }
            else
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to create wallet",
                    EmbeddedWalletError.CreateFailed); //This bubbles up to top level
            }
        }

        internal async Task<IframeResponse> CreateAdditionalWallet(string accessToken, WalletEntropy walletEntropy,
            ChainType chainType, int hdWalletIndex)
        {
            var data = new CreateAdditionalWalletRequestData
            {
                AccessToken = accessToken,
                ChainType = chainType,
                EntropyId = walletEntropy.Id,
                EntropyIdVerifier = walletEntropy.Verifier.ToVerifierName(),
                WalletIndex = hdWalletIndex
            };

            var createWalletRequest = new IframeRequest<CreateAdditionalWalletRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.CreateAdditional,
                Data = data
            };

            var res = await SendRequest<CreateAdditionalWalletRequestData>(createWalletRequest, 60);

            if (res is IframeResponseSuccess<CreateAdditionalWalletResponseData> successResponse)
            {
                return successResponse;
            }
            else if (res is IframeResponseError errorResponse)
            {
                return errorResponse;
            }
            else
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to create additional wallet",
                    EmbeddedWalletError.CreateAdditionalFailed); //This bubbles up to top level
            }
        }


        internal async Task<IframeResponse> ConnectWallet(string accessToken, WalletEntropy walletEntropy)
        {
            var data = new ConnectWalletRequestData
            {
                AccessToken = accessToken,
                ChainType = ChainType.Ethereum,
                EntropyId = walletEntropy.Id,
                EntropyIdVerifier = walletEntropy.Verifier.ToVerifierName()
            };

            var connectWalletRequest = new IframeRequest<ConnectWalletRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.Connect,
                Data = data
            };

            var res = await SendRequest<ConnectWalletRequestData>(connectWalletRequest);

            if (res is IframeResponseSuccess<ConnectWalletResponseData> successResponse)
            {
                return successResponse;
            }
            else if (res is IframeResponseError errorResponse)
            {
                return errorResponse;
            }
            else
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to connect wallet",
                    EmbeddedWalletError.ConnectionFailed);
            }
        }

        internal async Task<IframeResponse> RecoverWallet(string accessToken, WalletEntropy walletEntropy)
        {
            var data = new RecoverWalletRequestData
            {
                AccessToken = accessToken,
                EntropyId = walletEntropy.Id,
                EntropyIdVerifier = walletEntropy.Verifier.ToVerifierName()
            };

            var recoverWalletRequest = new IframeRequest<RecoverWalletRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.Recover,
                Data = data
            };

            var res = await SendRequest<RecoverWalletRequestData>(recoverWalletRequest);

            if (res is IframeResponseSuccess<RecoverWalletResponseData> successResponse)
            {
                return successResponse;
            }
            else if (res is IframeResponseError errorResponse)
            {
                return errorResponse;
            }
            else
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to recover wallet",
                    EmbeddedWalletError.RecoverFailed);
            }
        }

        internal async Task<IframeResponse> Request(string accessToken, WalletEntropy walletEntropy,
            ChainType chainType, int hdWalletIndex, RpcRequestData.IRpcRequestDetails request)
        {
            var data = new RpcRequestData
            {
                AccessToken = accessToken,
                ChainType = chainType,
                EntropyId = walletEntropy.Id,
                EntropyIdVerifier = walletEntropy.Verifier.ToVerifierName(),
                WalletIndex = hdWalletIndex,
                Request = request
            };

            var rpcRequest = new IframeRequest<RpcRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.Rpc,
                Data = data
            };

            var res = await SendRequest<RpcRequestData>(rpcRequest);

            if (res is IframeResponseSuccess<RpcResponseData> successResponse)
            {
                return successResponse;
            }
            else if (res is IframeResponseError errorResponse)
            {
                return errorResponse;
            }
            else
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to execute request",
                    EmbeddedWalletError.RpcRequestFailed);
            }
        }

        internal async Task<IframeResponse> SignWithUserSigner(string accessToken, byte[] message)
        {
            var data = new UserSignerSignRequestData
            {
                AccessToken = accessToken,
                Message = Convert.ToBase64String(message)
            };

            var userSignerSignRequest = new IframeRequest<UserSignerSignRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.SignWithUserSigner,
                Data = data
            };

            var res = await SendRequest(userSignerSignRequest);

            if (res is IframeResponseSuccess<UserSignerSignResponseData> successResponse)
            {
                return successResponse;
            }

            if (res is IframeResponseError errorResponse)
            {
                return errorResponse;
            }

            throw new PrivyException.EmbeddedWalletException($"Failed to sign with the user's authorization key",
                EmbeddedWalletError.UserSignerRequestFailed);
        }
    }
}