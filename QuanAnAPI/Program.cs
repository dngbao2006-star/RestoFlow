using Microsoft.EntityFrameworkCore;
using QuanAnAPI;

var builder = WebApplication.CreateBuilder(args);

// 1. CẤU HÌNH KẾT NỐI DATABASE
builder.Services.AddDbContext<QuanAnContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. CẤU HÌNH MÔI TRƯỜNG (SWAGGER)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();     
    app.UseSwaggerUI();   
}

app.UseHttpsRedirection();

// 3. API LẤY DANH SÁCH MÓN ĂN
app.MapGet("/api/Menu", async (QuanAnContext db) =>
{
    return await db.MonAns.ToListAsync();
});

// 4. API ĐĂNG KÝ TÀI KHOẢN

// MapPost: tạo API dạng POST
// DangKyModel: dữ liệu client gửi lên (JSON)
app.MapPost("/api/TaiKhoan/DangKy", (QuanAnContext db, DangKyModel model) =>
{
   
    var daTonTai = db.TaiKhoans.Any(t => t.TenDangNhap == model.TenDangNhap);

    
    if (daTonTai)
    {
        return Results.BadRequest(new
        {
            message = "Tên đăng nhập đã tồn tại!"
        });
    }

    
    var tkMoi = new TaiKhoan
    {
        TenDangNhap = model.TenDangNhap, 
        MatKhau = model.MatKhau,         
        Email = model.Email,           
        DaXacThucEmail = false,       
        Quyen = "NhanVien"             
    };

    
    db.TaiKhoans.Add(tkMoi);

    
    db.SaveChanges();

    
    return Results.Ok(new
    {
        message = "Đăng ký tài khoản thành công!"
    });
});

// 5. API ĐĂNG NHẬP

app.MapPost("/api/TaiKhoan/DangNhap", (QuanAnContext db, DangNhapModel model) =>
{
  
    var tk = db.TaiKhoans.FirstOrDefault(t =>
        t.TenDangNhap == model.TenDangNhap &&
        t.MatKhau == model.MatKhau
    );
    if (tk == null)
    {
        return Results.BadRequest(new
        {
            message = "Sai tên đăng nhập hoặc mật khẩu!"
        });
    }
    return Results.Ok(new
    {
        message = "Đăng nhập thành công!",
        tenDangNhap = tk.TenDangNhap,
        quyen = tk.Quyen
    });
});

// 6. CHẠY ỨNG DỤNG
app.Run();

//MODEL NHẬN DỮ LIỆU TỪ CLIENT
// Model dùng cho API Đăng ký
public class DangKyModel
{
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Model dùng cho API Đăng nhập
public class DangNhapModel
{
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
}