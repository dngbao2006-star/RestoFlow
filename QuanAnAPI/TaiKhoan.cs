using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanAnAPI
{
    [Table("TaiKhoan")]
    public class TaiKhoan
    {
        [Key]
        public int Id { get; set; }

        public string TenDangNhap { get; set; } = string.Empty;

        public string MatKhau { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool DaXacThucEmail { get; set; }

        public string? MaXacThuc { get; set; }

        public string? Quyen { get; set; }
    }
}