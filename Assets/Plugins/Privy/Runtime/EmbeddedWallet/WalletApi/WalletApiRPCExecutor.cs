using System.Threading.Tasks;
using static Privy.RpcRequestData;
using static Privy.RpcResponseData;

namespace Privy
{
    internal class WalletApiRPCExecutor : IRpcExecutor
    {
        private readonly WalletApiRepository _walletApiRepository;
        private readonly AuthDelegator _authDelegator;
        private readonly string _walletId;

        internal WalletApiRPCExecutor(WalletApiRepository walletApiRepository, AuthDelegator authDelegator,
            string walletId)
        {
            _walletApiRepository = walletApiRepository;
            _authDelegator = authDelegator;
            _walletId = walletId;
        }

        async Task<IRpcResponseDetails> IRpcExecutor.Evaluate(IRpcRequestDetails request)
        {
            string accessToken = await _authDelegator.GetAccessToken();

            if (request is EthereumRpcRequestDetails ethereumRequest)
            {
                string[] requestParams = ethereumRequest.Params;
                WalletApiRpcResponse response;
                WalletApiRpcRequest walletApiRequest;
                switch (ethereumRequest.Method)
                {
                    case "personal_sign":
                        if (!(requestParams.Length == 2 && !string.IsNullOrEmpty(requestParams[0])))
                        {
                            throw new PrivyException.EmbeddedWalletException(
                                "The params array should be [message, address]",
                                EmbeddedWalletError.RpcRequestFailed);
                        }

                        walletApiRequest =
                            WalletApiRpcRequest.EthereumPersonalSign(
                                WalletApiEthereumPersonalSignRpcParams.FromString(requestParams[0]));
                        response = await _walletApiRepository.Rpc(walletApiRequest, _walletId, accessToken);
                        var data = (WalletApiEthereumPersonalSignRpcResponse)response.Data;
                        return new EthereumRpcResponseDetails()
                        {
                            Method = response.Method,
                            Data = data.Signature
                        };
                    case "eth_sign":
                        if (!(requestParams.Length == 2 && !string.IsNullOrEmpty(requestParams[1])))
                        {
                            throw new PrivyException.EmbeddedWalletException(
                                "The params array should be [address, hash]",
                                EmbeddedWalletError.RpcRequestFailed);
                        }

                        walletApiRequest = WalletApiRpcRequest.EthereumSecp256k1Sign(
                            new WalletApiEthereumSecp256k1SignRpcParams()
                            {
                                Hash = requestParams[1],
                            }
                        );
                        response = await _walletApiRepository.Rpc(walletApiRequest, _walletId, accessToken);
                        return new EthereumRpcResponseDetails()
                        {
                            Method = response.Method,
                            Data = ((WalletApiEthereumSecp256k1SignRpcResponse)response.Data).Signature
                        };
                    case "secp256k1_sign":
                        if (!(requestParams.Length == 1 && !string.IsNullOrEmpty(requestParams[0])))
                        {
                            throw new PrivyException.EmbeddedWalletException(
                                "The params array should be [hash]",
                                EmbeddedWalletError.RpcRequestFailed);
                        }

                        walletApiRequest = WalletApiRpcRequest.EthereumSecp256k1Sign(
                            new WalletApiEthereumSecp256k1SignRpcParams()
                            {
                                Hash = requestParams[0],
                            }
                        );
                        response = await _walletApiRepository.Rpc(walletApiRequest, _walletId, accessToken);
                        return new EthereumRpcResponseDetails()
                        {
                            Method = response.Method,
                            Data = ((WalletApiEthereumSecp256k1SignRpcResponse)response.Data).Signature
                        };
                    case "eth_populateTransactionRequest":
                        // eth_populateTransactionRequest is explicitly not supported under TEE execution.
                        throw new PrivyException.EmbeddedWalletException(
                            $"The {ethereumRequest.Method} request is not supported by this wallet",
                            EmbeddedWalletError.RpcRequestFailed);
                    case "eth_signTypedData_v4":
                        if (!(requestParams.Length == 2 && !string.IsNullOrEmpty(requestParams[1])))
                        {
                            throw new PrivyException.EmbeddedWalletException(
                                "The params array should be [address, typedData]",
                                EmbeddedWalletError.RpcRequestFailed);
                        }

                        walletApiRequest = WalletApiRpcRequest.EthereumSignTypedDataV4(
                            WalletApiEthereumSignTypedDataV4RpcParams.FromString(requestParams[1])
                        );
                        response = await _walletApiRepository.Rpc(walletApiRequest, _walletId, accessToken);
                        return new EthereumRpcResponseDetails()
                        {
                            Method = response.Method,
                            Data = ((WalletApiEthereumSignTypedDataV4RpcResponse)response.Data).Signature
                        };
                    case "eth_sendTransaction":
                        if (!(requestParams.Length == 1 && !string.IsNullOrEmpty(requestParams[0])))
                        {
                            throw new PrivyException.EmbeddedWalletException(
                                "The params array should be [transaction]",
                                EmbeddedWalletError.RpcRequestFailed);
                        }

                        walletApiRequest = WalletApiRpcRequest.EthereumSendTransaction(
                            WalletApiEthereumSendTransactionRpcParams.FromString(requestParams[0])
                        );
                        response = await _walletApiRepository.Rpc(walletApiRequest, _walletId, accessToken);
                        return new EthereumRpcResponseDetails()
                        {
                            Method = response.Method,
                            Data = ((WalletApiEthereumSendTransactionRpcResponse)response.Data).Hash
                        };
                    case "eth_signTransaction":
                        if (!(requestParams.Length == 1 && !string.IsNullOrEmpty(requestParams[0])))
                        {
                            throw new PrivyException.EmbeddedWalletException(
                                "The params array should be [transaction]",
                                EmbeddedWalletError.RpcRequestFailed);
                        }

                        walletApiRequest = WalletApiRpcRequest.EthereumSignTransaction(
                            WalletApiEthereumSignTransactionRpcParams.FromString(requestParams[0])
                        );
                        response = await _walletApiRepository.Rpc(walletApiRequest, _walletId, accessToken);
                        return new EthereumRpcResponseDetails()
                        {
                            Method = response.Method,
                            Data = ((WalletApiEthereumSignTransactionRpcResponse)response.Data).SignedTransaction
                        };
                }
            }
            else if (request is SolanaRpcRequestDetails solanaRequest)
            {
                WalletApiRpcResponse response;
                WalletApiRpcRequest walletApiRequest;
                switch (solanaRequest.Method)
                {
                    case "signMessage":
                        walletApiRequest = WalletApiRpcRequest.SolanaSignMessage(
                            WalletApiSolanaSignMessageRpcParams.FromString(solanaRequest.Params.Message)
                        );
                        response = await _walletApiRepository.Rpc(walletApiRequest, _walletId, accessToken);
                        return new SolanaRpcResponseDetails
                        {
                            Method = response.Method,
                            Data = new SolanaSignMessageRpcResponseData
                            {
                                Signature = ((WalletApiSolanaSignMessageRpcResponse)response.Data).Signature
                            }
                        };
                }
            }

            throw new PrivyException.EmbeddedWalletException("RPC request could not be evaluated",
                EmbeddedWalletError.RpcRequestFailed);
        }
    }
}