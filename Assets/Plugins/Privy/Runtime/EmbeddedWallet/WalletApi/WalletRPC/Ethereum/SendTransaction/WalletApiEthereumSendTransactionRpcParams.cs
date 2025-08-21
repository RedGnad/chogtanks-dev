using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Privy
{
    internal struct WalletApiEthereumSendTransactionRpcParams
    {
        [JsonProperty("transaction")]
        internal JRaw Transaction;

        [JsonIgnore]
        internal string ChainId;

        internal static WalletApiEthereumSendTransactionRpcParams FromString(string transaction)
        {
            var transactionObj = JObject.Parse(transaction);
            foreach (var (inputKey, outputKey) in KeysToConvert)
            {
                if (transactionObj[inputKey] == null) continue;
                transactionObj[outputKey] = transactionObj[inputKey];
                transactionObj.Remove(inputKey);
            }

            JToken chainIdToken = transactionObj["chain_id"];
            ulong? chainId = null;
            if (chainIdToken?.Type == JTokenType.Integer)
            {
                chainId = chainIdToken.Value<ulong>();
            }
            else if (chainIdToken?.Type == JTokenType.String)
            {
                chainId = Convert.ToUInt64(chainIdToken.Value<string>(), 16);
            }

            return new WalletApiEthereumSendTransactionRpcParams
            {
                Transaction = new JRaw(transactionObj.ToString(Formatting.None)),
                ChainId = chainId != null ? $"eip155:{chainId.Value}" : null
            };
        }

        private static readonly (string, string)[] KeysToConvert =
        {
            ("gasLimit", "gas_limit"),
            ("gasPrice", "gas_price"),
            ("chainId", "chain_id"),
            ("maxFeePerGas", "max_fee_per_gas"),
            ("maxPriorityFeePerGas", "max_priority_fee_per_gas")
        };
    }
}