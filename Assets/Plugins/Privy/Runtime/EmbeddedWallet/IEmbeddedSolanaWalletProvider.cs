using System.Threading.Tasks;

namespace Privy
{
    /// <summary>
    /// Interface for acting on an embedded Solana wallet.
    /// </summary>
    public interface IEmbeddedSolanaWalletProvider
    {
        /// <summary>
        /// Request a signature on a Base64 encoded message or transaction
        /// </summary>
        /// <param name="message">Base 64 encoded message or transaction</param>
        /// <returns>Base64 encoded signature of the message</returns>
        Task<string> SignMessage(string message);
    }
}