//This class is for keep track of the wallet connected state
//It also ties the connected state to the wallet address
//Essentially, if the state is connected, you can also get the address from that call, without needing to call EmbeddedWallets and parse the primary address

namespace Privy
{
    public abstract class EmbeddedWalletState
    {
        public sealed class Disconnected : EmbeddedWalletState
        {
            // Provide a public constructor so you can instantiate this class
            public Disconnected()
            {
            }
        }

        public sealed class Connected : EmbeddedWalletState
        {
            public string WalletAddress { get; }

            public Connected(string walletAddress)
            {
                WalletAddress = walletAddress;
            }
        }
    }
}