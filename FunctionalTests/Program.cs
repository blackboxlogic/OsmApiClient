using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OsmSharp.API;
using OsmSharp.Changesets;
using OsmSharp.Tags;

namespace OsmSharp.IO.API.FunctionalTests
{
	class Program
	{
		private static Bounds WashingtonDC = new Bounds()
		{
			MinLongitude = -77.0371918f,
			MinLatitude = 38.9067186f,
			MaxLongitude = -77.03599990f,
			MaxLatitude = 38.907734f,
		};

		private static Bounds TraceArea = new Bounds()
		{
			MinLongitude = 0,
			MinLatitude = 51.5f,
			MaxLongitude = 0.25f,
			MaxLatitude = 51.75f,
		};

		private static TagsCollection ChangeSetTags = new TagsCollection()
		{
			new Tag("comment", "Running a functional test of an automated system."),
			new Tag("created_by", "https://github.com/blackboxlogic/OsmApiClient"),
			new Tag("bot", "yes")
		};

		private static GpxFile NewGpx = new GpxFile()
		{
			Name = "test.gpx",
			Description = "A file for testing upload functionality.",
			Visibility = Visibility.Public,
		};

		public static void Main()
		{
			var Config = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", false, true)
				.Build();

			Console.Write("Testing unauthenticated client");
			var client = new Client(Config["osmApiUrl"]);
			TestClient(client).Wait();
			Console.WriteLine("All tests passed for the unauthenticated client.");

			if (!string.IsNullOrEmpty(Config["basicAuth:Password"]))
			{
				if (!Config["osmApiUrl"].Contains("dev")) throw new Exception("These tests modify data, and it looks like your running them in PROD, please don't");

				Console.Write("Testing BasicAuth client");
				var basicAuth = new BasicAuthClient(Config["osmApiUrl"], Config["basicAuth:User"], Config["basicAuth:Password"]);
				TestAuthClient(basicAuth).Wait();
				Console.WriteLine("All tests passed for the BasicAuth client.");
			}
			else
			{
				Console.WriteLine("Skipped BasicAuth tests, no credentials supplied.");
			}

			if (!string.IsNullOrEmpty(Config["oAuth:consumerSecret"]))
			{
				if (!Config["osmApiUrl"].Contains("dev")) throw new Exception("These tests modify data, and it looks like your running them in PROD, please don't");

				Console.Write("Testing OAuth client");
				var oAuth = new OAuthClient(Config["osmApiUrl"], null, null, null, null);
				TestAuthClient(oAuth).Wait();
				Console.WriteLine("All tests passed for the OAuth client.");
			}
			else
			{
				Console.WriteLine("Skipped OAuth tests, no credentials supplied.");
			}

			Console.ReadKey(true);
		}

		private static async Task TestClient(Client client)
		{
			var capabilities = await client.GetCapabilities();
			var apiVersion = await client.GetVersions();
			NotNull(capabilities?.Api?.Status?.Api, capabilities?.Policy, apiVersion);
			var map = await client.GetMap(WashingtonDC);
			NotNull(map?.Nodes?.FirstOrDefault(), map?.Ways?.FirstOrDefault(), map?.Relations?.FirstOrDefault());
			var nodeId = map.Nodes.First().Id.Value;
			var wayId = map.Ways.First().Id.Value;
			var relationId = map.Relations.First().Id.Value;
			var node = await client.GetNode(nodeId);
			var way = await client.GetWay(wayId);
			var wayComplete = await client.GetCompleteWay(wayId);
			var relation = await client.GetRelation(relationId);
			var relationComplete = await client.GetCompleteRelation(relationId);
			var nodeVersion = await client.GetNodeVersion(nodeId, 3);
			NotNull(node, way, wayComplete, relation, relationComplete, nodeVersion);
			var nodeHistory = await client.GetNodeHistory(nodeId);
			var multifetchNodes = await client.GetNodes(new Dictionary<long, int?>() { { nodeId, null }, { nodeId + 1, 1 } });
			var nodeRelations = await client.GetNodeRelations(nodeId);
			var nodeWays = await client.GetNodeWays(nodeId);
			True(nodeHistory?.Any(), multifetchNodes?.Any(), nodeRelations?.Any(), nodeWays?.Any());
			var changeset = await client.GetChangeset(node.ChangeSetId.Value);
			var changesetWithDiscussion = await client.GetChangeset(node.ChangeSetId.Value, true);
			NotNull(changeset, changesetWithDiscussion?.Discussion);
			var changesets = await client.QueryChangesets(WashingtonDC, null, null, null, null, false, false, null);
			True(changesets?.Any());
			changesets = await client.QueryChangesets(null, node.UserId, null, null, null, false, false, null);
			True(changesets?.Any());
			changesets = await client.QueryChangesets(null, null, node.UserName, null, null, false, false, null);
			True(changesets?.Any());
			changesets = await client.QueryChangesets(null, null, null, null, null, false, false, new long[] { 151176, 151177 });
			True(changesets.Length == 2);
			var user = await client.GetUser(node.UserId.Value);
			NotNull(user);
			var users = await client.GetUsers(node.UserId.Value, node.UserId.Value + 1);
			True(users?.Any());
			var gpx = await client.GetTrackPoints(TraceArea);
			NotNull(gpx);
		}

		private static async Task TestAuthClient(AuthClient client)
		{
			var permissions = await client.GetPermissions();
			True(permissions?.UserPermission?.Any());
			var user = await client.GetUserDetails();
			var map = await client.GetMap(WashingtonDC);
			var node = map.Nodes.FirstOrDefault();
			NotNull(user, map, node);
			var changesetId = await client.CreateChangeset(ChangeSetTags);
			True(changesetId > 0);
			ChangeSetTags.Add("Remark", "This is a changeset tag");
			var changesetWithRemarkTag = await client.UpdateChangeset(changesetId, ChangeSetTags);
			True(changesetWithRemarkTag?.Tags["Remark"] == "This is a changeset tag");
			var diffResult = await client.UploadChangeset(changesetId, new OsmChange() { Modify = new[] { node } });
			NotNull(diffResult);
			var newRelation = new Relation() { Tags = new TagsCollection(new Tag("Name", "TestRelation")) };
			newRelation.Members = new RelationMember[] { new RelationMember(node.Id.Value, null, OsmGeoType.Node) };
			newRelation.Id = await client.CreateElement(changesetId, newRelation);
			True(newRelation.Id > 0);
			var newNode = new Node() { Latitude = node.Latitude + .1, Longitude = node.Longitude + .1, Version = 1 };
			newNode.Id = await client.CreateElement(changesetId, newNode);
			True(newNode.Id > 0);
			newNode.Tags = new TagsCollection() { { "name", "this is a node name" } };
			newNode.Version = await client.UpdateElement(changesetId, (OsmGeo)newNode);
			True(newNode.Version == 2);
			newNode.Version = await client.DeleteElement(changesetId, newNode);
			True(newNode.Version == 3);
			await client.CloseChangeset(changesetId);
			var download = await client.GetChangesetDownload(changesetId);
			var changeset = await client.GetChangeset(changesetId);
			NotNull(download, download?.Create, download?.Modify, download?.Delete, changeset);
			True(changeset.CommentsCount == 0, changeset.ChangesCount == 5);
			var changesetFromComment = await client.AddChangesetComment(changesetId, "This is a comment on the changeset");
			NotNull(changesetFromComment);
			True(changesetFromComment.CommentsCount == 1, changesetFromComment.ChangesCount == 5);
			var changesetWithComment = await client.GetChangeset(changesetId, true);
			NotNull(changesetWithComment);
			True(changesetWithComment.CommentsCount == 1, changesetWithComment.ChangesCount == 5,
				changesetWithComment.Discussion.Comments.Length == 1,
				changesetWithComment.Discussion.Comments[0].Text == "This is a comment on the changeset");
			// These throw their own exceptions if they fail.
			await client.ChangesetUnsubscribe(changesetId);
			await client.ChangesetSubscribe(changesetId);
			await client.ChangesetUnsubscribe(changesetId);

			using (var gpxStream = File.Open("test.gpx", FileMode.Open))
			{
				NewGpx.Id = await client.CreateTrace(NewGpx, gpxStream);
				True(NewGpx.Id > 0);
			}
			NewGpx.Description += "Updated";
			await client.UpdateTrace(NewGpx);
			var myTraces = await client.GetTraces();
			True(myTraces?.Length > 0);
			var gpxDetails = await client.GetTraceDetails(NewGpx.Id);
			NotNull(gpxDetails);
			True(gpxDetails.Description.EndsWith("Updated"));
			var gpxStreamBack = await client.GetTraceData(NewGpx.Id);
			NotNull(gpxStreamBack.Stream, gpxStreamBack.FileName, gpxStreamBack.ContentType);
			foreach (var trace in myTraces)
			{
				await client.DeleteTrace(trace.Id);
			}
			myTraces = await client.GetTraces();
			True(myTraces?.Length == 0);
		}

		private static void NotNull(params object[] os)
		{
			if (os == null)
			{
				throw new Exception("Test FAILED");
			}

			foreach (var o in os)
			{
				Console.Write('.');
				if (o == null)
				{
					throw new Exception("Test FAILED");
				}
			}
		}

		private static void True(params bool?[] tests)
		{
			Console.Write(new string('.', tests.Length));
			if (!tests.All(t => t == true))
			{
				throw new Exception("Test FAILED");
			}
		}
	}
}
