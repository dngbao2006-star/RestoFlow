using System;
using System.Collections.Generic;
using System.Text;

namespace AppManagermentRestaurant.Models
{
    public class FirebaseChatMessage
    {
        public string SenderId { get; set; } = string.Empty;

        public string SenderName { get; set; } = string.Empty;

        public string SenderRole { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public bool IsSystem { get; set; }
    }
}
