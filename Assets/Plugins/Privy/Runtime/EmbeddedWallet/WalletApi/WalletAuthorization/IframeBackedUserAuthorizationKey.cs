using System.Threading.Tasks;

namespace Privy
{
    /// <summary>
    /// A user's authorization key backed by the embedded iframe
    /// </summary>
    internal class IframeBackedUserAuthorizationKey : AuthorizationKey
    {
        private readonly EmbeddedWalletManager _embeddedWalletManager;
        private readonly AuthDelegator _authDelegator;

        internal IframeBackedUserAuthorizationKey(EmbeddedWalletManager embeddedWalletManager,
            AuthDelegator authDelegator)
        {
            _embeddedWalletManager = embeddedWalletManager;
            _authDelegator = authDelegator;
        }

        async Task<byte[]> AuthorizationKey.Signature(byte[] message)
        {
            string token = await _authDelegator.GetAccessToken();

            return await _embeddedWalletManager.SignWithUserSigner(token, message);
        }
    }
}