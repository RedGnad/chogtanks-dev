using System.Threading.Tasks;

namespace Privy
{
    internal class EmbeddedSolanaWalletProvider : IEmbeddedSolanaWalletProvider
    {
        private readonly IRpcExecutor _rpcExecutor;

        internal EmbeddedSolanaWalletProvider(IRpcExecutor rpcExecutor)
        {
            _rpcExecutor = rpcExecutor;
        }

        public async Task<string> SignMessage(string message)
        {
            var request = new RpcRequestData.SolanaRpcRequestDetails
            {
                Method = "signMessage",
                Params = new RpcRequestData.SolanaSignMessageRpcRequestParams { Message = message }
            };

            var response =
                await _rpcExecutor.Evaluate(request);

            if (response is RpcResponseData.SolanaRpcResponseDetails signatureResponse)
                return signatureResponse.Data.Signature;

            throw new PrivyException.EmbeddedWalletException("Failed to execute message signature",
                EmbeddedWalletError.RpcRequestFailed);
        }
    }
}