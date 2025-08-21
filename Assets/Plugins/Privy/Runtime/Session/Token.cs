using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Privy
{
    internal class Token
    {
        public string Value { get; private set; }
        private JObject _decoded;

        public Token(string value)
        {
            Value = value;
            _decoded = DecodeJwt(value);
        }

        public static Token Parse(string token)
        {
            try
            {
                return new Token(token);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private JObject DecodeJwt(string token)
        {
            string[] parts = token.Split('.');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Invalid JWT format.");
            }

            byte[] decodedBytes = Base64UrlDecode(parts[1]);
            string payloadJson = Encoding.UTF8.GetString(decodedBytes);
            return JObject.Parse(payloadJson);
        }

        private byte[] Base64UrlDecode(string input)
        {
            string output = input.Replace('-', '+').Replace('_', '/');
            switch (output.Length % 4)
            {
                case 0: break;
                case 2:
                    output += "==";
                    break;
                case 3:
                    output += "=";
                    break;
                default: throw new FormatException("Illegal base64url string!");
            }

            return Convert.FromBase64String(output);
        }

        public string Subject => _decoded["sub"]?.ToString();
        public long Expiration => _decoded["exp"]?.ToObject<long>() ?? 0;
        public string Issuer => _decoded["iss"]?.ToString();
        public string Audience => _decoded["aud"]?.ToString();

        public bool IsExpired(int seconds = 0)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return now >= Expiration - seconds;
        }
    }
}