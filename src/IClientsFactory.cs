namespace OsmSharp.IO.API
{
    /// <summary>
    /// This factory is used to create all the OSM API clients
    /// This inteface is provided to allow mocking and DI
    /// </summary>
    public interface IClientsFactory
    {
        /// <summary>
        /// Creates a client that does not need authentication and thus can't perform authenticated operations
        /// </summary>
        /// <returns></returns>
        INonAuthClient CreateNonAuthClient();
        /// <summary>
        /// Creates a client that needs user name and password credentials
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        IAuthClient CreateBasicAuthClient(string username, string password);
        /// <summary>
        /// Creates a client that will use OAuth 1.0 credentials provided from the OAuth OSM page
        /// </summary>
        /// <param name="consumerKey"></param>
        /// <param name="consumerSecret"></param>
        /// <param name="token"></param>
        /// <param name="tokenSecret"></param>
        /// <returns></returns>
        IAuthClient CreateOAuthClient(string consumerKey, string consumerSecret, string token, string tokenSecret);

        /// <summary>
        /// Creates a client that will use OAuth 2.0 credentials provided from the OAuth OSM page
        /// </summary>
        /// <param name="token">The token that you got after getting the code and posting it to the token server</param>
        /// <returns></returns>
        IAuthClient CreateOAuth2Client(string token);
    }
}