using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OsmSharp.API;
using OsmSharp.Changesets;
using OsmSharp.Tags;

namespace OsmSharp.IO.API.FunctionalTests
{
	public static class Tests
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

		private static Bounds NoteBounds = new Bounds()
		{
			MinLongitude = 0,
			MinLatitude = 50f,
			MaxLongitude = 5f,
			MaxLatitude = 55f,
		};

		private static TagsCollection ChangeSetTags = new TagsCollection()
		{
			new Tag("comment", "Running a functional test of an automated system."),
			new Tag("created_by", "https://github.com/OsmSharp/osm-api-client/"),
			new Tag("bot", "yes")
		};

		private static GpxFile NewGpx = new GpxFile()
		{
			Name = "test.gpx",
			Description = "A file for testing upload functionality.",
			Visibility = Visibility.Public,
		};

		public static async Task TestClient(INonAuthClient client)
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

			var notes = await client.GetNotes(NoteBounds);
			True(notes?.Length > 0, notes[0]?.Id.HasValue);
			var noteId = notes[0].Id.Value;
			var note = await client.GetNote(noteId);
			True(note?.Id == noteId);
			var feed = await client.GetNotesRssFeed(NoteBounds);
			NotNull(feed);
			await client.QueryNotes("ThisIsANote", null, null, null, null, null, null);
			await client.QueryNotes("ThisIsANote", node.UserId, null, null, null, null, null);
			await client.QueryNotes("ThisIsANote", null, node.UserName, null, null, null, null);
			await client.QueryNotes("ThisIsANote", null, null, 100, null, null, null);
			await client.QueryNotes("ThisIsANote", null, null, null, 1, null, null);
			await client.QueryNotes("ThisIsANote", null, null, null, null, DateTime.Now.Subtract(TimeSpan.FromDays(100)), null);
			await client.QueryNotes("ThisIsANote", null, null, null, null, null, DateTime.Now.Subtract(TimeSpan.FromDays(2)));
			var newNote = await client.CreateNote(10.1f, 10.2f, "HelloWorld");
			True(newNote?.Comments?.Comments?.FirstOrDefault()?.Text == "HelloWorld",
				newNote?.Comments?.Comments?.FirstOrDefault()?.Action == Note.Comment.CommentAction.Opened,
				newNote?.Comments?.Comments?.FirstOrDefault()?.UserId == null,
				newNote?.Status == Note.NoteStatus.Open);
		}

		public static async Task TestAuthClient(IAuthClient client)
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

			var preferences = await client.GetUserPreferences();
			NotNull(preferences);
			if (permissions.UserPermission.Contains(Permissions.Permission.allow_write_prefs))
			{
				await client.SetUserPreference("testKey", "testValue");
				var value = await client.GetUserPreference("testKey");
				True(value == "testValue");
				preferences = await client.GetUserPreferences();
				NotNull(preferences);
				True(preferences.Any(p => p.Key == "testKey" && p.Value == "testValue"));
				await client.DeleteUserPreference("testKey");
			}

			if (permissions.UserPermission.Contains(Permissions.Permission.allow_write_notes))
			{
				var note = await client.CreateNote(10.1f, 10.2f, "HelloWorld");
				True(note?.Comments?.Comments?.FirstOrDefault()?.Text == "HelloWorld",
					note?.Comments?.Comments?.FirstOrDefault()?.Action == Note.Comment.CommentAction.Opened,
					note?.Comments?.Comments?.FirstOrDefault()?.UserId != null,
					note?.Status == Note.NoteStatus.Open);
				note = await client.CommentNote(note.Id.Value, "second");
				True(note?.Comments?.Comments?.LastOrDefault()?.Text == "second",
					note?.Comments?.Comments?.LastOrDefault()?.Action == Note.Comment.CommentAction.Commented,
					note?.Comments?.Comments?.FirstOrDefault()?.UserId != null,
					note?.Status == Note.NoteStatus.Open);
				note = await client.CloseNote(note.Id.Value, "closing");
				True(note?.Comments?.Comments?.LastOrDefault()?.Text == "closing",
					note?.Comments?.Comments?.LastOrDefault()?.Action == Note.Comment.CommentAction.Closed,
					note?.Comments?.Comments?.FirstOrDefault()?.UserId != null,
					note?.Status == Note.NoteStatus.Closed);
				note = await client.ReOpenNote(note.Id.Value, "reopening");
				True(note?.Comments?.Comments?.LastOrDefault()?.Text == "reopening",
					note?.Comments?.Comments?.LastOrDefault()?.Action == Note.Comment.CommentAction.ReOpened,
					note?.Comments?.Comments?.FirstOrDefault()?.UserId != null,
					note?.Status == Note.NoteStatus.Open);
				note = await client.CommentNote(note.Id.Value, "second");
				True(note?.Comments?.Comments?.LastOrDefault()?.Text == "second",
					note?.Comments?.Comments?.LastOrDefault()?.Action == Note.Comment.CommentAction.Commented,
					note?.Comments?.Comments?.FirstOrDefault()?.UserId != null,
					note?.Status == Note.NoteStatus.Open);
			}
		}

		private static void NotNull(params object[] os)
		{
			if (os == null)
			{
				throw new Exception("Test FAILED");
			}

			foreach (var o in os)
			{
				if (o == null)
				{
					throw new Exception("Test FAILED");
				}
			}
		}

		private static void True(params bool?[] tests)
		{
			if (!tests.All(t => t == true))
			{
				throw new Exception("Test FAILED");
			}
		}
	}
}
