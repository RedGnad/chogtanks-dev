using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.Linq;

namespace Privy
{
    internal static class PrivyLinkedAccountExtensions
    {
        //These methods allow us to call methods on linkedAccounts, to get data specific to the types of account, such as EmbeddedWalletAccounts

        // Extension method to get the primary embedded wallet account (where walletIndex == 0)
        public static PrivyEmbeddedWalletAccount PrimaryEmbeddedWalletAccountOrNull(this PrivyLinkedAccount[] accounts)
        {
            return accounts
                .OfType<PrivyEmbeddedWalletAccount>()
                .FirstOrDefault(account => account.WalletIndex == 0);
        }

        internal static WalletEntropy? WalletEntropyOrNull(this PrivyLinkedAccount[] accounts)
        {
            // Wallet entropy prefers ETH as a source first.
            var primaryEthWallet = accounts
                .OfType<PrivyEmbeddedWalletAccount>()
                .FirstOrDefault(account => account.WalletIndex == 0);

            if (primaryEthWallet != null)
                return new WalletEntropy(primaryEthWallet.Address, EntropyIdVerifier.EthereumAddress);

            // SOL is used if no ETH account is available.
            var primarySolWallet = accounts
                .OfType<PrivyEmbeddedSolanaWalletAccount>()
                .FirstOrDefault(account => account.WalletIndex == 0);

            if (primarySolWallet != null)
                return new WalletEntropy(primarySolWallet.Address, EntropyIdVerifier.SolanaAddress);

            return null;
        }

        // Extension method to geta wallet account with a specified hd wallet index
        public static PrivyEmbeddedWalletAccount EmbeddedWalletByIndexOrNull(this PrivyLinkedAccount[] accounts,
            int hdWalletIndex)
        {
            return accounts
                .OfType<PrivyEmbeddedWalletAccount>()
                .FirstOrDefault(account => account.WalletIndex == hdWalletIndex);
        }

        // Extension method to get all embedded wallet accounts
        public static PrivyEmbeddedWalletAccount[] EmbeddedWalletAccounts(this PrivyLinkedAccount[] accounts)
        {
            return accounts
                .OfType<PrivyEmbeddedWalletAccount>()
                .ToArray();
        }

        /// <summary>
        /// Extension method to get all embedded Solana wallet accounts
        /// </summary>
        public static PrivyEmbeddedSolanaWalletAccount[] EmbeddedSolanaWalletAccounts(
            this PrivyLinkedAccount[] accounts)
        {
            return accounts
                .OfType<PrivyEmbeddedSolanaWalletAccount>()
                .ToArray();
        }

        // Extension method to get the embedded wallet with the greatest hd wallet index
        public static PrivyEmbeddedWalletAccount EmbeddedWalletWithGreatestWalletIndexOrNull(
            this PrivyLinkedAccount[] accounts)
        {
            return accounts
                .OfType<PrivyEmbeddedWalletAccount>()
                .OrderByDescending(account => account.WalletIndex)
                .FirstOrDefault();
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum OAuthProvider
    {
        [EnumMember(Value = "google")]
        Google,

        [EnumMember(Value = "apple")]
        Apple,

        [EnumMember(Value = "discord")]
        Discord,

        [EnumMember(Value = "twitter")]
        Twitter
    }

    public class PrivyAuthSession
    {
        public PrivyUser User;
    }
}