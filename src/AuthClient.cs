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
using System.Xml.Serialization;
using OsmSharp.IO.Xml;

namespace OsmSharp.IO.API
{
	public abstract class AuthClient : Client
	{
		public AuthClient(string baseAddress) : base(baseAddress)
		{ }

		#region Users
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
		#endregion

		#region Changesets and Element Changes
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
			var osm = await Put<Osm>(address, content);
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

		public async Task UpdateElement(long changesetId, ICompleteOsmGeo osmGeo)
		{
			switch (osmGeo.Type)
			{
				case OsmGeoType.Node:
					await UpdateElement(changesetId, osmGeo as OsmGeo);
					break;
				case OsmGeoType.Way:
					await UpdateElement(changesetId, ((CompleteWay)osmGeo).ToSimple());
					break;
				case OsmGeoType.Relation:
					await UpdateElement(changesetId, ((CompleteRelation)osmGeo).ToSimple());
					break;
				default:
					throw new Exception($"Invalid OSM geometry type: {osmGeo.Type}");
			}
		}

		public async Task<int> UpdateElement(long changesetId, OsmGeo osmGeo)
		{
			Validate.ElementHasAVersion(osmGeo);
			var address = BaseAddress + $"0.6/{osmGeo.Type.ToString().ToLower()}/{osmGeo.Id}";
			var osmRequest = GetOsmRequest(changesetId, osmGeo);
			var content = new StringContent(osmRequest.SerializeToXml());
			var responseContent = await Put(address, content);
			var newVersionNumber = await responseContent.ReadAsStringAsync();
			return int.Parse(newVersionNumber);
		}

		public async Task<int> DeleteElement(long changesetId, OsmGeo osmGeo)
		{
			Validate.ElementHasAVersion(osmGeo);
			var address = BaseAddress + $"0.6/{osmGeo.Type.ToString().ToLower()}/{osmGeo.Id}";
			var osmRequest = GetOsmRequest(changesetId, osmGeo);
			var content = new StringContent(osmRequest.SerializeToXml());
			var responseContent = await Delete(address, content);
			var newVersionNumber = await responseContent.ReadAsStringAsync();
			return int.Parse(newVersionNumber);
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
		public async Task<Changeset> AddChangesetComment(long changesetId, string text)
		{
			var address = BaseAddress + $"0.6/changeset/{changesetId}/comment";
			var content = new MultipartFormDataContent() { { new StringContent(text), "text" } };
			var osm = await Post<Osm>(address, content);
			return osm.Changesets[0];
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
		#endregion

		#region Traces
		public async Task<GpxFile[]> GetTraces()
		{
			var address = BaseAddress + "0.6/user/gpx_files";
			var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
			return osm.GpxFiles ?? new GpxFile[0];
		}

		public async Task<int> CreateTrace(GpxFile gpx, Stream fileStream)
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
			var content = await Post(address, form);
			var id = await content.ReadAsStringAsync();
			return int.Parse(id);
		}

		public async Task UpdateTrace(GpxFile trace)
		{
			var address = BaseAddress + $"0.6/gpx/{trace.Id}";
			var osm = new Osm { GpxFiles = new[] { trace } };
			var content = new StringContent(osm.SerializeToXml());
			await Put(address, content);
		}

		public async Task DeleteTrace(long traceId)
		{
			var address = BaseAddress + $"0.6/gpx/{traceId}";
			await Delete(address);
		}
		#endregion

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
			var responseContent = await Post(address, requestContent);
			var stream = await responseContent.ReadAsStreamAsync();
			var serializer = new XmlSerializer(typeof(T));
			var content = serializer.Deserialize(stream) as T;
			return content;
		}

		protected async Task<HttpContent> Post(string address, HttpContent requestContent = null)
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
			return response.Content;
		}

		protected async Task<T> Put<T>(string address, HttpContent requestContent = null) where T : class
		{
			var content = await Put(address, requestContent);
			var stream = await content.ReadAsStreamAsync();
			var serializer = new XmlSerializer(typeof(T));
			var element = serializer.Deserialize(stream) as T;
			return element;
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

		protected async Task<HttpContent> Delete(string address, HttpContent requestContent = null)
		{
			var client = new HttpClient();
			AddAuthentication(client, address, "DELETE");
			HttpResponseMessage response;

			if (requestContent != null)
			{
				HttpRequestMessage request = new HttpRequestMessage
				{
					Content = requestContent,
					Method = HttpMethod.Delete,
					RequestUri = new Uri(address)
				};
				response = await client.SendAsync(request);
			}
			else
			{
				response = await client.DeleteAsync(address);
			}

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				throw new Exception($"Request failed: {response.StatusCode}-{response.ReasonPhrase} {errorContent}");
			}

			return response.Content;
		}
	}
}

