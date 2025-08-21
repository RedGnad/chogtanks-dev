//Used for serializing polymorphic JSON objects

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Privy
{
    internal class LinkedAccountConverter : JsonConverter<LinkedAccountResponse>
    {
        public override void WriteJson(JsonWriter writer, LinkedAccountResponse value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override LinkedAccountResponse ReadJson(JsonReader reader, Type objectType,
            LinkedAccountResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            string type = jo["type"].ToString();

            LinkedAccountResponse account;

            switch (type)
            {
                case "wallet":
                    account = new WalletAccountResponse();
                    break;
                case "email":
                    account = new EmailAccountResponse();
                    break;
                case "google_oauth":
                    account = new GoogleOAuthAccountResponse();
                    break;
                case "discord_oauth":
                    account = new DiscordOAuthAccountResponse();
                    break;
                case "twitter_oauth":
                    account = new TwitterOAuthAccountResponse();
                    break;
                case "apple_oauth":
                    account = new AppleOAuthAccountResponse();
                    break;
                default:
                    //This will get filtered to a null in mapping, and not added to the linked accounts array
                    account = new LinkedAccountResponse();
                    break;
            }

            serializer.Populate(jo.CreateReader(), account);
            return account;
        }
    }
}