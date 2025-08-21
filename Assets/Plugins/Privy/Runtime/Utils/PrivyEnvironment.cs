using System;

namespace Privy
{
    internal static class PrivyEnvironment
    {
        private static readonly string[] _internalAppIds =
        {
            "clpijy3tw0001kz0g6ixs9z15",
            "cla06f34x0001mh08l8nsr496"
        };

        private static bool _isProduction = true; // Default to true, incase initialize is not called

        internal static string BASE_URL => _isProduction ? "https://auth.privy.io" : "https://auth.staging.privy.io";

        internal static void Initialize(PrivyConfig config)
        {
            _isProduction = !IsInternalAppId(config.AppId);
        }

        private static bool IsInternalAppId(string appId)
        {
            // Check if the provided AppId is in the array of internal app IDs
            return Array.Exists(_internalAppIds, id => id == appId);
        }
    }
}