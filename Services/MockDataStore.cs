using System.Collections.ObjectModel;
using AppManagermentRestaurant.Models;

namespace AppManagermentRestaurant.Services;

public class MockDataStore : IDataStore
{
    public Task SeedAsync(AppContext context)
    {
        Seed(context);
        return Task.CompletedTask;
    }

    private void Seed(AppContext context)
    {
        // TODO: [BACKEND] - Chỗ này thay bằng bước gọi API để lấy toàn bộ dữ liệu khởi tạo.
        //SeedStaff(context);
        SeedMenu(context);
        SeedTables(context);
        SeedOrders(context);
        SeedNotifications(context);
        //SeedChatMessages(context);
        SeedInvoices(context);
        SeedRevenue(context);
        SeedTopDishes(context);
    }

    //private static void SeedStaff(AppContext context)
    //{
    //    context.StaffMembers.Add(new Staff
    //    {
    //        Id = 1,
    //        Name = "Nguyen Minh Quan",
    //        Role = StaffRole.Manager,
    //        Email = "quan.manager@goldenplate.vn",
    //        Phone = "0900 111 222",
    //        Status = StaffStatus.Active,
    //        LastLogin = DateTime.Now.AddHours(-2),
    //        JoinDate = DateTime.Now.AddYears(-3),
    //        Permissions = ["dashboard", "hr", "menu", "revenue", "system"]
    //    });

    //    context.StaffMembers.Add(new Staff
    //    {
    //        Id = 2,
    //        Name = "Tran Minh Tuan",
    //        Role = StaffRole.Staff,
    //        Email = "tuan.staff@goldenplate.vn",
    //        Phone = "0900 222 333",
    //        Status = StaffStatus.Active,
    //        LastLogin = DateTime.Now.AddHours(-1),
    //        JoinDate = DateTime.Now.AddYears(-2),
    //        Permissions = ["tables", "orders", "payment"]
    //    });

    //    context.StaffMembers.Add(new Staff
    //    {
    //        Id = 3,
    //        Name = "Le Thi Lan Anh",
    //        Role = StaffRole.Staff,
    //        Email = "lananh.staff@goldenplate.vn",
    //        Phone = "0900 333 444",
    //        Status = StaffStatus.Active,
    //        LastLogin = DateTime.Now.AddHours(-4),
    //        JoinDate = DateTime.Now.AddMonths(-14),
    //        Permissions = ["tables", "orders", "chat"]
    //    });

    //    context.StaffMembers.Add(new Staff
    //    {
    //        Id = 4,
    //        Name = "Pham Van Duc",
    //        Role = StaffRole.Staff,
    //        Email = "duc.staff@goldenplate.vn",
    //        Phone = "0900 444 555",
    //        Status = StaffStatus.Inactive,
    //        LastLogin = DateTime.Now.AddDays(-7),
    //        JoinDate = DateTime.Now.AddYears(-1),
    //        Permissions = ["tables"]
    //    });

    //    context.StaffMembers.Add(new Staff
    //    {
    //        Id = 5,
    //        Name = "Hoang Thi Mai",
    //        Role = StaffRole.Staff,
    //        Email = "mai.staff@goldenplate.vn",
    //        Phone = "0900 555 666",
    //        Status = StaffStatus.Locked,
    //        LastLogin = DateTime.Now.AddMonths(-2),
    //        JoinDate = DateTime.Now.AddYears(-4),
    //        Permissions = ["tables", "orders"]
    //    });

    //    context.CurrentUser = context.StaffMembers[1];
    //}

    private static void SeedMenu(AppContext context)
    {
        var menuItems = new List<FoodItem>
        {
            new() { Id = 1, Name = "Truffle Mushroom Bruschetta", Category = "Appetizers", Price = 85000, Description = "Sourdough, truffle oil, wild mushrooms, parmesan.", Image = "https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?auto=format&fit=crop&w=900&q=80" },
            new() { Id = 2, Name = "Crispy Calamari", Category = "Appetizers", Price = 90000, Description = "Lightly battered squid, lemon aioli.", Image = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=900&q=80" },
            new() { Id = 3, Name = "Seared Salmon Bowl", Category = "Main Course", Price = 225000, Description = "Herb rice, grilled vegetables, citrus glaze.", Image = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=900&q=80" },
            new() { Id = 4, Name = "Signature Ribeye", Category = "Main Course", Price = 320000, Description = "Prime ribeye, rosemary butter, roasted potato.", Image = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=900&q=80" },
            new() { Id = 5, Name = "Beef Pho Soup", Category = "Soups", Price = 95000, Description = "Slow-cooked broth with rice noodles and herbs.", Image = "https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?auto=format&fit=crop&w=900&q=80", OutOfStock = true, Available = false },
            new() { Id = 6, Name = "Seafood Laksa", Category = "Soups", Price = 165000, Description = "Coconut broth, prawns, squid, fresh herbs.", Image = "https://images.unsplash.com/photo-1467003909585-2f8a72700288?auto=format&fit=crop&w=900&q=80" },
            new() { Id = 7, Name = "Garlic Butter Prawns", Category = "Seafood", Price = 210000, Description = "Sizzling prawns, herb butter, lemon.", Image = "https://images.unsplash.com/photo-1467003909585-2f8a72700288?auto=format&fit=crop&w=900&q=80" },
            new() { Id = 8, Name = "Grilled Sea Bass", Category = "Seafood", Price = 245000, Description = "Char-grilled sea bass, salsa verde.", Image = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=900&q=80" },
            new() { Id = 9, Name = "Vanilla Bean Panna Cotta", Category = "Desserts", Price = 75000, Description = "Vanilla cream, berry compote.", Image = "https://images.unsplash.com/photo-1505253216365-8c26c4f21b4c?auto=format&fit=crop&w=900&q=80" },
            new() { Id = 10, Name = "Chocolate Molten Cake", Category = "Desserts", Price = 80000, Description = "Warm chocolate cake, vanilla ice cream.", Image = "https://images.unsplash.com/photo-1505253216365-8c26c4f21b4c?auto=format&fit=crop&w=900&q=80" },
            new() { Id = 11, Name = "Signature Iced Latte", Category = "Drinks", Price = 55000, Description = "Single origin espresso, fresh milk.", Image = "https://images.unsplash.com/photo-1509042239860-f550ce710b93?auto=format&fit=crop&w=900&q=80" },
            new() { Id = 12, Name = "Lemongrass Sparkling", Category = "Drinks", Price = 45000, Description = "Lemongrass syrup, soda, lime.", Image = "https://images.unsplash.com/photo-1509042239860-f550ce710b93?auto=format&fit=crop&w=900&q=80" }
        };

        foreach (var item in menuItems)
        {
            context.MenuItems.Add(item);
        }
    }

    private static void SeedTables(AppContext context)
    {
        var tables = new List<Table>
        {
            // Có khách (Đã gọi món)
            new() { Id = 1, Number = 1, Floor = "Ground Floor", Status = TableStatus.Occupied, Capacity = 4, CurrentOrderId = 101, ArrivalTime = DateTime.Now.AddMinutes(-45), HasOrdered = true, OrderItemCount = 3, OrderTotal = "310,000đ" },
            new() { Id = 2, Number = 2, Floor = "Ground Floor", Status = TableStatus.Occupied, Capacity = 2, CurrentOrderId = 106, ArrivalTime = DateTime.Now.AddMinutes(-70), HasOrdered = true, OrderItemCount = 2, OrderTotal = "140,000đ" },
            
            // Đặt trước
            new() { Id = 3, Number = 3, Floor = "Ground Floor", Status = TableStatus.Reserved, Capacity = 4, ReservedFor = "Chị Linh", ReservedAt = DateTime.Now.AddHours(2) },
            
            // Có khách (Chưa gọi món)
            new() { Id = 4, Number = 4, Floor = "Ground Floor", Status = TableStatus.Occupied, Capacity = 6, ArrivalTime = DateTime.Now.AddMinutes(-5), HasOrdered = false },
            
            // Các bàn trống và cần dọn
            new() { Id = 5, Number = 5, Floor = "Ground Floor", Status = TableStatus.NeedsClearing, Capacity = 4 },
            new() { Id = 6, Number = 6, Floor = "Ground Floor", Status = TableStatus.Occupied, Capacity = 2, CurrentOrderId = 107, ArrivalTime = DateTime.Now.AddMinutes(-20), HasOrdered = true, OrderItemCount = 2, OrderTotal = "165,000đ" },
            new() { Id = 7, Number = 7, Floor = "Ground Floor", Status = TableStatus.Available, Capacity = 4 },
            new() { Id = 8, Number = 8, Floor = "Ground Floor", Status = TableStatus.Occupied, Capacity = 6, CurrentOrderId = 103, ArrivalTime = DateTime.Now.AddMinutes(-10), HasOrdered = true, OrderItemCount = 2, OrderTotal = "335,000đ" },
            
            // Tầng 2
            new() { Id = 9, Number = 9, Floor = "Second Floor", Status = TableStatus.Occupied, Capacity = 4, CurrentOrderId = 108, ArrivalTime = DateTime.Now.AddMinutes(-60), HasOrdered = true, OrderItemCount = 3, OrderTotal = "470,000đ" },
            new() { Id = 10, Number = 10, Floor = "Second Floor", Status = TableStatus.Occupied, Capacity = 4, CurrentOrderId = 104, ArrivalTime = DateTime.Now.AddMinutes(-15), HasOrdered = false },
            new() { Id = 11, Number = 11, Floor = "Second Floor", Status = TableStatus.Reserved, Capacity = 2, ReservedFor = "Anh Kim", ReservedAt = DateTime.Now.AddHours(1) },
            new() { Id = 12, Number = 12, Floor = "Second Floor", Status = TableStatus.Available, Capacity = 6 },
            new() { Id = 13, Number = 13, Floor = "Second Floor", Status = TableStatus.NeedsClearing, Capacity = 2 },
            new() { Id = 14, Number = 14, Floor = "Second Floor", Status = TableStatus.Occupied, Capacity = 4, CurrentOrderId = 109, ArrivalTime = DateTime.Now.AddMinutes(-30), HasOrdered = true, OrderItemCount = 2, OrderTotal = "455,000đ" },
            
            // Sân thượng
            new() { Id = 15, Number = 15, Floor = "Garden", Status = TableStatus.Available, Capacity = 6 },
            new() { Id = 16, Number = 16, Floor = "Garden", Status = TableStatus.Occupied, Capacity = 4, CurrentOrderId = 105, ArrivalTime = DateTime.Now.AddMinutes(-55), HasOrdered = true, OrderItemCount = 2, OrderTotal = "365,000đ" },
            new() { Id = 17, Number = 17, Floor = "Garden", Status = TableStatus.Reserved, Capacity = 2, ReservedFor = "Chú Bảo", ReservedAt = DateTime.Now.AddHours(3) },
            new() { Id = 18, Number = 18, Floor = "Garden", Status = TableStatus.Available, Capacity = 4 }
        };

        foreach (var table in tables)
        {
            context.Tables.Add(table);
        }
    }

    private static void SeedOrders(AppContext context)
    {
        var menuItems = context.MenuItems.ToList();

        context.Orders.Add(new Order
        {
            Id = 101,
            TableId = 1,
            TableNumber = 1,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-45),
            ServerName = "Tran Minh Tuan",
            ServerId = "2",
            Discount = 0,
            PaymentMethod = PaymentMethod.Cash,
            Items =
            [
                new() { Id = 1, MenuItemId = 1, Name = "Truffle Mushroom Bruschetta", Price = 85000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[0].Image },
                new() { Id = 2, MenuItemId = 3, Name = "Seared Salmon Bowl", Price = 225000, Quantity = 2, Status = DishStatus.Ready, Image = menuItems[2].Image }
            ]
        });

        context.Orders.Add(new Order
        {
            Id = 102,
            TableId = 4,
            TableNumber = 4,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-30),
            ServerName = "Le Thi Lan Anh",
            ServerId = "3",
            Discount = 15000,
            PaymentMethod = PaymentMethod.Qr,
            Items =
            [
                new() { Id = 3, MenuItemId = 7, Name = "Garlic Butter Prawns", Price = 210000, Quantity = 1, Status = DishStatus.Preparing, Image = menuItems[6].Image },
                new() { Id = 4, MenuItemId = 10, Name = "Chocolate Molten Cake", Price = 80000, Quantity = 2, Status = DishStatus.Pending, Image = menuItems[9].Image }
            ]
        });

        context.Orders.Add(new Order
        {
            Id = 103,
            TableId = 8,
            TableNumber = 8,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-20),
            ServerName = "Tran Minh Tuan",
            ServerId = "2",
            Discount = 0,
            PaymentMethod = PaymentMethod.Cash,
            Items =
            [
                new() { Id = 5, MenuItemId = 2, Name = "Crispy Calamari", Price = 90000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[1].Image },
                new() { Id = 6, MenuItemId = 8, Name = "Grilled Sea Bass", Price = 245000, Quantity = 1, Status = DishStatus.Ready, Image = menuItems[7].Image }
            ]
        });

        context.Orders.Add(new Order
        {
            Id = 104,
            TableId = 10,
            TableNumber = 10,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-10),
            ServerName = "Le Thi Lan Anh",
            ServerId = "3",
            Discount = 0,
            PaymentMethod = PaymentMethod.Qr,
            Items =
            [
                new() { Id = 7, MenuItemId = 11, Name = "Signature Iced Latte", Price = 55000, Quantity = 2, Status = DishStatus.Pending, Image = menuItems[10].Image },
                new() { Id = 8, MenuItemId = 9, Name = "Vanilla Bean Panna Cotta", Price = 75000, Quantity = 1, Status = DishStatus.Preparing, Image = menuItems[8].Image }
            ]
        });

        context.Orders.Add(new Order
        {
            Id = 105,
            TableId = 16,
            TableNumber = 16,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-55),
            ServerName = "Tran Minh Tuan",
            ServerId = "2",
            Discount = 50000,
            PaymentMethod = PaymentMethod.Cash,
            Items =
            [
                new() { Id = 9, MenuItemId = 4, Name = "Signature Ribeye", Price = 320000, Quantity = 1, Status = DishStatus.Ready, Image = menuItems[3].Image },
                new() { Id = 10, MenuItemId = 12, Name = "Lemongrass Sparkling", Price = 45000, Quantity = 2, Status = DishStatus.Served, Image = menuItems[11].Image }
            ]
        });

        context.Orders.Add(new Order
        {
            Id = 106,
            TableId = 2,
            TableNumber = 2,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-70),
            ServerName = "Tran Minh Tuan",
            ServerId = "2",
            Discount = 0,
            PaymentMethod = PaymentMethod.Cash,
            Items =
            [
                new() { Id = 11, MenuItemId = 1, Name = "Truffle Mushroom Bruschetta", Price = 85000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[0].Image },
                new() { Id = 12, MenuItemId = 11, Name = "Signature Iced Latte", Price = 55000, Quantity = 2, Status = DishStatus.Served, Image = menuItems[10].Image }
            ]
        });

        context.Orders.Add(new Order
        {
            Id = 107,
            TableId = 6,
            TableNumber = 6,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-85),
            ServerName = "Le Thi Lan Anh",
            ServerId = "3",
            Discount = 25000,
            PaymentMethod = PaymentMethod.Qr,
            Items =
            [
                new() { Id = 13, MenuItemId = 2, Name = "Crispy Calamari", Price = 90000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[1].Image },
                new() { Id = 14, MenuItemId = 9, Name = "Vanilla Bean Panna Cotta", Price = 75000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[8].Image }
            ]
        });

        context.Orders.Add(new Order
        {
            Id = 108,
            TableId = 9,
            TableNumber = 9,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-60),
            ServerName = "Tran Minh Tuan",
            ServerId = "2",
            Discount = 0,
            PaymentMethod = PaymentMethod.Cash,
            Items =
            [
                new() { Id = 15, MenuItemId = 3, Name = "Seared Salmon Bowl", Price = 225000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[2].Image },
                new() { Id = 16, MenuItemId = 6, Name = "Seafood Laksa", Price = 165000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[5].Image },
                new() { Id = 17, MenuItemId = 10, Name = "Chocolate Molten Cake", Price = 80000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[9].Image }
            ]
        });

        context.Orders.Add(new Order
        {
            Id = 109,
            TableId = 14,
            TableNumber = 14,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-50),
            ServerName = "Le Thi Lan Anh",
            ServerId = "3",
            Discount = 0,
            PaymentMethod = PaymentMethod.Qr,
            Items =
            [
                new() { Id = 18, MenuItemId = 7, Name = "Garlic Butter Prawns", Price = 210000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[6].Image },
                new() { Id = 19, MenuItemId = 8, Name = "Grilled Sea Bass", Price = 245000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[7].Image }
            ]
        });

        context.OrderHistory.Add(new Order
        {
            Id = 201,
            TableId = 6,
            TableNumber = 6,
            Status = OrderStatus.Paid,
            CreatedAt = DateTime.Now.AddDays(-1),
            ServerName = "Tran Minh Tuan",
            ServerId = "2",
            Discount = 20000,
            PaymentMethod = PaymentMethod.Cash,
            Items =
            [
                new() { Id = 11, MenuItemId = 3, Name = "Seared Salmon Bowl", Price = 225000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[2].Image },
                new() { Id = 12, MenuItemId = 11, Name = "Signature Iced Latte", Price = 55000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[10].Image }
            ]
        });

        context.OrderHistory.Add(new Order
        {
            Id = 202,
            TableId = 12,
            TableNumber = 12,
            Status = OrderStatus.Paid,
            CreatedAt = DateTime.Now.AddDays(-3),
            ServerName = "Le Thi Lan Anh",
            ServerId = "3",
            Discount = 0,
            PaymentMethod = PaymentMethod.Qr,
            Items =
            [
                new() { Id = 13, MenuItemId = 7, Name = "Garlic Butter Prawns", Price = 210000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[6].Image },
                new() { Id = 14, MenuItemId = 9, Name = "Vanilla Bean Panna Cotta", Price = 75000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[8].Image }
            ]
        });

        context.OrderHistory.Add(new Order
        {
            Id = 203,
            TableId = 15,
            TableNumber = 15,
            Status = OrderStatus.Paid,
            CreatedAt = DateTime.Now.AddDays(-4),
            ServerName = "Tran Minh Tuan",
            ServerId = "2",
            Discount = 30000,
            PaymentMethod = PaymentMethod.Cash,
            Items =
            [
                new() { Id = 15, MenuItemId = 2, Name = "Crispy Calamari", Price = 90000, Quantity = 2, Status = DishStatus.Served, Image = menuItems[1].Image },
                new() { Id = 16, MenuItemId = 12, Name = "Lemongrass Sparkling", Price = 45000, Quantity = 2, Status = DishStatus.Served, Image = menuItems[11].Image }
            ]
        });
    }

    private static void SeedNotifications(AppContext context)
    {
        context.Notifications.Add(new Notification
        {
            Id = 1,
            Type = NotificationType.Success,
            Title = "Món đã sẵn sàng",
            Message = "Bàn 8 - Grilled Sea Bass đã sẵn sàng phục vụ.",
            Timestamp = DateTime.Now.AddMinutes(-5),
            Read = false,
            TableId = 8,
            OrderId = 103
        });

        context.Notifications.Add(new Notification
        {
            Id = 2,
            Type = NotificationType.Warning,
            Title = "Cần dọn bàn",
            Message = "Bàn 5 đã được đánh dấu cần dọn.",
            Timestamp = DateTime.Now.AddMinutes(-12),
            Read = false,
            TableId = 5
        });

        context.Notifications.Add(new Notification
        {
            Id = 3,
            Type = NotificationType.Info,
            Title = "Cập nhật kho",
            Message = "Món Beef Pho Soup đã hết hàng.",
            Timestamp = DateTime.Now.AddMinutes(-40),
            Read = true
        });

        context.Notifications.Add(new Notification
        {
            Id = 4,
            Type = NotificationType.Warning,
            Title = "Đặt bàn VIP",
            Message = "Bàn 3 đã được đặt lúc 19:00.",
            Timestamp = DateTime.Now.AddMinutes(-65),
            Read = true
        });
    }

    //private static void SeedChatMessages(AppContext context)
    //{
    //    context.ChatMessages.Add(new ChatMessage
    //    {
    //        Id = 1,
    //        SenderId = 0,
    //        SenderName = "Hệ thống",
    //        SenderRole = "System",
    //        Message = "Tóm tắt ca: Có khách VIP lúc 19:00.",
    //        Timestamp = DateTime.Now.AddMinutes(-90),
    //        IsSystem = true,
    //        IsRead = true
    //    });

    //    context.ChatMessages.Add(new ChatMessage
    //    {
    //        Id = 2,
    //        SenderId = 1,
    //        SenderName = "Nguyen Minh Quan",
    //        SenderRole = "Manager",
    //        Message = "Nhớ dọn khu sân vườn trước 18:00 nhé.",
    //        Timestamp = DateTime.Now.AddMinutes(-60),
    //        IsSystem = false,
    //        IsMine = false,
    //        IsRead = false
    //    });

    //    context.ChatMessages.Add(new ChatMessage
    //    {
    //        Id = 3,
    //        SenderId = 3,
    //        SenderName = "Le Thi Lan Anh",
    //        SenderRole = "Staff",
    //        Message = "Bàn 10 đang xin menu tráng miệng.",
    //        Timestamp = DateTime.Now.AddMinutes(-40),
    //        IsSystem = false,
    //        IsMine = false,
    //        IsRead = true
    //    });

    //    context.ChatMessages.Add(new ChatMessage
    //    {
    //        Id = 4,
    //        SenderId = 2,
    //        SenderName = "Tran Minh Tuan",
    //        SenderRole = "Staff",
    //        Message = "Ok, mình mang ra ngay.",
    //        Timestamp = DateTime.Now.AddMinutes(-38),
    //        IsSystem = false,
    //        IsMine = true,
    //        IsRead = true
    //    });

    //    context.ChatMessages.Add(new ChatMessage
    //    {
    //        Id = 5,
    //        SenderId = 1,
    //        SenderName = "Nguyen Minh Quan",
    //        SenderRole = "Manager",
    //        Message = "Nhắc lại: báo tình trạng nguyên liệu trước 15:00.",
    //        Timestamp = DateTime.Now.AddMinutes(-15),
    //        IsSystem = false,
    //        IsMine = false,
    //        IsRead = false
    //    });
    //}

    private static void SeedInvoices(AppContext context)
    {
        context.Invoices.Add(new Invoice
        {
            Id = 8001,
            OrderId = 201,
            TableNumber = 6,
            ServerName = "Tran Minh Tuan",
            CreatedAt = DateTime.Now.AddDays(-1),
            PaymentMethod = PaymentMethod.Cash,
            Discount = 20000,
            Total = 260000,
            Items = context.OrderHistory[0].Items
        });

        context.Invoices.Add(new Invoice
        {
            Id = 8002,
            OrderId = 202,
            TableNumber = 12,
            ServerName = "Le Thi Lan Anh",
            CreatedAt = DateTime.Now.AddDays(-3),
            PaymentMethod = PaymentMethod.Qr,
            Discount = 0,
            Total = 285000,
            Items = context.OrderHistory[1].Items
        });

        context.Invoices.Add(new Invoice
        {
            Id = 8003,
            OrderId = 203,
            TableNumber = 15,
            ServerName = "Tran Minh Tuan",
            CreatedAt = DateTime.Now.AddDays(-4),
            PaymentMethod = PaymentMethod.Cash,
            Discount = 30000,
            Total = 240000,
            Items = context.OrderHistory[2].Items
        });

        context.Invoices.Add(new Invoice
        {
            Id = 8004,
            OrderId = 104,
            TableNumber = 10,
            ServerName = "Le Thi Lan Anh",
            CreatedAt = DateTime.Now.AddDays(-5),
            PaymentMethod = PaymentMethod.Qr,
            Discount = 0,
            Total = 185000,
            Items = context.Orders[3].Items
        });
    }

    private static void SeedRevenue(AppContext context)
    {
        var hourlyLabels =
            new[]
            {
                "08:00", "09:00", "10:00", "11:00", "12:00", "13:00", "14:00",
                "15:00", "16:00", "17:00", "18:00", "19:00", "20:00", "21:00"
            };

        var hourlyValues = new[] { 1.2m, 1.4m, 1.6m, 2.2m, 3.8m, 4.5m, 3.2m, 2.1m, 1.9m, 2.5m, 4.1m, 5.2m, 4.8m, 3.6m };

        for (var index = 0; index < hourlyLabels.Length; index++)
        {
            context.RevenueDaily.Add(new RevenuePoint
            {
                Label = hourlyLabels[index],
                Value = hourlyValues[index] * 1000000
            });
        }

        var weeklyLabels = new[] { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };
        var weeklyValues = new[] { 18m, 20m, 22m, 19m, 26m, 32m, 28m };

        for (var index = 0; index < weeklyLabels.Length; index++)
        {
            context.RevenueWeekly.Add(new RevenuePoint
            {
                Label = weeklyLabels[index],
                Value = weeklyValues[index] * 1000000
            });
        }

        var monthlyLabels =
            new[]
            {
                "Th1", "Th2", "Th3", "Th4", "Th5", "Th6", "Th7", "Th8", "Th9", "Th10", "Th11", "Th12"
            };

        var monthlyValues = new[] { 320m, 280m, 350m, 310m, 370m, 400m, 420m, 390m, 360m, 410m, 450m, 480m };

        for (var index = 0; index < monthlyLabels.Length; index++)
        {
            context.RevenueMonthly.Add(new RevenuePoint
            {
                Label = monthlyLabels[index],
                Value = monthlyValues[index] * 1000000
            });
        }
    }

    private static void SeedTopDishes(AppContext context)
    {
        context.TopDishes.Add(new DishRevenue { Name = "Signature Ribeye", Revenue = 32000000, Share = 0.24 });
        context.TopDishes.Add(new DishRevenue { Name = "Seared Salmon Bowl", Revenue = 28000000, Share = 0.21 });
        context.TopDishes.Add(new DishRevenue { Name = "Grilled Sea Bass", Revenue = 21000000, Share = 0.16 });
        context.TopDishes.Add(new DishRevenue { Name = "Garlic Butter Prawns", Revenue = 18000000, Share = 0.14 });
        context.TopDishes.Add(new DishRevenue { Name = "Truffle Bruschetta", Revenue = 15000000, Share = 0.12 });
        context.TopDishes.Add(new DishRevenue { Name = "Chocolate Molten Cake", Revenue = 13000000, Share = 0.10 });
    }
}