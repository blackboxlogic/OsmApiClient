using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;
namespace OsmSharp.IO.API.Overpass
{
    public class OverpassClient
    {

        static IEnumerable<string> BaseUrls = new List<string>()
        {
            "https://overpass-api.de/api/interpreter"
            // ...
        };

        HttpClient Client { get; } = new HttpClient();

        public async Task<T> Request<T>(string overpassQuery) where T : OsmGeo
        {

            var baseUrl = BaseUrls.GetEnumerator().Current;

            var url = baseUrl + overpassQuery;

            try
            {
                var json = await Client.GetStringAsync(url);
                return Deserialize<T>(json);
            }
            catch(Exception exc)
            {
                var hasNext = BaseUrls.GetEnumerator().MoveNext();

                if(!hasNext)
                {
                    return null;
                }

                return await Request<T>(overpassQuery);
            }

        }

        T Deserialize<T>(string json) where T : OsmGeo
        {
            var serializer = new XmlSerializer(typeof(T));
        }
    }
}
