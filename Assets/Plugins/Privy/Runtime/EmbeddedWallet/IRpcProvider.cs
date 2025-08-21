using System.Threading.Tasks;

namespace Privy
{
    /// <summary>
    /// Interface for sending RPC (Remote Procedure Call) requests to the Ethereum provider.
    /// </summary>
    public interface IRpcProvider
    {
        /// <summary>
        /// Sends an RPC request to the Ethereum provider.
        /// </summary>
        /// <param name="request">The RPC request to be sent. Contains the method and parameters for the request.</param>
        /// <returns>A task representing the asynchronous operation. The task result is the <see cref="RpcResponse"/> received from the provider.</returns>
        /// <exception cref="PrivyException.EmbeddedWalletException">Thrown if the RPC request fails due to an issue with the embedded wallet or webview message.</exception>
        Task<RpcResponse> Request(RpcRequest request);
    }
}