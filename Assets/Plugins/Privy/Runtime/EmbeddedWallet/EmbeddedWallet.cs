namespace Privy
{
    internal class EmbeddedWallet : IEmbeddedEthereumWallet
    {
        public string Id { get; }
        public string Address { get; }
        public string ChainId { get; }
        public string RecoveryMethod { get; }
        public int HdWalletIndex { get; }

        public IRpcProvider RpcProvider { get; } // Use IRpcProvider as the type

        public EmbeddedWallet(PrivyEmbeddedWalletAccount account, IRpcProvider rpcProvider)
        {
            Id = account.Id;
            Address = account.Address;
            ChainId = account.ChainId;
            RecoveryMethod = account.RecoveryMethod;
            HdWalletIndex = account.WalletIndex;
            RpcProvider = rpcProvider;
        }

        internal static EmbeddedWallet Create(PrivyEmbeddedWalletAccount account, WalletEntropy walletEntropy,
            EmbeddedWalletManager embeddedWalletManager, WalletApiRepository walletApiRepository,
            AuthDelegator authDelegator)
        {
            IRpcExecutor rpcExecutor = account.IsOnDevice
                ? new WebViewRPCExecutor(walletEntropy, account.WalletIndex, embeddedWalletManager)
                : new WalletApiRPCExecutor(walletApiRepository, authDelegator, account.Id);
            var rpcProvider = new RpcProvider(rpcExecutor);

            return new EmbeddedWallet(account, rpcProvider);
        }
    }
}