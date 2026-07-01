using System.Net;
using System.Text;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;

namespace AppManagermentRestaurant.Services;

public static class InvoiceDocumentService
{
    public static async Task OpenPrintableInvoiceAsync(
        Invoice invoice,
        SystemConfiguration? configuration = null)
    {
        configuration ??= AppContext.Instance.SystemConfiguration;
        var fileName = $"hoa-don-{invoice.Id}-{DateTime.Now:yyyyMMddHHmmss}.html";
        var path = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllTextAsync(path, BuildHtml(invoice, configuration), Encoding.UTF8);
        await Launcher.Default.OpenAsync(new OpenFileRequest(
            $"Hóa đơn #{invoice.Id}",
            new ReadOnlyFile(path)));
    }

    public static Invoice FromOrder(Order order)
    {
        var existing = AppContext.Instance.Invoices.FirstOrDefault(invoice => invoice.OrderId == order.Id);
        if (existing != null) return existing;

        // Resolve tên nhân viên thực từ ServerId
        var resolvedName = AppContext.Instance.ResolveServerName(order.ServerId, order.ServerName);

        return new Invoice
        {
            Id = order.Id,
            OrderId = order.Id,
            TableNumber = order.TableNumber,
            ServerName = resolvedName,
            CreatedAt = order.CreatedAt,
            PaymentMethod = order.PaymentMethod,
            Discount = order.Discount,
            Total = order.Total,
            Items = order.Items
        };
    }

    private static string BuildHtml(Invoice invoice, SystemConfiguration config)
    {
        static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var itemRows = string.Join(Environment.NewLine, invoice.Items.Select(item => $"""
            <tr>
              <td>{H(item.Name)}</td>
              <td class="number">{item.Quantity}</td>
              <td class="number">{H(Formatters.FormatCurrency(item.Price))}</td>
              <td class="number">{H(item.LineTotalDisplay)}</td>
            </tr>
            """));
        var subtotal = invoice.Items.Sum(item => item.LineTotal);
        var payment = invoice.PaymentMethod == PaymentMethod.Qr ? "Chuyển khoản QR" : "Tiền mặt";

        return $$$"""
        <!doctype html>
        <html lang="vi"><head><meta charset="utf-8"><title>Hóa đơn #{{{invoice.Id}}}</title>
        <style>
          body{font-family:Arial,sans-serif;color:#1f2937;max-width:760px;margin:32px auto;padding:0 20px}
          h1,h2,p{margin:4px 0}.center{text-align:center}.meta{margin:24px 0;line-height:1.6}
          table{width:100%;border-collapse:collapse;margin:18px 0}th,td{padding:10px 8px;border-bottom:1px solid #ddd;text-align:left}
          .number{text-align:right}.totals{margin-left:auto;width:320px}.totals div{display:flex;justify-content:space-between;padding:6px 0}
          .grand{font-size:20px;font-weight:bold;border-top:2px solid #1f2937}.footer{margin-top:34px;text-align:center;color:#6b7280}
          @media print{button{display:none}body{margin:0}}
        </style></head><body>
          <div class="center"><h1>{{{H(config.RestaurantName)}}}</h1><p>{{{H(config.Address)}}}</p><p>{{{H(config.Phone)}}} · {{{H(config.Email)}}}</p><p>MST: {{{H(config.TaxId)}}}</p></div>
          <div class="meta"><h2>HÓA ĐƠN #{{{invoice.Id}}}</h2><div>Bàn: {{{invoice.TableNumber}}}</div><div>Nhân viên: {{{H(invoice.ServerName)}}}</div><div>Thời gian: {{{H(invoice.CreatedAtDisplay)}}}</div><div>Thanh toán: {{{payment}}}</div></div>
          <table><thead><tr><th>Món</th><th class="number">SL</th><th class="number">Đơn giá</th><th class="number">Thành tiền</th></tr></thead><tbody>{{{itemRows}}}</tbody></table>
          <div class="totals"><div><span>Tạm tính</span><span>{{{H(Formatters.FormatCurrency(subtotal))}}}</span></div><div><span>Giảm giá</span><span>- {{{H(Formatters.FormatCurrency(invoice.Discount))}}}</span></div><div class="grand"><span>Tổng cộng</span><span>{{{H(invoice.TotalDisplay)}}}</span></div></div>
          <div class="footer">{{{H(config.InvoiceFooter)}}}</div>
          <script>window.addEventListener('load',()=>setTimeout(()=>window.print(),250));</script>
        </body></html>
        """;
    }
}
