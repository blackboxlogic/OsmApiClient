using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace OsmSharp.IO.API.FunctionalTests
{
	class Program
	{
		public static void Main()
		{
			var Config = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", false, true)
				.Build();

			Console.Write("Testing unauthenticated client");
            var factory = new ClientsFactory(null, new HttpClient(), Config["osmApiUrl"]);
            var client = factory.CreateNonAuthClient();
			Tests.TestClient(client).Wait();
			Console.WriteLine("All tests passed for the unauthenticated client.");

			if (!string.IsNullOrEmpty(Config["basicAuth:Password"]))
			{
				if (!Config["osmApiUrl"].Contains("dev")) throw new Exception("These tests modify data, and it looks like your running them in PROD, please don't");

				Console.Write("Testing BasicAuth client");
				var basicAuth = factory.CreateBasicAuthClient(Config["basicAuth:User"], Config["basicAuth:Password"]);
				Tests.TestAuthClient(basicAuth).Wait();
				Console.WriteLine("All tests passed for the BasicAuth client.");
			}
			else
			{
				Console.WriteLine("Skipped BasicAuth tests, no credentials supplied.");
			}

			if (!string.IsNullOrEmpty(Config["oAuth:consumerSecret"]))
			{
				if (!Config["osmApiUrl"].Contains("dev")) throw new Exception("These tests modify data, and it looks like your running them in PROD, please don't");

				Console.Write("Testing OAuth client");
				var oAuth = factory.CreateOAuthClient(Config["oAuth:consumerKey"], 
                    Config["oAuth:consumerSecret"], 
                    Config["oAuth:token"],
                    Config["oAuth:tokenSecret"]);
				Tests.TestAuthClient(oAuth).Wait();
				Console.WriteLine("All tests passed for the OAuth client.");
			}
			else
			{
				Console.WriteLine("Skipped OAuth tests, no credentials supplied.");
			}

			Console.ReadKey(true);
		}
	}
}
