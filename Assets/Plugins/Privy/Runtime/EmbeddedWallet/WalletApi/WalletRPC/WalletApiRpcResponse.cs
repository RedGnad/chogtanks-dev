using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Privy
{
    [JsonConverter(typeof(WalletApiRpcResponseConverter))]
    internal struct WalletApiRpcResponse
    {
        [JsonProperty("method")]
        internal string Method;

        [JsonProperty("data")]
        internal object Data;

        internal WalletApiRpcResponse(string method, object data)
        {
            Method = method;
            Data = data;
        }

        private class WalletApiRpcResponseConverter : JsonConverter<WalletApiRpcResponse>
        {
            public override void WriteJson(JsonWriter writer, WalletApiRpcResponse value, JsonSerializer serializer)
            {
                // Not necessary for a "response" type.
                throw new NotImplementedException();
            }

            public override WalletApiRpcResponse ReadJson(JsonReader reader, Type objectType,
                WalletApiRpcResponse existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                var jo = JObject.Load(reader);

                var method = jo.GetValue("method")?.Value<string>();
                object data;

                switch (method)
                {
                    case "personal_sign":
                        data = jo.GetValue("data")?.ToObject<WalletApiEthereumPersonalSignRpcResponse>(serializer);
                        break;
                    case "secp256k1_sign":
                        data = jo.GetValue("data")?.ToObject<WalletApiEthereumSecp256k1SignRpcResponse>(serializer);
                        break;
                    case "eth_signTypedData_v4":
                        data = jo.GetValue("data")?.ToObject<WalletApiEthereumSignTypedDataV4RpcResponse>(serializer);
                        break;
                    case "eth_sendTransaction":
                        data = jo.GetValue("data")?.ToObject<WalletApiEthereumSendTransactionRpcResponse>(serializer);
                        break;
                    case "eth_signTransaction":
                        data = jo.GetValue("data")?.ToObject<WalletApiEthereumSignTransactionRpcResponse>(serializer);
                        break;
                    case "signMessage":
                        data = jo.GetValue("data")?.ToObject<WalletApiSolanaSignMessageRpcResponse>(serializer);
                        break;
                    default:
                        throw new JsonSerializationException($"Invalid RPC Method: {method}");
                }

                return new(method, data);
            }
        }
    }
}