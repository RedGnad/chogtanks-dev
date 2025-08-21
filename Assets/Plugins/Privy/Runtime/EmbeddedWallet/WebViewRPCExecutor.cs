using System.Threading.Tasks;
using static Privy.RpcRequestData;
using static Privy.RpcResponseData;

namespace Privy
{
    internal class WebViewRPCExecutor : IRpcExecutor
    {
        private readonly WalletEntropy _entropy;
        private readonly int _hdWalletIndex;
        private readonly EmbeddedWalletManager _embeddedWalletManager;

        internal WebViewRPCExecutor(WalletEntropy walletEntropy, int hdWalletIndex,
            EmbeddedWalletManager embeddedWalletManager)
        {
            _entropy = walletEntropy;
            _hdWalletIndex = hdWalletIndex;
            _embeddedWalletManager = embeddedWalletManager;
        }

        async Task<IRpcResponseDetails> IRpcExecutor.Evaluate(IRpcRequestDetails request)
        {
            var chainType = request switch
            {
                EthereumRpcRequestDetails => ChainType.Ethereum,
                SolanaRpcRequestDetails => ChainType.Solana,
                _ => throw new PrivyException.EmbeddedWalletException("Unsupported wallet request type.",
                    EmbeddedWalletError.RpcRequestFailed)
            };
            return await _embeddedWalletManager.Request(_entropy, chainType, _hdWalletIndex, request);
        }
    }
}