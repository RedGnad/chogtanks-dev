using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Privy
{
    internal struct WalletApiEthereumSignTypedDataV4RpcParams
    {
        [JsonProperty("typed_data")]
        internal JRaw TypedData;

        internal static WalletApiEthereumSignTypedDataV4RpcParams FromString(string typedData)
        {
            // Typed-data objects are camelCase but the API expects the top-level properties to be snake_case
            // `primary_type` is the only one that qualifies, as the rest are single word (e.g. `message` or `types`).
            var typedDataObj = JObject.Parse(typedData);
            if (typedDataObj["primaryType"] != null)
            {
                typedDataObj["primary_type"] = typedDataObj["primaryType"];
                typedDataObj.Remove("primaryType");
            }

            return new WalletApiEthereumSignTypedDataV4RpcParams
                { TypedData = new JRaw(typedDataObj.ToString(Formatting.None)) };
        }
    }
}