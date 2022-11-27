using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsmSharp.API;
using OsmSharp.Changesets;
using OsmSharp.Tags;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OsmSharp.IO.API.Tests
{
    [TestClass]
    [Ignore("Should only be ran manually - comment-out for testing, do not check-in")]
    public class BasicAuthClientTests
    {
        private IAuthClient client;

        private static readonly GpxFile NewGpx = new GpxFile()
        {
            Name = "test.gpx",
            Description = "A file for testing upload functionality.",
            Visibility = Visibility.Public,
        };

        private static readonly TagsCollection ChangeSetTags = new TagsCollection()
        {
            new Tag("comment", "Running a functional test of an automated system."),
            new Tag("created_by", "https://github.com/OsmSharp/osm-api-client/"),
            new Tag("bot", "yes")
        };

        [TestInitialize]
        public void TestInitialize()
        {
            using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
            var logger = loggerFactory.CreateLogger("Tests");
            IClientsFactory clientFactory = new ClientsFactory(logger, new HttpClient(), ClientsFactory.DEVELOPMENT_URL);
            // Enter your user name and password here or OAuth credential below - do not check-in!
            client = clientFactory.CreateBasicAuthClient("user-email", "password");
            //client = clientFactory.CreateOAuthClient("customerkey", "customerSecret", "token", "tokenSecret");
            //client = clientFactory.CreateOAuth2Client("token");
        }

        [TestMethod]
        public async Task TestChangesetLifeCycle()
        {
            var user = await client.GetUserDetails();
            var map = await client.GetMap(NonAuthTests.WashingtonDC);
            var node = map.Nodes.FirstOrDefault();
            Assert.IsNotNull(user);
            Assert.IsNotNull(map);
            Assert.IsNotNull(node);
            var changesetId = await client.CreateChangeset(ChangeSetTags);
            Assert.IsTrue(changesetId > 0);
            var remarkKey = "Remark";
            var remarkValue = "This is a changeset tag";
            ChangeSetTags.Add(remarkKey, remarkValue);
            var changesetWithRemarkTag = await client.UpdateChangeset(changesetId, ChangeSetTags);
            Assert.AreEqual(remarkValue, changesetWithRemarkTag?.Tags[remarkKey]);
            var diffResult = await client.UploadChangeset(changesetId, new OsmChange() { Modify = new[] { node } });
            Assert.IsNotNull(diffResult);
            var newRelation = new Relation() { Tags = new TagsCollection(new Tag("Name", "TestRelation")) };
            newRelation.Members = new RelationMember[] { new RelationMember(node.Id.Value, null, OsmGeoType.Node) };
            newRelation.Id = await client.CreateElement(changesetId, newRelation);
            Assert.IsTrue(newRelation.Id > 0);
            var newNode = new Node() { Latitude = node.Latitude + .1, Longitude = node.Longitude + .1, Version = 1 };
            newNode.Id = await client.CreateElement(changesetId, newNode);
            Assert.IsTrue(newNode.Id > 0);
            newNode.Tags = new TagsCollection() { { "name", "this is a node name" } };
            newNode.Version = await client.UpdateElement(changesetId, (OsmGeo)newNode);
            Assert.AreEqual(2, newNode.Version);
            newNode.Version = await client.DeleteElement(changesetId, newNode);
            Assert.AreEqual(3, newNode.Version);
            await client.CloseChangeset(changesetId);
            var download = await client.GetChangesetDownload(changesetId);
            var changeset = await client.GetChangeset(changesetId);
            Assert.IsNotNull(download);
            Assert.IsNotNull(download?.Create);
            Assert.IsNotNull(download?.Modify);
            Assert.IsNotNull(download?.Delete);
            Assert.IsNotNull(changeset);
            Assert.AreEqual(0, changeset.CommentsCount);
            Assert.AreEqual(5, changeset.ChangesCount);
            var comment = "This is a comment on the changeset";
            var changesetFromComment = await client.AddChangesetComment(changesetId, comment);
            Assert.IsNotNull(changesetFromComment);
            Assert.AreEqual(1, changesetFromComment.CommentsCount);
            Assert.AreEqual(5, changesetFromComment.ChangesCount);
            var changesetWithComment = await client.GetChangeset(changesetId, true);
            Assert.IsNotNull(changesetWithComment);
            Assert.AreEqual(1, changesetWithComment.CommentsCount);
            Assert.AreEqual(5, changesetWithComment.ChangesCount);
            Assert.AreEqual(1, changesetWithComment.Discussion.Comments.Length);
            Assert.AreEqual(comment, changesetWithComment.Discussion.Comments[0].Text);
            // These throw their own exceptions if they fail.
            await client.ChangesetUnsubscribe(changesetId);
            await client.ChangesetSubscribe(changesetId);
            await client.ChangesetUnsubscribe(changesetId);
        }

        [TestMethod]
        public async Task TestPreferences()
        {
            var preferences = await client.GetUserPreferences();
            Assert.IsNotNull(preferences);
            var permissions = await client.GetPermissions();
            Assert.IsTrue(permissions.UserPermission.Any());
            if (permissions.UserPermission.Contains(Permissions.Permission.allow_write_prefs))
            {
                var testValue = "testValue";
                var testKey = "testKey";
                await client.SetUserPreference(testKey, testValue);
                var value = await client.GetUserPreference(testKey);
                Assert.AreEqual(testValue, value);
                preferences = await client.GetUserPreferences();
                Assert.IsNotNull(preferences);
                Assert.IsTrue(preferences.Any(p => p.Key == testKey && p.Value == testValue));
                await client.DeleteUserPreference(testKey);
            }
        }

        [TestMethod]
        public async Task TestNotes()
        {
            var permissions = await client.GetPermissions();
            Assert.IsTrue(permissions.UserPermission.Any());
            if (permissions.UserPermission.Contains(Permissions.Permission.allow_write_notes))
            {
                var noteText = "HelloWorld";
                var note = await client.CreateNote(10.1f, 10.2f, noteText);
                Assert.AreEqual(noteText, note?.Comments?.Comments?.FirstOrDefault()?.Text);
                Assert.AreEqual(Note.Comment.CommentAction.Opened, note?.Comments?.Comments?.FirstOrDefault()?.Action);
                Assert.AreEqual(Note.NoteStatus.Open, note?.Status);
                Assert.IsNotNull(note?.Comments?.Comments?.FirstOrDefault()?.UserId);
                noteText = "second";
                note = await client.CommentNote(note.Id.Value, noteText);
                Assert.AreEqual(noteText, note?.Comments?.Comments?.LastOrDefault()?.Text);
                Assert.AreEqual(Note.Comment.CommentAction.Commented, note?.Comments?.Comments?.LastOrDefault()?.Action);
                Assert.IsNotNull(note?.Comments?.Comments?.FirstOrDefault()?.UserId);
                Assert.AreEqual(Note.NoteStatus.Open, note?.Status);
                noteText = "closing";
                note = await client.CloseNote(note.Id.Value, noteText);
                Assert.AreEqual(noteText, note?.Comments?.Comments?.LastOrDefault()?.Text);
                Assert.AreEqual(Note.Comment.CommentAction.Closed, note?.Comments?.Comments?.LastOrDefault()?.Action);
                Assert.IsNotNull(note?.Comments?.Comments?.FirstOrDefault()?.UserId);
                Assert.AreEqual(Note.NoteStatus.Closed, note?.Status);
                noteText = "reopening";
                note = await client.ReOpenNote(note.Id.Value, noteText);
                Assert.AreEqual(noteText, note?.Comments?.Comments?.LastOrDefault()?.Text);
                Assert.AreEqual(Note.Comment.CommentAction.ReOpened, note?.Comments?.Comments?.LastOrDefault()?.Action);
                Assert.IsNotNull(note?.Comments?.Comments?.FirstOrDefault()?.UserId);
                Assert.AreEqual(Note.NoteStatus.Open, note?.Status);
                noteText = "second";
                note = await client.CommentNote(note.Id.Value, noteText);
                Assert.AreEqual(noteText, note?.Comments?.Comments?.LastOrDefault()?.Text);
                Assert.AreEqual(Note.Comment.CommentAction.Commented, note?.Comments?.Comments?.LastOrDefault()?.Action);
                Assert.IsNotNull(note?.Comments?.Comments?.FirstOrDefault()?.UserId);
                Assert.AreEqual(Note.NoteStatus.Open, note?.Status);
            }
        }

        [TestMethod]
        public async Task TestTraces()
        {
            using var gpxStream = File.Open("test.gpx", FileMode.Open);
            NewGpx.Id = await client.CreateTrace(NewGpx, gpxStream);
            Assert.IsTrue(NewGpx.Id > 0);
            var updatedText = "Updated";
            NewGpx.Description += updatedText;
            await client.UpdateTrace(NewGpx);
            var myTraces = await client.GetTraces();
            Assert.IsTrue(myTraces?.Length > 0);
            var gpxDetails = await client.GetTraceDetails(NewGpx.Id);
            Assert.IsNotNull(gpxDetails);
            Assert.IsTrue(gpxDetails.Description.EndsWith(updatedText));
            var gpxStreamBack = await client.GetTraceData(NewGpx.Id);
            Assert.IsNotNull(gpxStreamBack.Stream);
            Assert.IsNotNull(gpxStreamBack.FileName);
            Assert.IsNotNull(gpxStreamBack.ContentType);
            foreach (var trace in myTraces)
            {
                await client.DeleteTrace(trace.Id);
            }
            myTraces = await client.GetTraces();
            Assert.AreEqual(0 ,myTraces?.Length);
        }
    }
}
