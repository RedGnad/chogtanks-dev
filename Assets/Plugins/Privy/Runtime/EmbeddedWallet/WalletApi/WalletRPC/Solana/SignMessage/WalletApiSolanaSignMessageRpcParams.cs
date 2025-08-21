using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Privy
{
    internal struct WalletApiSolanaSignMessageRpcParams
    {
        [JsonProperty("message")]
        internal string Message;

        [JsonProperty("encoding")]
        internal MessageEncoding Encoding;

        [JsonConverter(typeof(StringEnumConverter))]
        internal enum MessageEncoding
        {
            [EnumMember(Value = "base64")]
            Base64
        }

        internal static WalletApiSolanaSignMessageRpcParams FromString(string message) =>
            new()
            {
                Message = message,
                Encoding = MessageEncoding.Base64
            };
    }
}