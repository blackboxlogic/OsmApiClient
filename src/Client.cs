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
	// Don't dispose httpClient
	// cite sources/licenses
	// track data transfered?
	// Choose better namespaces?
	// Make sure Client covers every API action from https://wiki.openstreetmap.org/wiki/API_v0.6#API_calls
	// Add a readme
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

		private string _versions => BaseAddress + "versions";
		private string _capabilities => BaseAddress + "0.6/capabilities";
		private string _mapAddress => BaseAddress + "0.6/map?bbox=:bbox";
		private string _elementAddress => BaseAddress + "0.6/:type/:id";
		private string _completeElementAddress => BaseAddress + "0.6/:type/:id/full";
		private string _getChangesetAddress => BaseAddress + "0.6/changeset/:id";
		private string _getChangesetsAddress => BaseAddress + "0.6/changesets";
		private string _getChangesetDownloadAddress => BaseAddress + "0.6/changeset/:id/download";

		private string OsmMaxPrecision = ".#######";

		public Client(string baseAddress)
		{
			BaseAddress = baseAddress;
		}

		public async Task<double?> GetVersions()
		{
			using (var client = new HttpClient())
			{
				var response = await client.GetAsync(_versions);

				if (!response.IsSuccessStatusCode)
				{
					throw new Exception($"Unable to retrieve versions: {response.StatusCode}-{response.ReasonPhrase}");
				}

				var stream = await response.Content.ReadAsStreamAsync();
				var osm = FromContent(stream);
				return osm.Api.Version.Maximum;
			}
		}

		public async Task<Osm> GetCapabilities()
		{
			using (var client = new HttpClient())
			{
				var response = await client.GetAsync(_capabilities);

				if (!response.IsSuccessStatusCode)
				{
					throw new Exception($"Unable to retrieve versions: {response.StatusCode}-{response.ReasonPhrase}");
				}

				var stream = await response.Content.ReadAsStreamAsync();

				var osm = FromContent(stream);
				return osm;
			}
		}

		public async Task<Osm> GetMap(Bounds bounds)
		{
			Validate.BoundLimits(bounds);

			using (var client = new HttpClient())
			{
				var address = _mapAddress.Replace(":bbox", ToString(bounds));
				var response = await client.GetAsync(address);

				if (!response.IsSuccessStatusCode)
				{
					throw new Exception($"Unable to retrieve map: {response.StatusCode}-{response.ReasonPhrase}");
				}

				var stream = await response.Content.ReadAsStreamAsync();
				var osm = FromContent(stream);
				return osm;
			}
		}

		public async Task<ICompleteOsmGeo> GetElement(string elementId, string type)
		{
			if (type.Equals(OsmGeoType.Node.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				return await GetNode(elementId);
			}
			if (type.Equals(OsmGeoType.Way.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				return await GetCompleteWay(elementId);
			}
			if (type.Equals(OsmGeoType.Relation.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				return await GetCompleteRelation(elementId);
			}
			throw new ArgumentException($"invalid {nameof(type)}: {type}");
		}

		public Task<CompleteWay> GetCompleteWay(string wayId)
		{
			return GetCompleteElement<CompleteWay>(wayId, "way");
		}

		public Task<CompleteRelation> GetCompleteRelation(string relationId)
		{
			return GetCompleteElement<CompleteRelation>(relationId, "relation");
		}

		private async Task<TCompleteOsmGeo> GetCompleteElement<TCompleteOsmGeo>(string id, string type) where TCompleteOsmGeo : class, ICompleteOsmGeo
		{
			using (var client = new HttpClient())
			{
				var address = _completeElementAddress.Replace(":id", id).Replace(":type", type);
				var response = await client.GetAsync(address);
				if (response.StatusCode != HttpStatusCode.OK)
				{
					throw new Exception($"Unable to retrieve {type} {id}: {response.StatusCode}-{response.ReasonPhrase}");
				}
				var streamSource = new XmlOsmStreamSource(await response.Content.ReadAsStreamAsync());
				var completeSource = new OsmSimpleCompleteStreamSource(streamSource);
				return completeSource.OfType<TCompleteOsmGeo>().FirstOrDefault();
			}
		}

		// TODO: This should take a long.
		public Task<Node> GetNode(string nodeId)
		{
			return GetElement<Node>(nodeId, "node");
		}

		public Task<Way> GetWay(string wayId)
		{
			return GetElement<Way>(wayId, "way");
		}

		public Task<Relation> GetRelation(string relationId)
		{
			return GetElement<Relation>(relationId, "relation");
		}

		private async Task<TOsmGeo> GetElement<TOsmGeo>(string id, string type) where TOsmGeo : OsmGeo
		{
			using (var client = new HttpClient())
			{
				var address = _elementAddress.Replace(":id", id).Replace(":type", type);
				var response = await client.GetAsync(address);
				if (response.StatusCode != HttpStatusCode.OK)
				{
					return null;
				}
				var streamSource = new XmlOsmStreamSource(await response.Content.ReadAsStreamAsync());
				return streamSource.OfType<TOsmGeo>().FirstOrDefault();
			}
		}

		/// <summary>
		/// Gets a changeset's metadata.
		/// </summary>
		public async Task<Osm> GetChangeset(long changesetId, bool includeDiscussion = false)
		{
			using (var client = new HttpClient())
			{
				var address = _getChangesetAddress.Replace(":id", changesetId.ToString());
				if (includeDiscussion)
				{
					address += "?include_discussion=true";
				}
				var response = await client.GetAsync(address);
				if (!response.IsSuccessStatusCode)
				{
					var content = await response.Content.ReadAsStringAsync();
					throw new Exception($"Unable to get changeset: {response.StatusCode}-{response.ReasonPhrase} {content}");
				}

				var stream = await response.Content.ReadAsStreamAsync();
				var osm = FromContent(stream);
				return osm;
			}
		}

		/// <summary>
		/// This is an API method for querying changesets.
		/// </summary>
		/// <remarks>
		/// It supports querying by different criteria. Where multiple queries are given the result
		/// will be those which match all of the requirements.The contents of the returned document
		/// are the changesets and their tags. To get the full set of changes associated with a
		/// changeset, use the download method on each changeset ID individually.
		/// </remarks>
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
				throw new Exception("Query must specify minClosedDate if maxOpenedDate is precified.");
			}

			var address = _getChangesetsAddress + "?";
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

			using (var client = new HttpClient())
			{
				var response = await client.GetAsync(address);
				if (!response.IsSuccessStatusCode)
				{
					var content = await response.Content.ReadAsStringAsync();
					throw new Exception($"Unable to get changesets: {response.StatusCode}-{response.ReasonPhrase} {content}");
				}

				var stream = await response.Content.ReadAsStreamAsync();
				var osm = FromContent(stream);
				return osm;
			}
		}

		/// <summary>
		/// Gets a changeset's changes.
		/// </summary>
		public async Task<OsmChange> GetChangesetDownload(long changesetId)
		{
			using (var client = new HttpClient())
			{
				var address = _getChangesetDownloadAddress.Replace(":id", changesetId.ToString());
				var response = await client.GetAsync(address);
				if (!response.IsSuccessStatusCode)
				{
					var content = await response.Content.ReadAsStringAsync();
					throw new Exception($"Unable to get changeset: {response.StatusCode}-{response.ReasonPhrase} {content}");
				}

				var stream = await response.Content.ReadAsStreamAsync();
				var serializer = new XmlSerializer(typeof(OsmChange));
				return serializer.Deserialize(stream) as OsmChange;
			}
		}

		protected Osm FromContent(Stream stream)
		{
			var serializer = new XmlSerializer(typeof(Osm));
			return serializer.Deserialize(stream) as Osm;
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

