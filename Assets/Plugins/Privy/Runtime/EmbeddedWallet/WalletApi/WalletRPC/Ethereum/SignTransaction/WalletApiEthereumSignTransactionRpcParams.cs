using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Privy
{
    internal struct WalletApiEthereumSignTransactionRpcParams
    {
        [JsonProperty("transaction")]
        internal JRaw Transaction;

        internal static WalletApiEthereumSignTransactionRpcParams FromString(string transaction)
        {
            var transactionObj = JObject.Parse(transaction);
            foreach (var (inputKey, outputKey) in KeysToConvert)
            {
                if (transactionObj[inputKey] == null) continue;
                transactionObj[outputKey] = transactionObj[inputKey];
                transactionObj.Remove(inputKey);
            }

            return new WalletApiEthereumSignTransactionRpcParams
                { Transaction = new JRaw(transactionObj.ToString(Formatting.None)) };
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