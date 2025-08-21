using Newtonsoft.Json;

namespace Privy
{
    internal struct WalletApiEthereumSignTransactionRpcResponse
    {
        [JsonProperty("signed_transaction")]
        internal string SignedTransaction;
    }
}