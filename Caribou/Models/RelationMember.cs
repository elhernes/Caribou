namespace Caribou.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a member of an OSM relation with role information.
    /// </summary>
    public struct RelationMember
    {
        public RelationMember(string role, List<Coord> coords, string wayRef)
        {
            this.Role = role;
            this.Coords = new List<Coord>(coords);
            this.WayRef = wayRef;
        }

        /// <summary>Gets the role of this member in the relation (e.g., "outer", "inner", or "").</summary>
        public string Role { get; }

        /// <summary>Gets the coordinates that make up this member's geometry.</summary>
        public List<Coord> Coords { get; }

        /// <summary>Gets the original way reference ID for debugging purposes.</summary>
        public string WayRef { get; }
    }
}
