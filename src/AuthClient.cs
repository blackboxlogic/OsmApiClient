using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
		private string _userDetailsAddress => BaseAddress + "0.6/user/details";
		private string _createChangesetAddress => BaseAddress + "0.6/changeset/create";
		private string _updateChangesetAddress => BaseAddress + "0.6/changeset/:id";
		private string _uploadChangesetAddress => BaseAddress + "0.6/changeset/:id/upload";
		private string _closeChangesetAddress => BaseAddress + "0.6/changeset/:id/close";
		private string _createElementAddress => BaseAddress + "0.6/:type/create";
		private string _elementAddress => BaseAddress + "0.6/:type/:id";
		private string _traceAddress => BaseAddress + "0.6/gpx/:id";
		private string _getTracesAddress => BaseAddress + "0.6/user/gpx_files";
		private string _createTraceAddress => BaseAddress + "0.6/gpx/create";
		private string _permissionsAddress => BaseAddress + "0.6/permissions";
		private string _changesetCommentAddress => BaseAddress + "0.6/changeset/:id/comment";
		private string _changesetSubscribeAddress => BaseAddress + "0.6/changeset/:id/subscribe";
		private string _changesetUnsubscribeAddress => BaseAddress + "0.6/changeset/:id/unsubscribe";

		public AuthClient(string baseAddress) : base(baseAddress)
		{ }

		public async Task<Permissions> GetPermissions()
		{
			using (var client = new HttpClient())
			{
				AddAuthentication(client, _userDetailsAddress);
				var response = await client.GetAsync(_permissionsAddress);

				if (!response.IsSuccessStatusCode)
				{
					throw new Exception($"Unable to retrieve versions: {response.StatusCode}-{response.ReasonPhrase}");
				}

				var stream = await response.Content.ReadAsStreamAsync();
				var osm = FromContent(stream);
				return osm.Permissions;
			}
		}

		public async Task<User> GetUserDetails()
		{
			using (var client = new HttpClient())
			{
				AddAuthentication(client, _userDetailsAddress);
				var response = await client.GetAsync(_userDetailsAddress);
				if (response.StatusCode != HttpStatusCode.OK)
				{
					throw new Exception($"Unable to retrieve User: {response.StatusCode}-{response.ReasonPhrase}");
				}
				var stream = await response.Content.ReadAsStreamAsync();

				var detailsResponse = FromContent(stream);
				return detailsResponse?.User;
			}
		}

		/// <param name="tags">Must at least contain 'comment' and 'created_by'.</param>
		public async Task<long> CreateChangeset(TagsCollection tags)
		{
			Validate.ContainsTags(tags, "comment", "created_by");

			using (var client = new HttpClient())
			{
				AddAuthentication(client, _createChangesetAddress, "PUT");
				var changeSet = new Osm
				{
					Changesets = new[]
					{
						new Changeset
						{
							Tags = tags
						}
					}
				};
				var response = await client.PutAsync(_createChangesetAddress, new StringContent(changeSet.SerializeToXml()));
				var content = await response.Content.ReadAsStringAsync();
				if (response.StatusCode != HttpStatusCode.OK
					|| !long.TryParse(content, out long changesetId))
				{
					
					throw new Exception($"Unable to create changeset: {content}");
				}

				return changesetId;
			}
		}

		/// <param name="tags">Must at least contain 'comment' and 'created_by'.</param>
		public async Task<Changeset> UpdateChangeset(long changesetId, TagsCollection tags)
		{
			Validate.ContainsTags(tags, "comment", "created_by");
			// TODO: Validate change meets OsmSharp.API.Capabilities

			using (var client = new HttpClient())
			{
				var address = _updateChangesetAddress.Replace(":id", changesetId.ToString());
				AddAuthentication(client, address, "PUT");
				var changeSet = new Osm
				{
					Changesets = new[]
					{
						new Changeset
						{
							Tags = tags
						}
					}
				};
				var response = await client.PutAsync(address, new StringContent(changeSet.SerializeToXml()));
				if (!response.IsSuccessStatusCode)
				{
					var content = await response.Content.ReadAsStringAsync();
					throw new Exception($"Unable to update changeset: {content}");
				}

				var stream = await response.Content.ReadAsStreamAsync();
				var osm = FromContent(stream);
				return osm.Changesets[0];
			}
		}

		/// <remarks>This automatically adds the ChangeSetId tag to each element.</remarks>
		public async Task<DiffResult> UploadChangeset(long changesetId, OsmChange osmChange)
		{
			using (var client = new HttpClient())
			{
				var elements = new OsmGeo[][] { osmChange.Create, osmChange.Modify, osmChange.Delete }
					.Where(c => c != null).SelectMany(c => c);
				foreach (var osmGeo in elements)
				{
					osmGeo.ChangeSetId = changesetId;
				}
				var address = _uploadChangesetAddress.Replace(":id", changesetId.ToString());
				AddAuthentication(client, address, "POST");
				var response = await client.PostAsync(address, new StringContent(osmChange.SerializeToXml()));
				if (response.StatusCode != HttpStatusCode.OK)
				{
					var message = await response.Content.ReadAsStringAsync();
					throw new Exception($"Unable to upload changeset: {message}");
				}
				var serializer = new XmlSerializer(typeof(DiffResult));
				return serializer.Deserialize(await response.Content.ReadAsStreamAsync()) as DiffResult;
			}
		}

		public async Task<string> CreateElement(long changesetId, OsmGeo osmGeo)
		{
			using (var client = new HttpClient())
			{
				var address = _createElementAddress.Replace(":type", osmGeo.Type.ToString().ToLower());
				AddAuthentication(client, address, "PUT");
				var osmRequest = GetOsmRequest(changesetId, osmGeo);
				var response = await client.PutAsync(address, new StringContent(osmRequest.SerializeToXml()));
				if (response.StatusCode != HttpStatusCode.OK)
				{
					return string.Empty;
				}
				return await response.Content.ReadAsStringAsync();
			}
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
			using (var client = new HttpClient())
			{
				var address = _elementAddress.Replace(":id", osmGeo.Id.ToString()).Replace(":type", osmGeo.Type.ToString().ToLower());
				AddAuthentication(client, address, "PUT");
				var osmRequest = GetOsmRequest(changesetId, osmGeo);
				var response = await client.PutAsync(address, new StringContent(osmRequest.SerializeToXml()));
				if (response.StatusCode != HttpStatusCode.OK)
				{
					var message = await response.Content.ReadAsStringAsync();
					throw new Exception($"Unable to update {osmGeo.Type} with id: {osmGeo.Id} {message}");
				}
			}
		}

		public async Task CloseChangeset(long changesetId)
		{
			using (var client = new HttpClient())
			{
				var address = _closeChangesetAddress.Replace(":id", changesetId.ToString());
				AddAuthentication(client, address, "PUT");
				var response = await client.PutAsync(address, new StringContent(string.Empty));
				if (response.StatusCode != HttpStatusCode.OK)
				{
					var message = await response.Content.ReadAsStringAsync();
					throw new Exception($"Unable to close changeset with id: {changesetId} {message}");
				}
			}
		}

		/// <summary>
		/// Comment
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Comment:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Fcomment">
		/// POST /api/0.6/changeset/#id/comment </see>
		/// </summary>
		public async Task AddChangesetComment(long changesetId, string text)
		{
			using (var client = new HttpClient())
			{
				var address = _changesetCommentAddress.Replace(":id", changesetId.ToString());
				AddAuthentication(client, address, "POST");
				var content = new MultipartFormDataContent() { { new StringContent(text), "text" } };
				var response = await client.PostAsync(address, content);
				if (response.StatusCode != HttpStatusCode.OK)
				{
					var message = await response.Content.ReadAsStringAsync();
					throw new Exception($"Unable to add comment: {changesetId} {message}");
				}
			}
		}

		/// <summary>
		/// Subscribe
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Subscribe:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Fsubscribe">
		/// POST /api/0.6/changeset/#id/subscribe </see>
		/// </summary>
		public async Task ChangesetSubscribe(long changesetId)
		{
			using (var client = new HttpClient())
			{
				var address = _changesetSubscribeAddress.Replace(":id", changesetId.ToString());
				AddAuthentication(client, address, "POST");
				var response = await client.PostAsync(address, new StringContent(""));
				if (response.StatusCode != HttpStatusCode.OK)
				{
					var message = await response.Content.ReadAsStringAsync();
					throw new Exception($"Unable to add comment: {changesetId} {message}");
				}
			}
		}

		/// <summary>
		/// Unsubscribe
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Subscribe:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Funsubscribe">
		/// POST /api/0.6/changeset/#id/unsubscribe </see>
		/// </summary>
		public async Task ChangesetUnsubscribe(long changesetId)
		{
			using (var client = new HttpClient())
			{
				var address = _changesetUnsubscribeAddress.Replace(":id", changesetId.ToString());
				AddAuthentication(client, address, "POST");
				var response = await client.PostAsync(address, new StringContent(""));
				if (response.StatusCode != HttpStatusCode.OK)
				{
					var message = await response.Content.ReadAsStringAsync();
					throw new Exception($"Unable to add comment: {changesetId} {message}");
				}
			}
		}

		public async Task<List<GpxFile>> GetTraces()
		{
			using (var client = new HttpClient())
			{
				AddAuthentication(client, _getTracesAddress);
				var response = await client.GetAsync(_getTracesAddress);
				var stream = await response.Content.ReadAsStreamAsync();
				var osm = FromContent(stream);
				return (osm.GpxFiles ?? new GpxFile[0]).ToList();
			}
		}

		public async Task CreateTrace(string fileName, MemoryStream fileStream)
		{
			using (var client = new HttpClient())
			{
				AddAuthentication(client, _createTraceAddress, "POST");
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

				var response = await client.PostAsync(_createTraceAddress, multipartFormDataContent);
				if (response.StatusCode != HttpStatusCode.OK)
				{
					throw new Exception($"Unable to upload the file: {fileName}");
				}
			}
		}

		public async Task UpdateTrace(GpxFile trace)
		{
			using (var client = new HttpClient())
			{
				var traceAddress = _traceAddress.Replace(":id", trace.Id.ToString());
				AddAuthentication(client, traceAddress, "PUT");

				var osmRequest = new Osm
				{
					GpxFiles = new[] { trace }
				};
				var response = await client.PutAsync(traceAddress, new StringContent(osmRequest.SerializeToXml()));
				if (response.StatusCode != HttpStatusCode.OK)
				{
					throw new Exception("Unable to update OSM trace");
				}
			}
		}

		public async Task DeleteTrace(string traceId)
		{
			using (var client = new HttpClient())
			{
				var traceAddress = _traceAddress.Replace(":id", traceId);
				AddAuthentication(client, traceAddress, "DELETE");
				var response = await client.DeleteAsync(traceAddress);
				if (response.StatusCode != HttpStatusCode.OK)
				{
					throw new Exception($"Unable to delete OSM trace with ID: {traceId}");
				}
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

		protected abstract void AddAuthentication(HttpClient client, string url, string method = "GET");
	}
}

