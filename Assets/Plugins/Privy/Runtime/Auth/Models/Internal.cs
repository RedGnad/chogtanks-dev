using System.Collections.Generic;

namespace Privy
{
    internal class InternalPrivyUser
    {
        public string Id;
        public PrivyLinkedAccount[] LinkedAccounts;
        public Dictionary<string, string> CustomMetadata;
    }

    internal class InternalAuthSession
    {
        public InternalPrivyUser User;
        public string AccessToken;
        public string RefreshToken;
        public string SessionUpdateAction;
    }
}