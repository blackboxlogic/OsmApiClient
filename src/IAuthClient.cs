using System.IO;
using System.Threading.Tasks;
using OsmSharp.API;
using OsmSharp.Changesets;
using OsmSharp.Complete;
using OsmSharp.Tags;

namespace OsmSharp.IO.API
{
    /// <summary>
    /// Authenticated OSM API Client
    /// </summary>
    public interface IAuthClient : INonAuthClient
    {
        /// <summary>
        /// Create a Changeset, it is better to use <see cref="UploadChangeset(long, OsmChange)"/>
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Create:_PUT_.2Fapi.2F0.6.2Fchangeset.2Fcreate">
        /// PUT /api/0.6/changeset/create</see>
        /// </summary>
        /// <param name="tags">Must at least contain 'comment' and 'created_by'.</param>
        /// <returns>The ID of the new Changeset</returns>
        Task<long> CreateChangeset(TagsCollectionBase tags);
        /// <summary>
        /// Updates a specific Changeset's metadata
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Update:_PUT_.2Fapi.2F0.6.2Fchangeset.2F.23id">
        /// PUT /api/0.6/changeset/#id</see>
        /// </summary>
        /// <param name="tags">Must at least contain 'comment' and 'created_by'.</param>
        /// <param name="changesetId">The ID of an OPEN Changeset.</param>
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
        /// Subscribes the current User to a Changeset
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Subscribe:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Fsubscribe">
        /// POST /api/0.6/changeset/#id/subscribe </see>
        /// </summary>
        /// <param name="changesetId">The changeset ID</param>
        /// <returns></returns>
        Task ChangesetSubscribe(long changesetId);
        /// <summary>
        /// Unsubscribe the current User from a Changeset
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Subscribe:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Funsubscribe">
        /// POST /api/0.6/changeset/#id/unsubscribe </see>
        /// </summary>
        /// <param name="changesetId"></param>
        /// <returns></returns>
        Task ChangesetUnsubscribe(long changesetId);
        /// <summary>
        /// Close a changeset - this closes the transaction and changes all the relevant elements.
        /// It is better to use <see cref="UploadChangeset(long, OsmChange)"/>
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Close:_PUT_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Fclose">
        /// PUT /api/0.6/changeset/#id/close</see>
        /// </summary>
        /// <param name="changesetId">The ID of an OPEN Changeset.</param>
        /// <returns></returns>
        Task CloseChangeset(long changesetId);
        /// <summary>
        /// Upload changeset using a <see cref="OsmChange"/> object
        /// This is the preferred method according to OSM
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Diff_upload:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Fupload">
        /// POST /api/0.6/changeset/#id/upload</see>
        /// </summary>
        /// <param name="changesetId">The ID of an OPEN Changeset.</param>
        /// <remarks>This automatically adds the ChangeSetId tag to each element</remarks>
        Task<DiffResult> UploadChangeset(long changesetId, OsmChange osmChange);
        /// <summary>
        /// Creates a new Comment on a Note
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Create_a_new_comment:_Create:_POST_.2Fapi.2F0.6.2Fnotes.2F.23id.2Fcomment">
        /// POST /api/0.6/notes/#id/comment</see>
        /// </summary>
        /// <param name="noteId"></param>
        /// <param name="text"></param>
        /// <returns>The updated Note, including the new Comment</returns>
        Task<Note> CommentNote(long noteId, string text);
        /// <summary>
        /// Closes a Note and creates a new Comment
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Close:_POST_.2Fapi.2F0.6.2Fnotes.2F.23id.2Fclose">
        /// POST /api/0.6/notes/#id/close</see>
        /// </summary>
        /// <param name="noteId"></param>
        /// <param name="text"></param>
        /// <returns>The updated Note, including the new Comment</returns>
        Task<Note> CloseNote(long noteId, string text);
        /// <summary>
        /// ReOpens a Note and creates a new Comment
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Reopen:_POST_.2Fapi.2F0.6.2Fnotes.2F.23id.2Freopen">
        /// POST /api/0.6/notes/#id/reopen</see>
        /// </summary>
        /// <param name="noteId"></param>
        /// <param name="text"></param>
        /// <returns>The updated Note, including the new Comment</returns>
        Task<Note> ReOpenNote(long noteId, string text);
        /// <summary>
        /// Adds a new Element's creation to a Changeset
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Create:_PUT_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2Fcreate">
        /// PUT /api/0.6/[node|way|relation]/create</see>
        /// </summary>
        /// <param name="changesetId">The ID of an OPEN Changeset.</param>
        /// <returns>The ID of the new Element</returns>
        Task<long> CreateElement(long changesetId, OsmGeo osmGeo);
        /// <summary>
        /// Updates an element using a <see cref="ICompleteOsmGeo"/> object
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Update:_PUT_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id">
        /// PUT /api/0.6/[node|way|relation]/#id</see>
        /// </summary>
        /// <param name="changesetId">The ID of an OPEN Changeset.</param>
        /// <param name="osmGeo"></param>
        /// <returns>The Element's new version number</returns>
        Task<long> UpdateElement(long changesetId, ICompleteOsmGeo osmGeo);
        /// <summary>
        /// Updates an element using a <see cref="OsmGeo"/> object
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Update:_PUT_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id">
        /// PUT /api/0.6/[node|way|relation]/#id</see>
        /// </summary>
        /// <param name="changesetId">The ID of an OPEN Changeset.</param>
        /// <returns>The Element's new version number</returns>
        Task<long> UpdateElement(long changesetId, OsmGeo osmGeo);
        /// <summary>
        /// Delete an OSM element as part of the given changeset
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Delete:_DELETE_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id">
        /// DELETE /api/0.6/[node|way|relation]/#id</see>
        /// </summary>
        /// <param name="changesetId">The ID of an OPEN Changeset.</param>
        /// <param name="osmGeo"></param>
        /// <returns>The Element's new version number</returns>
        Task<long> DeleteElement(long changesetId, OsmGeo osmGeo);
        /// <summary>
        /// Gets the current User's GPX Trace Files
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#List:_GET_.2Fapi.2F0.6.2Fuser.2Fgpx_files">
        /// GET /api/0.6/user/gpx_files</see>
        /// </summary>
        /// <returns></returns>
        Task<GpxFile[]> GetTraces();
        /// <summary>
        /// Creates a new GPX Trace File
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Create:_POST_.2Fapi.2F0.6.2Fgpx.2Fcreate">
        /// POST /api/0.6/gpx/create</see>
        /// </summary>s
        /// <param name="gpx"></param>
        /// <param name="fileStream"></param>
        /// <returns>The GPX Trace File's ID</returns>
        Task<long> CreateTrace(GpxFile gpx, Stream fileStream);
        /// <summary>
        /// Updates a GPX Trace File (overwrites with a new one)
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Update:_PUT_.2Fapi.2F0.6.2Fgpx.2F.23id">
        /// PUT /api/0.6/gpx/#id</see>
        /// </summary>
        /// <param name="trace"></param>
        /// <returns></returns>
        Task UpdateTrace(GpxFile trace);
        /// <summary>
        /// Deletes a GPX Trace File
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Delete:_DELETE_.2Fapi.2F0.6.2Fgpx.2F.23id">
        /// DELETE /api/0.6/gpx/#id</see>
        /// </summary>
        /// <param name="traceId"></param>
        /// <returns></returns>
        Task DeleteTrace(long traceId);
        /// <summary>
        /// Gets the Permissions for the current User
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Capabilities:_GET_.2Fapi.2Fcapabilities">
        /// GET /api/0.6/permissions</see>.
        /// </summary>
        Task<Permissions> GetPermissions();
        /// <summary>
        /// Gets details for the current User
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Details_of_the_logged-in_user">
        /// GET /api/0.6/user/details</see>.
        /// </summary>
        Task<User> GetUserDetails();
        /// <summary>
        /// Gets a specific preference for the current User
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Preferences_of_the_logged-in_user">
        /// GET /api/0.6/user/preferences/#key</see>.
        /// </summary>
        /// <param name="key">Must not exceed 255 characters</param>
        Task<string> GetUserPreference(string key);
        /// <summary>
        /// Deletes a specific preference for the current User
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Preferences_of_the_logged-in_user">
        /// DELETE /api/0.6/user/preferences/#key</see>.
        /// </summary>
        /// <param name="key">Must not exceed 255 characters</param>
        Task DeleteUserPreference(string key);
        /// <summary>
        /// Gets the Preferences for the current User
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Preferences_of_the_logged-in_user">
        /// GET /api/0.6/user/preferences</see>.
        /// </summary>
        Task<Preference[]> GetUserPreferences();
        /// <summary>
        /// Sets a specific preference for the current User
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Preferences_of_the_logged-in_user">
        /// PUT /api/0.6/user/preferences/#key</see>.
        /// </summary>
        /// <param name="key">Must not exceed 255 characters</param>
        /// <param name="value">Must not exceed 255 characters</param>
        Task SetUserPreference(string key, string value);
        /// <summary>
        /// Overrides all the preferences for the current User with a new set
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Preferences_of_the_logged-in_user">
        /// PUT /api/0.6/user/preferences</see>
        /// </summary>
        /// <param name="preferences">Must not contain more than 150 elements.
        /// Keys and values must not exceed 255 characters.</param>
        Task SetUserPreferences(Preferences preferences);
    }
}