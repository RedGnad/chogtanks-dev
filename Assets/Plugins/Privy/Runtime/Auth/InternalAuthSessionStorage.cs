using Newtonsoft.Json;

namespace Privy
{
    internal class InternalAuthSessionStorage
    {
        private readonly IPlayerPrefsDataManager _playerPrefsDataManager;

        internal InternalAuthSessionStorage(IPlayerPrefsDataManager playerPrefsDataManager)
        {
            _playerPrefsDataManager = playerPrefsDataManager;
        }

        internal InternalAuthSession RetrieveInternalAuthSessionFromStorage()
        {
            string persistedSession =
                _playerPrefsDataManager.LoadData<InternalAuthSession>(Constants
                    .INTERNAL_AUTH_SESSION_KEY); //key should be a constant

            if (string.IsNullOrEmpty(persistedSession))
            {
                return null;
            }

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
            };

            return JsonConvert.DeserializeObject<InternalAuthSession>(persistedSession, settings);
        }

        internal void SaveInternalAuthSessionInStorage(InternalAuthSession internalAuthSession)
        {
            _playerPrefsDataManager.SaveData(Constants.INTERNAL_AUTH_SESSION_KEY, internalAuthSession);
        }

        internal void ClearInternalAuthSessionInStorage()
        {
            _playerPrefsDataManager.DeleteData(Constants.INTERNAL_AUTH_SESSION_KEY);
        }
    }
}