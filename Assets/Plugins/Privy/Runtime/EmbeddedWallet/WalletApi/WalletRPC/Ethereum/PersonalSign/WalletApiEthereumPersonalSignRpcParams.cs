using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Privy
{
    internal struct WalletApiEthereumPersonalSignRpcParams
    {
        [JsonProperty("message")]
        internal string Message;

        [JsonProperty("encoding")]
        internal MessageEncoding Encoding;

        [JsonConverter(typeof(StringEnumConverter))]
        internal enum MessageEncoding
        {
            [EnumMember(Value = "utf-8")]
            Utf8,

            [EnumMember(Value = "hex")]
            Hex
        }

        internal static WalletApiEthereumPersonalSignRpcParams FromString(string message) =>
            new()
            {
                Message = message.StartsWith("0x") ? message.Substring(2) : message,
                Encoding = message.StartsWith("0x") ? MessageEncoding.Hex : MessageEncoding.Utf8
            };
    }
}