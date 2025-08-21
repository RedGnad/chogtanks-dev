using System;
using System.Threading.Tasks;

namespace Privy
{
    internal class LoginWithEmail : ILoginWithEmail
    {
        private IAuthDelegator _authDelegator;

        public LoginWithEmail(IAuthDelegator authDelegator)
        {
            _authDelegator = authDelegator ?? throw new ArgumentNullException(nameof(authDelegator));
        }

        public async Task<bool> SendCode(string email)
        {
            return await _authDelegator.SendEmailCode(email);
        }

        public async Task<AuthState> LoginWithCode(string email, string code)
        {
            return await _authDelegator.LoginWithEmailCode(email, code);
        }
    }
}