using Newtonsoft.Json;

namespace Privy
{
    internal struct WalletApiEthereumSecp256k1SignRpcResponse
    {
        [JsonProperty("signature")]
        internal string Signature;
    }
}