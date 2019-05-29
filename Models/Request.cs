using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Neighborly.Models
{
    public class Request
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Expiration { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public string NeighborhoodID { get; set; }
    }
}