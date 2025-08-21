using Newtonsoft.Json;

namespace Privy
{
    internal struct WalletApiCreateRequest
    {
        [JsonProperty("chain_type")]
        internal ChainType chainType;
    }
}