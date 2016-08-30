using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FacebookLoginASPnetWebForms.Models
{
    public class UserComment
    {
        public string created_time { get; set; }
        public string message { get; set; }
        public string id { get; set; }
        public NodeDestination from { get; set; }
    }
}