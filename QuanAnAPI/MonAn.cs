using System.ComponentModel.DataAnnotations.Schema;

namespace QuanAnAPI
{
    [Table("MonAn")] // Chỉ định chính xác tên bảng trong SQL của bạn
    public class MonAn
    {
        public int Id { get; set; }
        public string TenMon { get; set; }
        public decimal Gia { get; set; }
    }
}