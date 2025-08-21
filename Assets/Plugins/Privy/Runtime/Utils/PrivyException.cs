using System;

namespace Privy
{
    public class PrivyException : Exception
    {
        //Messgae property inherited from Exception

        public PrivyException(string message) : base(message)
        {
        }

        // Authentication-specific exception
        public class AuthenticationException : PrivyException
        {
            public AuthenticationError Error { get; }

            public AuthenticationException(string message, AuthenticationError error)
                : base(message)
            {
                Error = error;
            }
        }

        // Embedded Wallet-specific exception
        public class EmbeddedWalletException : PrivyException
        {
            public EmbeddedWalletError Error { get; }

            public EmbeddedWalletException(string message, EmbeddedWalletError error) : base(message)
            {
                Error = error;
            }
        }
    }


    // Enum for specific authentication errors
    public enum AuthenticationError
    {
        SendCodeFailed,
        EmailEmpty,
        OtpEmpty,
        CodeChallengeEmpty,
        RedirectUriEmpty,
        StateCodeEmpty,
        AuthorizationCodeEmpty,
        CodeVerifierEmpty,
        InvalidOAuthResult,
        RefreshFailed,
        WrongOtpCode,
        OAuthInitFailed,
        OAuthVerificationFailed,
        OAuthAuthenticateFailed,
        NotAuthenticated
    }

    public enum EmbeddedWalletError
    {
        WalletDoesNotExist,
        ConnectionFailed,
        CreateFailed,
        CreateAdditionalFailed,
        RecoverFailed,
        RpcRequestFailed,
        UserSignerRequestFailed,
        MaxWalletsCreated,
    }
}