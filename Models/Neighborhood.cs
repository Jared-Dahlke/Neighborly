using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Neighborly.Models
{
    public class Neighborhood
    {
        public string ID { get; set; }
        public string NeighborhoodName { get; set; }       
        public string NeighborhoodCode { get; set; }
        public string NeighborhoodCategory { get; set; }
    }
}