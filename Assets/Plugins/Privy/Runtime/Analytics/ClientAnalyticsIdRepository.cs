using System;

namespace Privy
{
    public class ClientAnalyticsIdRepository : IClientAnalyticsIdRepository
    {
        private string clientIdCache;

        private readonly IPlayerPrefsDataManager _playerPrefsDataManager;

        internal ClientAnalyticsIdRepository(IPlayerPrefsDataManager playerPrefsDataManager)
        {
            _playerPrefsDataManager = playerPrefsDataManager;
        }

        private ClientAnalyticsIdRepository()
        {
        }

        public string LoadClientId()
        {
            if (!string.IsNullOrEmpty(clientIdCache))
            {
                return clientIdCache;
            }

            string persistedClientId = _playerPrefsDataManager.LoadData<string>(Constants.ANALYTICS_CLIENT_ID_KEY);

            if (!string.IsNullOrEmpty(persistedClientId))
            {
                return persistedClientId;
            }

            string generatedClientId = Guid.NewGuid().ToString().ToLower();

            _playerPrefsDataManager.SaveData(Constants.ANALYTICS_CLIENT_ID_KEY, generatedClientId);

            clientIdCache = generatedClientId;

            return generatedClientId;
        }

        public void ResetClientId()
        {
            _playerPrefsDataManager.DeleteData(Constants.ANALYTICS_CLIENT_ID_KEY);
            clientIdCache = null;
        }
    }
}