namespace Caribou.Models
{
    using System;

    // Simplified version of OSMTag for cross-platform testing (no Grasshopper dependencies)
    public class OSMTag : IEquatable<OSMTag>
    {
        public OSMTag(string value)
        {
            this.Value = value.ToLower();
            this.Key = null;
            this.Name = value;
            this.Description = "";
        }

        public OSMTag(string key, string value)
        {
            this.Value = value.ToLower();
            this.Key = new OSMTag(key);
            this.Name = value;
            this.Description = "";
        }

        public string Value { get; set; }
        public OSMTag? Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            if (this.Key != null)
                return $"{this.Key.Value}={this.Value}";
            else
                return this.Value;
        }

        public override bool Equals(object? obj)
        {
            if (obj is OSMTag other)
                return Equals(other);
            return false;
        }

        public bool Equals(OSMTag? other)
        {
            if (other == null) return false;
            return this.Value == other.Value &&
                   ((this.Key == null && other.Key == null) ||
                    (this.Key != null && other.Key != null && this.Key.Equals(other.Key)));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Key?.GetHashCode() ?? 0);
        }
    }
}
