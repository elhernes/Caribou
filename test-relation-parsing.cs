// Quick test to verify relation parsing
// Compile and run: csc /r:Caribou.dll test-relation-parsing.cs && mono test-relation-parsing.exe

using System;
using System.Collections.Generic;
using Caribou.Models;
using Caribou.Processing;

class TestRelationParsing
{
    static void Main()
    {
        var multipolygonXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<osm version=""0.6"">
  <bounds minlat=""-37.82"" minlon=""144.96"" maxlat=""-37.81"" maxlon=""144.97""/>

  <!-- Outer boundary nodes -->
  <node id=""101"" lat=""-37.8150"" lon=""144.9630""/>
  <node id=""102"" lat=""-37.8150"" lon=""144.9640""/>
  <node id=""103"" lat=""-37.8160"" lon=""144.9640""/>
  <node id=""104"" lat=""-37.8160"" lon=""144.9630""/>

  <!-- Inner hole nodes -->
  <node id=""201"" lat=""-37.8153"" lon=""144.9633""/>
  <node id=""202"" lat=""-37.8153"" lon=""144.9637""/>
  <node id=""203"" lat=""-37.8157"" lon=""144.9637""/>
  <node id=""204"" lat=""-37.8157"" lon=""144.9633""/>

  <!-- Outer way -->
  <way id=""1001"">
    <nd ref=""101""/>
    <nd ref=""102""/>
    <nd ref=""103""/>
    <nd ref=""104""/>
    <nd ref=""101""/>
  </way>

  <!-- Inner way (hole) -->
  <way id=""1002"">
    <nd ref=""201""/>
    <nd ref=""202""/>
    <nd ref=""203""/>
    <nd ref=""204""/>
    <nd ref=""201""/>
  </way>

  <!-- Multipolygon relation -->
  <relation id=""2001"">
    <member type=""way"" ref=""1001"" role=""outer""/>
    <member type=""way"" ref=""1002"" role=""inner""/>
    <tag k=""type"" v=""multipolygon""/>
    <tag k=""building"" v=""yes""/>
    <tag k=""name"" v=""Building with Courtyard""/>
  </relation>
</osm>";

        var xmlPaths = new List<string> { multipolygonXml };
        var buildingFeatures = new ParseRequest(new List<OSMTag> { new OSMTag("building") });

        var request = new RequestHandler(xmlPaths, buildingFeatures, OSMGeometryType.Relation,
            (s, d) => { }, "test");

        ParseViaXMLReader.FindItemsByTag(ref request, OSMGeometryType.Relation, pathIsContents: true);

        Console.WriteLine($"Found {request.FoundData.Count} result sets");
        foreach (var kvp in request.FoundData)
        {
            Console.WriteLine($"Tag {kvp.Key.Key}={kvp.Key.Value}: {kvp.Value.Count} items");
            foreach (var item in kvp.Value)
            {
                Console.WriteLine($"  - {item.Kind} with {item.Members?.Count ?? 0} members");
                if (item.Members != null)
                {
                    foreach (var member in item.Members)
                    {
                        Console.WriteLine($"    - {member.Role}: {member.WayRef} ({member.Coords.Count} coords)");
                    }
                }
            }
        }
    }
}
