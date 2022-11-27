using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace OsmSharp.IO.API
{
    public class OAuth2Client : AuthClient
    {
        private readonly string Token;
        public OAuth2Client(HttpClient httpClient, ILogger logger, string baseAddress, string token) : base(baseAddress, httpClient, logger)
        {
            Token = token;
        }
        
        protected override void AddAuthentication(HttpRequestMessage message, string url, string method = "GET")
        {
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this.Token);
        }
    }
}