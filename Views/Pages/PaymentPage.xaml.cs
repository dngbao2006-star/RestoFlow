using System.Collections.ObjectModel;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class PaymentPage : ContentPage
{
    private PaymentMethod? _selectedPaymentMethod;
    private Order? _selectedTableOrder;
    private readonly FirebaseService _firebase = new();
    private string? _qrImageUrl;
    private bool _isQrLoading;
    private bool _isProcessingPayment;

    private ObservableCollection<Order> _activeOrders = new();

    public PaymentPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        RefreshPage();
    }

    private void RefreshPage()
    {
        _selectedPaymentMethod = null;
        DiscountCodeEntry.Text = "";
        DiscountErrorLabel.IsVisible = false;

        // 0. Dọn dẹp hóa đơn lỗi (stale invoices cho đơn còn Active)
        CleanupStaleInvoices();

        // 1. Tải bàn CÓ THỂ THANH TOÁN và sắp xếp (Bàn số nhỏ nhất đứng đầu)
        _activeOrders.Clear();
        var billable = AppContext.Instance.Orders
            .Where(o => o.Status == OrderStatus.Active && o.Items.Any())
            .OrderBy(o => o.TableNumber)
            .ToList();

        foreach (var o in billable)
        {
            _activeOrders.Add(o);
        }

        OnPropertyChanged(nameof(HasBillableTables));

        Order targetOrder = null;

        // 2. Xét xem có bàn nào đang được truyền từ trang sơ đồ qua không
        if (AppContext.Instance.SelectedTable != null)
        {
            var selTable = AppContext.Instance.SelectedTable;
            targetOrder = _activeOrders.FirstOrDefault(o => o.TableId == selTable.Id)
                       ?? _activeOrders.FirstOrDefault(o => o.TableNumber == selTable.Number);
        }

        // 3. TỰ ĐỘNG CHỌN BÀN SỐ NHỎ NHẤT (Nếu không có bàn nào đang chọn)
        if (targetOrder == null && _activeOrders.Any())
        {
            targetOrder = _activeOrders.First(); // Ưu tiên T1, T2...
        }

        // 4. Cập nhật giao diện
        SelectedTableOrder = targetOrder;

        OnPropertyChanged(nameof(IsQRSelected));
        OnPropertyChanged(nameof(IsCashSelected));
        OnPropertyChanged(nameof(CanConfirmPayment));
    }

    /// <summary>
    /// Xóa các hóa đơn mồ côi (stale invoices) — hóa đơn đã tồn tại trong Invoices
    /// nhưng đơn hàng tương ứng vẫn còn ở trạng thái Active (chưa thanh toán xong).
    /// Đây là dữ liệu lỗi do thanh toán trước đó bị gián đoạn giữa chừng.
    /// </summary>
    private void CleanupStaleInvoices()
    {
        var activeOrderIds = AppContext.Instance.Orders
            .Where(o => o.Status == OrderStatus.Active)
            .Select(o => o.Id)
            .ToHashSet();

        var staleInvoices = AppContext.Instance.Invoices
            .Where(inv => activeOrderIds.Contains(inv.OrderId))
            .ToList();

        foreach (var stale in staleInvoices)
        {
            AppContext.Instance.Invoices.Remove(stale);
            // Xóa bất đồng bộ trên Firebase (fire-and-forget, lỗi thì bỏ qua)
            _ = Task.Run(async () =>
            {
                try
                {
                    var key = $"invoice_{stale.Id}";
                    await _firebase.DeleteInvoiceAsync(key);
                    System.Diagnostics.Debug.WriteLine($"[Payment] Cleaned stale invoice #{stale.Id} for active order #{stale.OrderId}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Payment] Failed to clean stale invoice #{stale.Id}: {ex.Message}");
                }
            });
        }
    }

    public bool HasSelectedOrder => _selectedTableOrder != null;

    public Order? SelectedTableOrder
    {
        get => _selectedTableOrder;
        set
        {
            if (_selectedTableOrder != value)
            {
                _selectedTableOrder = value;

                if (value != null)
                {
                    AppContext.Instance.SelectedOrder = value;
                    AppContext.Instance.SelectedTable = AppContext.Instance.Tables.FirstOrDefault(t => t.Id == value.TableId)
                                                     ?? AppContext.Instance.Tables.FirstOrDefault(t => t.Number == value.TableNumber);
                }

                OnPropertyChanged(nameof(SelectedTableOrder));
                OnPropertyChanged(nameof(HasSelectedOrder));
                OnPropertyChanged(nameof(CanConfirmPayment));
                OnPropertyChanged(nameof(TransferContent));
                OnPropertyChanged(nameof(InvoiceCode));
                OnPropertyChanged(nameof(DiscountDisplay));

                // Reset cash entry khi đổi bàn
                if (CashReceivedEntry != null)
                    CashReceivedEntry.Text = "";
                if (ChangeAmountLabel != null)
                    ChangeAmountLabel.Text = "0 đ";
                if (CashErrorLabel != null)
                    CashErrorLabel.IsVisible = false;

                UpdateQrCode();
            }
        }
    }

    public ObservableCollection<Order> ActiveOrders => _activeOrders;
    public bool HasBillableTables => _activeOrders.Any();

    public bool IsQRSelected => _selectedPaymentMethod == PaymentMethod.Qr;
    public bool IsCashSelected => _selectedPaymentMethod == PaymentMethod.Cash;
    public bool CanConfirmPayment => _selectedPaymentMethod.HasValue && SelectedTableOrder != null;

    public string? QrImageUrl
    {
        get => _qrImageUrl;
        set { _qrImageUrl = value; OnPropertyChanged(nameof(QrImageUrl)); }
    }

    public bool IsQrLoading
    {
        get => _isQrLoading;
        set { _isQrLoading = value; OnPropertyChanged(nameof(IsQrLoading)); }
    }

    public string TransferContent
    {
        get
        {
            if (SelectedTableOrder == null)
                return "";

            var date = DateTime.Now.ToString("dd/MM/yyyy");
            return $"Nội dung: Thanh toán Bàn {SelectedTableOrder.TableNumber} - {date}";
        }
    }

    private string BuildInvoiceCode()
    {
        if (SelectedTableOrder == null) return "";
        var ts = DateTime.Now.ToString("ddMMyyHHmm");
        return $"HD{SelectedTableOrder.Id:D3}B{SelectedTableOrder.TableNumber}{ts}";
    }

    public string InvoiceCode => BuildInvoiceCode();

    public string DiscountDisplay
    {
        get
        {
            if (SelectedTableOrder == null)
                return "0 đ";

            return Formatters.FormatCurrency(SelectedTableOrder.Discount);
        }
    }

    private void UpdateQrCode()
    {
        if (SelectedTableOrder == null || !IsQRSelected)
        {
            QrImageUrl = null;
            return;
        }

        IsQrLoading = true;
        var amount = SelectedTableOrder.Total;
        var info = BuildInvoiceCode();
        IsQrLoading = false;
        QrImageUrl = VietQRService.BuildQrImageUrl(amount, info);
    }

    private void OnPaymentMethodTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not string method)
            return;

        _selectedPaymentMethod = method switch
        {
            "QR" => PaymentMethod.Qr,
            "Cash" => PaymentMethod.Cash,
            _ => null
        };

        OnPropertyChanged(nameof(IsQRSelected));
        OnPropertyChanged(nameof(IsCashSelected));
        OnPropertyChanged(nameof(CanConfirmPayment));

        // Highlight border
        QrBorder.Stroke = IsQRSelected ? Color.FromArgb("#6366F1") : (Color)Application.Current.Resources["BorderLight"];
        QrBorder.StrokeThickness = IsQRSelected ? 3 : 2;
        CashBorder.Stroke = IsCashSelected ? Color.FromArgb("#6366F1") : (Color)Application.Current.Resources["BorderLight"];
        CashBorder.StrokeThickness = IsCashSelected ? 3 : 2;

        // Reset cash entry khi đổi phương thức
        if (CashReceivedEntry != null)
            CashReceivedEntry.Text = "";
        if (ChangeAmountLabel != null)
            ChangeAmountLabel.Text = "0 đ";
        if (CashErrorLabel != null)
            CashErrorLabel.IsVisible = false;

        UpdateQrCode();
    }

    private bool _isFormattingCash;

    private void OnCashReceivedChanged(object sender, TextChangedEventArgs e)
    {
        if (_isFormattingCash || SelectedTableOrder == null) return;

        var entry = (Entry)sender;
        var raw = (e.NewTextValue ?? "").Replace(".", "").Replace("đ", "").Replace(" ", "");

        if (string.IsNullOrEmpty(raw))
        {
            ChangeAmountLabel.Text = "0 đ";
            CashErrorLabel.IsVisible = false;
            return;
        }

        if (!decimal.TryParse(raw, out var cashReceived))
            return;

        var total = SelectedTableOrder.Total;
        var change = cashReceived - total;

        if (change >= 0)
        {
            ChangeAmountLabel.Text = $"{change:N0} đ".Replace(",", ".");
            ChangeAmountLabel.TextColor = (Color)Application.Current.Resources["Success"];
            CashErrorLabel.IsVisible = false;
        }
        else
        {
            ChangeAmountLabel.Text = "0 đ";
            CashErrorLabel.Text = $"Còn thiếu {Math.Abs(change):N0} đ".Replace(",", ".");
            CashErrorLabel.IsVisible = true;
        }

        // Format with dots (no suffix — "đ" is a separate Label)
        _isFormattingCash = true;
        var formatted = $"{cashReceived:N0}".Replace(",", ".");
        entry.Text = formatted;
        entry.CursorPosition = formatted.Length;
        _isFormattingCash = false;
    }

    private async void OnApplyDiscountClicked(object sender, EventArgs e)
    {
        var discountCode = DiscountCodeEntry.Text?.ToUpper().Trim();

        if (string.IsNullOrEmpty(discountCode))
        {
            DiscountErrorLabel.Text = "Vui lòng nhập mã giảm giá";
            DiscountErrorLabel.IsVisible = true;
            return;
        }

        if (SelectedTableOrder == null)
            return;

        decimal discountAmount = 0;

        discountAmount = discountCode switch
        {
            "WELCOME10" => SelectedTableOrder.Subtotal * 0.1m,
            "VIP20" => SelectedTableOrder.Subtotal * 0.2m,
            "SAVE50K" => 50000m,
            _ => -1
        };

        if (discountAmount < 0)
        {
            DiscountErrorLabel.Text = "Mã giảm giá không hợp lệ";
            DiscountErrorLabel.IsVisible = true;
            return;
        }

        SelectedTableOrder.Discount = (int)discountAmount;
        DiscountErrorLabel.IsVisible = false;
        DiscountCodeEntry.Text = "";

        OnPropertyChanged(nameof(SelectedTableOrder));
        OnPropertyChanged(nameof(DiscountDisplay));
        UpdateQrCode();

        await DisplayAlert("Thành công", $"Áp dụng mã giảm giá thành công: {Formatters.FormatCurrency(discountAmount)}", "OK");
    }

    private async void OnConfirmPaymentClicked(object sender, EventArgs e)
    {
        if (_isProcessingPayment) return;

        if (!_selectedPaymentMethod.HasValue)
        {
            await DisplayAlert("Lỗi", "Vui lòng chọn phương thức thanh toán", "OK");
            return;
        }

        if (SelectedTableOrder == null) return;

        // Chặn thanh toán tiền mặt khi chưa đủ tiền
        if (_selectedPaymentMethod == PaymentMethod.Cash)
        {
            var rawCash = (CashReceivedEntry.Text ?? "").Replace(".", "").Replace("đ", "").Replace(" ", "");
            if (!decimal.TryParse(rawCash, out var cashAmount) || cashAmount < SelectedTableOrder.Total)
            {
                await DisplayAlert("Lỗi", "Số tiền khách đưa chưa đủ để thanh toán.", "OK");
                return;
            }
        }

        // Hộp thoại xác nhận
        var methodName = _selectedPaymentMethod == PaymentMethod.Qr ? "QR chuyển khoản" : "Tiền mặt";
        var confirm = await DisplayAlert(
            "Xác nhận thanh toán",
            $"Bàn {SelectedTableOrder.TableNumber} — {SelectedTableOrder.TotalDisplay}\nPhương thức: {methodName}\n\nBạn có chắc chắn muốn thanh toán?",
            "Xác nhận", "Hủy");

        if (!confirm) return;

        var order = SelectedTableOrder;
        var table = AppContext.Instance.Tables.FirstOrDefault(t => t.Id == order.TableId)
                 ?? AppContext.Instance.Tables.FirstOrDefault(t => t.Number == order.TableNumber);
        if (table == null)
        {
            await DisplayAlert("Lỗi", "Không tìm thấy bàn của đơn hàng.", "OK");
            return;
        }

        // Kiểm tra và xử lý hóa đơn đã tồn tại
        var existingInvoice = AppContext.Instance.Invoices.FirstOrDefault(invoice => invoice.OrderId == order.Id);
        if (existingInvoice != null)
        {
            if (order.Status == OrderStatus.Active)
            {
                // Đơn hàng vẫn Active nhưng đã có invoice → invoice mồ côi từ lần thanh toán lỗi trước.
                // Xóa invoice cũ và cho phép thanh toán lại.
                AppContext.Instance.Invoices.Remove(existingInvoice);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var delKey = $"invoice_{existingInvoice.Id}";
                        await _firebase.DeleteInvoiceAsync(delKey);
                        System.Diagnostics.Debug.WriteLine($"[Payment] Removed stale invoice #{existingInvoice.Id} for retry.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Payment] Stale invoice cleanup error: {ex.Message}");
                    }
                });
            }
            else
            {
                await DisplayAlert("Thông báo", "Đơn hàng này đã có hóa đơn thanh toán.", "OK");
                RefreshPage();
                return;
            }
        }

        _isProcessingPayment = true;
        try
        {
            var paidAt = DateTime.Now;
            var invoiceId = AppContext.Instance.Invoices.Count == 0
                ? 1
                : AppContext.Instance.Invoices.Max(invoice => invoice.Id) + 1;
            // Resolve tên nhân viên thực từ Firebase thông qua ServerId
            var resolvedServerName = AppContext.Instance.ResolveServerName(order.ServerId, order.ServerName);

            var invoice = new Invoice
            {
                Id = invoiceId,
                OrderId = order.Id,
                TableNumber = order.TableNumber,
                ServerName = resolvedServerName,
                CreatedAt = paidAt,
                PaymentMethod = _selectedPaymentMethod.Value,
                Discount = order.Discount,
                Total = order.Total,
                Items = order.Items.ToList()
            };

            await _firebase.CompletePaymentAsync(order, table, invoice);

            RevenueUpdateResult? revenueUpdate = null;
            Exception? revenueError = null;
            try
            {
                revenueUpdate = await _firebase.IncrementRevenueAsync(paidAt, invoice.Total);
            }
            catch (Exception ex)
            {
                revenueError = ex;
            }

            order.Status = OrderStatus.Paid;
            order.PaymentMethod = invoice.PaymentMethod;
            table.Status = TableStatus.NeedsClearing;
            table.CurrentOrderId = null;
            table.HasOrdered = false;
            table.OrderTotal = string.Empty;
            table.OrderItemCount = 0;

            if (!AppContext.Instance.Invoices.Any(existing => existing.OrderId == order.Id))
                AppContext.Instance.Invoices.Add(invoice);
            AppContext.Instance.Orders.Remove(order);
            if (!AppContext.Instance.OrderHistory.Any(existing => existing.Id == order.Id))
                AppContext.Instance.OrderHistory.Add(order);

            if (revenueUpdate != null)
            {
                ApplyRevenuePoint(AppContext.Instance.RevenueDaily, revenueUpdate.DailyLabel, revenueUpdate.DailyValue);
                ApplyRevenuePoint(AppContext.Instance.RevenueWeekly, revenueUpdate.WeeklyLabel, revenueUpdate.WeeklyValue);
                ApplyRevenuePoint(AppContext.Instance.RevenueMonthly, revenueUpdate.MonthlyLabel, revenueUpdate.MonthlyValue);
            }

            ActivityLogService.Instance.LogPayment(table.Number.ToString());
            AppContext.Instance.SelectedOrder = null;
            AppContext.Instance.SelectedTable = null;
            AppContext.Instance.RefreshBadges();

            if (revenueError == null)
            {
                await DisplayAlert("Thành công", $"✓ Đã thanh toán Bàn {order.TableNumber}\nSố tiền: {order.TotalDisplay}", "OK");
            }
            else
            {
                await DisplayAlert(
                    "Đã thanh toán",
                    $"Hóa đơn đã được lưu, nhưng cập nhật báo cáo doanh thu chưa thành công: {revenueError.Message}",
                    "OK");
            }

            RefreshPage();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể hoàn tất thanh toán: {ex.Message}", "OK");
        }
        finally
        {
            _isProcessingPayment = false;
        }
    }

    private static void ApplyRevenuePoint(
        ObservableCollection<RevenuePoint> collection, string label, decimal value)
    {
        var existing = collection.FirstOrDefault(point =>
            string.Equals(point.Label, label, StringComparison.OrdinalIgnoreCase));
        var replacement = new RevenuePoint { Label = label, Value = value };
        if (existing == null)
        {
            collection.Add(replacement);
            return;
        }

        collection[collection.IndexOf(existing)] = replacement;
    }
}
