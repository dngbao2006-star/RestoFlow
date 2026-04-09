using System.Collections.ObjectModel;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;

namespace AppManagermentRestaurant.Services;

public class AppContext : ObservableObject
{
    private static AppContext? _instance;

    public static AppContext Instance => _instance ??= new AppContext();

    private Staff? _currentUser;
    private Order? _selectedOrder;
    private Table? _selectedTable;
    internal static string? BaseDirectory;

    public Staff? CurrentUser
    {
        get => _currentUser;
        set
        {
            if (SetProperty(ref _currentUser, value))
            {
                OnPropertyChanged(nameof(IsStaff));
                OnPropertyChanged(nameof(IsManager));
                RefreshBadges();
            }
        }
    }

    public bool IsStaff => CurrentUser?.Role == StaffRole.Staff;
    public bool IsManager => CurrentUser?.Role == StaffRole.Manager;

    public ObservableCollection<Order> Orders { get; } = new();
    public ObservableCollection<Order> FilteredOrders { get; } = new();
    public ObservableCollection<Table> Tables { get; } = new();
    public ObservableCollection<AppManagermentRestaurant.Models.MenuItem> MenuItems { get; } = new();
    public ObservableCollection<AppManagermentRestaurant.Models.MenuItem> FilteredMenuItems { get; } = new();
    public ObservableCollection<Notification> Notifications { get; } = new();
    public ObservableCollection<ChatMessage> ChatMessages { get; } = new();
    public ObservableCollection<Order> OrderHistory { get; } = new();
    public ObservableCollection<Staff> StaffMembers { get; } = new();
    public ObservableCollection<Invoice> Invoices { get; } = new();
    public ObservableCollection<RevenuePoint> RevenueDaily { get; } = new();
    public ObservableCollection<RevenuePoint> RevenueWeekly { get; } = new();
    public ObservableCollection<RevenuePoint> RevenueMonthly { get; } = new();
    public ObservableCollection<DishRevenue> TopDishes { get; } = new();

    public Order? SelectedOrder
    {
        get => _selectedOrder;
        set => SetProperty(ref _selectedOrder, value);
    }

    public Table? SelectedTable
    {
        get => _selectedTable;
        set => SetProperty(ref _selectedTable, value);
    }

    public int UnreadNotifications => Notifications.Count(notification => !notification.Read);
    public int UnreadMessages => ChatMessages.Count(message => !message.IsSystem && !message.IsRead);
    public int ReadyDishCount => Orders.SelectMany(order => order.Items).Count(item => item.Status == DishStatus.Ready);

    public int PendingDishCount => Orders.SelectMany(order => order.Items).Count(item => item.Status == DishStatus.Pending);
    public int PreparingDishCount => Orders.SelectMany(order => order.Items).Count(item => item.Status == DishStatus.Preparing);
    public int ServedDishCount => Orders.SelectMany(order => order.Items).Count(item => item.Status == DishStatus.Served);

    public IEnumerable<OrderItem> ReadyItems => Orders.SelectMany(order => order.Items).Where(item => item.Status == DishStatus.Ready);

    public string StaffChatTitle => UnreadMessages > 0 ? $"Chat ({UnreadMessages})" : "Chat";
    public string DishStatusTitle => ReadyDishCount > 0 ? $"Dish Status ({ReadyDishCount})" : "Dish Status";
    public string NotificationsTitle => UnreadNotifications > 0 ? $"Notifications ({UnreadNotifications})" : "Notifications";

    public int AvailableCount => Tables.Count(table => table.Status == TableStatus.Available);
    public int OccupiedCount => Tables.Count(table => table.Status == TableStatus.Occupied);
    public int ReservedCount => Tables.Count(table => table.Status == TableStatus.Reserved);
    public int NeedsClearingCount => Tables.Count(table => table.Status == TableStatus.NeedsClearing);

    public IEnumerable<Table> GroundFloorTables => Tables.Where(table => table.Floor == "Ground Floor");
    public IEnumerable<Table> SecondFloorTables => Tables.Where(table => table.Floor == "Second Floor");
    public IEnumerable<Table> GardenTables => Tables.Where(table => table.Floor == "Garden");

    public string TodayRevenueDisplay => RevenueDaily.Count > 0 ? RevenueDaily[Math.Min(6, RevenueDaily.Count - 1)].ValueDisplay : Formatters.FormatCurrency(0);
    public string AvgPerDayDisplay => RevenueWeekly.Count > 0 ? Formatters.FormatCurrency(RevenueWeekly.Average(point => point.Value)) : Formatters.FormatCurrency(0);
    public string TotalRevenueDisplay => RevenueWeekly.Count > 0 ? Formatters.FormatCurrency(RevenueWeekly.Sum(point => point.Value)) : Formatters.FormatCurrency(0);
    public string AvgOrderValueDisplay => Formatters.FormatCurrency(250000);

    public decimal FilteredOrdersTotal => FilteredOrders.Sum(o => o.Total);
    public string FilteredOrdersTotalDisplay => Formatters.FormatCurrency(FilteredOrdersTotal);
    public int FilteredOrdersCount => FilteredOrders.Count;

    private AppContext()
    {
        SeedMockData();
        SelectedOrder = Orders.FirstOrDefault();

        Notifications.CollectionChanged += (_, _) => RefreshBadges();
        ChatMessages.CollectionChanged += (_, _) => RefreshBadges();
        Orders.CollectionChanged += (_, _) => RefreshBadges();
    }

    public void MarkNotificationsRead()
    {
        foreach (var notification in Notifications)
        {
            notification.Read = true;
        }

        RefreshBadges();
    }

    public void RefreshBadges()
    {
        OnPropertyChanged(nameof(UnreadNotifications));
        OnPropertyChanged(nameof(UnreadMessages));
        OnPropertyChanged(nameof(ReadyDishCount));
        OnPropertyChanged(nameof(PendingDishCount));
        OnPropertyChanged(nameof(PreparingDishCount));
        OnPropertyChanged(nameof(ServedDishCount));
        OnPropertyChanged(nameof(ReadyItems));
        OnPropertyChanged(nameof(StaffChatTitle));
        OnPropertyChanged(nameof(DishStatusTitle));
        OnPropertyChanged(nameof(NotificationsTitle));
    }

    public void TransferOrder(int targetTableId)
    {
        if (SelectedOrder == null)
            return;

        var targetTable = Tables.FirstOrDefault(t => t.Id == targetTableId);
        if (targetTable == null)
            return;

        // Update the selected order to the target table
        SelectedOrder.TableId = targetTable.Id;
        SelectedOrder.TableNumber = targetTable.Number;

        // Update table status
        var currentTable = Tables.FirstOrDefault(t => t.Id == SelectedOrder.TableId);
        if (currentTable != null)
        {
            currentTable.CurrentOrderId = null;
            currentTable.Status = TableStatus.Available;
        }

        targetTable.Status = TableStatus.Occupied;
        targetTable.CurrentOrderId = SelectedOrder.Id;

        RefreshBadges();
    }

    public void MergeOrder(int targetOrderId)
    {
        if (SelectedOrder == null)
            return;

        var targetOrder = Orders.FirstOrDefault(o => o.Id == targetOrderId);
        if (targetOrder == null || targetOrder.Id == SelectedOrder.Id)
            return;

        // Merge items from target order into selected order
        foreach (var item in targetOrder.Items)
        {
            var existingItem = SelectedOrder.Items.FirstOrDefault(i => i.MenuItemId == item.MenuItemId);

            if (existingItem != null)
            {
                // Item already exists, increase quantity
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                // Add new item
                SelectedOrder.Items.Add(new OrderItem
                {
                    MenuItemId = item.MenuItemId,
                    Name = item.Name,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    Image = item.Image,
                    Status = item.Status,
                    Notes = item.Notes
                });
            }
        }

        // Update the target table status to available
        var targetTable = Tables.FirstOrDefault(t => t.Id == targetOrder.TableId);
        if (targetTable != null)
        {
            targetTable.Status = TableStatus.Available;
            targetTable.CurrentOrderId = null;
        }

        // Remove the merged order
        Orders.Remove(targetOrder);

        // Recalculate totals
        SelectedOrder.NotifyItemsChanged();
        RefreshBadges();
    }

    private void SeedMockData()
    {
        StaffMembers.Add(new Staff
        {
            Id = 1,
            Name = "Nguyen Minh Quan",
            Role = StaffRole.Manager,
            Email = "quan.manager@goldenplate.vn",
            Phone = "0900 111 222",
            Status = StaffStatus.Active,
            LastLogin = DateTime.Now.AddHours(-2),
            JoinDate = DateTime.Now.AddYears(-3),
            Permissions = new List<string> { "dashboard", "hr", "menu", "revenue", "system" }
        });

        StaffMembers.Add(new Staff
        {
            Id = 2,
            Name = "Tran Minh Tuan",
            Role = StaffRole.Staff,
            Email = "tuan.staff@goldenplate.vn",
            Phone = "0900 222 333",
            Status = StaffStatus.Active,
            LastLogin = DateTime.Now.AddHours(-1),
            JoinDate = DateTime.Now.AddYears(-2),
            Permissions = new List<string> { "tables", "orders", "payment" }
        });

        StaffMembers.Add(new Staff
        {
            Id = 3,
            Name = "Le Thi Lan Anh",
            Role = StaffRole.Staff,
            Email = "lananh.staff@goldenplate.vn",
            Phone = "0900 333 444",
            Status = StaffStatus.Active,
            LastLogin = DateTime.Now.AddHours(-4),
            JoinDate = DateTime.Now.AddMonths(-14),
            Permissions = new List<string> { "tables", "orders", "chat" }
        });

        StaffMembers.Add(new Staff
        {
            Id = 4,
            Name = "Pham Van Duc",
            Role = StaffRole.Staff,
            Email = "duc.staff@goldenplate.vn",
            Phone = "0900 444 555",
            Status = StaffStatus.Inactive,
            LastLogin = DateTime.Now.AddDays(-7),
            JoinDate = DateTime.Now.AddYears(-1),
            Permissions = new List<string> { "tables" }
        });

        StaffMembers.Add(new Staff
        {
            Id = 5,
            Name = "Hoang Thi Mai",
            Role = StaffRole.Staff,
            Email = "mai.staff@goldenplate.vn",
            Phone = "0900 555 666",
            Status = StaffStatus.Locked,
            LastLogin = DateTime.Now.AddMonths(-2),
            JoinDate = DateTime.Now.AddYears(-4),
            Permissions = new List<string> { "tables", "orders" }
        });

        CurrentUser = StaffMembers[1];

        var menuItems = new List<AppManagermentRestaurant.Models.MenuItem>
        {
            new()
            {
                Id = 1,
                Name = "Truffle Mushroom Bruschetta",
                Category = "Appetizers",
                Price = 85000,
                Description = "Sourdough, truffle oil, wild mushrooms, parmesan.",
                Image = "https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?auto=format&fit=crop&w=900&q=80"
            },
            new()
            {
                Id = 2,
                Name = "Crispy Calamari",
                Category = "Appetizers",
                Price = 90000,
                Description = "Lightly battered squid, lemon aioli.",
                Image = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=900&q=80"
            },
            new()
            {
                Id = 3,
                Name = "Seared Salmon Bowl",
                Category = "Main Course",
                Price = 225000,
                Description = "Herb rice, grilled vegetables, citrus glaze.",
                Image = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=900&q=80"
            },
            new()
            {
                Id = 4,
                Name = "Signature Ribeye",
                Category = "Main Course",
                Price = 320000,
                Description = "Prime ribeye, rosemary butter, roasted potato.",
                Image = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=900&q=80"
            },
            new()
            {
                Id = 5,
                Name = "Beef Pho Soup",
                Category = "Soups",
                Price = 95000,
                Description = "Slow-cooked broth with rice noodles and herbs.",
                Image = "https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?auto=format&fit=crop&w=900&q=80",
                OutOfStock = true,
                Available = false
            },
            new()
            {
                Id = 6,
                Name = "Seafood Laksa",
                Category = "Soups",
                Price = 165000,
                Description = "Coconut broth, prawns, squid, fresh herbs.",
                Image = "https://images.unsplash.com/photo-1467003909585-2f8a72700288?auto=format&fit=crop&w=900&q=80"
            },
            new()
            {
                Id = 7,
                Name = "Garlic Butter Prawns",
                Category = "Seafood",
                Price = 210000,
                Description = "Sizzling prawns, herb butter, lemon.",
                Image = "https://images.unsplash.com/photo-1467003909585-2f8a72700288?auto=format&fit=crop&w=900&q=80"
            },
            new()
            {
                Id = 8,
                Name = "Grilled Sea Bass",
                Category = "Seafood",
                Price = 245000,
                Description = "Char-grilled sea bass, salsa verde.",
                Image = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=900&q=80"
            },
            new()
            {
                Id = 9,
                Name = "Vanilla Bean Panna Cotta",
                Category = "Desserts",
                Price = 75000,
                Description = "Vanilla cream, berry compote.",
                Image = "https://images.unsplash.com/photo-1505253216365-8c26c4f21b4c?auto=format&fit=crop&w=900&q=80"
            },
            new()
            {
                Id = 10,
                Name = "Chocolate Molten Cake",
                Category = "Desserts",
                Price = 80000,
                Description = "Warm chocolate cake, vanilla ice cream.",
                Image = "https://images.unsplash.com/photo-1505253216365-8c26c4f21b4c?auto=format&fit=crop&w=900&q=80"
            },
            new()
            {
                Id = 11,
                Name = "Signature Iced Latte",
                Category = "Drinks",
                Price = 55000,
                Description = "Single origin espresso, fresh milk.",
                Image = "https://images.unsplash.com/photo-1509042239860-f550ce710b93?auto=format&fit=crop&w=900&q=80"
            },
            new()
            {
                Id = 12,
                Name = "Lemongrass Sparkling",
                Category = "Drinks",
                Price = 45000,
                Description = "Lemongrass syrup, soda, lime.",
                Image = "https://images.unsplash.com/photo-1509042239860-f550ce710b93?auto=format&fit=crop&w=900&q=80"
            }
        };

        foreach (var item in menuItems)
        {
            MenuItems.Add(item);
        }

        var tables = new List<Table>
        {
            new() { Id = 1, Number = 1, Floor = "Ground Floor", Status = TableStatus.Occupied, Capacity = 4, CurrentOrderId = 101 },
            new() { Id = 2, Number = 2, Floor = "Ground Floor", Status = TableStatus.Occupied, Capacity = 2, CurrentOrderId = 106 },
            new() { Id = 3, Number = 3, Floor = "Ground Floor", Status = TableStatus.Reserved, Capacity = 4, ReservedFor = "Linh", ReservedAt = DateTime.Now.AddHours(2) },
            new() { Id = 4, Number = 4, Floor = "Ground Floor", Status = TableStatus.Occupied, Capacity = 6, CurrentOrderId = 102 },
            new() { Id = 5, Number = 5, Floor = "Ground Floor", Status = TableStatus.NeedsClearing, Capacity = 4 },
            new() { Id = 6, Number = 6, Floor = "Ground Floor", Status = TableStatus.Occupied, Capacity = 2, CurrentOrderId = 107 },
            new() { Id = 7, Number = 7, Floor = "Ground Floor", Status = TableStatus.Available, Capacity = 4 },
            new() { Id = 8, Number = 8, Floor = "Ground Floor", Status = TableStatus.Occupied, Capacity = 6, CurrentOrderId = 103 },
            new() { Id = 9, Number = 9, Floor = "Second Floor", Status = TableStatus.Occupied, Capacity = 4, CurrentOrderId = 108 },
            new() { Id = 10, Number = 10, Floor = "Second Floor", Status = TableStatus.Occupied, Capacity = 4, CurrentOrderId = 104 },
            new() { Id = 11, Number = 11, Floor = "Second Floor", Status = TableStatus.Reserved, Capacity = 2, ReservedFor = "Kim", ReservedAt = DateTime.Now.AddHours(1) },
            new() { Id = 12, Number = 12, Floor = "Second Floor", Status = TableStatus.Available, Capacity = 6 },
            new() { Id = 13, Number = 13, Floor = "Second Floor", Status = TableStatus.NeedsClearing, Capacity = 2 },
            new() { Id = 14, Number = 14, Floor = "Second Floor", Status = TableStatus.Occupied, Capacity = 4, CurrentOrderId = 109 },
            new() { Id = 15, Number = 15, Floor = "Garden", Status = TableStatus.Available, Capacity = 6 },
            new() { Id = 16, Number = 16, Floor = "Garden", Status = TableStatus.Occupied, Capacity = 4, CurrentOrderId = 105 },
            new() { Id = 17, Number = 17, Floor = "Garden", Status = TableStatus.Reserved, Capacity = 2, ReservedFor = "Bao", ReservedAt = DateTime.Now.AddHours(3) },
            new() { Id = 18, Number = 18, Floor = "Garden", Status = TableStatus.Available, Capacity = 4 }
        };

        foreach (var table in tables)
        {
            Tables.Add(table);
        }

        Orders.Add(new Order
        {
            Id = 101,
            TableId = 1,
            TableNumber = 1,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-45),
            ServerName = "Tran Minh Tuan",
            ServerId = 2,
            Discount = 0,
            PaymentMethod = PaymentMethod.Cash,
            Items = new ObservableCollection<OrderItem>
            {
                new() { Id = 1, MenuItemId = 1, Name = "Truffle Mushroom Bruschetta", Price = 85000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[0].Image },
                new() { Id = 2, MenuItemId = 3, Name = "Seared Salmon Bowl", Price = 225000, Quantity = 2, Status = DishStatus.Ready, Image = menuItems[2].Image }
            }
        });

        Orders.Add(new Order
        {
            Id = 102,
            TableId = 4,
            TableNumber = 4,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-30),
            ServerName = "Le Thi Lan Anh",
            ServerId = 3,
            Discount = 15000,
            PaymentMethod = PaymentMethod.Qr,
            Items = new ObservableCollection<OrderItem>
            {
                new() { Id = 3, MenuItemId = 7, Name = "Garlic Butter Prawns", Price = 210000, Quantity = 1, Status = DishStatus.Preparing, Image = menuItems[6].Image },
                new() { Id = 4, MenuItemId = 10, Name = "Chocolate Molten Cake", Price = 80000, Quantity = 2, Status = DishStatus.Pending, Image = menuItems[9].Image }
            }
        });

        Orders.Add(new Order
        {
            Id = 103,
            TableId = 8,
            TableNumber = 8,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-20),
            ServerName = "Tran Minh Tuan",
            ServerId = 2,
            Discount = 0,
            PaymentMethod = PaymentMethod.Cash,
            Items = new ObservableCollection<OrderItem>
            {
                new() { Id = 5, MenuItemId = 2, Name = "Crispy Calamari", Price = 90000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[1].Image },
                new() { Id = 6, MenuItemId = 8, Name = "Grilled Sea Bass", Price = 245000, Quantity = 1, Status = DishStatus.Ready, Image = menuItems[7].Image }
            }
        });

        Orders.Add(new Order
        {
            Id = 104,
            TableId = 10,
            TableNumber = 10,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-10),
            ServerName = "Le Thi Lan Anh",
            ServerId = 3,
            Discount = 0,
            PaymentMethod = PaymentMethod.Qr,
            Items = new ObservableCollection<OrderItem>
            {
                new() { Id = 7, MenuItemId = 11, Name = "Signature Iced Latte", Price = 55000, Quantity = 2, Status = DishStatus.Pending, Image = menuItems[10].Image },
                new() { Id = 8, MenuItemId = 9, Name = "Vanilla Bean Panna Cotta", Price = 75000, Quantity = 1, Status = DishStatus.Preparing, Image = menuItems[8].Image }
            }
        });

        Orders.Add(new Order
        {
            Id = 105,
            TableId = 16,
            TableNumber = 16,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-55),
            ServerName = "Tran Minh Tuan",
            ServerId = 2,
            Discount = 50000,
            PaymentMethod = PaymentMethod.Cash,
            Items = new ObservableCollection<OrderItem>
            {
                new() { Id = 9, MenuItemId = 4, Name = "Signature Ribeye", Price = 320000, Quantity = 1, Status = DishStatus.Ready, Image = menuItems[3].Image },
                new() { Id = 10, MenuItemId = 12, Name = "Lemongrass Sparkling", Price = 45000, Quantity = 2, Status = DishStatus.Served, Image = menuItems[11].Image }
            }
        });

        // Sample finished orders for payment testing - ALL ITEMS SERVED
        Orders.Add(new Order
        {
            Id = 106,
            TableId = 2,
            TableNumber = 2,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-70),
            ServerName = "Tran Minh Tuan",
            ServerId = 2,
            Discount = 0,
            PaymentMethod = PaymentMethod.Cash,
            Items = new ObservableCollection<OrderItem>
            {
                new() { Id = 11, MenuItemId = 1, Name = "Truffle Mushroom Bruschetta", Price = 85000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[0].Image },
                new() { Id = 12, MenuItemId = 11, Name = "Signature Iced Latte", Price = 55000, Quantity = 2, Status = DishStatus.Served, Image = menuItems[10].Image }
            }
        });

        Orders.Add(new Order
        {
            Id = 107,
            TableId = 6,
            TableNumber = 6,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-85),
            ServerName = "Le Thi Lan Anh",
            ServerId = 3,
            Discount = 25000,
            PaymentMethod = PaymentMethod.Qr,
            Items = new ObservableCollection<OrderItem>
            {
                new() { Id = 13, MenuItemId = 2, Name = "Crispy Calamari", Price = 90000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[1].Image },
                new() { Id = 14, MenuItemId = 9, Name = "Vanilla Bean Panna Cotta", Price = 75000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[8].Image }
            }
        });

        Orders.Add(new Order
        {
            Id = 108,
            TableId = 9,
            TableNumber = 9,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-60),
            ServerName = "Tran Minh Tuan",
            ServerId = 2,
            Discount = 0,
            PaymentMethod = PaymentMethod.Cash,
            Items = new ObservableCollection<OrderItem>
            {
                new() { Id = 15, MenuItemId = 3, Name = "Seared Salmon Bowl", Price = 225000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[2].Image },
                new() { Id = 16, MenuItemId = 6, Name = "Seafood Laksa", Price = 165000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[5].Image },
                new() { Id = 17, MenuItemId = 10, Name = "Chocolate Molten Cake", Price = 80000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[9].Image }
            }
        });

        Orders.Add(new Order
        {
            Id = 109,
            TableId = 14,
            TableNumber = 14,
            Status = OrderStatus.Active,
            CreatedAt = DateTime.Now.AddMinutes(-50),
            ServerName = "Le Thi Lan Anh",
            ServerId = 3,
            Discount = 0,
            PaymentMethod = PaymentMethod.Qr,
            Items = new ObservableCollection<OrderItem>
            {
                new() { Id = 18, MenuItemId = 7, Name = "Garlic Butter Prawns", Price = 210000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[6].Image },
                new() { Id = 19, MenuItemId = 8, Name = "Grilled Sea Bass", Price = 245000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[7].Image }
            }
        });

        OrderHistory.Add(new Order
        {
            Id = 201,
            TableId = 6,
            TableNumber = 6,
            Status = OrderStatus.Paid,
            CreatedAt = DateTime.Now.AddDays(-1),
            ServerName = "Tran Minh Tuan",
            ServerId = 2,
            Discount = 20000,
            PaymentMethod = PaymentMethod.Cash,
            Items = new ObservableCollection<OrderItem>
            {
                new() { Id = 11, MenuItemId = 3, Name = "Seared Salmon Bowl", Price = 225000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[2].Image },
                new() { Id = 12, MenuItemId = 11, Name = "Signature Iced Latte", Price = 55000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[10].Image }
            }
        });

        OrderHistory.Add(new Order
        {
            Id = 202,
            TableId = 12,
            TableNumber = 12,
            Status = OrderStatus.Paid,
            CreatedAt = DateTime.Now.AddDays(-3),
            ServerName = "Le Thi Lan Anh",
            ServerId = 3,
            Discount = 0,
            PaymentMethod = PaymentMethod.Qr,
            Items = new ObservableCollection<OrderItem>
            {
                new() { Id = 13, MenuItemId = 7, Name = "Garlic Butter Prawns", Price = 210000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[6].Image },
                new() { Id = 14, MenuItemId = 9, Name = "Vanilla Bean Panna Cotta", Price = 75000, Quantity = 1, Status = DishStatus.Served, Image = menuItems[8].Image }
            }
        });

        OrderHistory.Add(new Order
        {
            Id = 203,
            TableId = 15,
            TableNumber = 15,
            Status = OrderStatus.Paid,
            CreatedAt = DateTime.Now.AddDays(-4),
            ServerName = "Tran Minh Tuan",
            ServerId = 2,
            Discount = 30000,
            PaymentMethod = PaymentMethod.Cash,
            Items = new ObservableCollection<OrderItem>
            {
                new() { Id = 15, MenuItemId = 2, Name = "Crispy Calamari", Price = 90000, Quantity = 2, Status = DishStatus.Served, Image = menuItems[1].Image },
                new() { Id = 16, MenuItemId = 12, Name = "Lemongrass Sparkling", Price = 45000, Quantity = 2, Status = DishStatus.Served, Image = menuItems[11].Image }
            }
        });

        Notifications.Add(new Notification
        {
            Id = 1,
            Type = NotificationType.Success,
            Title = "Dish Ready",
            Message = "Table 8 - Grilled Sea Bass is ready to serve.",
            Timestamp = DateTime.Now.AddMinutes(-5),
            Read = false,
            TableId = 8,
            OrderId = 103
        });

        Notifications.Add(new Notification
        {
            Id = 2,
            Type = NotificationType.Warning,
            Title = "Needs Clearing",
            Message = "Table 5 has been marked for clearing.",
            Timestamp = DateTime.Now.AddMinutes(-12),
            Read = false,
            TableId = 5
        });

        Notifications.Add(new Notification
        {
            Id = 3,
            Type = NotificationType.Info,
            Title = "Inventory Update",
            Message = "Beef Pho Soup is out of stock.",
            Timestamp = DateTime.Now.AddMinutes(-40),
            Read = true
        });

        Notifications.Add(new Notification
        {
            Id = 4,
            Type = NotificationType.Warning,
            Title = "VIP Reservation",
            Message = "Table 3 reserved at 7:00 PM.",
            Timestamp = DateTime.Now.AddMinutes(-65),
            Read = true
        });

        ChatMessages.Add(new ChatMessage
        {
            Id = 1,
            SenderId = 0,
            SenderName = "System",
            SenderRole = "System",
            Message = "Daily briefing: VIP booking at 7:00 PM.",
            Timestamp = DateTime.Now.AddMinutes(-90),
            IsSystem = true,
            IsRead = true
        });

        ChatMessages.Add(new ChatMessage
        {
            Id = 2,
            SenderId = 1,
            SenderName = "Nguyen Minh Quan",
            SenderRole = "Manager",
            Message = "Please keep the garden area tidy before 6 PM.",
            Timestamp = DateTime.Now.AddMinutes(-60),
            IsSystem = false,
            IsMine = false,
            IsRead = false
        });

        ChatMessages.Add(new ChatMessage
        {
            Id = 3,
            SenderId = 3,
            SenderName = "Le Thi Lan Anh",
            SenderRole = "Staff",
            Message = "Table 10 is asking for the dessert menu.",
            Timestamp = DateTime.Now.AddMinutes(-40),
            IsSystem = false,
            IsMine = false,
            IsRead = true
        });

        ChatMessages.Add(new ChatMessage
        {
            Id = 4,
            SenderId = 2,
            SenderName = "Tran Minh Tuan",
            SenderRole = "Staff",
            Message = "On it. I will bring it right away.",
            Timestamp = DateTime.Now.AddMinutes(-38),
            IsSystem = false,
            IsMine = true,
            IsRead = true
        });

        ChatMessages.Add(new ChatMessage
        {
            Id = 5,
            SenderId = 1,
            SenderName = "Nguyen Minh Quan",
            SenderRole = "Manager",
            Message = "Reminder: report any stock issues by 3 PM.",
            Timestamp = DateTime.Now.AddMinutes(-15),
            IsSystem = false,
            IsMine = false,
            IsRead = false
        });

        ChatMessages.Add(new ChatMessage
        {
            Id = 6,
            SenderId = 2,
            SenderName = "Tran Minh Tuan",
            SenderRole = "Staff",
            Message = "Noted. Thanks!",
            Timestamp = DateTime.Now.AddMinutes(-12),
            IsSystem = false,
            IsMine = true,
            IsRead = true
        });

        Invoices.Add(new Invoice
        {
            Id = 8001,
            OrderId = 201,
            TableNumber = 6,
            ServerName = "Tran Minh Tuan",
            CreatedAt = DateTime.Now.AddDays(-1),
            PaymentMethod = PaymentMethod.Cash,
            Discount = 20000,
            Total = 260000,
            Items = OrderHistory[0].Items
        });

        Invoices.Add(new Invoice
        {
            Id = 8002,
            OrderId = 202,
            TableNumber = 12,
            ServerName = "Le Thi Lan Anh",
            CreatedAt = DateTime.Now.AddDays(-3),
            PaymentMethod = PaymentMethod.Qr,
            Discount = 0,
            Total = 285000,
            Items = OrderHistory[1].Items
        });

        Invoices.Add(new Invoice
        {
            Id = 8003,
            OrderId = 203,
            TableNumber = 15,
            ServerName = "Tran Minh Tuan",
            CreatedAt = DateTime.Now.AddDays(-4),
            PaymentMethod = PaymentMethod.Cash,
            Discount = 30000,
            Total = 240000,
            Items = OrderHistory[2].Items
        });

        Invoices.Add(new Invoice
        {
            Id = 8004,
            OrderId = 104,
            TableNumber = 10,
            ServerName = "Le Thi Lan Anh",
            CreatedAt = DateTime.Now.AddDays(-5),
            PaymentMethod = PaymentMethod.Qr,
            Discount = 0,
            Total = 185000,
            Items = Orders[3].Items
        });

        var hourlyLabels = new[]
        {
            "08:00", "09:00", "10:00", "11:00", "12:00", "13:00", "14:00",
            "15:00", "16:00", "17:00", "18:00", "19:00", "20:00", "21:00"
        };

        var hourlyValues = new[] { 1.2m, 1.4m, 1.6m, 2.2m, 3.8m, 4.5m, 3.2m, 2.1m, 1.9m, 2.5m, 4.1m, 5.2m, 4.8m, 3.6m };

        for (var index = 0; index < hourlyLabels.Length; index++)
        {
            RevenueDaily.Add(new RevenuePoint
            {
                Label = hourlyLabels[index],
                Value = hourlyValues[index] * 1000000
            });
        }

        var weeklyLabels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        var weeklyValues = new[] { 18m, 20m, 22m, 19m, 26m, 32m, 28m };

        for (var index = 0; index < weeklyLabels.Length; index++)
        {
            RevenueWeekly.Add(new RevenuePoint
            {
                Label = weeklyLabels[index],
                Value = weeklyValues[index] * 1000000
            });
        }

        var monthlyLabels = new[]
        {
            "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
        };

        var monthlyValues = new[] { 320m, 280m, 350m, 310m, 370m, 400m, 420m, 390m, 360m, 410m, 450m, 480m };

        for (var index = 0; index < monthlyLabels.Length; index++)
        {
            RevenueMonthly.Add(new RevenuePoint
            {
                Label = monthlyLabels[index],
                Value = monthlyValues[index] * 1000000
            });
        }

        TopDishes.Add(new DishRevenue { Name = "Signature Ribeye", Revenue = 32000000, Share = 0.24 });
        TopDishes.Add(new DishRevenue { Name = "Seared Salmon Bowl", Revenue = 28000000, Share = 0.21 });
        TopDishes.Add(new DishRevenue { Name = "Grilled Sea Bass", Revenue = 21000000, Share = 0.16 });
        TopDishes.Add(new DishRevenue { Name = "Garlic Butter Prawns", Revenue = 18000000, Share = 0.14 });
        TopDishes.Add(new DishRevenue { Name = "Truffle Bruschetta", Revenue = 15000000, Share = 0.12 });
        TopDishes.Add(new DishRevenue { Name = "Chocolate Molten Cake", Revenue = 13000000, Share = 0.10 });
    }
}
