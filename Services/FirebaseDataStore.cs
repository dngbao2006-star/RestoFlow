using System.Collections.ObjectModel;
using System.Diagnostics;
using AppManagermentRestaurant.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;

namespace AppManagermentRestaurant.Services;

public class FirebaseDataStore : IDataStore
{
    private const string FIREBASE_URL = "https://doanquanan-6a948-default-rtdb.firebaseio.com";
    private readonly FirebaseClient _client = new(FIREBASE_URL);

    // Keep references so listeners aren't garbage-collected
    private readonly List<IDisposable> _subscriptions = new();

    public async Task SeedAsync(AppContext context)
    {
        try
        {
            // ── 1. Fetch all data in parallel ──────────────────────────
            var menuTask      = _client.Child("MenuItems").OnceAsync<FoodItem>();
            var tablesTask    = _client.Child("Tables").OnceAsync<Table>();
            var ordersTask    = _client.Child("Orders").OnceAsync<Order>();
            var orderItemsTask = _client.Child("OrderItems").OnceAsync<FirebaseOrderItemDto>();
            var notifTask     = _client.Child("Notifications").OnceAsync<Notification>();
            var invoicesTask  = _client.Child("Invoices").OnceAsync<Invoice>();
            var dailyTask     = _client.Child("Revenue").Child("Daily").OnceAsync<RevenuePoint>();
            var weeklyTask    = _client.Child("Revenue").Child("Weekly").OnceAsync<RevenuePoint>();
            var monthlyTask   = _client.Child("Revenue").Child("Monthly").OnceAsync<RevenuePoint>();
            var topDishTask   = _client.Child("TopDishes").OnceAsync<DishRevenue>();
            var usersTask     = _client.Child("Users").OnceAsync<FirebaseUserDto>();
            var chatsTask     = _client.Child("Chats").OnceAsync<FirebaseChatMessage>();
            var presenceTask  = _client.Child("Presence").OnceAsync<PresenceModel>();
            var configTask    = _client.Child("SystemConfig").Child("general")
                .OnceSingleAsync<SystemConfiguration>();

            await Task.WhenAll(menuTask, tablesTask, ordersTask, orderItemsTask,
                               notifTask, invoicesTask, dailyTask, weeklyTask,
                               monthlyTask, topDishTask, usersTask, chatsTask, presenceTask,
                               configTask);

            // ── 2. Populate MenuItems (sorted by Id) ───────────────────
            foreach (var item in menuTask.Result.OrderBy(i => i.Object.Id))
                context.MenuItems.Add(item.Object);

            // ── 3. Populate Tables (sorted by Number) ──────────────────
            foreach (var item in tablesTask.Result.OrderBy(t => t.Object.Number))
                context.Tables.Add(item.Object);

            // ── 4. Build OrderItems lookup ─────────────────────────────
            var orderItemsByOrderId = orderItemsTask.Result
                .GroupBy(oi => oi.Object.OrderId)
                .ToDictionary(g => g.Key, g => g.Select(oi =>
                {
                    var dto = oi.Object;
                    return new OrderItem
                    {
                        FirebaseKey = oi.Key,
                        Id         = dto.Id,
                        MenuItemId = dto.MenuItemId,
                        Name       = dto.Name,
                        Price      = dto.Price,
                        Quantity   = dto.Quantity,
                        Status     = dto.Status,
                        Image      = dto.Image,
                        Notes      = dto.Notes
                    };
                }).ToList());

            // ── 5. Populate Orders + OrderHistory ──────────────────────
            foreach (var item in ordersTask.Result)
            {
                var order = item.Object;
                if (orderItemsByOrderId.TryGetValue(item.Key, out var items))
                {
                    foreach (var oi in items)
                        order.Items.Add(oi);
                }

                if (order.Status == OrderStatus.Paid)
                    context.OrderHistory.Add(order);
                else
                    context.Orders.Add(order);
            }

            // ── 6. Populate Notifications ──────────────────────────────
            foreach (var item in notifTask.Result)
            {
                item.Object.FirebaseKey = item.Key;
                context.Notifications.Add(item.Object);
            }

            // ── 7. Populate Invoices ───────────────────────────────────
            foreach (var item in invoicesTask.Result)
            {
                var invoice = item.Object;
                // Link invoice items from the matching order
                var matchingOrderKey = $"order_{invoice.OrderId}";
                if (orderItemsByOrderId.TryGetValue(matchingOrderKey, out var invItems))
                    invoice.Items = invItems;
                context.Invoices.Add(invoice);
            }

            // ── 8. Populate Revenue ────────────────────────────────────
            foreach (var item in dailyTask.Result)
                context.RevenueDaily.Add(item.Object);
            foreach (var item in weeklyTask.Result)
                context.RevenueWeekly.Add(item.Object);
            foreach (var item in monthlyTask.Result)
                context.RevenueMonthly.Add(item.Object);

            // ── 9. Populate TopDishes ──────────────────────────────────
            foreach (var item in topDishTask.Result)
                context.TopDishes.Add(item.Object);

            // ── 10. Populate StaffMembers from Users ────────────────────
            var presenceByUid = presenceTask.Result.ToDictionary(item => item.Key, item => item.Object);
            foreach (var item in usersTask.Result)
            {
                var dto = item.Object;
                presenceByUid.TryGetValue(item.Key, out var presence);
                context.StaffMembers.Add(new Staff
                {
                    FirebaseUid = item.Key,
                    Name   = dto.hoTen,
                    Email  = dto.email,
                    Phone  = dto.sdt,
                    Role   = dto.quyen == "QuanLy" ? StaffRole.Manager : StaffRole.Staff,
                    Status = dto.trangThai switch
                    {
                        "TamKhoa" => StaffStatus.Locked,
                        "Khóa"    => StaffStatus.Locked,
                        _         => StaffStatus.Active
                    },
                    JoinDate  = DateTime.TryParse(dto.ngayVaoLam, out var jd) ? jd : DateTime.MinValue,
                    LastLogin = DateTime.Now,
                    IsOnline = presence?.IsOnline == true,
                    LastSeen = presence?.LastSeen ?? DateTime.MinValue
                });
            }

            // ── 11. Populate chat history ──────────────────────────────
            foreach (var item in chatsTask.Result.OrderBy(message => message.Object.Timestamp))
                context.AddOrUpdateChatMessage(item.Key, item.Object);

            context.SystemConfiguration = configTask.Result ?? new SystemConfiguration();

            // ── 12. Start real-time listeners ──────────────────────────
            StartListeners(context);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FirebaseDataStore] SeedAsync error: {ex.Message}");
            throw;
        }
    }

    private void StartListeners(AppContext context)
    {
        // ── Tables listener ────────────────────────────────────────
        _subscriptions.Add(
            _client.Child("Tables")
                .AsObservable<Table>()
                .Subscribe(evt => RunOnMainThread("Tables", () =>
                {
                    if (string.IsNullOrEmpty(evt.Key)) return;
                    if (evt.EventType == FirebaseEventType.Delete)
                    {
                        if (int.TryParse(evt.Key.Replace("table_", string.Empty), out var deletedId))
                        {
                            var deleted = context.Tables.FirstOrDefault(table => table.Id == deletedId);
                            if (deleted != null) context.Tables.Remove(deleted);
                        }
                        return;
                    }
                    if (evt.Object == null) return;
                    var existing = context.Tables.FirstOrDefault(t => t.Id == evt.Object.Id);
                    if (existing != null)
                    {
                        var idx = context.Tables.IndexOf(existing);
                        context.Tables[idx] = evt.Object;
                    }
                    else
                    {
                        context.Tables.Add(evt.Object);
                    }
                    context.RefreshTableMetrics();
                }), error => LogListenerError("Tables", error))
        );

        // ── Menu listener ──────────────────────────────────────────
        _subscriptions.Add(
            _client.Child("MenuItems")
                .AsObservable<FoodItem>()
                .Subscribe(evt => RunOnMainThread("MenuItems", () =>
                {
                    if (string.IsNullOrEmpty(evt.Key)) return;
                    if (evt.EventType == FirebaseEventType.Delete)
                    {
                        var deleted = context.MenuItems.FirstOrDefault(item =>
                            $"menu_{item.Id}" == evt.Key || item.Id.ToString() == evt.Key);
                        if (deleted != null) context.MenuItems.Remove(deleted);
                        return;
                    }
                    if (evt.Object == null) return;
                    var existing = context.MenuItems.FirstOrDefault(item => item.Id == evt.Object.Id);
                    if (existing == null)
                        context.MenuItems.Add(evt.Object);
                    else
                        context.MenuItems[context.MenuItems.IndexOf(existing)] = evt.Object;
                }), error => LogListenerError("MenuItems", error))
        );

        // ── Presence listener ──────────────────────────────────────
        _subscriptions.Add(
            _client.Child("Presence")
                .AsObservable<PresenceModel>()
                .Subscribe(evt => RunOnMainThread("Presence", () =>
                {
                    if (evt.Object == null || string.IsNullOrEmpty(evt.Key)) return;
                    var staff = context.StaffMembers.FirstOrDefault(item => item.FirebaseUid == evt.Key);
                    if (staff == null) return;
                    staff.IsOnline = evt.Object.IsOnline;
                    staff.LastSeen = evt.Object.LastSeen;
                    context.RefreshStaffMetrics();
                }), error => LogListenerError("Presence", error))
        );

        // ── Orders listener ────────────────────────────────────────
        _subscriptions.Add(
            _client.Child("Orders")
                .AsObservable<Order>()
                .Subscribe(evt => RunOnMainThreadAsync("Orders", async () =>
                {
                    if (evt.Object == null || string.IsNullOrEmpty(evt.Key)) return;
                    var order = evt.Object;

                    if (order.Status == OrderStatus.Paid)
                    {
                        // Preserve the active instance because it owns the linked OrderItems.
                        var active = context.Orders.FirstOrDefault(o => o.Id == order.Id);
                        if (active != null)
                        {
                            active.TableId = order.TableId;
                            active.TableNumber = order.TableNumber;
                            active.Status = order.Status;
                            active.PaymentMethod = order.PaymentMethod;
                            active.Discount = order.Discount;
                            order = active;
                            context.Orders.Remove(active);
                        }
                        var existingHist = context.OrderHistory.FirstOrDefault(o => o.Id == order.Id);
                        if (existingHist != null)
                        {
                            existingHist.TableId = order.TableId;
                            existingHist.TableNumber = order.TableNumber;
                            existingHist.Status = order.Status;
                            existingHist.PaymentMethod = order.PaymentMethod;
                            existingHist.Discount = order.Discount;
                        }
                        else
                        {
                            context.OrderHistory.Add(order);
                        }
                    }
                    else
                    {
                        var existing = context.Orders.FirstOrDefault(o => o.Id == order.Id);
                        if (existing != null)
                        {
                            // CẬP NHẬT thuộc tính thay vì GHI ĐÈ object
                            // → giữ nguyên reference Items và UI bindings
                            existing.TableId = order.TableId;
                            existing.TableNumber = order.TableNumber;
                            existing.Status = order.Status;
                            existing.ServerName = order.ServerName;
                            existing.ServerId = order.ServerId;
                            existing.Discount = order.Discount;
                            existing.PaymentMethod = order.PaymentMethod;
                            // KHÔNG ghi đè existing.Items
                        }
                        else
                        {
                            // Order mới từ Firebase → thêm vào collection
                            context.Orders.Add(order);

                            // Chủ động fetch items cho order mới vì Orders node
                            // không chứa items (chúng nằm ở node OrderItems riêng)
                            try
                            {
                                var orderKey = evt.Key; // vd: "order_7"
                                var allItems = await _client.Child("OrderItems").OnceAsync<FirebaseOrderItemDto>();
                                var matchingItems = allItems
                                    .Where(oi => oi.Object.OrderId == orderKey)
                                    .Select(oi =>
                                    {
                                        var dto = oi.Object;
                                        return new OrderItem
                                        {
                                            FirebaseKey = oi.Key,
                                            Id         = dto.Id,
                                            MenuItemId = dto.MenuItemId,
                                            Name       = dto.Name,
                                            Price      = dto.Price,
                                            Quantity   = dto.Quantity,
                                            Status     = dto.Status,
                                            Image      = dto.Image,
                                            Notes      = dto.Notes
                                        };
                                    })
                                    .ToList();

                                foreach (var item in matchingItems)
                                {
                                    if (!order.Items.Any(i => i.Id == item.Id))
                                        order.Items.Add(item);
                                }
                                order.NotifyItemsChanged();
                                context.NotifyOrderItemsChanged();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[FirebaseDataStore] Fetch items for new order error: {ex.Message}");
                            }
                        }
                    }
                    context.NotifyOrderItemsChanged();
                }), error => LogListenerError("Orders", error))
        );

        // ── Notifications listener ─────────────────────────────────
        _subscriptions.Add(
            _client.Child("Notifications")
                .AsObservable<Notification>()
                .Subscribe(evt => RunOnMainThread("Notifications", () =>
                {
                    if (string.IsNullOrEmpty(evt.Key)) return;
                    if (evt.EventType == FirebaseEventType.Delete)
                    {
                        var deleted = context.Notifications.FirstOrDefault(n => n.FirebaseKey == evt.Key);
                        if (deleted != null) context.Notifications.Remove(deleted);
                        context.RefreshBadges();
                        return;
                    }

                    if (evt.Object == null) return;
                    evt.Object.FirebaseKey = evt.Key;
                    var existing = context.Notifications.FirstOrDefault(n =>
                        n.FirebaseKey == evt.Key || n.Id == evt.Object.Id);
                    if (existing != null)
                    {
                        var idx = context.Notifications.IndexOf(existing);
                        context.Notifications[idx] = evt.Object;
                    }
                    else
                    {
                        context.Notifications.Add(evt.Object);
                    }
                    context.RefreshBadges();
                }), error => LogListenerError("Notifications", error))
        );

        // ── OrderItems listener ────────────────────────────────────
        _subscriptions.Add(
            _client.Child("OrderItems")
                .AsObservable<FirebaseOrderItemDto>()
                .Subscribe(evt => RunOnMainThread("OrderItems", () =>
                {
                    if (string.IsNullOrEmpty(evt.Key)) return;

                    if (evt.EventType == FirebaseEventType.Delete)
                    {
                        var parent = context.Orders.Concat(context.OrderHistory)
                            .FirstOrDefault(order => order.Items.Any(item => item.FirebaseKey == evt.Key));
                        var deletedItem = parent?.Items.FirstOrDefault(item => item.FirebaseKey == evt.Key);
                        if (parent != null && deletedItem != null)
                        {
                            parent.Items.Remove(deletedItem);
                            parent.NotifyItemsChanged();
                            context.NotifyOrderItemsChanged();
                        }
                        return;
                    }

                    if (evt.Object == null) return;
                    var dto = evt.Object;
                    var order = context.Orders.FirstOrDefault(o => $"order_{o.Id}" == dto.OrderId)
                             ?? context.OrderHistory.FirstOrDefault(o => $"order_{o.Id}" == dto.OrderId);
                    if (order == null) return;

                    var existingItem = order.Items.FirstOrDefault(i => i.FirebaseKey == evt.Key)
                                    ?? order.Items.FirstOrDefault(i => i.Id == dto.Id);
                    var newItem = new OrderItem
                    {
                        FirebaseKey = evt.Key,
                        Id         = dto.Id,
                        MenuItemId = dto.MenuItemId,
                        Name       = dto.Name,
                        Price      = dto.Price,
                        Quantity   = dto.Quantity,
                        Status     = dto.Status,
                        Image      = dto.Image,
                        Notes      = dto.Notes
                    };

                    if (existingItem != null)
                    {
                        var idx = order.Items.IndexOf(existingItem);
                        order.Items[idx] = newItem;
                    }
                    else
                    {
                        order.Items.Add(newItem);
                    }
                    order.NotifyItemsChanged();
                    context.NotifyOrderItemsChanged();
                }), error => LogListenerError("OrderItems", error))
        );

        // ── Invoices and revenue listeners ────────────────────────
        _subscriptions.Add(
            _client.Child("Invoices")
                .AsObservable<Invoice>()
                .Subscribe(evt => RunOnMainThread("Invoices", () =>
                {
                    if (evt.Object == null || string.IsNullOrEmpty(evt.Key)) return;
                    var invoice = evt.Object;
                    var order = context.OrderHistory.FirstOrDefault(item => item.Id == invoice.OrderId)
                             ?? context.Orders.FirstOrDefault(item => item.Id == invoice.OrderId);
                    if (order != null)
                        invoice.Items = order.Items;

                    var existing = context.Invoices.FirstOrDefault(item => item.OrderId == invoice.OrderId);
                    if (existing == null)
                        context.Invoices.Add(invoice);
                    else
                        context.Invoices[context.Invoices.IndexOf(existing)] = invoice;

                    context.RefreshFinancials();
                }), error => LogListenerError("Invoices", error))
        );

        _subscriptions.Add(ListenForRevenue("Daily", context.RevenueDaily, context));
        _subscriptions.Add(ListenForRevenue("Weekly", context.RevenueWeekly, context));
        _subscriptions.Add(ListenForRevenue("Monthly", context.RevenueMonthly, context));

        _subscriptions.Add(
            _client.Child("SystemConfig")
                .AsObservable<SystemConfiguration>()
                .Subscribe(evt => RunOnMainThread("SystemConfig", () =>
                {
                    if (evt.Key == "general" && evt.Object != null)
                        context.SystemConfiguration = evt.Object;
                }), error => LogListenerError("SystemConfig", error))
        );

        // ── Chats listener ──────────────────────────────────────────
        _subscriptions.Add(
            _client.Child("Chats")
                .AsObservable<FirebaseChatMessage>()
                .Subscribe(evt => RunOnMainThread("Chats", () =>
                {
                    if (string.IsNullOrEmpty(evt.Key)) return;

                    if (evt.EventType == FirebaseEventType.Delete)
                    {
                        var deleted = context.ChatMessages.FirstOrDefault(message => message.FirebaseKey == evt.Key);
                        if (deleted != null)
                            context.ChatMessages.Remove(deleted);

                        context.RefreshBadges();
                        return;
                    }

                    if (evt.Object == null) return;
                    context.AddOrUpdateChatMessage(evt.Key, evt.Object);
                    context.RefreshBadges();
                }), error => LogListenerError("Chats", error))
        );
    }


    private IDisposable ListenForRevenue(
        string period,
        ObservableCollection<RevenuePoint> collection,
        AppContext context)
    {
        return _client.Child("Revenue").Child(period)
            .AsObservable<RevenuePoint>()
            .Subscribe(evt => RunOnMainThread($"Revenue/{period}", () =>
            {
                if (evt.Object == null || string.IsNullOrEmpty(evt.Key)) return;
                var point = evt.Object;
                var existing = collection.FirstOrDefault(item =>
                    string.Equals(item.Label, point.Label, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                    collection.Add(point);
                else
                    collection[collection.IndexOf(existing)] = point;

                context.RefreshFinancials();
            }), error => LogListenerError($"Revenue/{period}", error));
    }

    private static void RunOnMainThread(string source, Action action)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebaseDataStore:{source}] UI update failed: {ex}");
            }
        });
    }

    private static void LogListenerError(string source, Exception error)
        => Debug.WriteLine($"[FirebaseDataStore:{source}] Listener stopped: {error}");

    private static void RunOnMainThreadAsync(string source, Func<Task> action)
    {
        MainThread.BeginInvokeOnMainThread(() => _ = ExecuteSafelyAsync(source, action));
    }

    private static async Task ExecuteSafelyAsync(string source, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FirebaseDataStore:{source}] Async UI update failed: {ex}");
        }
    }
}
