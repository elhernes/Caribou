namespace Caribou.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Caribou.Processing;

    // Simplified version of RequestHandler for cross-platform testing (no Grasshopper dependencies)
    public class RequestHandler
    {
        public List<string> XmlPaths;
        public ParseRequest RequestedMetaData;
        public Coord MinBounds;
        public Coord MaxBounds;
        public List<Tuple<Coord, Coord>> AllBounds;

        public Dictionary<OSMTag, List<FoundItem>> FoundData;
        public List<string> FoundItemIds;

        public string WorkerId;
        public Action<string, double>? ReportProgress;
        public List<int>? LinesPerFile;

        public RequestHandler(List<string> providedXMLs, ParseRequest requestedMetaData, OSMGeometryType requestedType,
                              Action<string, double>? reportProgress, string workerId)
        {
            this.XmlPaths = providedXMLs;
            this.RequestedMetaData = requestedMetaData;

            this.FoundItemIds = new List<string>();
            this.FoundData = new Dictionary<OSMTag, List<FoundItem>>();
            foreach (OSMTag metaData in requestedMetaData.Requests)
            {
                this.FoundData[metaData] = new List<FoundItem>();
            }

            this.WorkerId = workerId;
            this.ReportProgress = reportProgress;
            if (providedXMLs.Count > 0 && providedXMLs[0].Length < 1000)
                this.LinesPerFile = ProgressReporting.GetLineLengthsForFiles(providedXMLs, requestedType);
        }

        public void AddWayIfMatchesRequest(string nodeId, Dictionary<string, string> nodeTags, List<Coord> coords)
        {
            var matches = RequestsThatWantItem(nodeId, nodeTags);
            if (matches.Count > 0)
            {
                this.FoundItemIds.Add(nodeId);
                foreach (var match in matches)
                {
                    AddItem(match, nodeTags, coords);
                }
            }
        }

        public void AddBuildingIfMatchesRequest(string nodeId, Dictionary<string, string> nodeTags, List<Coord> coords)
        {
            if (nodeTags.ContainsKey("building"))
                AddWayIfMatchesRequest(nodeId, nodeTags, coords);
        }

        public void AddNodeIfMatchesRequest(string nodeId, Dictionary<string, string> nodeTags, double lat, double lon)
        {
            var matches = RequestsThatWantItem(nodeId, nodeTags);
            if (matches.Count > 0)
            {
                this.FoundItemIds.Add(nodeId);
                var coords = new List<Coord>() { new Coord(lat, lon) };
                foreach (var match in matches)
                {
                    AddItem(match, nodeTags, coords);
                }
            }
        }

        public void AddRelationIfMatchesRequest(string relationId, Dictionary<string, string> relationTags, List<RelationMember> members)
        {
            var matches = RequestsThatWantItem(relationId, relationTags);
            if (matches.Count > 0)
            {
                this.FoundItemIds.Add(relationId);
                foreach (var match in matches)
                {
                    AddRelationItem(match, relationTags, members);
                }
            }
        }

        private void AddItem(OSMTag match, Dictionary<string, string> nodeTags, List<Coord> coords)
        {
            var newFind = new FoundItem(nodeTags, coords);
            this.FoundData[match].Add(newFind);
        }

        private void AddRelationItem(OSMTag match, Dictionary<string, string> relationTags, List<RelationMember> members)
        {
            var newFind = new FoundItem(relationTags, members);
            this.FoundData[match].Add(newFind);
        }

        private List<OSMTag> RequestsThatWantItem(string nodeId, Dictionary<string, string> tagsOfFoundNode)
        {
            var matches = new List<OSMTag>();
            var ci = CultureInfo.InvariantCulture;

            if (string.IsNullOrEmpty(nodeId) || this.FoundItemIds.Contains(nodeId))
            {
                return matches;
            }

            foreach (var request in this.RequestedMetaData.Requests)
            {
                var requestedKey = request.Key;
                var requestedValue = request.Value;

                if (requestedKey == null)
                {
                    if (tagsOfFoundNode.ContainsKey(requestedValue))
                    {
                        matches.Add(request);
                    }
                }
                else if (tagsOfFoundNode.ContainsKey(requestedKey.Value))
                {
                    var testValue = tagsOfFoundNode[requestedKey.Value];
                    if (testValue != null && testValue.ToLower(ci) == requestedValue.ToLower(ci))
                    {
                        matches.Add(request);
                    }
                }
            }

            return matches;
        }
    }
}
