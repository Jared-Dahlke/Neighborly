using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Neighborly.Models
{
    public class User
    {
        public string userID { get; set; }
        public string firstName { get; set; }
        public string userEmail { get; set; }
        public string joinDate { get; set; }
        public string phone { get; set; }
    }
}