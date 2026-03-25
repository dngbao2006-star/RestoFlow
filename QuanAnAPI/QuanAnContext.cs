using Microsoft.EntityFrameworkCore;

namespace QuanAnAPI
{
    public class QuanAnContext : DbContext
    {
        public QuanAnContext(DbContextOptions<QuanAnContext> options) : base(options) { }
        public DbSet<MonAn> MonAns { get; set; }
        public DbSet<TaiKhoan> TaiKhoans { get; set; }
    }
}