using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using OsmSharp;
using OsmSharp.API;

namespace OsmSharp.IO.API.Overpass
{
    public class OverpassQuery
    {
        

        private static List<KeyValuePair<string, string>> Nodes { get; set; }
        private static List<KeyValuePair<string, string>> Ways { get; set; }
        private static List<KeyValuePair<string, string>> Relations { get; set; }

        // should be:
        // "https://overpass-api.de/api/interpreter?data=[out:json][timeout:2];(node[name](57.69417400839879,11.900681422098906,57.71320555817524,11.927288935231376);<;);out meta;";
        private string OverpassUrl { get; set; } 

        private Bounds Bounds { get; }

        protected OverpassQuery(Bounds bounds, int timeout)
        {
            var url = "https://overpass-api.de/api/interpreter";

            url += "?data=[out:json]";
            url +="[timeout:"+timeout+"];";

            OverpassUrl = url;

            Bounds = bounds;

            
        }

        public OverpassQuery Add(string key, string value = null)
        {
            Nodes?.Add(new KeyValuePair<string, string>(key, value));
            Ways?.Add(new KeyValuePair<string, string>(key, value));
            Relations?.Add(new KeyValuePair<string, string>(key, value));

            return this;
        }

        public static OverpassQuery ForNodes(Bounds bounds, int timeout)
        {
            var osmQuery = new OverpassQuery(bounds, timeout);

            Nodes = new List<KeyValuePair<string, string>>();
            osmQuery.OverpassUrl += "(node";
           
            return osmQuery;
        }

        /*
        public static OsmQuery ForWays(BoundingBox boundingBox, int timeout)
        {
            Ways = new List<KeyValuePair<string, string>>();
            return new OsmQuery(boundingBox, timeout);
        }

        public static OsmQuery ForRelations(BoundingBox boundingBox, int timeout)
        {
            Relations = new List<KeyValuePair<string, string>>();
            return new OsmQuery(boundingBox, timeout);
        }
        */

        // make generic 'T'
        public string Create()
        {
            OverpassUrl += ToOverpassString(Nodes);
            OverpassUrl += ToOverpassString(Bounds);
            OverpassUrl += ";";
            OverpassUrl += "<;);out meta;";

            Console.WriteLine("request url...");
            Console.Write(OverpassUrl);

            return OverpassUrl;
        }


        static string ToOverpassString(List<KeyValuePair<string, string>> elements)
        {
            string tags = "";
            elements.ForEach(tag => tags += ToOverpassString(tag));
            return tags;
        }

        static string ToOverpassString(Bounds bounds)
        {
            var minLat = bounds.MinLatitude;
            var minLon = bounds.MinLongitude;
            var maxLat = bounds.MaxLatitude;
            var maxLon = bounds.MaxLongitude;

            return "("+minLat+","+minLon+","+maxLat+","+maxLon+")";
        }

        static string ToOverpassString(KeyValuePair<string, string> keyValuePair)
        {
            var key = keyValuePair.Key;
            var value = keyValuePair.Value;

            if(string.IsNullOrEmpty(value))
            {
                return "["+key+"]";
            }

            return "["+keyValuePair.Key+"="+value+"]";
        }

    }
}
