using System.Threading.Tasks;

namespace Privy
{
    /// <summary>
    /// Keys used for authorizing requests to the Wallet API.
    /// </summary>
    internal interface AuthorizationKey
    {
        /// <summary>
        /// Signs a byte sequence for authorization.
        /// </summary>
        /// <param name="message">The byte sequence to sign over</param>
        /// <returns>The signature of the message</returns>
        internal Task<byte[]> Signature(byte[] message);
    }
}