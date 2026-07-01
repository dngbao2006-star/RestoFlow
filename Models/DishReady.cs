using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AppManagermentRestaurant.Models
{
    public class DishReady
    {
        public int Id { get; set; }

        public string DishName { get; set; } = string.Empty;

        public string Image { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int TableId { get; set; }

        public int TableNumber { get; set; }

        public string Status { get; set; } = string.Empty;

        public string CreatedAt { get; set; } = string.Empty;

        public int MenuItemId { get; set; }

        public int OrderId { get; set; }

        [JsonIgnore]
        public Order? ParentOrder { get; set; }

        [JsonIgnore]
        public OrderItem? SourceItem { get; set; }
    }
}
