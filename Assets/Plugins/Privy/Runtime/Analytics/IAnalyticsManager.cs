using System.Threading.Tasks;

namespace Privy
{
    internal interface IAnalyticsManager
    {
        Task LogEvent(AnalyticsEvent analyticsEvent);
    }

    internal class AnalyticsManager : IAnalyticsManager
    {
        private IAnalyticsRepository _analyticsRepository;

        public AnalyticsManager(IAnalyticsRepository analyticsRepository)
        {
            _analyticsRepository = analyticsRepository;
        }

        public Task LogEvent(AnalyticsEvent analyticsEvent)
        {
            return _analyticsRepository.LogEvent(analyticsEvent);
        }
    }
}