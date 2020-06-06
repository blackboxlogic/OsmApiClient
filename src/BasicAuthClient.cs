using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;

namespace OsmSharp.IO.API
{
    /// <summary>
    /// Use of basic auth is discouraged. Use OAuth when practical.
    /// </summary>
    public class BasicAuthClient : AuthClient
    {
        private readonly string Username;
        private readonly string Password;

        public BasicAuthClient(HttpClient httpClient,
            ILogger logger,
            string baseAddress, 
            string username, 
            string password)
            : base (baseAddress, httpClient, logger)
        {
            Username = username;
            Password = password;
        }

        protected override void AddAuthentication(HttpRequestMessage request, string url, string method = "GET")
        {
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(Username + ":" + Password));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
        }
    }
}
