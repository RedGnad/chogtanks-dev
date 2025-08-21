using System.Linq;

namespace Privy
{
    internal static class AuthSessionResponseMapper
    {
        public static InternalAuthSession MapToInternalSession(ValidSessionResponse authResponse)
        {
            return new InternalAuthSession
            {
                User = new InternalPrivyUser
                {
                    Id = authResponse.User.Id,
                    LinkedAccounts = authResponse.User.LinkedAccounts
                        .Select(account =>
                        {
                            var mappedAccount = LinkedAccountResponseMapper.MapToPublic(account);
                            PrivyLogger.Debug($"Mapped account type: {mappedAccount?.GetType().Name ?? "null"}");
                            return mappedAccount;
                        })
                        .Where(mappedAccount => mappedAccount != null) // Filter out null values
                        .ToArray(), // Convert to array
                    CustomMetadata = authResponse.User.CustomMetadata
                },
                AccessToken = authResponse.Token,
                RefreshToken = authResponse.RefreshToken,
                SessionUpdateAction = authResponse.SessionUpdateAction //TEMP
            };
        }
    }
}