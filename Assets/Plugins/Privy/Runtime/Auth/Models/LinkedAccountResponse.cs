using Newtonsoft.Json;

namespace Privy
{
    internal class LinkedAccountResponse
    {
        // TODO: convert type to enum
        [JsonProperty("type")]
        public string Type;

        // TODO: convert below date longs to dates
        [JsonProperty("verified_at")]
        public long VerifiedAt;

        [JsonProperty("first_verified_at")]
        public long FirstVerifiedAt;

        [JsonProperty("latest_verified_at")]
        public long LatestVerifiedAt;
    }

    internal class WalletAccountResponse : LinkedAccountResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("imported")]
        public bool Imported { get; set; }

        [JsonProperty("wallet_index")]
        public int WalletIndex { get; set; }

        [JsonProperty("chain_id")]
        public string ChainId { get; set; }

        [JsonProperty("chain_type")]
        public string ChainType { get; set; }

        [JsonProperty("wallet_client")]
        public string WalletClient { get; set; }

        [JsonProperty("wallet_client_type")]
        public string WalletClientType { get; set; }

        [JsonProperty("connector_type")]
        public string ConnectorType { get; set; }

        [JsonProperty("public_key")]
        public string PublicKey { get; set; }

        [JsonProperty("recovery_method")]
        public string RecoveryMethod { get; set; }
    }

    internal class EmailAccountResponse : LinkedAccountResponse
    {
        [JsonProperty("address")]
        public string Address { get; set; }
    }

    internal class GoogleOAuthAccountResponse : LinkedAccountResponse
    {
        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    internal class DiscordOAuthAccountResponse : LinkedAccountResponse
    {
        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("username")]
        public string UserName { get; set; }
    }

    internal class TwitterOAuthAccountResponse : LinkedAccountResponse
    {
        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("profile_picture_url")]
        public string ProfilePictureUrl { get; set; }
    }

    internal class AppleOAuthAccountResponse : LinkedAccountResponse
    {
        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}