using System.IO;
using System.Threading.Tasks;
using OsmSharp.API;
using OsmSharp.Changesets;
using OsmSharp.Complete;
using OsmSharp.Tags;

namespace OsmSharp.IO.API
{
    /// <summary>
    /// Authenticationed OSM API Client
    /// </summary>
    public interface IAuthClient
    {
        /// <summary>
        /// Create a Changeset, it is better to use <see cref="UploadChangeset(long, OsmChange)"/>
        /// </summary>
        /// <param name="tags">Must at least contain 'comment' and 'created_by'.</param>
        /// <returns></returns>
        Task<long> CreateChangeset(TagsCollectionBase tags);
        /// <summary>
        /// Updates a changeset
        /// </summary>
        /// <param name="changesetId"></param>
        /// <param name="tags">Must at least contain 'comment' and 'created_by'.</param>
        /// <returns></returns>
        Task<Changeset> UpdateChangeset(long changesetId, TagsCollectionBase tags);
        /// <summary>
        /// Add a comment to a change set
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Comment:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Fcomment">
        /// POST /api/0.6/changeset/#id/comment </see>
        /// </summary>
        /// <param name="changesetId">The changeset ID</param>
        /// <param name="text">The comment</param>
        /// <returns></returns>
        Task<Changeset> AddChangesetComment(long changesetId, string text);
        /// <summary>
        /// Subscribe to changeset
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Subscribe:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Fsubscribe">
        /// POST /api/0.6/changeset/#id/subscribe </see>
        /// </summary>
        /// <param name="changesetId">The changeset ID</param>
        /// <returns></returns>
        Task ChangesetSubscribe(long changesetId);
        /// <summary>
        /// Unsubscribe to a changeset
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Subscribe:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Funsubscribe">
        /// POST /api/0.6/changeset/#id/unsubscribe </see>
        /// </summary>
        /// <param name="changesetId"></param>
        /// <returns></returns>
        Task ChangesetUnsubscribe(long changesetId);
        /// <summary>
        /// Close a changeset - this closes the transaction and changes all the relevant elements.
        /// It is better to use <see cref="UploadChangeset(long, OsmChange)"/>
        /// </summary>
        /// <param name="changesetId"></param>
        /// <returns></returns>
        Task CloseChangeset(long changesetId);
        /// <summary>
        /// Upload changeset using a <see cref="OsmChange"/> object
        /// This is the preferred method according to OSM
        /// </summary>
        /// <param name="changesetId"></param>
        /// <param name="osmChange"></param>
        /// <returns></returns>
        /// <remarks>This automatically adds the ChangeSetId tag to each element.</remarks>
        Task<DiffResult> UploadChangeset(long changesetId, OsmChange osmChange);
        /// <summary>
        /// Comment on a note
        /// </summary>
        /// <param name="noteId"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        Task<Note> CommentNote(long noteId, string text);
        /// <summary>
        /// Close a note
        /// </summary>
        /// <param name="noteId"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        Task<Note> CloseNote(long noteId, string text);
        /// <summary>
        /// Reopens a note
        /// </summary>
        /// <param name="noteId"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        Task<Note> ReOpenNote(long noteId, string text);
        /// <summary>
        /// Creates an OSM element as part of the given changeset
        /// </summary>
        /// <param name="changesetId"></param>
        /// <param name="osmGeo"></param>
        /// <returns></returns>
        Task<long> CreateElement(long changesetId, OsmGeo osmGeo);
        /// <summary>
        /// Updates an element using a <see cref="=ICompleteOsmGeo"/> object
        /// </summary>
        /// <param name="changesetId"></param>
        /// <param name="osmGeo"></param>
        /// <returns></returns>
        Task UpdateElement(long changesetId, ICompleteOsmGeo osmGeo);
        /// <summary>
        /// Updates an element using a <see cref="OsmGeo"/> object
        /// </summary>
        /// <param name="changesetId"></param>
        /// <param name="osmGeo"></param>
        /// <returns></returns>
        Task<int> UpdateElement(long changesetId, OsmGeo osmGeo);
        /// <summary>
        /// Delete an OSM element as part of the given changeset
        /// </summary>
        /// <param name="changesetId"></param>
        /// <param name="osmGeo"></param>
        /// <returns></returns>
        Task<int> DeleteElement(long changesetId, OsmGeo osmGeo);
        /// <summary>
        /// Get all user traces
        /// </summary>
        /// <returns></returns>
        Task<GpxFile[]> GetTraces();
        /// <summary>
        /// Adds a trace using the file
        /// </summary>
        /// <param name="gpx"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        Task<int> CreateTrace(GpxFile gpx, Stream fileStream);
        /// <summary>
        /// Updates a trace
        /// </summary>
        /// <param name="trace"></param>
        /// <returns></returns>
        Task UpdateTrace(GpxFile trace);
        /// <summary>
        /// Deletes a trace
        /// </summary>
        /// <param name="traceId"></param>
        /// <returns></returns>
        Task DeleteTrace(long traceId);
        /// <summary>
        /// Get permissions
        /// </summary>
        /// <returns></returns>
        Task<Permissions> GetPermissions();
        /// <summary>
        /// Get user details
        /// </summary>
        /// <returns></returns>
        Task<User> GetUserDetails();
        /// <summary>
        /// Get user's preference
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<string> GetUserPreference(string key);
        /// <summary>
        /// Deletes user preference
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task DeleteUserPreference(string key);
        /// <summary>
        /// Get user's preferences
        /// </summary>
        /// <returns></returns>
        Task<Preference[]> GetUserPreferences();
        /// <summary>
        /// Sets a user preference
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task SetUserPreference(string key, string value);
        /// <summary>
        /// Set a user's preferences
        /// </summary>
        /// <param name="preferences"></param>
        /// <returns></returns>
        Task SetUserPreferences(Preferences preferences);
    }
}