using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Privy
{
    internal class WalletApiRepository
    {
        private PrivyConfig _privyConfig;
        private IHttpRequestHandler _httpRequestHandler;
        private AuthorizationKey _authorizationKey;

        internal WalletApiRepository(PrivyConfig privyConfig, IHttpRequestHandler httpRequestHandler,
            AuthorizationKey authorizationKey)
        {
            _privyConfig = privyConfig;
            _httpRequestHandler = httpRequestHandler;
            _authorizationKey = authorizationKey;
        }

        internal async Task<WalletApiCreateResponse> CreateWallet(WalletApiCreateRequest request, string accessToken)
        {
            var headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {accessToken}" }
            };

            string serializedRequest = JsonConvert.SerializeObject(request);

            try
            {
                string jsonResponse =
                    await _httpRequestHandler.SendRequestAsync("wallets", serializedRequest,
                        customHeaders: headers, method: "POST");

                var response = JsonConvert.DeserializeObject<WalletApiCreateResponse>(jsonResponse);
                return response;
            }
            catch (Exception errorResponse)
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to create wallet: {errorResponse.Message}",
                    EmbeddedWalletError.CreateFailed);
            }
        }

        internal async Task<WalletApiRpcResponse> Rpc(WalletApiRpcRequest request, string walletId, string accessToken)
        {
            var path = $"wallets/{walletId}/rpc";
            var payload = new WalletApiPayload
            {
                Version = 1,
                Url = _httpRequestHandler.GetFullUrl(path),
                Method = "POST",
                Headers = new Dictionary<string, string> { { Constants.PRIVY_APP_ID_HEADER, _privyConfig.AppId } },
                Body = request
            };

            byte[] encodedPayload = payload.EncodePayload();
            byte[] signature = await _authorizationKey.Signature(encodedPayload);

            var headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {accessToken}" },
                { "privy-authorization-signature", Convert.ToBase64String(signature) }
            };

            string serializedRequest = JsonConvert.SerializeObject(request);

            try
            {
                string jsonResponse =
                    await _httpRequestHandler.SendRequestAsync($"wallets/{walletId}/rpc", serializedRequest,
                        customHeaders: headers, method: "POST");

                var response = JsonConvert.DeserializeObject<WalletApiRpcResponse>(jsonResponse);
                return response;
            }
            catch (Exception errorResponse)
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to execute RPC: {errorResponse.Message}",
                    EmbeddedWalletError.RpcRequestFailed);
            }
        }
    }
}