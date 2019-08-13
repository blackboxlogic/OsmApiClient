using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OsmSharp.API;
using OsmSharp.Changesets;
using OsmSharp.Tags;
using OsmSharp.Complete;
using System.Xml.Serialization;
using OsmSharp.IO.Xml;

namespace OsmSharp.IO.API
{
	public abstract class AuthClient : Client
	{
		public AuthClient(string baseAddress) : base(baseAddress)
		{ }

		public async Task<Permissions> GetPermissions()
		{
			var address = BaseAddress + "0.6/permissions";
			var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
			return osm.Permissions;
		}

		public async Task<User> GetUserDetails()
		{

			var address = BaseAddress + "0.6/user/details";
			var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
			return osm.User;
		}

		/// <param name="tags">Must at least contain 'comment' and 'created_by'.</param>
		public async Task<long> CreateChangeset(TagsCollection tags)
		{
			Validate.ContainsTags(tags, "comment", "created_by");
			var address = BaseAddress + "0.6/changeset/create";
			var changeSet = new Osm { Changesets = new[] { new Changeset { Tags = tags } } };
			var content = new StringContent(changeSet.SerializeToXml());
			var resultContent = await Put(address, content);
			var id = await resultContent.ReadAsStringAsync();
			return long.Parse(id);
		}

		/// <param name="tags">Must at least contain 'comment' and 'created_by'.</param>
		public async Task<Changeset> UpdateChangeset(long changesetId, TagsCollection tags)
		{
			Validate.ContainsTags(tags, "comment", "created_by");
			// TODO: Validate change meets OsmSharp.API.Capabilities?
			var address = BaseAddress + $"0.6/changeset/{changesetId}";
			var changeSet = new Osm { Changesets = new[] { new Changeset { Tags = tags } } };
			var content = new StringContent(changeSet.SerializeToXml());
			var response = await Put(address, content);
			var osm = FromContent(await response.ReadAsStreamAsync());
			return osm.Changesets[0];
		}

		/// <remarks>This automatically adds the ChangeSetId tag to each element.</remarks>
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

		public async Task<long> CreateElement(long changesetId, OsmGeo osmGeo)
		{
			var address = BaseAddress + $"0.6/{osmGeo.Type.ToString().ToLower()}/create";
			var osmRequest = GetOsmRequest(changesetId, osmGeo);
			var content = new StringContent(osmRequest.SerializeToXml());
			var response = await Put(address, content);
			var id = await response.ReadAsStringAsync();
			return long.Parse(id);
		}

		public Task UpdateElement(long changesetId, ICompleteOsmGeo osmGeo)
		{
			switch (osmGeo.Type)
			{
				case OsmGeoType.Node:
					return UpdateElement(changesetId, osmGeo as OsmGeo);
				case OsmGeoType.Way:
					return UpdateElement(changesetId, ((CompleteWay)osmGeo).ToSimple());
				case OsmGeoType.Relation:
					return UpdateElement(changesetId, ((CompleteRelation)osmGeo).ToSimple());
				default:
					throw new Exception($"Invalid OSM geometry type: {osmGeo.Type}");
			}
		}

		public async Task UpdateElement(long changesetId, OsmGeo osmGeo)
		{
			var address = BaseAddress + $"0.6/{osmGeo.Type.ToString().ToLower()}/{osmGeo.Id}";
			var osmRequest = GetOsmRequest(changesetId, osmGeo);
			var content = new StringContent(osmRequest.SerializeToXml());
			await Put(address, content);
		}

		public async Task CloseChangeset(long changesetId)
		{
			var address = BaseAddress + $"0.6/changeset/{changesetId}/close";
			await Put(address);
		}

		/// <summary>
		/// Comment
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Comment:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Fcomment">
		/// POST /api/0.6/changeset/#id/comment </see>
		/// </summary>
		public async Task AddChangesetComment(long changesetId, string text)
		{
			var address = BaseAddress + $"0.6/changeset/{changesetId}/comment";
			var content = new MultipartFormDataContent() { { new StringContent(text), "text" } };
			await Post(address, content);
		}

		/// <summary>
		/// Subscribe
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Subscribe:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Fsubscribe">
		/// POST /api/0.6/changeset/#id/subscribe </see>
		/// </summary>
		public async Task ChangesetSubscribe(long changesetId)
		{
			var address = BaseAddress + $"0.6/changeset/{changesetId}/subscribe";
			await Post(address);
		}

		/// <summary>
		/// Unsubscribe
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Subscribe:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Funsubscribe">
		/// POST /api/0.6/changeset/#id/unsubscribe </see>
		/// </summary>
		public async Task ChangesetUnsubscribe(long changesetId)
		{
			var address = BaseAddress + $"0.6/changeset/{changesetId}/unsubscribe";
			await Post(address);
		}

		public async Task<List<GpxFile>> GetTraces()
		{
			var address = BaseAddress + "0.6/user/gpx_files";
			var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
			return (osm.GpxFiles ?? new GpxFile[0]).ToList();
		}

		public async Task CreateTrace(string fileName, MemoryStream fileStream)
		{
			var address = BaseAddress + "0.6/gpx/create";

			var parameters = new Dictionary<string, string>
				{
					{ "description", Path.GetFileNameWithoutExtension(fileName) },
					{ "visibility", "public" },
					{ "tags", "" },
				};
			var multipartFormDataContent = new MultipartFormDataContent();
			foreach (var keyValuePair in parameters)
			{
				multipartFormDataContent.Add(new StringContent(keyValuePair.Value),
					$"\"{keyValuePair.Key}\"");
			}
			var streamContent = new StreamContent(fileStream);
			multipartFormDataContent.Add(streamContent, "file", Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(fileName)));

			await Post(address, multipartFormDataContent);
		}

		public async Task UpdateTrace(GpxFile trace)
		{
			var address = BaseAddress + $"0.6/gpx/{trace.Id}";
			var osmRequest = new Osm
			{
				GpxFiles = new[] { trace }
			};
			var content = new StringContent(osmRequest.SerializeToXml());
			await Put(address, content);
		}

		public async Task DeleteTrace(long traceId)
		{
			var address = BaseAddress + $"0.6/gpx/{traceId}";
			var client = new HttpClient();
			AddAuthentication(client, address, "DELETE");
			var response = await client.DeleteAsync(address);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				throw new Exception($"Request failed: {response.StatusCode}-{response.ReasonPhrase} {errorContent}");
			}
		}

		protected Osm GetOsmRequest(long changesetId, OsmGeo osmGeo)
		{
			var osm = new Osm();
			switch (osmGeo.Type)
			{
				case OsmGeoType.Node:
					osm.Nodes = new[] { osmGeo as Node };
					osm.Nodes.First().ChangeSetId = changesetId;
					break;
				case OsmGeoType.Way:
					osm.Ways = new[] { osmGeo as Way };
					osm.Ways.First().ChangeSetId = changesetId;
					break;
				case OsmGeoType.Relation:
					osm.Relations = new[] { osmGeo as Relation };
					osm.Relations.First().ChangeSetId = changesetId;
					break;
			}
			return osm;
		}

		protected async Task<T> Post<T>(string address, HttpContent requestContent = null) where T : class
		{
			requestContent = requestContent ?? new StringContent("");

			var client = new HttpClient();
			AddAuthentication(client, address, "Post");
			var response = await client.PostAsync(address, requestContent);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				throw new Exception($"Request failed: {response.StatusCode}-{response.ReasonPhrase} {errorContent}");
			}

			var stream = await response.Content.ReadAsStreamAsync();
			var serializer = new XmlSerializer(typeof(T));
			var content = serializer.Deserialize(stream) as T;
			return content;
		}

		protected async Task Post(string address, HttpContent requestContent = null)
		{
			requestContent = requestContent ?? new StringContent("");

			var client = new HttpClient();
			AddAuthentication(client, address, "Post");
			var response = await client.PostAsync(address, requestContent);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				throw new Exception($"Request failed: {response.StatusCode}-{response.ReasonPhrase} {errorContent}");
			}
		}

		protected async Task<HttpContent> Put(string address, HttpContent requestContent = null)
		{
			requestContent = requestContent ?? new StringContent("");

			var client = new HttpClient();
			AddAuthentication(client, address, "PUT");
			var response = await client.PutAsync(address, requestContent);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				throw new Exception($"Request failed: {response.StatusCode}-{response.ReasonPhrase} {errorContent}");
			}

			return response.Content;
		}

		protected abstract void AddAuthentication(HttpClient client, string url, string method = "GET");
	}
}

