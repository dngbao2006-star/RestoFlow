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

app.Run();