using Newtonsoft.Json;

namespace Privy
{
    internal struct WalletApiEthereumSecp256k1SignRpcParams
    {
        /// <summary>
        /// The bytes to sign with the wallet. Must be a hex encoding (0x...)
        /// </summary>
        [JsonProperty("hash")]
        internal string Hash;
    }
}