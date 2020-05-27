using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OsmSharp.API;
using OsmSharp.Changesets;
using OsmSharp.Complete;
using OsmSharp.Db;

namespace OsmSharp.IO.API
{
    public interface INonAuthClient
    {
        Task<double?> GetVersions();
        Task<Osm> GetCapabilities();
        Task<Osm> GetMap(Bounds bounds);
        Task<User> GetUser(long id);
        Task<User[]> GetUsers(params long[] ids);
        Task<CompleteWay> GetCompleteWay(long id);
        Task<CompleteRelation> GetCompleteRelation(long id);
        Task<Node> GetNode(long id);
        Task<Way> GetWay(long id);
        Task<Relation> GetRelation(long id);
        Task<Node[]> GetNodeHistory(long id);
        Task<Way[]> GetWayHistory(long id);
        Task<Relation[]> GetRelationHistory(long id);
        Task<Node> GetNodeVersion(long id, long version);
        Task<Way> GetWayVersion(long id, long version);
        Task<Relation> GetRelationVersion(long id, long version);
        Task<Node[]> GetNodes(params long[] ids);
        Task<Way[]> GetWays(params long[] ids);
        Task<Relation[]> GetRelations(params long[] ids);
        Task<OsmGeo[]> GetElements(params OsmGeoKey[] elementKeys);
        Task<OsmGeo[]> GetElements(Dictionary<OsmGeoKey, long?> elementKeyVersions);
        Task<Node[]> GetNodes(IEnumerable<KeyValuePair<long, long?>> idVersions);
        Task<Way[]> GetWays(IEnumerable<KeyValuePair<long, long?>> idVersions);
        Task<Relation[]> GetRelations(IEnumerable<KeyValuePair<long, long?>> idVersions);
        Task<Relation[]> GetNodeRelations(long id);
        Task<Relation[]> GetWayRelations(long id);
        Task<Relation[]> GetRelationRelations(long id);
        Task<Way[]> GetNodeWays(long id);
        Task<Changeset> GetChangeset(long changesetId, bool includeDiscussion = false);
        Task<Changeset[]> QueryChangesets(Bounds bounds, long? userId, string userName, DateTime? minClosedDate, DateTime? maxOpenedDate, bool openOnly, bool closedOnly, long[] ids);
        Task<OsmChange> GetChangesetDownload(long changesetId);
        Task<Stream> GetTrackPoints(Bounds bounds, int pageNumber = 0);
        Task<GpxFile> GetTraceDetails(long id);
        Task<TypedStream> GetTraceData(long id);
        Task<Note> GetNote(long id);
        Task<Note[]> GetNotes(Bounds bounds, int limit = 100, int maxClosedDays = 7);
        Task<Stream> GetNotesRssFeed(Bounds bounds);
        Task<Note[]> QueryNotes(string searchText, long? userId, string userName, int? limit, int? maxClosedDays, DateTime? fromDate, DateTime? toDate);
        Task<Note> CreateNote(double latitude, double longitude, string text);
    }
}