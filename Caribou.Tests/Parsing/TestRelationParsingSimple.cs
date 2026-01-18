namespace Caribou.Tests.Parsing
{
    using System.Collections.Generic;
    using System.Linq;
    using Caribou.Models;
    using Caribou.Tests.Cases;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestRelationParsingSimple : BaseParsingTest
    {
        private static readonly List<string> MultipolygonXML = new List<string>() {
            Properties.Resources.MultipolygonOSM
        };

        // Test data for multipolygon-test.xml
        const int AllBuildingRelations = 1;
        const int MembersInBuildingRelation = 2;

        // Test data from simple.xml
        private static readonly List<string> SimpleXML = new List<string>() {
            Properties.Resources.SimpleOSM
        };

        private static readonly ParseRequest tramRouteFeatures = new ParseRequest(
            new List<OSMTag>() {
                new OSMTag("route_master", "tram")
            });

        protected static int CountRelationsForMetaData(RequestHandler results, OSMTag request)
        {
            var allResults = results.FoundData[request];
            var relationResults = allResults.Where(o => o.Kind == OSMGeometryType.Relation);
            return relationResults.Count();
        }

        [TestMethod]
        public void ParseRelationsGivenFeatureViaXMLReader()
        {
            var buildingFeatures = new ParseRequest(new List<OSMTag>() { buildingsData });
            var results = fetchResultsViaXMLReader(MultipolygonXML, buildingFeatures, OSMGeometryType.Relation);

            Assert.AreEqual(AllBuildingRelations, CountRelationsForMetaData(results, buildingsData));

            var buildingRelation = results.FoundData[buildingsData][0];
            Assert.AreEqual(MembersInBuildingRelation, buildingRelation.Members.Count);
            Assert.AreEqual("multipolygon", buildingRelation.Tags["type"]);
            Assert.AreEqual("yes", buildingRelation.Tags["building"]);
            Assert.AreEqual("Building with Courtyard", buildingRelation.Tags["name"]);
        }

        [TestMethod]
        public void ParseRelationMemberRoles()
        {
            var buildingFeatures = new ParseRequest(new List<OSMTag>() { buildingsData });
            var results = fetchResultsViaXMLReader(MultipolygonXML, buildingFeatures, OSMGeometryType.Relation);

            var buildingRelation = results.FoundData[buildingsData][0];
            Assert.AreEqual(2, buildingRelation.Members.Count);

            // First member should be outer
            Assert.AreEqual("outer", buildingRelation.Members[0].Role);
            Assert.AreEqual("1001", buildingRelation.Members[0].WayRef);
            Assert.AreEqual(5, buildingRelation.Members[0].Coords.Count); // 5 nodes for outer ring

            // Second member should be inner
            Assert.AreEqual("inner", buildingRelation.Members[1].Role);
            Assert.AreEqual("1002", buildingRelation.Members[1].WayRef);
            Assert.AreEqual(5, buildingRelation.Members[1].Coords.Count); // 5 nodes for inner ring
        }

        [TestMethod]
        public void ParseRelationsFromSimpleXML()
        {
            var results = fetchResultsViaXMLReader(SimpleXML, tramRouteFeatures, OSMGeometryType.Relation);

            // simple.xml contains a route_master relation for trams (lines 52-61)
            var tramRoutesData = new OSMTag("route_master", "tram");
            Assert.AreEqual(1, CountRelationsForMetaData(results, tramRoutesData));

            var tramRelation = results.FoundData[tramRoutesData][0];
            Assert.AreEqual("route_master", tramRelation.Tags["type"]);
            Assert.AreEqual("tram", tramRelation.Tags["route_master"]);
        }
    }
}
