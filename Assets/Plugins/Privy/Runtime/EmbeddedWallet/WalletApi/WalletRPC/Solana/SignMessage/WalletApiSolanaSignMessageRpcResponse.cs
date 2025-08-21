using Newtonsoft.Json;

namespace Privy
{
    internal struct WalletApiSolanaSignMessageRpcResponse
    {
        [JsonProperty("signature")]
        internal string Signature;
    }
}