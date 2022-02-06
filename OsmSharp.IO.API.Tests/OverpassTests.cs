using System;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OsmSharp.IO.API.Tests
{
    [TestClass]
    public class OverpassTests
    {
        OverpassClient OverpassClient { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            OverpassClient = new OverpassClient();
        }
    }
}
