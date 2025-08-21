using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Privy
{
    internal struct AppConfig
    {
        [JsonProperty("embedded_wallet_config")]
        internal EmbeddedWalletConfig EmbeddedWalletConfig;
    }

    internal struct EmbeddedWalletConfig
    {
        [JsonProperty("mode")]
        internal EmbeddedWalletMode Mode;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum EmbeddedWalletMode
    {
        [EnumMember(Value = "legacy-embedded-wallets-only")]
        LegacyEmbeddedWalletsOnly,

        [EnumMember(Value = "user-controlled-server-wallets-only")]
        UserControlledServerWalletsOnly
    }
}