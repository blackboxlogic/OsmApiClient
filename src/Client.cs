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
using System.Collections.Generic;

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

		/// <summary>
		/// Available API versions
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Available_API_versions:_GET_.2Fapi.2Fversions">
		/// GET /api/versions</see>.
		/// </summary>
		public async Task<double?> GetVersions()
		{
			var osm = await Get<Osm>(BaseAddress + "versions");
			return osm.Api.Version.Maximum;
		}

		/// <summary>
		/// API Capabilities
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Capabilities:_GET_.2Fapi.2Fcapabilities">
		/// GET /api/capabilities</see>.
		/// </summary>
		public async Task<Osm> GetCapabilities()
		{
			return await Get<Osm>(BaseAddress + "0.6/capabilities");
		}

		/// <summary>
		/// Retrieving map data by bounding box
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Retrieving_map_data_by_bounding_box:_GET_.2Fapi.2F0.6.2Fmap">
		/// GET /api/0.6/map</see>.
		/// </summary>
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

		/// <summary>
		/// Element Full
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Full:_GET_.2Fapi.2F0.6.2F.5Bway.7Crelation.5D.2F.23id.2Ffull">
		/// GET /api/0.6/[way|relation]/#id/full</see>.
		/// </summary>
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

		/// <summary>
		/// Element Read
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Read:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id">
		/// GET /api/0.6/[node|way|relation]/#id</see>.
		/// </summary>
		private async Task<TOsmGeo> GetElement<TOsmGeo>(long id) where TOsmGeo : OsmGeo, new()
		{
			var type = new TOsmGeo().Type.ToString().ToLower();
			var address = BaseAddress + $"0.6/{type}/{id}";
			var content = await Get(address);
			var streamSource = new XmlOsmStreamSource(await content.ReadAsStreamAsync());
			var element = streamSource.OfType<TOsmGeo>().FirstOrDefault();
			return element;
		}

		public async Task<Node[]> GetNodeHistory(long id)
		{
			return await GetElementHistory<Node>(id);
		}

		public async Task<Way[]> GetWayHistory(long id)
		{
			return await GetElementHistory<Way>(id);
		}

		public async Task<Relation[]> GetRelationHistory(long id)
		{
			return await GetElementHistory<Relation>(id);
		}

		/// <summary>
		/// Element History
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#History:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2Fhistory">
		/// GET /api/0.6/[node|way|relation]/#id/history</see>.
		/// </summary>
		private async Task<TOsmGeo[]> GetElementHistory<TOsmGeo>(long id) where TOsmGeo : OsmGeo, new()
		{
			var type = new TOsmGeo().Type.ToString().ToLower();
			var address = BaseAddress + $"0.6/{type}/{id}/history";
			var content = await Get(address);
			var streamSource = new XmlOsmStreamSource(await content.ReadAsStreamAsync());
			var elements = streamSource.OfType<TOsmGeo>().ToArray();
			return elements;
		}

		public async Task<Node> GetNodeVersion(long id, int version)
		{
			return await GetElementVersion<Node>(id, version);
		}

		public async Task<Way> GetWayVersion(long id, int version)
		{
			return await GetElementVersion<Way>(id, version);
		}

		public async Task<Relation> GetRelationVersion(long id, int version)
		{
			return await GetElementVersion<Relation>(id, version);
		}

		/// <summary>
		/// Element Version
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Version:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2F.23version">
		/// GET /api/0.6/[node|way|relation]/#id/#version</see>.
		/// </summary>
		private async Task<TOsmGeo> GetElementVersion<TOsmGeo>(long id, int version) where TOsmGeo : OsmGeo, new()
		{
			var type = new TOsmGeo().Type.ToString().ToLower();
			var address = BaseAddress + $"0.6/{type}/{id}/{version}";
			var content = await Get(address);
			var streamSource = new XmlOsmStreamSource(await content.ReadAsStreamAsync());
			var element = streamSource.OfType<TOsmGeo>().FirstOrDefault();
			return element;
		}

		public async Task<Node[]> GetNodes(long[] ids)
		{
			return await GetElements<Node>(ids);
		}

		public async Task<Way[]> GetWays(long[] ids)
		{
			return await GetElements<Way>(ids);
		}

		public async Task<Relation[]> GetRelations(long[] ids)
		{
			return await GetElements<Relation>(ids);
		}

		private async Task<TOsmGeo[]> GetElements<TOsmGeo>(long[] ids) where TOsmGeo : OsmGeo, new()
		{
			var idVersions = ids.Select(id => new KeyValuePair<long, int?>(id, null));
			return await GetElements<TOsmGeo>(idVersions);
		}

		public async Task<Node[]> GetNodes(IEnumerable<KeyValuePair<long, int?>> idVersions)
		{
			return await GetElements<Node>(idVersions);
		}

		public async Task<Way[]> GetWays(IEnumerable<KeyValuePair<long, int?>> idVersions)
		{
			return await GetElements<Way>(idVersions);
		}

		public async Task<Relation[]> GetRelations(IEnumerable<KeyValuePair<long, int?>> idVersions)
		{
			return await GetElements<Relation>(idVersions);
		}

		/// <summary>
		/// Elements Multifetch
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Multi_fetch:_GET_.2Fapi.2F0.6.2F.5Bnodes.7Cways.7Crelations.5D.3F.23parameters">
		/// GET /api/0.6/[nodes|ways|relations]?#parameters</see>.
		/// </summary>
		private async Task<TOsmGeo[]> GetElements<TOsmGeo>(IEnumerable<KeyValuePair<long, int?>> idVersions) where TOsmGeo : OsmGeo, new()
		{
			var type = new TOsmGeo().Type.ToString().ToLower();
			// For exmple: "12,13,14v1,15v1"
			var ids = string.Join(",", idVersions.Select(e => e.Value.HasValue ? $"{e.Key}v{e.Value}" : e.Key.ToString()));
			var address = BaseAddress + $"0.6/{type}s?{type}s={ids}";
			var content = await Get(address);
			var streamSource = new XmlOsmStreamSource(await content.ReadAsStreamAsync());
			var elements = streamSource.OfType<TOsmGeo>().ToArray();
			return elements;
		}

		public async Task<Relation[]> GetNodeRelations(long id)
		{
			return await GetElementRelations<Node>(id);
		}

		public async Task<Relation[]> GetWayRelations(long id)
		{
			return await GetElementRelations<Way>(id);
		}

		public async Task<Relation[]> GetRelationRelations(long id)
		{
			return await GetElementRelations<Relation>(id);
		}

		/// <summary>
		/// Element Relations
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Relations_for_element:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2Frelations">
		/// GET /api/0.6/[node|way|relation]/#id/relations</see>.
		/// </summary>
		private async Task<Relation[]> GetElementRelations<TOsmGeo>(long id) where TOsmGeo : OsmGeo, new()
		{
			var type = new TOsmGeo().Type.ToString().ToLower();
			var address = BaseAddress + $"0.6/{type}/{id}/relations";
			var content = await Get(address);
			var streamSource = new XmlOsmStreamSource(await content.ReadAsStreamAsync());
			var elements = streamSource.OfType<Relation>().ToArray();
			return elements;
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

