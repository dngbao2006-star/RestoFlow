using Microsoft.EntityFrameworkCore;

namespace QuanAnAPI
{
    public class QuanAnContext : DbContext
    {
        public QuanAnContext(DbContextOptions<QuanAnContext> options) : base(options) { }
        public DbSet<MonAn> MonAns { get; set; } // Nếu bảng trong SQL của bạn tên khác, nhớ báo mình nhé!
    }
}