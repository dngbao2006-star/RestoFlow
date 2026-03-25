using Microsoft.EntityFrameworkCore;
using QuanAnAPI;

var builder = WebApplication.CreateBuilder(args);

// 1. Kết nối Database
builder.Services.AddDbContext<QuanAnContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 2. Viết API lấy danh sách món ăn
app.MapGet("/api/Menu", async (QuanAnContext db) =>
{
    return await db.MonAns.ToListAsync();
});


// ==========================================
// 1. API ĐĂNG KÝ TÀI KHOẢN
// ==========================================
app.MapPost("/api/TaiKhoan/DangKy", (QuanAnAPI.QuanAnContext db, DangKyModel model) =>
{
    // Kiểm tra xem tên đăng nhập đã bị ai lấy chưa
    var daTonTai = db.TaiKhoans.Any(t => t.TenDangNhap == model.TenDangNhap);
    if (daTonTai)
    {
        return Results.BadRequest(new { message = "Tên đăng nhập đã tồn tại!" });
    }

    // Tạo tài khoản mới
    var tkMoi = new QuanAnAPI.TaiKhoan
    {
        TenDangNhap = model.TenDangNhap,
        MatKhau = model.MatKhau, // Demo đồ án tạm lưu chuỗi thường
        Email = model.Email,
        DaXacThucEmail = false,
        Quyen = "NhanVien"
    };

    db.TaiKhoans.Add(tkMoi);
    db.SaveChanges();

    return Results.Ok(new { message = "Đăng ký tài khoản thành công!" });
});

// ==========================================
// 2. API ĐĂNG NHẬP
// ==========================================
app.MapPost("/api/TaiKhoan/DangNhap", (QuanAnAPI.QuanAnContext db, DangNhapModel model) =>
{
    // Tìm tài khoản khớp cả Tên đăng nhập và Mật khẩu
    var tk = db.TaiKhoans.FirstOrDefault(t => t.TenDangNhap == model.TenDangNhap && t.MatKhau == model.MatKhau);

    if (tk == null)
    {
        return Results.BadRequest(new { message = "Sai tên đăng nhập hoặc mật khẩu!" });
    }

    // Trả về thông tin khi đăng nhập thành công
    return Results.Ok(new
    {
        message = "Đăng nhập thành công!",
        tenDangNhap = tk.TenDangNhap,
        quyen = tk.Quyen
    });
});


app.Run();
public class DangKyModel
{
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class DangNhapModel
{
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
}

