using System.Threading.Tasks;
using System.Collections.Generic;

namespace Privy
{
    internal interface IHttpRequestHandler
    {
        /// <summary>
        /// Gets the full url corresponding to an API call to a given path
        /// </summary>
        /// <param name="path">The API path to call, without a preceding slash</param>
        /// <returns>The full url corresponding to the API call</returns>
        string GetFullUrl(string path);

        //Generic Response/Request Data values, to place responsibility of data validation on the delegators/repositories
        Task<string> SendRequestAsync(string path, string jsonData, Dictionary<string, string> customHeaders = null,
            string method = "POST");
    }
}