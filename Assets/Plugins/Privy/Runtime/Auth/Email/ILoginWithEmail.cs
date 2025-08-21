using System.Threading.Tasks;

namespace Privy
{
    /// <summary>
    /// Interface for email-based authentication methods.
    /// Provides functionality to send a one-time password (OTP) code to an email
    /// and to authenticate using the sent OTP code.
    /// </summary>
    public interface ILoginWithEmail
    {
        /// <summary>
        /// Sends a one-time password (OTP) code to the specified email address.
        /// </summary>
        /// <param name="email">The email address to which the OTP code will be sent. Must be a valid, non-empty email address.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is a boolean indicating whether the code was successfully sent.</returns>
        /// <exception cref="PrivyException.AuthenticationException">Thrown if there is an error while sending the OTP code.</exception>
        Task<bool> SendCode(string email);

        /// <summary>
        /// Authenticates a user using the provided email and OTP code.
        /// </summary>
        /// <param name="email">The email address associated with the OTP code. Must be the same email address used to request the code.</param>
        /// <param name="code">The OTP code received via email. Must be a valid, non-empty code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is the authentication state (Authenticated, Unauthenticated), indicating whether the login was successful or failed.</returns>
        /// <exception cref="PrivyException.AuthenticationException">Thrown if the authentication fails due to an invalid OTP code.</exception>
        Task<AuthState> LoginWithCode(string email, string code);
    }
}