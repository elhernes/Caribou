namespace Caribou.Tests.Parsing
{
    using System.Collections.Generic;
    using System.Linq;
    using Caribou.Models;
    using Caribou.Tests.Cases;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestRelationParsingGolfCourse : BaseParsingTest
    {
        private static readonly List<string> GolfCourseXML = new List<string>() {
            Properties.Resources.GolfCourseOSM
        };

        protected static int CountRelationsForMetaData(RequestHandler results, OSMTag request)
        {
            var allResults = results.FoundData[request];
            var relationResults = allResults.Where(o => o.Kind == OSMGeometryType.Relation);
            return relationResults.Count();
        }

        [TestMethod]
        public void ParseGolfFairwayMultipolygons()
        {
            var golfFeatures = new ParseRequest(new List<OSMTag>() {
                new OSMTag("golf", "fairway")
            });
            var results = fetchResultsViaXMLReader(GolfCourseXML, golfFeatures, OSMGeometryType.Relation);

            var golfTag = new OSMTag("golf", "fairway");
            var fairwayCount = CountRelationsForMetaData(results, golfTag);

            // ortonville-golf-course.xml has 14 golf fairway multipolygons
            Assert.IsTrue(fairwayCount > 0, $"Expected golf fairways but found {fairwayCount}");

            // Check that at least one has inner members (holes)
            var fairways = results.FoundData[golfTag];
            var hasInnerMembers = fairways.Any(f => f.Members != null && f.Members.Any(m => m.Role == "inner"));
            Assert.IsTrue(hasInnerMembers, "Expected at least one fairway with inner (hole) members");
        }

        [TestMethod]
        public void ParseBoundaryRelations()
        {
            var boundaryFeatures = new ParseRequest(new List<OSMTag>() {
                new OSMTag("boundary", "administrative")
            });
            var results = fetchResultsViaXMLReader(GolfCourseXML, boundaryFeatures, OSMGeometryType.Relation);

            var boundaryTag = new OSMTag("boundary", "administrative");
            var boundaryCount = CountRelationsForMetaData(results, boundaryTag);

            // ortonville-golf-course.xml has at least 1 boundary relation (Ortonville city)
            Assert.IsTrue(boundaryCount > 0, $"Expected boundary relations but found {boundaryCount}");

            // Check the Ortonville boundary has way members
            var boundaries = results.FoundData[boundaryTag];
            var ortonville = boundaries.FirstOrDefault(b => b.Tags.ContainsKey("name") && b.Tags["name"] == "Ortonville");
            Assert.IsNotNull(ortonville, "Expected to find Ortonville boundary");
            Assert.IsTrue(ortonville.Members != null && ortonville.Members.Count > 0, "Ortonville boundary should have members");
        }

        [TestMethod]
        public void ParseWaterMultipolygons()
        {
            var waterFeatures = new ParseRequest(new List<OSMTag>() {
                new OSMTag("natural", "water")
            });
            var results = fetchResultsViaXMLReader(GolfCourseXML, waterFeatures, OSMGeometryType.Relation);

            var waterTag = new OSMTag("natural", "water");
            var waterCount = CountRelationsForMetaData(results, waterTag);

            // ortonville-golf-course.xml has at least 1 water multipolygon
            Assert.IsTrue(waterCount > 0, $"Expected water multipolygons but found {waterCount}");
        }
    }
}
