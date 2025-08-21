using JetBrains.Annotations;

namespace Privy
{
    public class PrivyLinkedAccount
    {
        // TODO: convert type to enum
        public string Type;

        // TODO: convert below date longs to dates
        public long VerifiedAt;

        public long FirstVerifiedAt;

        public long LatestVerifiedAt;
    }

    //Privy Embedded Wallet Account
    public class PrivyEmbeddedWalletAccount : PrivyLinkedAccount
    {
        [CanBeNull]
        public string Id { get; set; }

        public string Address { get; set; }

        public bool Imported { get; set; }

        public int WalletIndex { get; set; }

        public string ChainId { get; set; }

        public string ChainType { get; set; }

        public string WalletClient { get; set; }

        public string WalletClientType { get; set; }

        public string ConnectorType { get; set; }

        public string PublicKey { get; set; }

        public string RecoveryMethod { get; set; }

        internal bool IsOnDevice => Id == null || RecoveryMethod != "privy-v2";
    }

    /// <summary>
    /// An embedded Solana wallet account linked to the user.
    /// </summary>
    public class PrivyEmbeddedSolanaWalletAccount : PrivyLinkedAccount
    {
        public string ChainType => "solana";

        [CanBeNull]
        public string Id { get; set; }

        /// <summary>
        /// The embedded wallet's address, as the base58 encoded public key.
        /// </summary>
        public string Address { get; set; }

        public int WalletIndex { get; set; }

        public bool Imported { get; set; }

        public string RecoveryMethod { get; set; }

        internal bool IsOnDevice => Id == null || RecoveryMethod != "privy-v2";
    }

    /// <summary>
    /// An external wallet account linked to the user.
    /// </summary>
    public class ExternalWalletAccount : PrivyLinkedAccount
    {
        public string Address { get; set; }

        public string ChainType { get; set; }

        public string WalletClientType { get; set; }

        public string ConnectorType { get; set; }
    }

    public class PrivyEmailAccount : PrivyLinkedAccount
    {
        public string Address { get; set; }
    }

    public class GoogleAccount : PrivyLinkedAccount
    {
        public string Subject { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }
    }

    public class DiscordAccount : PrivyLinkedAccount
    {
        public string Subject { get; set; }

        public string Email { get; set; }

        public string UserName { get; set; }
    }

    public class TwitterAccount : PrivyLinkedAccount
    {
        public string Subject { get; set; }

        public string UserName { get; set; }

        public string Name { get; set; }

        public string ProfilePictureUrl { get; set; }
    }

    public class AppleAccount : PrivyLinkedAccount
    {
        public string Subject { get; set; }

        public string Email { get; set; }
    }
}