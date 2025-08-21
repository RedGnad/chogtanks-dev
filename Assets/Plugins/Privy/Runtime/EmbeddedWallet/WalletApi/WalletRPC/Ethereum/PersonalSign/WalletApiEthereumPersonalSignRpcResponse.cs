using Newtonsoft.Json;

namespace Privy
{
    internal struct WalletApiEthereumPersonalSignRpcResponse
    {
        [JsonProperty("signature")]
        internal string Signature;
    }
}