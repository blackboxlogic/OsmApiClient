using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OsmSharp.API;
using OsmSharp.Changesets;
using OsmSharp.Tags;
using OsmSharp.Complete;
using OsmSharp.IO.Xml;
using System.Web;
using Microsoft.Extensions.Logging;

namespace OsmSharp.IO.API
{
    public abstract class AuthClient : NonAuthClient, IAuthClient
    {
        /// <summary>
        /// Creates an instance of an AuthClient which can make
        /// authenticated (read and write) calls to the OSM API.
        /// </summary>
        /// <param name="baseAddress">The base address for the OSM API (for example: 'https://www.openstreetmap.org/api/0.6/')</param>
        /// <param name="httpClient">An HttpClient</param>
        /// <param name="logger">For logging out details of requests. Optional.</param>
        protected AuthClient(string baseAddress, HttpClient httpClient, ILogger logger = null)
            : base(baseAddress, httpClient, logger)
        { }

        #region Users
        /// <inheritdoc />
        public async Task<Permissions> GetPermissions()
        {
            var address = BaseAddress + "0.6/permissions";
            var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
            return osm.Permissions;
        }

        /// <inheritdoc />
        public async Task<User> GetUserDetails()
        {
            var address = BaseAddress + "0.6/user/details";
            var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
            return osm.User;
        }

        /// <inheritdoc />
        public async Task<Preference[]> GetUserPreferences()
        {
            var address = BaseAddress + "0.6/user/preferences";
            var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
            return osm.Preferences.UserPreferences;
        }

        /// <inheritdoc />
        public async Task SetUserPreferences(Preferences preferences)
        {
            var address = BaseAddress + "0.6/user/preferences";
            var osm = new Osm() { Preferences = preferences };
            var content = new StringContent(osm.SerializeToXml());
            await SendAuthRequest(HttpMethod.Put, address, content);
        }

        /// <inheritdoc />
        public async Task<string> GetUserPreference(string key)
        {
            var address = BaseAddress + $"0.6/user/preferences/{Encode(key)}";
            var content = await Get(address, c => AddAuthentication(c, address));
            var value = await content.ReadAsStringAsync();
            return value;
        }

        /// <inheritdoc />
        public async Task SetUserPreference(string key, string value)
        {
            var address = BaseAddress + $"0.6/user/preferences/{Encode(key)}";
            var content = new StringContent(value);
            await SendAuthRequest(HttpMethod.Put, address, content);
        }

        /// <inheritdoc />
        public async Task DeleteUserPreference(string key)
        {
            var address = BaseAddress + $"0.6/user/preferences/{Encode(key)}";
            await SendAuthRequest(HttpMethod.Delete, address, null);
        }
        #endregion

        #region Changesets and Element Changes
        /// <inheritdoc />
        public async Task<long> CreateChangeset(TagsCollectionBase tags)
        {
            Validate.ContainsTags(tags, "comment", "created_by");
            var address = BaseAddress + "0.6/changeset/create";
            var changeSet = new Osm { Changesets = new[] { new Changeset { Tags = tags } } };
            var content = new StringContent(changeSet.SerializeToXml());
            var resultContent = await SendAuthRequest(HttpMethod.Put, address, content);
            var id = await resultContent.ReadAsStringAsync();
            return long.Parse(id);
        }

        /// <inheritdoc />
        public async Task<Changeset> UpdateChangeset(long changesetId, TagsCollectionBase tags)
        {
            Validate.ContainsTags(tags, "comment", "created_by");
            // TODO: Validate change meets OsmSharp.API.Capabilities?
            var address = BaseAddress + $"0.6/changeset/{changesetId}";
            var changeSet = new Osm { Changesets = new[] { new Changeset { Tags = tags } } };
            var content = new StringContent(changeSet.SerializeToXml());
            var osm = await Put<Osm>(address, content);
            return osm.Changesets[0];
        }

        /// <inheritdoc />
        public async Task<DiffResult> UploadChangeset(long changesetId, OsmChange osmChange)
        {
            var elements = new OsmGeo[][] { osmChange.Create, osmChange.Modify, osmChange.Delete }
                .Where(c => c != null).SelectMany(c => c);

            foreach (var osmGeo in elements)
            {
                osmGeo.ChangeSetId = changesetId;
            }

            var address = BaseAddress + $"0.6/changeset/{changesetId}/upload";
            var request = new StringContent(osmChange.SerializeToXml());

            return await Post<DiffResult>(address, request);
        }

        /// <inheritdoc />
        public async Task<long> CreateElement(long changesetId, OsmGeo osmGeo)
        {
            var address = BaseAddress + $"0.6/{osmGeo.Type.ToString().ToLower()}/create";
            var osmRequest = GetOsmRequest(changesetId, osmGeo);
            var content = new StringContent(osmRequest.SerializeToXml());
            var response = await SendAuthRequest(HttpMethod.Put, address, content);
            var id = await response.ReadAsStringAsync();
            return long.Parse(id);
        }

        /// <inheritdoc />
        public async Task<long> UpdateElement(long changesetId, ICompleteOsmGeo osmGeo)
        {
            switch (osmGeo.Type)
            {
                case OsmGeoType.Node:
                    return await UpdateElement(changesetId, osmGeo as OsmGeo);
                case OsmGeoType.Way:
                    return await UpdateElement(changesetId, ((CompleteWay)osmGeo).ToSimple());
                case OsmGeoType.Relation:
                    return await UpdateElement(changesetId, ((CompleteRelation)osmGeo).ToSimple());
                default:
                    throw new Exception($"Invalid OSM geometry type: {osmGeo.Type}");
            }
        }

        /// <inheritdoc />
        public async Task<long> UpdateElement(long changesetId, OsmGeo osmGeo)
        {
            Validate.ElementHasAVersion(osmGeo);
            var address = BaseAddress + $"0.6/{osmGeo.Type.ToString().ToLower()}/{osmGeo.Id}";
            var osmRequest = GetOsmRequest(changesetId, osmGeo);
            var content = new StringContent(osmRequest.SerializeToXml());
            var responseContent = await SendAuthRequest(HttpMethod.Put, address, content);
            var newVersionNumber = await responseContent.ReadAsStringAsync();
            return long.Parse(newVersionNumber);
        }

        /// <inheritdoc />
        public async Task<long> DeleteElement(long changesetId, OsmGeo osmGeo)
        {
            Validate.ElementHasAVersion(osmGeo);
            var address = BaseAddress + $"0.6/{osmGeo.Type.ToString().ToLower()}/{osmGeo.Id}";
            var osmRequest = GetOsmRequest(changesetId, osmGeo);
            var content = new StringContent(osmRequest.SerializeToXml());
            var responseContent = await SendAuthRequest(HttpMethod.Delete, address, content);
            var newVersionNumber = await responseContent.ReadAsStringAsync();
            return long.Parse(newVersionNumber);
        }

        /// <inheritdoc />
        public async Task CloseChangeset(long changesetId)
        {
            var address = BaseAddress + $"0.6/changeset/{changesetId}/close";
            await SendAuthRequest(HttpMethod.Put, address, new StringContent(""));
        }

        /// <inheritdoc />
        public async Task<Changeset> AddChangesetComment(long changesetId, string text)
        {
            var address = BaseAddress + $"0.6/changeset/{changesetId}/comment";
            var content = new MultipartFormDataContent() { { new StringContent(text), "text" } };
            var osm = await Post<Osm>(address, content);
            return osm.Changesets[0];
        }

        /// <inheritdoc />
        public async Task ChangesetSubscribe(long changesetId)
        {
            var address = BaseAddress + $"0.6/changeset/{changesetId}/subscribe";
            await SendAuthRequest(HttpMethod.Post, address, new StringContent(""));
        }

        /// <inheritdoc />
        public async Task ChangesetUnsubscribe(long changesetId)
        {
            var address = BaseAddress + $"0.6/changeset/{changesetId}/unsubscribe";
            await SendAuthRequest(HttpMethod.Post, address, new StringContent(""));
        }
        #endregion

        #region Traces
        /// <inheritdoc />
        public async Task<GpxFile[]> GetTraces()
        {
            var address = BaseAddress + "0.6/user/gpx_files";
            var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
            return osm.GpxFiles ?? new GpxFile[0];
        }

        /// <inheritdoc />
        public async Task<long> CreateTrace(GpxFile gpx, Stream fileStream)
        {
            var address = BaseAddress + "0.6/gpx/create";
            var form = new MultipartFormDataContent();
            form.Add(new StringContent(gpx.Description), "\"description\"");
            form.Add(new StringContent(gpx.Visibility.ToString().ToLower()), "\"visibility\"");
            var tags = string.Join(",", gpx.Tags ?? new string[0]);
            form.Add(new StringContent(tags), "\"tags\"");
            var stream = new StreamContent(fileStream);
            var cleanName = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(gpx.Name));
            form.Add(stream, "file", cleanName);
            var content = await SendAuthRequest(HttpMethod.Post, address, form);
            var id = await content.ReadAsStringAsync();
            return long.Parse(id);
        }

        /// <inheritdoc />
        public async Task UpdateTrace(GpxFile trace)
        {
            var address = BaseAddress + $"0.6/gpx/{trace.Id}";
            var osm = new Osm { GpxFiles = new[] { trace } };
            var content = new StringContent(osm.SerializeToXml());
            await SendAuthRequest(HttpMethod.Put, address, content);
        }

        /// <inheritdoc />
        public async Task DeleteTrace(long traceId)
        {
            var address = BaseAddress + $"0.6/gpx/{traceId}";
            await SendAuthRequest(HttpMethod.Delete, address, null);
        }
        #endregion

        #region Notes
        /// <inheritdoc />
        public async Task<Note> CommentNote(long noteId, string text)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["text"] = text;
            var address = BaseAddress + $"0.6/notes/{noteId}/comment?{query}";
            // Can be with Auth or without.
            var osm = await Post<Osm>(address);
            return osm.Notes[0];
        }

        /// <inheritdoc />
        public async Task<Note> CloseNote(long noteId, string text)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["text"] = text;
            var address = BaseAddress + $"0.6/notes/{noteId}/close?{query}";
            var osm = await Post<Osm>(address);
            return osm.Notes[0];
        }

        /// <inheritdoc />
        public async Task<Note> ReOpenNote(long noteId, string text)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["text"] = text;
            var address = BaseAddress + $"0.6/notes/{noteId}/reopen?{query}";
            var osm = await Post<Osm>(address);
            return osm.Notes[0];
        }
        #endregion

        private Osm GetOsmRequest(long changesetId, OsmGeo osmGeo)
        {
            osmGeo.ChangeSetId = changesetId;
            var osm = new Osm();
            switch (osmGeo.Type)
            {
                case OsmGeoType.Node:
                    osm.Nodes = new[] { osmGeo as Node };
                    break;
                case OsmGeoType.Way:
                    osm.Ways = new[] { osmGeo as Way };
                    break;
                case OsmGeoType.Relation:
                    osm.Relations = new[] { osmGeo as Relation };
                    break;
            }
            return osm;
        }
    }
}

