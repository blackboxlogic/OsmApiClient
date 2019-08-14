using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OsmSharp.API;
using OsmSharp.Streams;
using OsmSharp.Streams.Complete;
using OsmSharp.Complete;
using System.IO;
using System.Xml.Serialization;
using OsmSharp.Changesets;
using System.Text;

namespace OsmSharp.IO.API
{
	// TODO:
	// get auth to work
	// functional tests
	// logging
	// cite sources/licenses
	// track data transfered?
	// Choose better namespaces?
	// Make sure Client covers every API action from https://wiki.openstreetmap.org/wiki/API_v0.6#API_calls
	public class Client
	{
		/// <summary>
		/// The OSM base address
		/// </summary>
		/// <example>
		/// "https://master.apis.dev.openstreetmap.org/api/0.6/"
		/// "https://www.openstreetmap.org/api/0.6/"
		/// </example>
		protected readonly string BaseAddress;

		private string OsmMaxPrecision = ".#######";

		public Client(string baseAddress)
		{
			BaseAddress = baseAddress;
		}

		public async Task<double?> GetVersions()
		{
			var osm = await Get<Osm>(BaseAddress + "versions");
			return osm.Api.Version.Maximum;
		}

		public async Task<Osm> GetCapabilities()
		{
			return await Get<Osm>(BaseAddress + "0.6/capabilities");
		}

		public async Task<Osm> GetMap(Bounds bounds)
		{
			Validate.BoundLimits(bounds);
			var address = BaseAddress + $"0.6/map?bbox={ToString(bounds)}";

			return await Get<Osm>(address);
		}

		public Task<CompleteWay> GetCompleteWay(long id)
		{
			return GetCompleteElement<CompleteWay>(id);
		}

		public Task<CompleteRelation> GetCompleteRelation(long id)
		{
			return GetCompleteElement<CompleteRelation>(id);
		}

		private async Task<TCompleteOsmGeo> GetCompleteElement<TCompleteOsmGeo>(long id) where TCompleteOsmGeo : ICompleteOsmGeo, new()
		{
			var type = new TCompleteOsmGeo().Type.ToString().ToLower();
			var address = BaseAddress + $"0.6/{type}/{id}/full";
			var content = await Get(address);
			var stream = await content.ReadAsStreamAsync();
			var streamSource = new XmlOsmStreamSource(stream);
			var completeSource = new OsmSimpleCompleteStreamSource(streamSource);
			var element = completeSource.OfType<TCompleteOsmGeo>().FirstOrDefault();
			return element;
		}

		public async Task<Node> GetNode(long id)
		{
			return await GetElement<Node>(id);
		}

		public async Task<Way> GetWay(long id)
		{
			return await GetElement<Way>(id);
		}

		public async Task<Relation> GetRelation(long id)
		{
			return await GetElement<Relation>(id);
		}

		private async Task<TOsmGeo> GetElement<TOsmGeo>(long id) where TOsmGeo : OsmGeo, new()
		{
			var type = new TOsmGeo().Type.ToString().ToLower();
			var address = BaseAddress + $"0.6/{type}/{id}";
			var content = await Get(address);
			var streamSource = new XmlOsmStreamSource(await content.ReadAsStreamAsync());
			var element = streamSource.OfType<TOsmGeo>().FirstOrDefault();
			return element;
		}

		/// <summary>
		/// Changeset Read
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Read:_GET_.2Fapi.2F0.6.2Fchangeset.2F.23id.3Finclude_discussion.3Dtrue">
		/// GET /api/0.6/changeset/#id?include_discussion=true</see>.
		/// </summary>
		public async Task<Changeset> GetChangeset(long changesetId, bool includeDiscussion = false)
		{
			var address = BaseAddress + $"0.6/changeset/{changesetId}";
			if (includeDiscussion)
			{
				address += "?include_discussion=true";
			}
			var osm = await Get<Osm>(address);
			return osm.Changesets[0];
		}

		/// <summary>
		/// Changeset Query
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Query:_GET_.2Fapi.2F0.6.2Fchangesets">
		/// GET /api/0.6/changesets</see>
		/// </summary>
		public async Task<Osm> GetChangesets(Bounds bounds, int? userId, string userName,
			DateTime? minClosedDate, DateTime? maxOpenedDate, bool openOnly, bool closedOnly,
			long[] changesetIds)
		{
			if (userId.HasValue && userName != null)
			{
				throw new Exception("Query can only specify userID OR userName, not both.");
			}
			if (openOnly && closedOnly)
			{
				throw new Exception("Query can only specify openOnly OR closedOnly, not both.");
			}
			if (!minClosedDate.HasValue && maxOpenedDate.HasValue)
			{
				throw new Exception("Query must specify minClosedDate if maxOpenedDate is specified.");
			}

			var address = BaseAddress + "0.6/changesets?";
			if (bounds != null)
			{
				address += "bbox=" + ToString(bounds) + '&';
			}

			if (userId.HasValue)
			{
				address += "user=" + userId + '&';
			}
			else if (userName != null)
			{
				address += "display_name=" + userName + '&';
			}

			if (minClosedDate.HasValue)
			{
				address += "time=" + minClosedDate;
				if (maxOpenedDate.HasValue)
				{
					address += "," + maxOpenedDate;
				}
				address += '&';
			}

			if (openOnly)
			{
				address += "open=true&";
			}
			else if (closedOnly)
			{
				address += "closed=true&";
			}

			if (changesetIds != null)
			{
				address += $"changesets={string.Join(",", changesetIds)}&";
			}

			address = address.Substring(0, address.Length - 1); // remove the last &

			return await Get<Osm>(address);
		}

		/// <summary>
		/// Changeset Download
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Read:_GET_.2Fapi.2F0.6.2Fchangeset.2F.23id.3Finclude_discussion.3Dtrue">
		/// GET /api/0.6/changeset/#id/download</see>
		/// </summary>
		public async Task<OsmChange> GetChangesetDownload(long changesetId)
		{
			return await Get<OsmChange>(BaseAddress + $"0.6/changeset/{changesetId}/download");
		}

		protected async Task<T> Get<T>(string address, Action<HttpClient> auth = null) where T : class
		{
			var content = await Get(address, auth);
			var stream = await content.ReadAsStreamAsync();
			var serializer = new XmlSerializer(typeof(T));
			var element = serializer.Deserialize(stream) as T;
			return element;
		}

		protected async Task<HttpContent> Get(string address, Action<HttpClient> auth = null)
		{
			var client = new HttpClient();
			if (auth != null) auth(client);
			var response = await client.GetAsync(address);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				throw new Exception($"Request failed: {response.StatusCode}-{response.ReasonPhrase} {errorContent}");
			}

			return response.Content;
		}

		protected string ToString(Bounds bounds)
		{
			StringBuilder x = new StringBuilder();
			x.Append(bounds.MinLongitude.Value.ToString(OsmMaxPrecision));
			x.Append(',');
			x.Append(bounds.MinLatitude.Value.ToString(OsmMaxPrecision));
			x.Append(',');
			x.Append(bounds.MaxLongitude.Value.ToString(OsmMaxPrecision));
			x.Append(',');
			x.Append(bounds.MaxLatitude.Value.ToString(OsmMaxPrecision));

			return x.ToString();
		}
	}
}

