using Newtonsoft.Json;

namespace Privy
{
    internal struct WalletApiEthereumSignTypedDataV4RpcResponse
    {
        [JsonProperty("signature")]
        internal string Signature;
    }
}