using System;

namespace Privy
{
    /// <summary>
    /// Represents an embedded Ethereum wallet within the Privy SDK.
    /// Provides access to wallet details and an RPC provider for blockchain interactions.
    /// </summary>
    [Obsolete(
        "This interface has been renamed to IEmbeddedEthereumWallet for clarity. Please use IEmbeddedEthereumWallet instead.")]
    public interface IEmbeddedWallet
    {
        /// <summary>
        /// Gets the id of the linked wallet account.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the address of the embedded wallet.
        /// </summary>
        string Address { get; }

        /// <summary>
        /// Gets the chain ID associated with the embedded wallet.
        /// </summary>
        string ChainId { get; }

        /// <summary>
        /// Gets the recovery method used for restoring access to the wallet.
        /// </summary>
        string RecoveryMethod { get; }

        /// <summary>
        /// Gets the HD wallet index associated with the embedded wallet.
        /// </summary>
        int HdWalletIndex { get; }

        /// <summary>
        /// Gets the RPC provider associated with the embedded wallet for executing blockchain requests.
        /// </summary>
        IRpcProvider RpcProvider { get; }
    }

#pragma warning disable CS0618 // Type or member is obsolete
    /// <summary>
    /// Represents an embedded Ethereum wallet within the Privy SDK.
    /// Provides access to wallet details and an RPC provider for blockchain interactions.
    /// </summary>
    public interface IEmbeddedEthereumWallet : IEmbeddedWallet
    {
    }
#pragma warning restore CS0618 // Type or member is obsolete

    internal static class EmbeddedEthereumWalletExtensions
    {
        internal static bool IsOnDevice(this IEmbeddedEthereumWallet wallet)
        {
            return wallet.Id == null || wallet.RecoveryMethod != "privy-v2";
        }
    }
}