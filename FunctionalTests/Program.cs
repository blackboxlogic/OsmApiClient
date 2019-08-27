using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OsmSharp.IO.API.FunctionalTests
{
	class Program
	{
		public static void Main()
		{
			var Config = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", false, true)
				.Build();

			var loggerFactory = MakeLoggerFactory();
			var clientLogger = loggerFactory.CreateLogger("Client");
			var testsLogger = loggerFactory.CreateLogger("Tests");

			var clientFactory = new ClientsFactory(clientLogger, new HttpClient(), Config["osmApiUrl"]);

			try
			{
				// Test no auth
				testsLogger.LogInformation("Testing unauthenticated client");
				var client = clientFactory.CreateNonAuthClient();
				Tests.TestClient(client).Wait();
				testsLogger.LogInformation("All tests passed for the unauthenticated client.");

				// Test BasicAuth
				if (!string.IsNullOrEmpty(Config["basicAuth:Password"]))
				{
					if (!Config["osmApiUrl"].Contains("dev")) throw new Exception("These tests modify data, and it looks like your running them in PROD, please don't");

					testsLogger.LogInformation("Testing BasicAuth client");
					var basicAuth = clientFactory.CreateBasicAuthClient(Config["basicAuth:User"], Config["basicAuth:Password"]);
					Tests.TestAuthClient(basicAuth).Wait();
					testsLogger.LogInformation("All tests passed for the BasicAuth client.");
				}
				else
				{
					testsLogger.LogWarning("Skipped BasicAuth tests, no credentials supplied.");
				}

				// Test OAuth
				if (!string.IsNullOrEmpty(Config["oAuth:consumerSecret"]))
				{
					if (!Config["osmApiUrl"].Contains("dev")) throw new Exception("These tests modify data, and it looks like your running them in PROD, please don't");

					testsLogger.LogInformation("Testing OAuth client");
					var oAuth = clientFactory.CreateOAuthClient(Config["oAuth:consumerKey"],
						Config["oAuth:consumerSecret"],
						Config["oAuth:token"],
						Config["oAuth:tokenSecret"]);
					Tests.TestAuthClient(oAuth).Wait();
					testsLogger.LogInformation("All tests passed for the OAuth client.");
				}
				else
				{
					testsLogger.LogWarning("Skipped OAuth tests, no credentials supplied.");
				}
			}
			catch (Exception e)
			{
				testsLogger.LogCritical("Tests failed: {0}", e);
			}

			Console.ReadKey(true);
		}

		private static ILoggerFactory MakeLoggerFactory()
		{
			IServiceCollection serviceCollection = new ServiceCollection();
			serviceCollection.AddLogging(builder => builder.AddConsole().AddFilter(level => true));
			var loggerFactory = serviceCollection.BuildServiceProvider().GetService<ILoggerFactory>();

			return loggerFactory;
		}
	}
}
