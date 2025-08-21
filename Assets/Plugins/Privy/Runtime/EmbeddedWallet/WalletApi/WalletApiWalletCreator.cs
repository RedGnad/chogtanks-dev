using System.Linq;
using System.Threading.Tasks;

namespace Privy
{
    internal class WalletApiWalletCreator
    {
        internal struct CreatedWallet
        {
            internal ChainType ChainType;
            internal string Address;
        }

        private readonly AuthDelegator _authDelegator;
        private readonly WalletApiRepository _walletApiRepository;

        internal WalletApiWalletCreator(AuthDelegator authDelegator, WalletApiRepository walletApiRepository)
        {
            _authDelegator = authDelegator;
            _walletApiRepository = walletApiRepository;
        }

        internal async Task<CreatedWallet> CreateWallet(ChainType chainType, bool allowAdditional)
        {
            var accessToken = await _authDelegator.GetAccessToken();

            if (!allowAdditional)
            {
                var accounts = _authDelegator.GetAuthSession().User.LinkedAccounts;
                var hasEthereumWallets = accounts.Any(account => account is PrivyEmbeddedWalletAccount);
                var hasSolanaWallets = accounts.Any(account => account is PrivyEmbeddedSolanaWalletAccount);
                if ((chainType == ChainType.Ethereum && hasEthereumWallets) ||
                    (chainType == ChainType.Solana && hasSolanaWallets))
                    throw new PrivyException.EmbeddedWalletException(
                        "Wallet Create Failed: Primary wallet already exists. To create an additional wallet, set allowAdditional to true.",
                        EmbeddedWalletError.CreateFailed);
            }

            var request = new WalletApiCreateRequest { chainType = chainType };
            var response = await _walletApiRepository.CreateWallet(request, accessToken);

            return new CreatedWallet()
            {
                Address = response.Address,
                ChainType = response.ChainType,
            };
        }
    }
}