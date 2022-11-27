using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace OsmSharp.IO.API
{
    /// <inheritdoc/>
    public class ClientsFactory : IClientsFactory
    {
        /// <summary>
        /// The URL of the production instance of OSM's API. Use with care.
        /// </summary>
        public const string PRODUCTION_URL = @"https://www.openstreetmap.org/api/";

        /// <summary>
        /// The URL of the development instance of OSM's API. The correct place to do testing.
        /// </summary>
        public const string DEVELOPMENT_URL = @"https://master.apis.dev.openstreetmap.org/api/";

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly string _baseAddress;

        public ClientsFactory(ILogger logger, HttpClient httpClient, string baseAddress)
        {
            _logger = logger;
            _httpClient = httpClient;
            _baseAddress = baseAddress;
        }

        /// <inheritdoc/>
        public INonAuthClient CreateNonAuthClient()
        {
            return new NonAuthClient(_baseAddress, _httpClient, _logger);
        }

        /// <inheritdoc/>
        public IAuthClient CreateBasicAuthClient(string username, string password)
        {
            return new BasicAuthClient(_httpClient, _logger, _baseAddress, username, password);
        }

        /// <inheritdoc/>
        public IAuthClient CreateOAuthClient(string consumerKey, string consumerSecret, string token, string tokenSecret)
        {
            return new OAuthClient(_httpClient, _logger, _baseAddress, consumerKey, consumerSecret, token, tokenSecret);
        }
        
        /// <inheritdoc/>
        public IAuthClient CreateOAuth2Client(string token)
        {
            return new OAuth2Client(_httpClient, _logger, _baseAddress, token);
        }
    }
}
