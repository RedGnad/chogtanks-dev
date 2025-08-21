using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Privy
{
    internal interface IAnalyticsRepository
    {
        Task LogEvent(AnalyticsEvent analyticsEvent);
    }

    internal class AnalyticsEventRequestData
    {
        [JsonProperty("event_name")]
        public string EventName;

        [JsonProperty("client_id")]
        public string ClientId;
    }

    class AnalyticsRepository : IAnalyticsRepository
    {
        private IHttpRequestHandler _httpRequestHandler;
        private IClientAnalyticsIdRepository _clientAnalyticsIdRepository;

        public AnalyticsRepository(IHttpRequestHandler httpRequestHandler,
            IClientAnalyticsIdRepository clientAnalyticsIdRepository)
        {
            _httpRequestHandler = httpRequestHandler;
            _clientAnalyticsIdRepository = clientAnalyticsIdRepository;
        }

        public async Task LogEvent(AnalyticsEvent analyticsEvent)
        {
            string clientId = _clientAnalyticsIdRepository.LoadClientId();

            AnalyticsEventRequestData requestData = new AnalyticsEventRequestData
            {
                EventName = analyticsEvent.GetName(),
                ClientId = clientId
            };


            string serializedRequest = JsonConvert.SerializeObject(requestData);

            string path = "analytics_events";

            // Execute the request
            try
            {
                // Request will throw an error if it fails
                await _httpRequestHandler.SendRequestAsync(path, serializedRequest);
                PrivyLogger.Internal($"Logging {analyticsEvent.GetName()} analytics event succeeded!");
            }
            catch (Exception ex)
            {
                PrivyLogger.Internal($"Logging {analyticsEvent.GetName()} analytics event failed. {ex}");
            }
        }
    }

    // Extension method for the AnalyticsEvent enum to get the event name
    internal static class AnalyticsEventExtensions
    {
        public static string GetName(this AnalyticsEvent analyticsEvent)
        {
            switch (analyticsEvent)
            {
                case AnalyticsEvent.SdkInitialize:
                    return "sdk_initialize";
                default:
                    return "";
            }
        }
    }

    // AnalyticsEvent enum
    internal enum AnalyticsEvent
    {
        SdkInitialize
    }
}