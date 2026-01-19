namespace Caribou.Processing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Caribou.Models;

    /// <summary>
    /// Methods for parsing an XML file and extracting data that use XMLReader-based methods.
    /// </summary>
    public static class ParseViaXMLReader
    {
        private delegate void DispatchDelegate(XmlReader reader, ref RequestHandler request, 
                                               int fileIndex, bool onlyBuildings = false);
        private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        public static void FindItemsByTag(ref RequestHandler request, OSMGeometryType typeToFind, bool pathIsContents = false)
        {
            var dispatchForType = GetDispatchForType(typeToFind);
            bool onlyBuildings = false;
            if (typeToFind == OSMGeometryType.Building)
                onlyBuildings = true;

            GetBounds(ref request, pathIsContents);
            for (var i = 0; i < request.XmlPaths.Count; i++)
            {
                var xmlPath = request.XmlPaths[i];
                if (pathIsContents)
                {
                    using (XmlReader reader = XmlReader.Create(new StringReader(xmlPath)))
                    {
                        dispatchForType(reader, ref request, i, onlyBuildings); // Only used in testing
                    }
                }
                else
                {
                    using (XmlReader reader = XmlReader.Create(xmlPath))
                    {
                        dispatchForType(reader, ref request, i);
                    }
                }
            }
        }

        public static void FindNodesInXML(XmlReader reader, ref RequestHandler request, int fileIndex, bool onlyBuildings)
        {
            string currentNodeId = "";
            double currentLat = 0;
            double currentLon = 0;
            var currentNodeMetaData = new Dictionary<string, string>();
            var xli = (IXmlLineInfo)reader; // Used to track read progress
            var nodesCollected = 0;

            // Loop (linearly) through all tags. Keep track of each node's metadata and coords while looping through its tags.
            // When encountering the next node add the tracked data.
            while (reader.Read())
            {
                if (reader.IsStartElement())
                {
                    if (reader.Name == "node")
                    {
                        currentNodeId = reader.GetAttribute("id");
                        currentLat = Convert.ToDouble(reader.GetAttribute("lat"), CI);
                        currentLon = Convert.ToDouble(reader.GetAttribute("lon"), CI);
                    }
                    else if (reader.Name == "tag")
                    {
                        currentNodeMetaData[reader.GetAttribute("k").ToLower(CI)] = reader.GetAttribute("v");
                    }
                    else if (reader.Name == "way")
                    {
                         break;
                    }
                }
                else if (reader.Name == "node") // Closing out a node tag
                {
                    request.AddNodeIfMatchesRequest(currentNodeId, currentNodeMetaData, currentLat, currentLon);
                    currentNodeMetaData.Clear();
                    nodesCollected++;
                    if (nodesCollected % 3000 == 0) // roughly every half a second
                        ProgressReporting.Ping(xli.LineNumber, fileIndex, request);
                }
            }
        }

        public static void FindWaysInXML(XmlReader reader, ref RequestHandler request, int fileIndex, bool onlyBuildings)
        {
            string currentWayId = "";
            var currentWayMetaData = new Dictionary<string, string>();
            var currentWayNodes = new List<Coord>();
            var allNodes = new Dictionary<string, Coord>();
            var inANode = false; // Only needed for ways
            var xli = (IXmlLineInfo)reader; // Used to track read progress
            var waysCollected = 0;

            while (reader.Read())
            {
                if (reader.IsStartElement())
                {
                    if (reader.Name == "way")
                    {
                        currentWayId = reader.GetAttribute("id");
                        inANode = true; // Add tag data
                    }
                    else if (reader.Name == "node")
                    {
                        var nodeId = reader.GetAttribute("id");
                        allNodes[nodeId] = new Coord(
                            Convert.ToDouble(reader.GetAttribute("lat"), CI),
                            Convert.ToDouble(reader.GetAttribute("lon"), CI));
                    }
                    else if (inANode && reader.Name == "nd")
                    {
                        var ndId = reader.GetAttribute("ref");
                        currentWayNodes.Add(allNodes[ndId]);
                    }
                    else if (inANode && reader.Name == "tag")
                    {
                        currentWayMetaData[reader.GetAttribute("k").ToLower(CI)] = reader.GetAttribute("v");
                    }
                    else if (reader.Name == "relation")
                    {
                        break;
                    }
                }
                else
                {
                    inANode = false;
                    if (reader.Name == "way") // Closing out a way tag
                    {
                        // If finished looping over a prior node
                        if (onlyBuildings) 
                            request.AddBuildingIfMatchesRequest(currentWayId, currentWayMetaData, currentWayNodes); 
                        else
                            request.AddWayIfMatchesRequest(currentWayId, currentWayMetaData, currentWayNodes);

                        waysCollected += 1;
                        if (waysCollected % 2000 == 0)
                            ProgressReporting.Ping(xli.LineNumber, fileIndex, request);
                    }

                    currentWayMetaData.Clear();
                    currentWayNodes.Clear();
                }
            }
        }

        public static void FindRelationsInXML(XmlReader reader, ref RequestHandler request, int fileIndex, bool onlyBuildings)
        {
            // We need to get the XML path to create multiple readers for the three-pass approach
            // Since XmlReader is forward-only, we can't rewind it
            var xmlPathOrContent = request.XmlPaths[fileIndex];

            // Determine if we're reading from a file path or XML content string (for testing)
            // Check if it's a file that exists - if not, treat as XML content string
            bool isTestContent = !System.IO.File.Exists(xmlPathOrContent);

            var allNodes = new Dictionary<string, Coord>();
            var allWays = new Dictionary<string, List<Coord>>();
            var relationsCollected = 0;

            // FIRST PASS: Collect all nodes
            using (XmlReader pass1Reader = isTestContent
                ? XmlReader.Create(new StringReader(xmlPathOrContent))
                : XmlReader.Create(xmlPathOrContent))
            {
                while (pass1Reader.Read())
                {
                    if (pass1Reader.IsStartElement() && pass1Reader.Name == "node")
                    {
                        var nodeId = pass1Reader.GetAttribute("id");
                        allNodes[nodeId] = new Coord(
                            Convert.ToDouble(pass1Reader.GetAttribute("lat"), CI),
                            Convert.ToDouble(pass1Reader.GetAttribute("lon"), CI));
                    }
                }
            }

            // SECOND PASS: Collect all ways and their node references
            using (XmlReader pass2Reader = isTestContent
                ? XmlReader.Create(new StringReader(xmlPathOrContent))
                : XmlReader.Create(xmlPathOrContent))
            {
                var currentWayNodes = new List<Coord>();
                string currentWayId = "";
                bool inAWay = false;

                while (pass2Reader.Read())
                {
                    if (pass2Reader.IsStartElement())
                    {
                        if (pass2Reader.Name == "way")
                        {
                            currentWayId = pass2Reader.GetAttribute("id");
                            inAWay = true;
                            currentWayNodes.Clear();
                        }
                        else if (inAWay && pass2Reader.Name == "nd")
                        {
                            var ndId = pass2Reader.GetAttribute("ref");
                            if (allNodes.ContainsKey(ndId))
                                currentWayNodes.Add(allNodes[ndId]);
                        }
                    }
                    else if (pass2Reader.Name == "way")
                    {
                        inAWay = false;
                        if (!string.IsNullOrEmpty(currentWayId) && currentWayNodes.Count > 0)
                        {
                            allWays[currentWayId] = new List<Coord>(currentWayNodes);
                        }
                    }
                }
            }

            // THIRD PASS: Parse relations
            using (XmlReader pass3Reader = isTestContent
                ? XmlReader.Create(new StringReader(xmlPathOrContent))
                : XmlReader.Create(xmlPathOrContent))
            {
                var xli = (IXmlLineInfo)pass3Reader;
                string currentRelationId = "";
                var currentRelationMetaData = new Dictionary<string, string>();
                var currentRelationMembers = new List<RelationMember>();
                bool inARelation = false;
                int totalRelations = 0;
                int relationsWithWayMembers = 0;

                while (pass3Reader.Read())
                {
                    if (pass3Reader.IsStartElement())
                    {
                        if (pass3Reader.Name == "relation")
                        {
                            currentRelationId = pass3Reader.GetAttribute("id");
                            inARelation = true;
                            totalRelations++;
                        }
                        else if (inARelation && pass3Reader.Name == "member")
                        {
                            var memberType = pass3Reader.GetAttribute("type");
                            var memberRef = pass3Reader.GetAttribute("ref");
                            var memberRole = pass3Reader.GetAttribute("role") ?? "";

                            // Only handle way members for now (multipolygon/boundary support)
                            if (memberType == "way" && allWays.ContainsKey(memberRef))
                            {
                                var memberCoords = allWays[memberRef];
                                var member = new RelationMember(memberRole, memberCoords, memberRef);
                                currentRelationMembers.Add(member);
                            }
                        }
                        else if (inARelation && pass3Reader.Name == "tag")
                        {
                            currentRelationMetaData[pass3Reader.GetAttribute("k").ToLower(CI)] = pass3Reader.GetAttribute("v");
                        }
                    }
                    else
                    {
                        inARelation = false;
                        if (pass3Reader.Name == "relation")
                        {
                            // Only process if we have members (avoid empty relations)
                            if (currentRelationMembers.Count > 0)
                            {
                                relationsWithWayMembers++;
                                request.AddRelationIfMatchesRequest(
                                    currentRelationId,
                                    currentRelationMetaData,
                                    currentRelationMembers);
                            }

                            currentRelationMetaData.Clear();
                            currentRelationMembers.Clear();
                            relationsCollected++;
                            if (relationsCollected % 500 == 0)
                                ProgressReporting.Ping(xli.LineNumber, fileIndex, request);
                        }
                    }
                }

                // Store parsing statistics for diagnostic output
                request.RelationParsingStats = $"Parsed {allNodes.Count} nodes, {allWays.Count} ways, {totalRelations} relations ({relationsWithWayMembers} with way members)";
            }
        }

        // Retur n correct parser for each geometry type
        private static DispatchDelegate GetDispatchForType(OSMGeometryType typeToFind)
        {
            DispatchDelegate dispatchForType;
            if (typeToFind == OSMGeometryType.Node)
                dispatchForType = FindNodesInXML;
            else if (typeToFind == OSMGeometryType.Way || typeToFind == OSMGeometryType.Building)
                dispatchForType = FindWaysInXML;
            else if (typeToFind == OSMGeometryType.Relation)
                dispatchForType = FindRelationsInXML;
            else
                dispatchForType = null; // Necessary to prevent below paths thinking variable not set
            return dispatchForType;
        }

        // Identify a minimum and maximum boundary that encompasses all of the provided files' boundaries
        public static void GetBounds(ref RequestHandler result, bool readPathAsContents = false)
        {
            double? currentMinLat = null;
            double? currentMinLon = null;
            double? currentMaxLat = null;
            double? currentMaxLon = null;
            result.AllBounds = new List<Tuple<Coord, Coord>>();

            foreach (string xmlPath in result.XmlPaths)
            {
                XmlReader reader;
                if (readPathAsContents) 
                    reader = XmlReader.Create(new StringReader(xmlPath)); // Only used in testing
                else
                    reader = XmlReader.Create(xmlPath);
   
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        if (reader.Name == "bounds")
                        {
                            var minLat = Convert.ToDouble(reader.GetAttribute("minlat"), CI);
                            var minLon = Convert.ToDouble(reader.GetAttribute("minlon"), CI);
                            var maxLat = Convert.ToDouble(reader.GetAttribute("maxlat"), CI);
                            var maxLon = Convert.ToDouble(reader.GetAttribute("maxlon"), CI);
                            var bounds = new Tuple<Coord, Coord>(new Coord(minLat, minLon), new Coord(maxLat, maxLon));
                            result.AllBounds.Add(bounds);

                            CheckBounds(bounds, ref currentMinLat, ref currentMinLon, ref currentMaxLat, ref currentMaxLon);
                            break;
                        }
                    }
                }
            }

            result.MinBounds = new Coord(currentMinLat.Value, currentMinLon.Value);
            result.MaxBounds = new Coord(currentMaxLat.Value, currentMaxLon.Value);
        }

        public static void CheckBounds(Tuple<Coord, Coord> bounds, 
            ref double? currentMinLat, ref double? currentMinLon, ref double? currentMaxLat, ref double? currentMaxLon)
        {
            var boundsMinLat = bounds.Item1.Latitude;
            if (!currentMinLat.HasValue || boundsMinLat < currentMinLat)
            {
                currentMinLat = boundsMinLat;
            }

            var boundsMinLon = bounds.Item1.Longitude;
            if (!currentMinLon.HasValue || boundsMinLon < currentMinLon)
            {
                currentMinLon = boundsMinLon;
            }

            var boundsMaxLat = bounds.Item2.Latitude;
            if (!currentMaxLat.HasValue || boundsMaxLat > currentMaxLat)
            {
                currentMaxLat = boundsMaxLat;
            }

            var boundsMaxLon = bounds.Item2.Longitude;
            if (!currentMaxLon.HasValue || boundsMaxLon > currentMaxLon)
            {
                currentMaxLon = boundsMaxLon;
            }
        }

        /// <summary>Extracts all unique tags from an OSM file grouped by key</summary>
        /// <returns>Dictionary where key is the OSM key (e.g. "building") and value is a HashSet of all values found</returns>
        public static Dictionary<string, HashSet<string>> ExtractAllUniqueTags(string xmlPathOrContent)
        {
            var tagsByKey = new Dictionary<string, HashSet<string>>();
            bool isTestContent = !System.IO.File.Exists(xmlPathOrContent);

            using (XmlReader reader = isTestContent
                ? XmlReader.Create(new StringReader(xmlPathOrContent))
                : XmlReader.Create(xmlPathOrContent))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement() && reader.Name == "tag")
                    {
                        string key = reader.GetAttribute("k").ToLower(CI);
                        string value = reader.GetAttribute("v");

                        if (!tagsByKey.ContainsKey(key))
                            tagsByKey[key] = new HashSet<string>();

                        tagsByKey[key].Add(value);
                    }
                }
            }

            return tagsByKey;
        }

        /// <summary>Extracts all unique tags from an OSM file grouped by key, filtered by element type</summary>
        /// <param name="xmlPathOrContent">Path to OSM file or XML content string</param>
        /// <param name="elementType">Filter to only include tags from this element type (null = all types)</param>
        /// <returns>Dictionary where key is the OSM key and value is a HashSet of all values found on the specified element type</returns>
        public static Dictionary<string, HashSet<string>> ExtractAllUniqueTagsFiltered(string xmlPathOrContent, OSMGeometryType? elementType = null)
        {
            var tagsByKey = new Dictionary<string, HashSet<string>>();
            bool isTestContent = !System.IO.File.Exists(xmlPathOrContent);

            using (XmlReader reader = isTestContent
                ? XmlReader.Create(new StringReader(xmlPathOrContent))
                : XmlReader.Create(xmlPathOrContent))
            {
                string currentElementType = null;
                bool inElement = false;

                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        if (reader.Name == "node" || reader.Name == "way" || reader.Name == "relation")
                        {
                            currentElementType = reader.Name;
                            inElement = true;
                        }
                        else if (inElement && reader.Name == "tag")
                        {
                            // Check if we should include tags from this element type
                            bool shouldInclude = !elementType.HasValue ||
                                (elementType.Value == OSMGeometryType.Node && currentElementType == "node") ||
                                (elementType.Value == OSMGeometryType.Way && currentElementType == "way") ||
                                (elementType.Value == OSMGeometryType.Relation && currentElementType == "relation");

                            if (shouldInclude)
                            {
                                string key = reader.GetAttribute("k").ToLower(CI);
                                string value = reader.GetAttribute("v");

                                if (!tagsByKey.ContainsKey(key))
                                    tagsByKey[key] = new HashSet<string>();

                                tagsByKey[key].Add(value);
                            }
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (reader.Name == "node" || reader.Name == "way" || reader.Name == "relation")
                        {
                            inElement = false;
                            currentElementType = null;
                        }
                    }
                }
            }

            return tagsByKey;
        }
    }
}
