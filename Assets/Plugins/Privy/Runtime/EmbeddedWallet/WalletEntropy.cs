namespace Privy
{
    /// <summary>
    /// Representes the identity of an embedded wallet's entropy, namely via its id and verifier.
    /// </summary>
    internal readonly struct WalletEntropy
    {
        internal WalletEntropy(string id, EntropyIdVerifier verifier)
        {
            Id = id;
            Verifier = verifier;
        }

        /// <summary>
        /// The entropy id itself, used to look up the device share in storage
        /// </summary>
        internal string Id { get; }

        /// <summary>
        /// Source of the `entropyId` property, describing how the entropyId will be used.
        /// </summary>
        internal EntropyIdVerifier Verifier { get; }
    }

    /// <summary>
    /// An entropy id source
    /// </summary>
    /// <see cref="WalletEntropy"/>
    internal enum EntropyIdVerifier
    {
        EthereumAddress,
        SolanaAddress,
    }
}