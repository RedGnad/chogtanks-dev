namespace Privy
{
    static internal class Constants
    {
        // Refresh Defaults
        public const int DEFAULT_EXPIRATION_PADDING_IN_SECONDS = 30;

        // Request Headers
        public const string PRIVY_APP_ID_HEADER = "privy-app-id";

        public const string PRIVY_CLIENT_HEADER = "privy-client";

        public const string PRIVY_CLIENT_ID_HEADER = "privy-client-id";

        public const string PRIVY_NATIVE_APP_IDENTIFIER = "x-native-app-identifier";

        public const string PRIVY_CLIENT_ANALYTICS_ID_HEADER = "privy-ca-id";

        //Storage Keys
        public const string INTERNAL_AUTH_SESSION_KEY = "internalAuthSession";

        public const string ANALYTICS_CLIENT_ID_KEY = "privyAnalyticsClientId";
    }
}