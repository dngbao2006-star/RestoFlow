using System;
using System.Collections.Generic;
using System.Text;

namespace AppManagermentRestaurant.Models
{
    public class PresenceModel
    {
        public string HoTen { get; set; } = "";
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }
        public string Device { get; set; } = "";
        public string SessionId { get; set; } = "";
    }
}
