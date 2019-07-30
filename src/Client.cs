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
using OsmSharp.Streams;
using OsmSharp.Streams.Complete;
using OsmSharp.Complete;
using System.Xml.Serialization;
using OsmSharp.IO.Xml;

namespace OsmSharp.IO.API
{
	// TODO: breakout, tests, whitespace, logging, disposing, cite/license, throttling, namespace
	public class Client
	{
		/// <summary>
		/// The OSM base address
		/// </summary>
		/// <example>
		/// "https://master.apis.dev.openstreetmap.org/api/0.6/"
		/// "https://www.openstreetmap.org/api/0.6/"
		/// </example>
		private readonly string BaseAddress;

		private string _userDetailsAddress => BaseAddress + "user/details";
		private string _createChangesetAddress => BaseAddress + "changeset/create";
		private string _uploadChangesetAddress => BaseAddress + "changeset/:id/upload";
		private string _closeChangesetAddress => BaseAddress + "changeset/:id/close";
		private string _createElementAddress => BaseAddress + ":type/create";
		private string _elementAddress => BaseAddress + ":type/:id";
		private string _completeElementAddress => BaseAddress + ":type/:id/full";
		private string _traceAddress => BaseAddress + "gpx/:id";
		private string _getTracesAddress => BaseAddress + "user/gpx_files";
		private string _createTraceAddress => BaseAddress + "gpx/create";

		public Client(string baseAddress)
		{
			BaseAddress = baseAddress;
		}

		public async Task<ICompleteOsmGeo> GetElement(string elementId, string type)
		{
			if (type.Equals(OsmGeoType.Node.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				return await GetNode(elementId);
			}
			if (type.Equals(OsmGeoType.Way.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				return await GetCompleteWay(elementId);
			}
			if (type.Equals(OsmGeoType.Relation.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				return await GetCompleteRelation(elementId);
			}
			throw new ArgumentException($"invalid {nameof(type)}: {type}");
		}

		public Task<CompleteWay> GetCompleteWay(string wayId)
		{
			return GetCompleteElement<CompleteWay>(wayId, "way");
		}

		public Task<CompleteRelation> GetCompleteRelation(string relationId)
		{
			return GetCompleteElement<CompleteRelation>(relationId, "relation");
		}

		private async Task<TCompleteOsmGeo> GetCompleteElement<TCompleteOsmGeo>(string id, string type) where TCompleteOsmGeo : class, ICompleteOsmGeo
		{
			using (var client = new HttpClient())
			{
				var address = _completeElementAddress.Replace(":id", id).Replace(":type", type);
				var response = await client.GetAsync(address);
				if (response.StatusCode != HttpStatusCode.OK)
				{
					return null;
				}
				var streamSource = new XmlOsmStreamSource(await response.Content.ReadAsStreamAsync());
				var completeSource = new OsmSimpleCompleteStreamSource(streamSource);
				return completeSource.OfType<TCompleteOsmGeo>().FirstOrDefault();
			}
		}

		public Task<Node> GetNode(string nodeId)
		{
			return GetElement<Node>(nodeId, "node");
		}

		public Task<Way> GetWay(string wayId)
		{
			return GetElement<Way>(wayId, "way");
		}

		public Task<Relation> GetRelation(string relationId)
		{
			return GetElement<Relation>(relationId, "relation");
		}

		private async Task<TOsmGeo> GetElement<TOsmGeo>(string id, string type) where TOsmGeo : OsmGeo
		{
			using (var client = new HttpClient())
			{
				var address = _elementAddress.Replace(":id", id).Replace(":type", type);
				var response = await client.GetAsync(address);
				if (response.StatusCode != HttpStatusCode.OK)
				{
					return null;
				}
				var streamSource = new XmlOsmStreamSource(await response.Content.ReadAsStreamAsync());
				return streamSource.OfType<TOsmGeo>().FirstOrDefault();
			}
		}

		private Osm GetOsmRequest(string changesetId, OsmGeo osmGeo)
		{
			var osm = new Osm();
			long changeSetId = long.Parse(changesetId);
			switch (osmGeo.Type)
			{
				case OsmGeoType.Node:
					osm.Nodes = new[] { osmGeo as Node };
					osm.Nodes.First().ChangeSetId = changeSetId;
					break;
				case OsmGeoType.Way:
					osm.Ways = new[] { osmGeo as Way };
					osm.Ways.First().ChangeSetId = changeSetId;
					break;
				case OsmGeoType.Relation:
					osm.Relations = new[] { osmGeo as Relation };
					osm.Relations.First().ChangeSetId = changeSetId;
					break;
			}
			return osm;
		}

		private Osm FromContent(Stream stream)
		{
			var serializer = new XmlSerializer(typeof(Osm));
			return serializer.Deserialize(stream) as Osm;
		}

		public async Task<User> GetUser()
		{
			using (var client = new HttpClient())
			{
				AddAuthentication(client, _userDetailsAddress);
				var response = await client.GetAsync(_userDetailsAddress);
				if (response.StatusCode != HttpStatusCode.OK)
				{
					return null;
				}
				var streamContent = await response.Content.ReadAsStreamAsync();
				var detailsResponse = FromContent(streamContent);
				return detailsResponse?.User;
			}
		}

		public async Task<string> CreateChangeset(string comment)
		{
			using (var client = new HttpClient())
			{
				AddAuthentication(client, _createChangesetAddress, "PUT");
				var changeSet = new Osm
				{
					Changesets = new[]
					{
						new Changeset
						{
							Tags = new TagsCollection
							{
								new Tag {Key = "created_by", Value = "IsraelHiking.osm.org.il"},
								new Tag {Key = "comment", Value = comment}
							}
						}
					}
				};
				var response = await client.PutAsync(_createChangesetAddress, new StringContent(changeSet.SerializeToXml()));
				if (response.StatusCode != HttpStatusCode.OK)
				{
					var message = await response.Content.ReadAsStringAsync();
					throw new Exception($"Unable to create changeset: {message}");
				}
				return await response.Content.ReadAsStringAsync();
			}
		}

		public async Task<DiffResult> UploadChangeset(string changesetId, OsmChange osmChange)
		{
			using (var client = new HttpClient())
			{
				foreach (var osmGeo in osmChange.Create.Concat(osmChange.Modify).Concat(osmChange.Delete))
				{
					osmGeo.ChangeSetId = long.Parse(changesetId);
				}
				var address = _uploadChangesetAddress.Replace(":id", changesetId);
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

		public async Task<string> CreateElement(string changesetId, OsmGeo osmGeo)
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

		public Task UpdateElement(string changesetId, ICompleteOsmGeo osmGeo)
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

		public async Task UpdateElement(string changesetId, OsmGeo osmGeo)
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

		public async Task CloseChangeset(string changesetId)
		{
			using (var client = new HttpClient())
			{
				var address = _closeChangesetAddress.Replace(":id", changesetId);
				AddAuthentication(client, address, "PUT");
				var response = await client.PutAsync(address, new StringContent(string.Empty));
				if (response.StatusCode != HttpStatusCode.OK)
				{
					var message = await response.Content.ReadAsStringAsync();
					throw new Exception($"Unable to close changeset with id: {changesetId} {message}");
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

		protected virtual void AddAuthentication(HttpClient client, string url, string method = "GET")
		{
			throw new Exception("Calls that modify map data or request user data require BasicAuthClient or OAuthClient.");
		}
	}
}

