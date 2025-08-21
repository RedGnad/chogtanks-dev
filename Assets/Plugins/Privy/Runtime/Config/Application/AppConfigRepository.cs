using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Privy
{
    /// <summary>
    /// Repository responsible for fetching the app configuration from the Privy API
    /// </summary>
    internal class AppConfigRepository
    {
        private readonly PrivyConfig _privyConfig;
        private readonly IHttpRequestHandler _httpRequestHandler;

        private AppConfig? _cachedAppConfig;

        internal AppConfigRepository(PrivyConfig privyConfig, IHttpRequestHandler httpRequestHandler)
        {
            _privyConfig = privyConfig;
            _httpRequestHandler = httpRequestHandler;

            // Best effort fetch so the cache is populated during app init, ready for callers
            _ = LoadAppConfig();
        }

        internal async Task<AppConfig> LoadAppConfig()
        {
            if (_cachedAppConfig != null)
            {
                return _cachedAppConfig.Value;
            }

            string jsonResponse =
                await _httpRequestHandler.SendRequestAsync($"apps/{_privyConfig.AppId}", "", method: "GET");
            var response = JsonConvert.DeserializeObject<AppConfig>(jsonResponse);
            _cachedAppConfig = response;
            return response;
        }
    }
}