using Newtonsoft.Json;

namespace Privy
{
    internal struct WalletApiRpcRequest
    {
        [JsonProperty("method")]
        internal string Method;

        [JsonProperty("params")]
        internal object Params;

        [JsonProperty("caip2", NullValueHandling = NullValueHandling.Ignore)]
        internal string Caip2;

        private WalletApiRpcRequest(string method, object parameters)
        {
            Method = method;
            Params = parameters;
            Caip2 = null;
        }

        internal static WalletApiRpcRequest EthereumPersonalSign(WalletApiEthereumPersonalSignRpcParams parameters) =>
            new("personal_sign", parameters);

        internal static WalletApiRpcRequest EthereumSecp256k1Sign(WalletApiEthereumSecp256k1SignRpcParams parameters) =>
            new("secp256k1_sign", parameters);

        internal static WalletApiRpcRequest EthereumSignTypedDataV4(
            WalletApiEthereumSignTypedDataV4RpcParams parameters) =>
            new("eth_signTypedData_v4", parameters);

        internal static WalletApiRpcRequest EthereumSendTransaction(
            WalletApiEthereumSendTransactionRpcParams parameters) =>
            new("eth_sendTransaction", parameters) { Caip2 = parameters.ChainId };

        internal static WalletApiRpcRequest EthereumSignTransaction(
            WalletApiEthereumSignTransactionRpcParams parameters) =>
            new("eth_signTransaction", parameters);

        internal static WalletApiRpcRequest SolanaSignMessage(WalletApiSolanaSignMessageRpcParams parameters) =>
            new("signMessage", parameters);
    }
}