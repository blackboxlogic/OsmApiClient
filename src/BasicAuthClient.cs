using System;
using System.Net.Http;
using System.Text;

namespace OsmSharp.IO.API
{
	/// <summary>
	/// Use of basic auth is discouraged. Use OAuth when practical.
	/// </summary>
	public class BasicAuthClient : Client
	{
		private readonly string Username;
		private readonly string Password;

		public BasicAuthClient(string baseAddress, string username, string password)
			: base (baseAddress)
		{
			Username = username;
			Password = password;
		}

		protected override void AddAuthentication(HttpClient client, string url, string method = "GET")
		{
			var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(Username + ":" + Password));
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
		}
	}
}
