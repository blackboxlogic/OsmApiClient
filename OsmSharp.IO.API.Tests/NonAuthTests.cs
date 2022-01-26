using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsmSharp.API;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OsmSharp.IO.API.Tests
{
    [TestClass]
    public class NonAuthTests
    {
        private INonAuthClient client;

        public static readonly Bounds WashingtonDC = new Bounds()
        {
            MinLongitude = -77.0671918f,
            MinLatitude = 38.9007186f,
            MaxLongitude = -77.00099990f,
            MaxLatitude = 38.98734f
        };

        private readonly Bounds TraceArea = new Bounds()
        {
            MinLongitude = 0,
            MinLatitude = 51.5f,
            MaxLongitude = 0.25f,
            MaxLatitude = 51.75f,
        };

        private readonly Bounds NoteBounds = new Bounds()
        {
            MinLongitude = 0,
            MinLatitude = 50f,
            MaxLongitude = 5f,
            MaxLatitude = 55f,
        };

        private Task<Osm> GetWashingtonObject()
        {
            return client.GetMap(WashingtonDC);
        }

        private async Task<Node> GetFirstNodeInWashington()
        {
            var map = await client.GetMap(WashingtonDC);
            return await client.GetNode(map.Nodes.First().Id.Value);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
            var logger = loggerFactory.CreateLogger("Tests");
            var clientFactory = new ClientsFactory(logger, new HttpClient(), ClientsFactory.DEVELOPMENT_URL);
            client = clientFactory.CreateNonAuthClient();
        }

        [TestMethod]
        public async Task TestCapabilities()
        {
            var capabilities = await client.GetCapabilities();
            Assert.IsNotNull(capabilities?.Api?.Status?.Api);
            Assert.IsNotNull(capabilities?.Policy);
        }

        [TestMethod]
        public async Task TestApiVersion()
        {
            var apiVersion = await client.GetVersions();
            Assert.IsNotNull(apiVersion);
        }

        [TestMethod]
        public async Task TestMap()
        {
            var map = await GetWashingtonObject();
            Assert.IsNotNull(map?.Nodes?.FirstOrDefault());
            Assert.IsNotNull(map?.Ways?.FirstOrDefault());
            Assert.IsNotNull(map?.Relations?.FirstOrDefault());
        }

        [TestMethod]
        public async Task TestNode()
        {
            var map = await GetWashingtonObject();
            var nodeId = map.Nodes.First().Id.Value;
            var node = await client.GetNode(nodeId);
            Assert.IsNotNull(node);
            var nodeVersion = client.GetNodeVersion(nodeId, 1);
            Assert.IsNotNull(nodeVersion);
            var nodeHistory = await client.GetNodeHistory(nodeId);
            Assert.IsTrue(nodeHistory.Any());
            var multifetchNodes = await client.GetNodes(new Dictionary<long, long?>() { { nodeId, null }, { nodeId + 1, 1 } });
            Assert.IsTrue(multifetchNodes.Any());
            var nodeRelations = await client.GetNodeRelations(nodeId);
            Assert.IsNotNull(nodeRelations);
            var nodeWays = await client.GetNodeWays(nodeId);
            Assert.IsNotNull(nodeWays);
        }

        [TestMethod]
        public async Task TestWay()
        {
            var map = await GetWashingtonObject();
            var wayId = map.Ways.First().Id.Value;
            var way = await client.GetWay(wayId);
            Assert.IsNotNull(way);
            var wayComplete = await client.GetCompleteWay(wayId);
            Assert.IsNotNull(wayComplete);
        }

        [TestMethod]
        public async Task TestRelation()
        {
            var map = await GetWashingtonObject();
            var relationId = map.Relations.First().Id.Value;
            var relation = client.GetRelation(relationId).Result;
            Assert.IsNotNull(relation);
            var relationComplete = client.GetCompleteRelation(relationId).Result;
            Assert.IsNotNull(relationComplete);
        }

        [TestMethod]
        public async Task TestGetElements()
        {
            var map = await GetWashingtonObject();
            var multifetchElements = await client.GetElements(
                map.Nodes.Select(n => new OsmGeoKey(n)).Concat(map.Ways.Select(n => new OsmGeoKey(n))).ToArray());
            Assert.IsNotNull(multifetchElements);
        }

        [TestMethod]
        public async Task TestGetChangeset()
        {
            var node = await GetFirstNodeInWashington();
            var changeset = await client.GetChangeset(node.ChangeSetId.Value);
            Assert.IsNotNull(changeset);
            var changesetWithDiscussion = await client.GetChangeset(node.ChangeSetId.Value, true);
            Assert.IsNotNull(changesetWithDiscussion?.Discussion);
        }

        [TestMethod]
        public async Task TestQueryChangesets()
        {
            var node = await GetFirstNodeInWashington();
            var changesets = await client.QueryChangesets(WashingtonDC, null, null, null, null, false, false, null);
            Assert.IsTrue(changesets.Any());
            changesets = await client.QueryChangesets(null, node.UserId, null, null, null, false, false, null);
            Assert.IsTrue(changesets.Any());
            changesets = await client.QueryChangesets(null, node.UserId, null, DateTime.MinValue, null, false, false, null);
            Assert.IsTrue(changesets.Any());
            changesets = await client.QueryChangesets(null, node.UserId, null, DateTime.MinValue, DateTime.UtcNow, false, false, null);
            Assert.IsTrue(changesets.Any());
            changesets = await client.QueryChangesets(null, null, node.UserName, null, null, false, false, null);
            Assert.IsTrue(changesets.Any());
            changesets = await client.QueryChangesets(null, null, null, null, null, false, false, new long[] { 151176, 151177 });
            Assert.AreEqual(2, changesets.Length);
        }

        [TestMethod]
        public async Task TestUser()
        {
            var node = await GetFirstNodeInWashington();
            var user = await client.GetUser(node.UserId.Value);
            Assert.IsNotNull(user);
            var users = await client.GetUsers(node.UserId.Value, node.UserId.Value + 1);
            Assert.IsTrue(users.Any());
        }

        [TestMethod]
        public async Task TestTrack()
        {
            var gpx = await client.GetTrackPoints(TraceArea);
            Assert.IsNotNull(gpx);
        }

        [TestMethod]
        public async Task TestNotes()
        {
            var notes = await client.GetNotes(NoteBounds);
            Assert.IsTrue(notes?.Length > 0);
            Assert.IsTrue(notes[0].Id.HasValue);
            var noteId = notes[0].Id.Value;
            var note = await client.GetNote(noteId);
            Assert.IsTrue(note?.Id == noteId);
            var feed = await client.GetNotesRssFeed(NoteBounds);
            Assert.IsNotNull(feed);
            var node = await GetFirstNodeInWashington();
            await client.QueryNotes("ThisIsANote", null, null, null, null, null, null);
            await client.QueryNotes("ThisIsANote", node.UserId, null, null, null, null, null);
            await client.QueryNotes("ThisIsANote", null, node.UserName, null, null, null, null);
            await client.QueryNotes("ThisIsANote", null, null, 100, null, null, null);
            await client.QueryNotes("ThisIsANote", null, null, null, 1, null, null);
            await client.QueryNotes("ThisIsANote", null, null, null, null, DateTime.Now.Subtract(TimeSpan.FromDays(100)), null);
            await client.QueryNotes("ThisIsANote", null, null, null, null, null, DateTime.Now.Subtract(TimeSpan.FromDays(2)));
            var newNote = await client.CreateNote(10.1f, 10.2f, "HelloWorld");
            Assert.IsTrue(newNote?.Comments?.Comments?.FirstOrDefault()?.Text == "HelloWorld");
            Assert.IsTrue(newNote?.Comments?.Comments?.FirstOrDefault()?.Action == Note.Comment.CommentAction.Opened);
            Assert.IsTrue(newNote?.Comments?.Comments?.FirstOrDefault()?.UserId == null);
            Assert.AreEqual(Note.NoteStatus.Open, newNote?.Status);
        }

        [TestMethod]
        public void TestToStringCulture()
        {
            const string washingtonString = "-77.06719,38.90072,-77.001,38.98734";
            const string floatString = "-77.06719";
            const string doubleString = "-77.06719208";

            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            var clientAsChild = client as NonAuthClient;

            foreach (var culture in cultures)
            {
                Thread.CurrentThread.CurrentCulture = culture;
                var boundsValue = clientAsChild.ToString(WashingtonDC);
                Assert.AreEqual(washingtonString, boundsValue);
                var floatValue = clientAsChild.ToString(WashingtonDC.MinLongitude.Value);
                Assert.AreEqual(floatString, floatValue);
                var doubleValue = clientAsChild.ToString((double)WashingtonDC.MinLongitude);
                Assert.AreEqual(doubleString, doubleValue);
            }
        }
    }
}
