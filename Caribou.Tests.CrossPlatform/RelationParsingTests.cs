using Caribou.Models;
using Caribou.Processing;

namespace Caribou.Tests.CrossPlatform;

[TestClass]
public class RelationParsingTests
{
    private static string GetTestDataPath(string filename)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", filename);
    }

    [TestMethod]
    public void ParseRelation_FromMultipolygonFile_FindsBuildingRelation()
    {
        // Arrange
        var xmlPath = GetTestDataPath("multipolygon-test.xml");
        var buildingFeature = new OSMTag("building");
        var request = new ParseRequest(new List<OSMTag> { buildingFeature });
        var paths = new List<string> { xmlPath };
        var handler = new RequestHandler(paths, request, OSMGeometryType.Relation, null!, "Test");

        // Act
        ParseViaXMLReader.FindItemsByTag(ref handler, OSMGeometryType.Relation, false);

        // Assert
        Assert.IsTrue(handler.FoundData.ContainsKey(buildingFeature));
        var relations = handler.FoundData[buildingFeature]
            .Where(item => item.Kind == OSMGeometryType.Relation)
            .ToList();

        Assert.AreEqual(1, relations.Count, "Should find exactly one building relation");
    }

    [TestMethod]
    public void ParseRelation_ChecksRelationMetadata()
    {
        // Arrange
        var xmlPath = GetTestDataPath("multipolygon-test.xml");
        var buildingFeature = new OSMTag("building");
        var request = new ParseRequest(new List<OSMTag> { buildingFeature });
        var paths = new List<string> { xmlPath };
        var handler = new RequestHandler(paths, request, OSMGeometryType.Relation, null!, "Test");

        // Act
        ParseViaXMLReader.FindItemsByTag(ref handler, OSMGeometryType.Relation, false);

        // Assert
        var relation = handler.FoundData[buildingFeature][0];
        Assert.AreEqual("multipolygon", relation.Tags["type"]);
        Assert.AreEqual("yes", relation.Tags["building"]);
        Assert.AreEqual("Building with Courtyard", relation.Tags["name"]);
    }

    [TestMethod]
    public void ParseRelation_HasCorrectMembers()
    {
        // Arrange
        var xmlPath = GetTestDataPath("multipolygon-test.xml");
        var buildingFeature = new OSMTag("building");
        var request = new ParseRequest(new List<OSMTag> { buildingFeature });
        var paths = new List<string> { xmlPath };
        var handler = new RequestHandler(paths, request, OSMGeometryType.Relation, null!, "Test");

        // Act
        ParseViaXMLReader.FindItemsByTag(ref handler, OSMGeometryType.Relation, false);

        // Assert
        var relation = handler.FoundData[buildingFeature][0];
        Assert.IsNotNull(relation.Members);
        Assert.AreEqual(2, relation.Members.Count, "Should have 2 members (outer and inner)");
    }

    [TestMethod]
    public void ParseRelation_MembersHaveCorrectRoles()
    {
        // Arrange
        var xmlPath = GetTestDataPath("multipolygon-test.xml");
        var buildingFeature = new OSMTag("building");
        var request = new ParseRequest(new List<OSMTag> { buildingFeature });
        var paths = new List<string> { xmlPath };
        var handler = new RequestHandler(paths, request, OSMGeometryType.Relation, null!, "Test");

        // Act
        ParseViaXMLReader.FindItemsByTag(ref handler, OSMGeometryType.Relation, false);

        // Assert
        var relation = handler.FoundData[buildingFeature][0];
        var outerMember = relation.Members[0];
        var innerMember = relation.Members[1];

        Assert.AreEqual("outer", outerMember.Role);
        Assert.AreEqual("1001", outerMember.WayRef);
        Assert.AreEqual(5, outerMember.Coords.Count, "Outer ring should have 5 coordinates");

        Assert.AreEqual("inner", innerMember.Role);
        Assert.AreEqual("1002", innerMember.WayRef);
        Assert.AreEqual(5, innerMember.Coords.Count, "Inner ring should have 5 coordinates");
    }

    [TestMethod]
    public void ParseRelation_MemberCoordinatesAreResolved()
    {
        // Arrange
        var xmlPath = GetTestDataPath("multipolygon-test.xml");
        var buildingFeature = new OSMTag("building");
        var request = new ParseRequest(new List<OSMTag> { buildingFeature });
        var paths = new List<string> { xmlPath };
        var handler = new RequestHandler(paths, request, OSMGeometryType.Relation, null!, "Test");

        // Act
        ParseViaXMLReader.FindItemsByTag(ref handler, OSMGeometryType.Relation, false);

        // Assert
        var relation = handler.FoundData[buildingFeature][0];
        var outerMember = relation.Members[0];

        // Check that coordinates were properly resolved from node references
        Assert.IsNotNull(outerMember.Coords);
        Assert.IsTrue(outerMember.Coords.Count > 0);

        // Verify first coordinate matches the first node in the outer way
        var firstCoord = outerMember.Coords[0];
        Assert.AreEqual(-37.8150, firstCoord.Latitude, 0.0001);
        Assert.AreEqual(144.9630, firstCoord.Longitude, 0.0001);
    }

    [TestMethod]
    public void ParseRelation_FromSimpleXML_FindsTramRoute()
    {
        // Arrange
        var xmlPath = GetTestDataPath("simple.xml");
        var routeMasterFeature = new OSMTag("route_master", "tram");
        var request = new ParseRequest(new List<OSMTag> { routeMasterFeature });
        var paths = new List<string> { xmlPath };
        var handler = new RequestHandler(paths, request, OSMGeometryType.Relation, null!, "Test");

        // Act
        ParseViaXMLReader.FindItemsByTag(ref handler, OSMGeometryType.Relation, false);

        // Assert
        Assert.IsTrue(handler.FoundData.ContainsKey(routeMasterFeature));
        var relations = handler.FoundData[routeMasterFeature]
            .Where(item => item.Kind == OSMGeometryType.Relation)
            .ToList();

        Assert.AreEqual(1, relations.Count, "Should find the tram route_master relation");
        Assert.AreEqual("route_master", relations[0].Tags["type"]);
        Assert.AreEqual("tram", relations[0].Tags["route_master"]);
    }

    [TestMethod]
    public void ParseRelation_DoesNotDuplicateAcrossFiles()
    {
        // Arrange
        var xmlPath = GetTestDataPath("multipolygon-test.xml");
        var buildingFeature = new OSMTag("building");
        var request = new ParseRequest(new List<OSMTag> { buildingFeature });
        // Load the same file twice to test deduplication
        var paths = new List<string> { xmlPath, xmlPath };
        var handler = new RequestHandler(paths, request, OSMGeometryType.Relation, null!, "Test");

        // Act
        ParseViaXMLReader.FindItemsByTag(ref handler, OSMGeometryType.Relation, false);

        // Assert
        var relations = handler.FoundData[buildingFeature]
            .Where(item => item.Kind == OSMGeometryType.Relation)
            .ToList();

        Assert.AreEqual(1, relations.Count, "Should only find one relation despite loading file twice (deduplication)");
    }

    [TestMethod]
    public void OSMGeometryType_IncludesRelation()
    {
        // Verify the enum was properly extended
        var relationValue = OSMGeometryType.Relation;
        Assert.IsTrue(Enum.IsDefined(typeof(OSMGeometryType), relationValue));
    }

    [TestMethod]
    public void FoundItem_SupportsRelationConstructor()
    {
        // Arrange
        var tags = new Dictionary<string, string>
        {
            { "type", "multipolygon" },
            { "building", "yes" }
        };
        var members = new List<RelationMember>
        {
            new RelationMember("outer", new List<Coord>(), "1001"),
            new RelationMember("inner", new List<Coord>(), "1002")
        };

        // Act
        var item = new FoundItem(tags, members);

        // Assert
        Assert.AreEqual(OSMGeometryType.Relation, item.Kind);
        Assert.IsNotNull(item.Members);
        Assert.AreEqual(2, item.Members.Count);
        Assert.AreEqual(0, item.Coords.Count);
    }
}
