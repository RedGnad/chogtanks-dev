namespace Privy
{
    public interface IClientAnalyticsIdRepository
    {
        public string LoadClientId();

        void ResetClientId();
    }
}