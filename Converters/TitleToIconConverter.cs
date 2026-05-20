using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace AppManagermentRestaurant.Converters;

public class TitleToIconConverter : IValueConverter
{
    private const string DefaultIconPath = "M12,2A10,10 0 1,0 12,22A10,10 0 1,0 12,2Z";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string title)
        {
            return title switch
            {
                //Staff tabs icon
                "Sơ đồ bàn" => "so_do_ban.png",
                "Tạo đơn" => "dat_mon.png",
                "Trạng thái món" => "trang_thai_mon.png",
                "Thanh toán" => "thanh_toan.png",
                "Nhắn tin" => "nhan_tin.png",
                "Lịch sử đơn" => "lich_su.png",
                "Tài khoản" => "tai_khoan.png",

                //Manager tabs icon
                "Quản lý đơn hàng" => "dat_mon.png",
                "Tổng quan" => "tong_quan.png",
                "Nhân sự" => "nhan_su.png",
                "Thực đơn" => "thuc_don.png",
                "Sơ đồ khu vực" => "so_do_khu_vuc.png",
                "Doanh thu" => "doanh_thu.png",
                "Hóa đơn" => "hoa_don.png",
                "Thông báo" => "thong_bao.png",
                "Cấu hình hệ thống" => "cai_dat.png",
                _ => "dotnet_bot.png" // File ảnh dự phòng nếu không tìm thấy tab
            };
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
