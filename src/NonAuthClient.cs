using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OsmSharp.API;
using OsmSharp.Streams;
using OsmSharp.Streams.Complete;
using OsmSharp.Complete;
using System.Xml.Serialization;
using OsmSharp.Changesets;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Web;
using Microsoft.Extensions.Logging;
using OsmSharp.Db;
using System.Globalization;

namespace OsmSharp.IO.API
{
    public class NonAuthClient : INonAuthClient
    {
        /// <summary>
        /// The OSM base address
        /// </summary>
        /// <example>
        /// "https://master.apis.dev.openstreetmap.org/api/"
        /// "https://www.openstreetmap.org/api/"
        /// </example>
        protected readonly string BaseAddress;

        // Prevent scientific notation in a url.
        protected string OsmMaxPrecision = "0.########";

        private readonly HttpClient _httpClient;
        protected readonly ILogger _logger;

        /// <summary>
        /// Creates an instance of a NonAuthClient which can make
        /// unauthenticated (generally read-only) calls to the OSM API.
        /// </summary>
        /// <param name="baseAddress">The base address for the OSM API (for example: 'https://www.openstreetmap.org/api/0.6/')</param>
        /// <param name="httpClient">An HttpClient</param>
        /// <param name="logger">For logging out details of requests. Optional.</param>
        public NonAuthClient(string baseAddress,
            HttpClient httpClient,
            ILogger logger = null)
        {
            BaseAddress = baseAddress;
            _httpClient = httpClient;
            _logger = logger;
        }

        #region Miscellaneous
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

        /// <summary>
        /// Details of a User
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Details_of_a_user">
        /// GET /api/0.6/user/#id</see>.
        /// </summary>
        public async Task<User> GetUser(long id)
        {
            var address = BaseAddress + $"0.6/user/{id}";
            var osm = await Get<Osm>(address);
            return osm.User;
        }

        /// <summary>
        /// Details of multiple Users
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Details_of_multiple_users">
        /// GET /api/0.6/users?users=#id1,#id2,...,#idn</see>.
        /// </summary>
        public async Task<User[]> GetUsers(params long[] ids)
        {
            var address = BaseAddress + $"0.6/users?users={string.Join(",", ids)}";
            var osm = await Get<Osm>(address);
            return osm.Users;
        }
        #endregion

        #region Elements
        /// <summary>
        /// Gets a Way, including the details of each Node in it
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Full:_GET_.2Fapi.2F0.6.2F.5Bway.7Crelation.5D.2F.23id.2Ffull">
        /// GET /api/0.6/way/#id/full</see>.
        /// </summary>
        public Task<CompleteWay> GetCompleteWay(long id)
        {
            return GetCompleteElement<CompleteWay>(id);
        }

        /// <summary>
        /// Gats a Relation, including the details of each Element in it
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Full:_GET_.2Fapi.2F0.6.2F.5Bway.7Crelation.5D.2F.23id.2Ffull">
        /// GET /api/0.6/relation/#id/full</see>.
        /// </summary>
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

        /// <summary>
        /// Gets a Node and its details
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Read:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id">
        /// GET /api/0.6/node/#id</see>.
        /// </summary>
        public async Task<Node> GetNode(long id)
        {
            return await GetElement<Node>(id);
        }

        /// <summary>
        /// Gets a Way and its details (but not the details of its Nodes)
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Read:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id">
        /// GET /api/0.6/way/#id</see>.
        /// </summary>
        public async Task<Way> GetWay(long id)
        {
            return await GetElement<Way>(id);
        }

        /// <summary>
        /// Gets a Relation and its details (but not the details of its elements)
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Read:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id">
        /// GET /api/0.6/relation/#id</see>.
        /// </summary>
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
            var elements = await GetOfType<TOsmGeo>(address);
            return elements.FirstOrDefault();
        }

        /// <summary>
        /// Gets a Node's history
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#History:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2Fhistory">
        /// GET /api/0.6/node/#id/history</see>.
        /// </summary>
        public async Task<Node[]> GetNodeHistory(long id)
        {
            return await GetElementHistory<Node>(id);
        }

        /// <summary>
        /// Gets a Way's history
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#History:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2Fhistory">
        /// GET /api/0.6/way/#id/history</see>.
        /// </summary>
        public async Task<Way[]> GetWayHistory(long id)
        {
            return await GetElementHistory<Way>(id);
        }

        /// <summary>
        /// Gets a Relation's history
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#History:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2Fhistory">
        /// GET /api/0.6/relation/#id/history</see>.
        /// </summary>
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
            var elements = await GetOfType<TOsmGeo>(address);
            return elements.ToArray();
        }

        /// <summary>
        /// Gets a Node's version
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Version:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2F.23version">
        /// GET /api/0.6/node/#id/#version</see>.
        /// </summary>
        public async Task<Node> GetNodeVersion(long id, long version)
        {
            return await GetElementVersion<Node>(id, version);
        }

        /// <summary>
        /// Gets a Way's version
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Version:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2F.23version">
        /// GET /api/0.6/way/#id/#version</see>.
        /// </summary>
        public async Task<Way> GetWayVersion(long id, long version)
        {
            return await GetElementVersion<Way>(id, version);
        }

        /// <summary>
        /// Gets a Relation's version
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Version:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2F.23version">
        /// GET /api/0.6/relation/#id/#version</see>.
        /// </summary>
        public async Task<Relation> GetRelationVersion(long id, long version)
        {
            return await GetElementVersion<Relation>(id, version);
        }

        /// <summary>
        /// Element Version
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Version:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2F.23version">
        /// GET /api/0.6/[node|way|relation]/#id/#version</see>.
        /// </summary>
        private async Task<TOsmGeo> GetElementVersion<TOsmGeo>(long id, long version) where TOsmGeo : OsmGeo, new()
        {
            var type = new TOsmGeo().Type.ToString().ToLower();
            var address = BaseAddress + $"0.6/{type}/{id}/{version}";
            var elements = await GetOfType<TOsmGeo>(address);
            return elements.FirstOrDefault();
        }

        /// <summary>
        /// Gets many Nodes
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Multi_fetch:_GET_.2Fapi.2F0.6.2F.5Bnodes.7Cways.7Crelations.5D.3F.23parameters">
        /// GET /api/0.6/nodes?#parameters</see>.
        /// </summary>
        public async Task<Node[]> GetNodes(params long[] ids)
        {
            return await GetElements<Node>(ids);
        }

        /// <summary>
        /// Gets many Ways
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Multi_fetch:_GET_.2Fapi.2F0.6.2F.5Bnodes.7Cways.7Crelations.5D.3F.23parameters">
        /// GET /api/0.6/ways?#parameters</see>.
        /// </summary>
        public async Task<Way[]> GetWays(params long[] ids)
        {
            return await GetElements<Way>(ids);
        }

        /// <summary>
        /// Gets many Relations
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Multi_fetch:_GET_.2Fapi.2F0.6.2F.5Bnodes.7Cways.7Crelations.5D.3F.23parameters">
        /// GET /api/0.6/relations?#parameters</see>.
        /// </summary>
        public async Task<Relation[]> GetRelations(params long[] ids)
        {
            return await GetElements<Relation>(ids);
        }

        private async Task<TOsmGeo[]> GetElements<TOsmGeo>(params long[] ids) where TOsmGeo : OsmGeo, new()
        {
            var idVersions = ids.Select(id => new KeyValuePair<long, long?>(id, null));
            return await GetElements<TOsmGeo>(idVersions);
        }

        /// <summary>
        /// Gets many Nodes at specific versions
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Multi_fetch:_GET_.2Fapi.2F0.6.2F.5Bnodes.7Cways.7Crelations.5D.3F.23parameters">
        /// GET /api/0.6/nodes?#parameters</see>.
        /// </summary>
        public async Task<Node[]> GetNodes(IEnumerable<KeyValuePair<long, long?>> idVersions)
        {
            return await GetElements<Node>(idVersions);
        }

        /// <summary>
        /// Gets many Ways at specific versions
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Multi_fetch:_GET_.2Fapi.2F0.6.2F.5Bnodes.7Cways.7Crelations.5D.3F.23parameters">
        /// GET /api/0.6/ways?#parameters</see>.
        /// </summary>
        public async Task<Way[]> GetWays(IEnumerable<KeyValuePair<long, long?>> idVersions)
        {
            return await GetElements<Way>(idVersions);
        }

        /// <summary>
        /// Gets many Relations at specific versions
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Multi_fetch:_GET_.2Fapi.2F0.6.2F.5Bnodes.7Cways.7Crelations.5D.3F.23parameters">
        /// GET /api/0.6/relations?#parameters</see>.
        /// </summary>
        public async Task<Relation[]> GetRelations(IEnumerable<KeyValuePair<long, long?>> idVersions)
        {
            return await GetElements<Relation>(idVersions);
        }

        public async Task<OsmGeo[]> GetElements(params OsmGeoKey[] elementKeys)
        {
            return await GetElements(elementKeys.ToDictionary(ek => ek, ek => (long?)null));
        }

        public async Task<OsmGeo[]> GetElements(Dictionary<OsmGeoKey, long?> elementKeyVersions)
        {
            var elements = new List<OsmGeo>();

            foreach (var typeGroup in elementKeyVersions.GroupBy(kvp => kvp.Key.Type))
            {
                var chunkAsDictionary = typeGroup.ToDictionary(kvp => kvp.Key.Id, kvp => kvp.Value);
                if(typeGroup.Key == OsmGeoType.Node)
                    elements.AddRange(await GetElements<Node>(chunkAsDictionary));
                else if (typeGroup.Key == OsmGeoType.Way)
                    elements.AddRange(await GetElements<Way>(chunkAsDictionary));
                else if (typeGroup.Key == OsmGeoType.Relation)
                    elements.AddRange(await GetElements<Relation>(chunkAsDictionary));
            }

            return elements.ToArray();
        }

        /// <summary>
        /// Elements Multifetch
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Multi_fetch:_GET_.2Fapi.2F0.6.2F.5Bnodes.7Cways.7Crelations.5D.3F.23parameters">
        /// GET /api/0.6/[nodes|ways|relations]?#parameters</see>.
        /// </summary>
        private async Task<TOsmGeo[]> GetElements<TOsmGeo>(IEnumerable<KeyValuePair<long, long?>> idVersions) where TOsmGeo : OsmGeo, new()
        {
            var tasks = new List<Task<IEnumerable<TOsmGeo>>>();

            foreach (var chunk in Chunks(idVersions, 400)) // to avoid http error code 414, UIR too long.
            {
                var type = new TOsmGeo().Type.ToString().ToLower();
                // For exmple: "12,13,14v1,15v1"
                var parameters = string.Join(",", chunk.Select(e => e.Value.HasValue ? $"{e.Key}v{e.Value}" : e.Key.ToString()));
                var address = BaseAddress + $"0.6/{type}s?{type}s={parameters}";
                tasks.Add(GetOfType<TOsmGeo>(address));
            }

            await Task.WhenAll(tasks);

            return tasks.SelectMany(t => t.Result).ToArray();
        }

        private IEnumerable<T[]> Chunks<T>(IEnumerable<T> elements, int chunkSize)
        {
            return elements.Select((e, i) => new { e, i })
                .GroupBy(ei => ei.i / chunkSize) // Intentional integer division
                .Select(g => g.Select(ei => ei.e).ToArray());
        }

        /// <summary>
        /// Gets the Relations containing a specific Node
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Relations_for_element:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2Frelations">
        /// GET /api/0.6/node/#id/relations</see>.
        /// </summary>
        public async Task<Relation[]> GetNodeRelations(long id)
        {
            return await GetElementRelations<Node>(id);
        }

        /// <summary>
        /// Gets the Relations containing a specific Way
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Relations_for_element:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2Frelations">
        /// GET /api/0.6/way/#id/relations</see>.
        /// </summary>
        public async Task<Relation[]> GetWayRelations(long id)
        {
            return await GetElementRelations<Way>(id);
        }

        /// <summary>
        /// Gets the Relations containing a specific Relation
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Relations_for_element:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2Frelations">
        /// GET /api/0.6/relation/#id/relations</see>.
        /// </summary>
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
            var elements = await GetOfType<Relation>(address);
            return elements.ToArray();
        }

        /// <summary>
        /// Node Ways
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Ways_for_node:_GET_.2Fapi.2F0.6.2Fnode.2F.23id.2Fways">
        /// GET /api/0.6/node/#id/ways</see>.
        /// </summary>
        public async Task<Way[]> GetNodeWays(long id)
        {
            var address = BaseAddress + $"0.6/node/{id}/ways";
            var elements = await GetOfType<Way>(address);
            return elements.ToArray();
        }
        #endregion

        #region Changesets
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
        public async Task<Changeset[]> QueryChangesets(Bounds bounds, long? userId, string userName,
            DateTime? minClosedDate, DateTime? maxOpenedDate, bool openOnly, bool closedOnly,
            long[] ids)
        {
            if (userId.HasValue && userName != null)
                throw new ArgumentException("Query can only specify userID OR userName, not both.");
            if (openOnly && closedOnly)
                throw new ArgumentException("Query can only specify openOnly OR closedOnly, not both.");
            if (!minClosedDate.HasValue && maxOpenedDate.HasValue)
                throw new ArgumentException("Query must specify minClosedDate if maxOpenedDate is specified.");

            var query = HttpUtility.ParseQueryString(string.Empty);
            if (bounds != null) query["bbox"] = ToString(bounds);
            if (userId.HasValue) query["user"] = userId.ToString();
            if (userName != null) query["display_name"] = userName;
            if (minClosedDate.HasValue) query["time"] = FormatNoteDate(minClosedDate.Value);
            if (maxOpenedDate.HasValue) query["time"] += "," + FormatNoteDate(maxOpenedDate.Value);
            if (openOnly) query["open"] = "true";
            if (closedOnly) query["closed"] = "true";
            if (ids != null) query["changesets"] = string.Join(",", ids);

            var address = BaseAddress + "0.6/changesets?" + query.ToString();
            var osm = await Get<Osm>(address);
            return osm.Changesets;
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
        #endregion

        #region Traces
        /// <summary>
        /// Get GPS Points
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Get_GPS_Points:_Get_.2Fapi.2F0.6.2Ftrackpoints.3Fbbox.3Dleft.2Cbottom.2Cright.2Ctop.26page.3DpageNumber">
        /// Get /api/0.6/trackpoints?bbox=left,bottom,right,top&page=pageNumber</see>.
        /// Retrieve the GPS track points that are inside a given bounding box (formatted in a GPX format).
        /// Warning: GPX version 1.0 is not the current version. Your tools might not support it.
        /// </summary>
        /// <returns>A stream of a GPX (version 1.0) file.</returns>
        public virtual async Task<Stream> GetTrackPoints(Bounds bounds, int pageNumber = 0)
        {
            var address = BaseAddress + $"0.6/trackpoints?bbox={ToString(bounds)}&page={pageNumber}";
            var content = await Get(address);
            var stream = await content.ReadAsStreamAsync();
            return stream;
        }

        /// <summary>
        /// Download Metadata
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Download_Metadata:_GET_.2Fapi.2F0.6.2Fgpx.2F.23id.2Fdetails">
        /// GET /api/0.6/gpx/#id/details</see>.
        /// </summary>
        public async Task<GpxFile> GetTraceDetails(long id)
        {
            var address = BaseAddress + $"0.6/gpx/{id}/details";
            var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
            return osm.GpxFiles[0];
        }

        /// <summary>
        /// Download Data
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Download_Data:_GET_.2Fapi.2F0.6.2Fgpx.2F.23id.2Fdata">
        /// GET /api/0.6/gpx/#id/data</see>.
        /// This will return exactly what was uploaded, which might not be a gpx file (it could be a zip etc.)
        /// </summary>
        /// <returns>A stream of a GPX (version 1.0) file.</returns>
        public async Task<TypedStream> GetTraceData(long id)
        {
            var address = BaseAddress + $"0.6/gpx/{id}/data";
            var content = await Get(address, c => AddAuthentication(c, address));
            return await TypedStream.Create(content);
        }
        #endregion

        #region Notes
        /// <summary>
        /// Gets a Note
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Read:_GET_.2Fapi.2F0.6.2Fnotes.2F.23id">
        /// GET /api/0.6/notes/#id</see>.
        /// </summary>
        public async Task<Note> GetNote(long id)
        {
            var address = BaseAddress + $"0.6/notes/{id}";
            var osm = await Get<Osm>(address);
            return osm.Notes?.FirstOrDefault();
        }

        /// <summary>
        /// Gets many a Notes in a box and with the spcified time since closed
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Retrieving_notes_data_by_bounding_box:_GET_.2Fapi.2F0.6.2Fnotes">
        /// GET /api/0.6/notes?[parameters]</see>.
        /// </summary>
        /// <param name="limit">Must be between 1 and 10,000.</param>
        /// <param name="maxClosedDays">0 means only open notes. -1 mean all (open and closed) notes.</param>
        public async Task<Note[]> GetNotes(Bounds bounds, int limit = 100, int maxClosedDays = 7)
        {
            string format = ".xml";
            var address = BaseAddress + $"0.6/notes{format}?bbox={ToString(bounds)}&limit={limit}&closed={maxClosedDays}";
            var osm = await Get<Osm>(address);
            return osm.Notes;
        }

        /// <summary>
        /// Gets an RSS feed of Notes in an area
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#RSS_Feed:_GET_.2Fapi.2F0.6.2Fnotes.2Ffeed">
        /// GET /api/0.6/notes/feed</see>.
        /// </summary>
        public async Task<Stream> GetNotesRssFeed(Bounds bounds)
        {
            var address = BaseAddress + $"0.6/notes/feed?bbox={ToString(bounds)}";
            var content = await Get(address);
            var stream = await content.ReadAsStreamAsync();
            return stream;
        }

        /// <summary>
        /// Search for Notes
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Search_for_notes:_GET_.2Fapi.2F0.6.2Fnotes.2Fsearch">
        /// GET /api/0.6/notes/search</see>.
        /// </summary>
        /// <param name="searchText">Specifies the search query. This is the only required field.</param>
        /// <param name="userId">Specifies the creator of the returned notes by the id of the user. Does not work together with the display_name parameter</param>
        /// <param name="userName">Specifies the creator of the returned notes by the display name. Does not work together with the user parameter</param>
        /// <param name="limit">Must be between 1 and 10,000. 100 is default if null.</param>
        /// <param name="maxClosedDays">0 means only open notes. -1 mean all (open and closed) notes. 7 is default if null.</param>
        /// <param name="fromDate">Specifies the beginning of a date range to search in for a note</param>
        /// <param name="toDate">Specifies the end of a date range to search in for a note</param>
        public async Task<Note[]> QueryNotes(string searchText, long? userId, string userName,
            int? limit, int? maxClosedDays, DateTime? fromDate, DateTime? toDate)
        {
            if (userId.HasValue && userName != null)
                throw new ArgumentException("Query can only specify userID OR userName, not both.");
            if (fromDate > toDate)
                throw new ArgumentException("Query [fromDate] must be before [toDate] if both are provided.");
            if (searchText == null)
                throw new ArgumentException("Query searchText is required.");

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["q"] = searchText;
            if (limit != null) query["limit"] = limit.ToString();
            if (maxClosedDays != null) query["closed"] = maxClosedDays.ToString();
            if (userName != null) query["display_name"] = userName;
            if (userId != null) query["user"] = userId.ToString();
            if (fromDate != null) query["from"] = FormatNoteDate(fromDate.Value);
            if (toDate != null) query["to"] = FormatNoteDate(toDate.Value);

            string format = ".xml";
            var address = BaseAddress + $"0.6/notes/search{format}?{query}";
            var osm = await Get<Osm>(address);
            return osm.Notes;
        }

        private static string FormatNoteDate(DateTime date)
        {
            // DateTimes in notes are 'different'.
            return date.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
        }

        /// <summary>
        /// Creates a new Note
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Create_a_new_note:_Create:_POST_.2Fapi.2F0.6.2Fnotes">
        /// POST /api/0.6/notes</see>.
        /// </summary>
        public async Task<Note> CreateNote(double latitude, double longitude, string text)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["text"] = text;
            query["lat"] = ToString(latitude);
            query["lon"] = ToString(longitude);

            var address = BaseAddress + $"0.6/notes?{query}";
            // Can be with Auth or without.
            var osm = await Post<Osm>(address);
            return osm.Notes[0];
        }
        #endregion

        protected async Task<IEnumerable<T>> GetOfType<T>(string address, Action<HttpRequestMessage> auth = null) where T : class
        {
            var content = await Get(address, auth);
            var streamSource = new XmlOsmStreamSource(await content.ReadAsStreamAsync());
            var elements = streamSource.OfType<T>();
            return elements;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string ToString(Bounds bounds)
        {
            StringBuilder x = new StringBuilder();
            x.Append(bounds.MinLongitude.Value.ToString(OsmMaxPrecision, CultureInfo.InvariantCulture));
            x.Append(',');
            x.Append(bounds.MinLatitude.Value.ToString(OsmMaxPrecision, CultureInfo.InvariantCulture));
            x.Append(',');
            x.Append(bounds.MaxLongitude.Value.ToString(OsmMaxPrecision, CultureInfo.InvariantCulture));
            x.Append(',');
            x.Append(bounds.MaxLatitude.Value.ToString(OsmMaxPrecision, CultureInfo.InvariantCulture));

            return x.ToString();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string ToString(float number)
        {
            return number.ToString(OsmMaxPrecision, CultureInfo.InvariantCulture);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string ToString(double number)
        {
            return number.ToString(OsmMaxPrecision, CultureInfo.InvariantCulture);
        }

        #region Http
        protected static readonly Func<string, string> Encode = HttpUtility.UrlEncode;

        protected async Task<T> Get<T>(string address, Action<HttpRequestMessage> auth = null) where T : class
        {
            var content = await Get(address, auth);
            var stream = await content.ReadAsStreamAsync();
            var serializer = new XmlSerializer(typeof(T));
            var element = serializer.Deserialize(stream) as T;
            return element;
        }

        protected async Task<HttpContent> Get(string address, Action<HttpRequestMessage> auth = null)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, address))
            {
                auth?.Invoke(request);
                var response = await _httpClient.SendAsync(request);
                await VerifyAndLogReponse(response, $"{GetType().Name} GET: {address}");
                return response.Content;
            }
        }

        protected async Task<T> Post<T>(string address, HttpContent requestContent = null) where T : class
        {
            var responseContent = await SendAuthRequest(HttpMethod.Post, address, requestContent);
            var stream = await responseContent.ReadAsStreamAsync();
            var serializer = new XmlSerializer(typeof(T));
            var content = serializer.Deserialize(stream) as T;
            return content;
        }

        protected async Task<T> Put<T>(string address, HttpContent requestContent = null) where T : class
        {
            var content = await SendAuthRequest(HttpMethod.Put, address, requestContent);
            var stream = await content.ReadAsStreamAsync();
            var serializer = new XmlSerializer(typeof(T));
            var element = serializer.Deserialize(stream) as T;
            return element;
        }

        /// <summary>
        /// For GetTraceDetails() and GetTraceData(), which may be authenticated or not.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="url"></param>
        /// <param name="method"></param>
        protected virtual void AddAuthentication(HttpRequestMessage message, string url, string method = "GET") { }

        protected async Task<HttpContent> SendAuthRequest(HttpMethod method, string address, HttpContent requestContent)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(method, address))
            {
                AddAuthentication(request, address, method.ToString());
                request.Content = requestContent;
                var response = await _httpClient.SendAsync(request);
                await VerifyAndLogReponse(response, $"{GetType().Name} {method}: {address}");
                return response.Content;
            }
        }

        protected async Task VerifyAndLogReponse(HttpResponseMessage response, string logMessage)
        {
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                _logger?.LogError($"{logMessage}: failed: {response.StatusCode}-{response.ReasonPhrase} {message}");
                throw new OsmApiException(response.RequestMessage?.RequestUri, message, response.StatusCode);
            }
            else
            {
                _logger?.LogInformation($"{logMessage}: succeeded");
            }
        }
        #endregion
    }
}

