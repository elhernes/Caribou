// Quick test for ortonville-golf-course.xml relation parsing
// This file contains multiple relation types:
// - boundary relations (administrative boundaries)
// - route relations (roads)
// - multipolygon relations (golf fairways with holes, water features)

using System;
using System.Collections.Generic;
using System.IO;
using Caribou.Models;
using Caribou.Processing;

class TestGolfCourse
{
    static void Main()
    {
        var filePath = "/Volumes/CSData/Development/github/Caribou/OSM Test Data/ortonville-golf-course.xml";

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"ERROR: File not found at {filePath}");
            return;
        }

        Console.WriteLine("=== Testing Golf Course Relations ===\n");

        // Test 1: Find boundary relations
        Console.WriteLine("Test 1: Finding boundary relations...");
        var boundaryRequest = new ParseRequest(new List<OSMTag> { new OSMTag("boundary", "administrative") });
        var boundaryResult = new RequestHandler(new List<string> { filePath }, boundaryRequest,
            OSMGeometryType.Relation, (s, d) => { }, "test-boundary");
        ParseViaXMLReader.FindItemsByTag(ref boundaryResult, OSMGeometryType.Relation);

        foreach (var kvp in boundaryResult.FoundData)
        {
            Console.WriteLine($"  Found {kvp.Value.Count} boundary relations");
            foreach (var item in kvp.Value)
            {
                var name = item.Tags.ContainsKey("name") ? item.Tags["name"] : "unnamed";
                Console.WriteLine($"    - {name}: {item.Members?.Count ?? 0} members");
            }
        }

        // Test 2: Find golf fairway multipolygons
        Console.WriteLine("\nTest 2: Finding golf fairways...");
        var golfRequest = new ParseRequest(new List<OSMTag> { new OSMTag("golf", "fairway") });
        var golfResult = new RequestHandler(new List<string> { filePath }, golfRequest,
            OSMGeometryType.Relation, (s, d) => { }, "test-golf");
        ParseViaXMLReader.FindItemsByTag(ref golfResult, OSMGeometryType.Relation);

        foreach (var kvp in golfResult.FoundData)
        {
            Console.WriteLine($"  Found {kvp.Value.Count} golf fairway multipolygons");
            foreach (var item in kvp.Value)
            {
                if (item.Members != null)
                {
                    var outer = item.Members.FindAll(m => m.Role == "outer").Count;
                    var inner = item.Members.FindAll(m => m.Role == "inner").Count;
                    Console.WriteLine($"    - Fairway: {outer} outer, {inner} inner (holes)");
                }
            }
        }

        // Test 3: Find natural water multipolygons
        Console.WriteLine("\nTest 3: Finding water features...");
        var waterRequest = new ParseRequest(new List<OSMTag> { new OSMTag("natural", "water") });
        var waterResult = new RequestHandler(new List<string> { filePath }, waterRequest,
            OSMGeometryType.Relation, (s, d) => { }, "test-water");
        ParseViaXMLReader.FindItemsByTag(ref waterResult, OSMGeometryType.Relation);

        foreach (var kvp in waterResult.FoundData)
        {
            Console.WriteLine($"  Found {kvp.Value.Count} water feature multipolygons");
            foreach (var item in kvp.Value)
            {
                if (item.Members != null)
                {
                    var outer = item.Members.FindAll(m => m.Role == "outer").Count;
                    var inner = item.Members.FindAll(m => m.Role == "inner").Count;
                    Console.WriteLine($"    - Water: {outer} outer, {inner} inner (islands)");
                }
            }
        }

        Console.WriteLine("\n=== Test Complete ===");
    }
}
