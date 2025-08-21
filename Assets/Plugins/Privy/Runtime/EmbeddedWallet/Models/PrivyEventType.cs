namespace Privy
{
    public static class PrivyEventType
    {
        public const string Ready = "privy:iframe:ready";
        public const string CreateEthereumWallet = "privy:wallet:create"; // ETH-only
        public const string CreateSolanaWallet = "privy:solana-wallet:create"; // SOL-only
        public const string CreateAdditional = "privy:wallets:add";
        public const string Connect = "privy:wallets:connect";
        public const string Recover = "privy:wallets:recover";
        public const string Rpc = "privy:wallets:rpc";
        public const string SignWithUserSigner = "privy:user-signer:sign";
    }
}