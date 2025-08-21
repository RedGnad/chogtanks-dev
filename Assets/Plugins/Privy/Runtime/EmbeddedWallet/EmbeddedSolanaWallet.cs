namespace Privy
{
    internal class EmbeddedSolanaWallet : IEmbeddedSolanaWallet
    {
        public string Id { get; }
        public string Address { get; }
        public string RecoveryMethod { get; }
        public int WalletIndex { get; }

        public IEmbeddedSolanaWalletProvider EmbeddedSolanaWalletProvider { get; }

        private EmbeddedSolanaWallet(PrivyEmbeddedSolanaWalletAccount account,
            IEmbeddedSolanaWalletProvider embeddedSolanaWalletProvider)
        {
            Id = account.Id;
            Address = account.Address;
            RecoveryMethod = account.RecoveryMethod;
            WalletIndex = account.WalletIndex;
            EmbeddedSolanaWalletProvider = embeddedSolanaWalletProvider;
        }

        internal static EmbeddedSolanaWallet Create(PrivyEmbeddedSolanaWalletAccount account,
            WalletEntropy walletEntropy, EmbeddedWalletManager embeddedWalletManager,
            WalletApiRepository walletApiRepository, AuthDelegator authDelegator)
        {
            IRpcExecutor rpcExecutor = account.IsOnDevice
                ? new WebViewRPCExecutor(walletEntropy, account.WalletIndex, embeddedWalletManager)
                : new WalletApiRPCExecutor(walletApiRepository, authDelegator, account.Id);
            var embeddedSolanaWalletProvider =
                new EmbeddedSolanaWalletProvider(rpcExecutor);

            return new EmbeddedSolanaWallet(account, embeddedSolanaWalletProvider);
        }
    }
}