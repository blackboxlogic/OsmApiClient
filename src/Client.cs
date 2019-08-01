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
	// TODO:
	// get auth to work
	// functional tests
	// logging
	// Don't dispose httpClient
	// cite sources/licenses
	// track data transfered
	// fix namespace
	public class Client
	{
		/// <summary>
		/// The OSM base address
		/// </summary>
		/// <example>
		/// "https://master.apis.dev.openstreetmap.org/api/0.6/"
		/// "https://www.openstreetmap.org/api/0.6/"
		/// </example>
		protected readonly string BaseAddress;

		private string _elementAddress => BaseAddress + ":type/:id";
		private string _completeElementAddress => BaseAddress + ":type/:id/full";

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

		protected Osm GetOsmRequest(string changesetId, OsmGeo osmGeo)
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
	}
}

