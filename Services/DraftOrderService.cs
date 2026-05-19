using System.Text.Json;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Constants;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AppManagermentRestaurant.Services;

/// <summary>
/// Singleton service quản lý đơn hàng nháp (chưa gửi lên bếp).
/// Tầng 1: Lưu trong RAM (chống mất khi chuyển page).
/// Tầng 2: Auto-save JSON xuống Local Storage (chống mất khi crash/tắt app).
/// </summary>
public class DraftOrderService
{
    private static readonly Lazy<DraftOrderService> _instance = new(() => new DraftOrderService());
    public static DraftOrderService Instance => _instance.Value;

    // Tầng 1: Dictionary lưu draft order theo TableId (RAM)
    private readonly Dictionary<int, Order> _drafts = new();

    private DraftOrderService() { }

    /// <summary>
    /// Lưu draft order vào RAM + ghi JSON xuống disk.
    /// </summary>
    public void SaveDraft(int tableId, Order order)
    {
        _drafts[tableId] = order;
        _ = SaveToDiskAsync(tableId, order);
    }

    /// <summary>
    /// Lấy draft từ RAM. Nếu không có trong RAM, thử đọc từ disk.
    /// </summary>
    public async Task<Order?> GetDraftAsync(int tableId)
    {
        if (_drafts.TryGetValue(tableId, out var draft))
            return draft;

        // Fallback: đọc từ disk
        var diskDraft = await LoadFromDiskAsync(tableId);
        if (diskDraft != null)
            _drafts[tableId] = diskDraft;

        return diskDraft;
    }

    /// <summary>
    /// Lấy draft từ RAM (sync, không đọc disk).
    /// </summary>
    public Order? GetDraft(int tableId)
    {
        return _drafts.TryGetValue(tableId, out var draft) ? draft : null;
    }

    /// <summary>
    /// Xóa draft sau khi gửi lên bếp thành công.
    /// </summary>
    public void ClearDraft(int tableId)
    {
        _drafts.Remove(tableId);
        _ = DeleteFromDiskAsync(tableId);
    }

    /// <summary>
    /// Cập nhật draft (gọi mỗi khi thêm/bớt/sửa món).
    /// </summary>
    public void UpdateDraft(int tableId, Order order)
    {
        SaveDraft(tableId, order);
    }

    // === Tầng 2: Local Storage (JSON) ===

    private static string GetFilePath(int tableId)
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "drafts");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"draft_table_{tableId}.json");
    }

    private static async Task SaveToDiskAsync(int tableId, Order order)
    {
        try
        {
            var dto = new DraftOrderDto
            {
                OrderId = order.Id,
                TableId = order.TableId,
                TableNumber = order.TableNumber,
                ServerName = order.ServerName,
                ServerId = order.ServerId,
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(i => new DraftOrderItemDto
                {
                    Id = i.Id,
                    MenuItemId = i.MenuItemId,
                    Name = i.Name,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    Image = i.Image,
                    Notes = i.Notes,
                    Status = i.Status.ToString()
                }).ToList()
            };

            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = false });
            await File.WriteAllTextAsync(GetFilePath(tableId), json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DraftOrderService] SaveToDisk error: {ex.Message}");
        }
    }

    private static async Task<Order?> LoadFromDiskAsync(int tableId)
    {
        try
        {
            var path = GetFilePath(tableId);
            if (!File.Exists(path)) return null;

            var json = await File.ReadAllTextAsync(path);
            var dto = JsonSerializer.Deserialize<DraftOrderDto>(json);
            if (dto == null) return null;

            var order = new Order
            {
                Id = dto.OrderId,
                TableId = dto.TableId,
                TableNumber = dto.TableNumber,
                ServerName = dto.ServerName,
                ServerId = dto.ServerId,
                CreatedAt = dto.CreatedAt,
                Status = OrderStatus.Active,
                Items = new ObservableCollection<OrderItem>()
            };

            foreach (var itemDto in dto.Items)
            {
                order.Items.Add(new OrderItem
                {
                    Id = itemDto.Id,
                    MenuItemId = itemDto.MenuItemId,
                    Name = itemDto.Name,
                    Price = itemDto.Price,
                    Quantity = itemDto.Quantity,
                    Image = itemDto.Image,
                    Notes = itemDto.Notes,
                    Status = Enum.TryParse<DishStatus>(itemDto.Status, out var s) ? s : DishStatus.Pending
                });
            }

            return order;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DraftOrderService] LoadFromDisk error: {ex.Message}");
            return null;
        }
    }

    private static async Task DeleteFromDiskAsync(int tableId)
    {
        try
        {
            var path = GetFilePath(tableId);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DraftOrderService] DeleteFromDisk error: {ex.Message}");
        }
    }
}

// === DTOs for JSON serialization ===

internal class DraftOrderDto
{
    public int OrderId { get; set; }
    public int TableId { get; set; }
    public int TableNumber { get; set; }
    public string ServerName { get; set; } = "";
    public string ServerId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<DraftOrderItemDto> Items { get; set; } = new();
}

internal class DraftOrderItemDto
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Image { get; set; } = "";
    public string? Notes { get; set; }
    public string Status { get; set; } = "Pending";
}
