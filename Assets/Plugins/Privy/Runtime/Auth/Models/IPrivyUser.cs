using System.Threading.Tasks;
using System.Collections.Generic;

namespace Privy
{
    /// <summary>
    /// Represents a Privy user with properties and methods for managing the user's identity and embedded wallets.
    /// </summary>
    public interface IPrivyUser
    {
        /// <summary>
        /// Gets the user's unique identifier.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the list of the user's linked accounts.
        /// </summary>
        PrivyLinkedAccount[] LinkedAccounts { get; }

        /// <summary>
        /// Gets the list of the user's embedded wallets.
        /// </summary>
        IEmbeddedEthereumWallet[] EmbeddedWallets { get; }

        /// <summary>
        /// Gets the list of the user's embedded Solana wallets.
        /// </summary>
        IEmbeddedSolanaWallet[] EmbeddedSolanaWallets { get; }

        /// <summary>
        /// Gets the user's custom metadata key-value mapping.
        /// </summary>
        Dictionary<string, string> CustomMetadata { get; }

        /// <summary>
        /// Gets the user's access token, refreshing the session if necessary.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user's access token.</returns>
        Task<string> GetAccessToken();

        /// <summary>
        /// Creates a new embedded wallet for the user.
        /// </summary>
        /// <param name="allowAditional">Whether to allow the creation of additional wallets derived from the primary HD wallet</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the newly created embedded wallet.</returns>
        /// <exception cref="PrivyException.AuthenticationException">
        /// Thrown if there is an issue with authentication, such as a failure to refresh the access token.
        /// </exception>
        /// <exception cref="PrivyException.EmbeddedWalletException">
        /// Thrown if the wallet creation fails or the wallet cannot be added to the user's account.
        /// </exception>
        Task<IEmbeddedEthereumWallet> CreateWallet(bool allowAdditional = false);


        /// <summary>
        /// Creates an Ethereum wallet at the specified HD index, or returns the existing wallet if one already exists at that index.
        /// A wallet with HD index 0 must be created before creating a wallet at greater HD indices.
        /// This method is idempotent. Calling it multiple times with the same HD index will have the same effect as calling it once.
        /// 
        /// </summary>
        /// <param name="hdWalletIndex">The specified HD wallet index of the wallet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the newly created embedded wallet.</returns>
        /// <exception cref="PrivyException.AuthenticationException">
        /// Thrown if there is an issue with authentication, such as a failure to refresh the access token.
        /// </exception>
        /// <exception cref="PrivyException.EmbeddedWalletException">
        /// Thrown if the wallet creation fails or the wallet cannot be added to the user's account.
        /// Can also be thrown if an invalid HD wallet index is supplied, i.e. hdWalletIndex is less than 0,
        /// or if HD wallet index is greater than 0 while user has no wallet with HD index 0.
        /// </exception>
        Task<IEmbeddedEthereumWallet> CreateWalletAtHdIndex(int hdWalletIndex);
    }
}