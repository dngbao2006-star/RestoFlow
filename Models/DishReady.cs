using System;
using System.Collections.Generic;
using System.Text;

namespace AppManagermentRestaurant.Models
{
    public class DishReady
    {
        public int Id { get; set; }

        public string DishName { get; set; }

        public string Image { get; set; }

        public int Quantity { get; set; }

        public int TableId { get; set; }

        public int TableNumber { get; set; }

        public string Status { get; set; }

        public string CreatedAt { get; set; }

        public int MenuItemId { get; set; }

        public int OrderId { get; set; }
    }
}
