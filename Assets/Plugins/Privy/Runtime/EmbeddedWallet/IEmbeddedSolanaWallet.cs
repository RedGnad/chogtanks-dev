namespace Privy
{
    /// <summary>
    /// Represents an embedded Solana wallet within the Privy SDK.
    /// Provides access to wallet details and a provider for blockchain interactions.
    /// </summary>
    public interface IEmbeddedSolanaWallet
    {
        string ChainType => "solana";

        /// <summary>
        /// Gets the id of the linked wallet account.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the address of the embedded wallet.
        /// </summary>
        string Address { get; }

        /// <summary>
        /// Gets the recovery method used for restoring access to the wallet.
        /// </summary>
        string RecoveryMethod { get; }

        /// <summary>
        /// Gets the HD wallet index associated with the embedded wallet.
        /// </summary>
        int WalletIndex { get; }

        /// <summary>
        /// The Solana provider to use for acting on the embedded wallet.
        /// </summary>
        IEmbeddedSolanaWalletProvider EmbeddedSolanaWalletProvider { get; }
    }

    internal static class EmbeddedSolanaWalletExtensions
    {
        internal static bool IsOnDevice(this IEmbeddedSolanaWallet wallet)
        {
            return wallet.Id == null || wallet.RecoveryMethod != "privy-v2";
        }
    }
}