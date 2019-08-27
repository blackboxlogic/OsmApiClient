using System;
using System.Net;

namespace OsmSharp.IO.API
{
	public class OsmApiException : Exception
	{
		public readonly HttpStatusCode StatusCode;
		public readonly Uri Request;

		public OsmApiException(Uri request, string reason, HttpStatusCode statusCode)
			: base(reason)
		{
			StatusCode = statusCode;
			Request = request;
		}
	}
}
