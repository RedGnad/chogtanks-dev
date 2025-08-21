using System;
using System.Security.Cryptography;
using System.Text;

namespace Privy
{
    /// <summary>
    ///     Provides a randomly generating PKCE code verifier and it's corresponding code challenge.
    /// </summary>
    public static class PKCE
    {
        /// <summary>
        ///     Generates a code_verifier and the corresponding code_challenge, as specified in the rfc-7636.
        /// </summary>
        /// <remarks>
        ///     See https://datatracker.ietf.org/doc/html/rfc7636#section-4.1 and
        ///     https://datatracker.ietf.org/doc/html/rfc7636#section-4.2
        /// </remarks>
        public static (string code_challenge, string verifier) Generate(int size = 32)
        {
            var verifier = GenerateCodeVerifier(size);
            var challenge = GenerateCodeChallenge(verifier);

            return (challenge, verifier);
        }

        public static string GenerateStateCode(int size = 32)
        {
            // We can reuse the same method for generating random bytes as the code verifier
            return GenerateCodeVerifier(size);
        }

        private static string GenerateCodeVerifier(int size = 32)
        {
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[size];
            rng.GetBytes(randomBytes);
            return Base64UrlEncode(randomBytes);
        }

        private static string GenerateCodeChallenge(string verifier)
        {
            var buffer = Encoding.UTF8.GetBytes(verifier);
            var hash = SHA256.Create().ComputeHash(buffer);
            return Base64UrlEncode(hash);
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}