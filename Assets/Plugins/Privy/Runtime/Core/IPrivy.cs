using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Privy
{
    /// <summary>
    /// Interface representing the core functionality of the Privy SDK.
    /// Provides access to authentication, user data, and embedded wallet management.
    /// </summary>
    public interface IPrivy
    {
        /// <summary>
        /// Gets a value indicating whether the SDK is ready for use.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Gets the email-based authentication interface.
        /// </summary>
        ILoginWithEmail Email { get; }

        /// <summary>
        /// Gets the oauth-based authentication interface.
        /// </summary>
        ILoginWithOAuth OAuth { get; }

        /// <summary>
        /// Gets the authenticated user and provides access to their properties and methods.
        /// </summary>
        [Obsolete("Use privy.GetUser() instead, which handles awaiting ready under the hood.")]
        PrivyUser User { get; }

        /// <summary>
        /// Gets the authenticated user and provides access to their properties and methods, after waiting for SDK
        /// initialization to complete.
        /// </summary>
        /// <returns>The authenticated user if one exists, or null</returns>
        [ItemCanBeNull]
        Task<PrivyUser> GetUser();

        /// <summary>
        /// Gets the current authentication state of the user.
        /// </summary>
        [Obsolete("Use privy.GetAuthState() instead, which handles awaiting ready under the hood.")]
        AuthState AuthState { get; }

        /// <summary>
        /// Gets the current authentication state of the user, after waiting for SDK initialization to complete.
        /// </summary>
        /// <returns>The authentication state</returns>
        Task<AuthState> GetAuthState();

        /// <summary>
        /// Sets a callback method to be invoked when the authentication state changes.
        /// </summary>
        /// <param name="callback">A method to be called whenever the authentication state changes. The new state is passed as a parameter.</param>
        void SetAuthStateChangeCallback(Action<AuthState> callback);

        /// <summary>
        /// Logs the user out of the application, clearing the session and authentication data.
        /// </summary>
        void Logout();
    }
}