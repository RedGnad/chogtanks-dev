using System.Collections.Generic;
using Newtonsoft.Json;
using Org.Webpki.JsonCanonicalizer;

namespace Privy
{
    internal struct WalletApiPayload
    {
        [JsonProperty("version")]
        internal int Version;

        [JsonProperty("url")]
        internal string Url;

        [JsonProperty("method")]
        internal string Method;

        [JsonProperty("headers")]
        internal Dictionary<string, string> Headers;

        [JsonProperty("body")]
        internal object Body;

        internal byte[] EncodePayload()
        {
            string json = JsonConvert.SerializeObject(this);
            var jsoncanicalizer = new JsonCanonicalizer(json);
            return jsoncanicalizer.GetEncodedUTF8();
        }
    }
}