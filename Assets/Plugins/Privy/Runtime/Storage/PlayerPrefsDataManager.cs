using UnityEngine;
using Newtonsoft.Json;

namespace Privy
{
    internal class PlayerPrefsDataManager : IPlayerPrefsDataManager
    {
        // Save data of any type
        public void SaveData<T>(string key, T data)
        {
            if (data is string stringValue)
            {
                PlayerPrefs.SetString(key, stringValue);
            }
            else
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                };

                string json = JsonConvert.SerializeObject(data, settings);
                PlayerPrefs.SetString(key, json);
            }

            PlayerPrefs.Save();
        }

        // Load data of any type
        public string LoadData<T>(string key)
        {
            return PlayerPrefs.GetString(key, string.Empty);
        }

        public void DeleteData(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }
    }
}