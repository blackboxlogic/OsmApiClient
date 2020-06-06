using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace OsmSharp.IO.API
{
    public class OAuthClient : AuthClient
    {
        /// <summary>
        /// The OSM consumer key that was generated from OSM site
        /// </summary>
        private readonly string ConsumerKey;
        /// <summary>
        /// The OSM consumer secret that was generated from OSM site
        /// </summary>
        private readonly string ConsumerSecret;
        private readonly string Token;
        private readonly string TokenSecret;

        public OAuthClient(HttpClient httpClient,
            ILogger logger,
            string baseAddress,
            string consumerKey, 
            string consumerSecret, 
            string token, 
            string tokenSecret)
            : base (baseAddress, httpClient, logger)
        {
            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;
            Token = token;
            TokenSecret = tokenSecret;
        }

        protected override void AddAuthentication(HttpRequestMessage message, string url, string method = "GET")
        {
            var request = new OAuth.OAuthRequest
            {
                ConsumerKey = ConsumerKey,
                ConsumerSecret = ConsumerSecret,
                Token = Token,
                TokenSecret = TokenSecret,
                Type = OAuth.OAuthRequestType.ProtectedResource,
                SignatureMethod = OAuth.OAuthSignatureMethod.HmacSha1,
                RequestUrl = url,
                Version = "1.0",
                Method = method
            };
            var auth = request.GetAuthorizationHeader().Replace("OAuth ", "");
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", auth);
        }
    }
}