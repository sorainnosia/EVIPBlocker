using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EVIPBlocker
{
    public class EventDetails
    {
        public DateTime dateAdded { get; set; }
        public string source { get; set; }
        public string data { get; set; }
        public string ip { get; set; }
        public string workstation { get; set; }
        public string type { get; set; } = "Fail";
    }
}
