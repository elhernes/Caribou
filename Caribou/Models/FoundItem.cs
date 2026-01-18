namespace Caribou.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a geometry form (way, node, or relation) that has been succesfully matched to a requested OSM Data
    /// It is stored within a RequestHandler and then parsed out to specific Grasshopper data (geometry/text).
    /// </summary>
    public struct FoundItem
    {
        public FoundItem(Dictionary<string, string> tags, List<Coord> coords)
        {
            this.Tags = new Dictionary<string, string>(tags);
            this.Coords = new List<Coord>(coords);
            this.Members = null;
            if (this.Coords.Count > 1)
            {
                this.Kind = OSMGeometryType.Way;
            }
            else
            {
                this.Kind = OSMGeometryType.Node;
            }
        }

        public FoundItem(Dictionary<string, string> tags, List<RelationMember> members)
        {
            this.Tags = new Dictionary<string, string>(tags);
            this.Coords = new List<Coord>();
            this.Members = new List<RelationMember>(members); // Make a copy to avoid clearing by reference
            this.Kind = OSMGeometryType.Relation;
        }

        public OSMGeometryType Kind { get; }
        public List<Coord> Coords { get; } // If only a single item then represents a node
        public Dictionary<string, string> Tags { get; } // All key:value pairs attached to the item
        public List<RelationMember> Members { get; } // For relations: list of members with roles (null for non-relations)
    }
}
