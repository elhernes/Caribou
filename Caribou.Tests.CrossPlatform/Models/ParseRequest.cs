namespace Caribou.Models
{
    using System.Collections.Generic;
    using System.Linq;

    // Simplified version of ParseRequest for cross-platform testing
    public class ParseRequest
    {
        public List<OSMTag> Requests { get; set; }

        public ParseRequest(List<string> requestStrings)
        {
            this.Requests = new List<OSMTag>();
            foreach (var requestString in requestStrings)
            {
                var splitRequest = requestString.ToLower().Split('=');
                if (splitRequest.Length == 1)
                {
                    this.Requests.Add(new OSMTag(splitRequest[0]));
                }
                else if (splitRequest.Length == 2)
                {
                    this.Requests.Add(new OSMTag(splitRequest[0], splitRequest[1]));
                }
            }
        }

        public ParseRequest(List<OSMTag> requests)
        {
            this.Requests = requests;
        }
    }
}
