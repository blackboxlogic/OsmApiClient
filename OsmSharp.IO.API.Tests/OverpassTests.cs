using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsmSharp.API;
using OsmSharp.IO.API.Overpass;

namespace OsmSharp.IO.API.Tests
{
    [TestClass]
    public class OverpassTests
    {
        private static readonly OverpassClient overpassClient = new OverpassClient();

        public static readonly Bounds WashingtonDC = new Bounds()
        {
            MinLatitude = 57.69379f,
            MinLongitude = 11.90072f,
            MaxLatitude = 57.7118f,
            MaxLongitude = 11.93443f
        };

        public static readonly string historicalBuildings = OverpassQuery.ForNodes(WashingtonDC, 3).Add("historical", "monument").Create();

        [TestInitialize]
        public void TestInitialize()
        {
            
        }

        [TestMethod]
        public void GetHistoricalBuildings()
        {
            
        }
    }
}
