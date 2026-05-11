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

            await Task.WhenAll(menuTask, tablesTask, ordersTask, orderItemsTask,
                               notifTask, invoicesTask, dailyTask, weeklyTask,
                               monthlyTask, topDishTask);

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
                context.Notifications.Add(item.Object);

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

            // ── 10. Start real-time listeners ──────────────────────────
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
                .Subscribe(evt => MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (evt.Object == null || string.IsNullOrEmpty(evt.Key)) return;
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
                    context.RefreshBadges();
                }))
        );

        // ── Orders listener ────────────────────────────────────────
        _subscriptions.Add(
            _client.Child("Orders")
                .AsObservable<Order>()
                .Subscribe(evt => MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (evt.Object == null || string.IsNullOrEmpty(evt.Key)) return;
                    var order = evt.Object;

                    if (order.Status == OrderStatus.Paid)
                    {
                        // Move to history if currently in active
                        var active = context.Orders.FirstOrDefault(o => o.Id == order.Id);
                        if (active != null) context.Orders.Remove(active);
                        var existingHist = context.OrderHistory.FirstOrDefault(o => o.Id == order.Id);
                        if (existingHist != null)
                        {
                            var idx = context.OrderHistory.IndexOf(existingHist);
                            context.OrderHistory[idx] = order;
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
                            var idx = context.Orders.IndexOf(existing);
                            // Preserve items from existing order since Firebase Orders node doesn't have items
                            order.Items = existing.Items;
                            context.Orders[idx] = order;
                        }
                        else
                        {
                            context.Orders.Add(order);
                        }
                    }
                    context.RefreshBadges();
                }))
        );

        // ── Notifications listener ─────────────────────────────────
        _subscriptions.Add(
            _client.Child("Notifications")
                .AsObservable<Notification>()
                .Subscribe(evt => MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (evt.Object == null || string.IsNullOrEmpty(evt.Key)) return;
                    var existing = context.Notifications.FirstOrDefault(n => n.Id == evt.Object.Id);
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
                }))
        );

        // ── OrderItems listener ────────────────────────────────────
        _subscriptions.Add(
            _client.Child("OrderItems")
                .AsObservable<FirebaseOrderItemDto>()
                .Subscribe(evt => MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (evt.Object == null || string.IsNullOrEmpty(evt.Key)) return;
                    var dto = evt.Object;
                    var order = context.Orders.FirstOrDefault(o => $"order_{o.Id}" == dto.OrderId)
                             ?? context.OrderHistory.FirstOrDefault(o => $"order_{o.Id}" == dto.OrderId);
                    if (order == null) return;

                    var existingItem = order.Items.FirstOrDefault(i => i.Id == dto.Id);
                    var newItem = new OrderItem
                    {
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
                    context.RefreshBadges();
                }))
        );
    }
}
