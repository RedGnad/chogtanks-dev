namespace Privy
{
    internal interface IPlayerPrefsDataManager
    {
        //Generic Types here
        void SaveData<T>(string key, T data);
        string LoadData<T>(string key);
        void DeleteData(string key);
    }
}