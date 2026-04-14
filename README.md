Hướng dẫn Cài đặt và Sử dụng AppManagermentRestaurant
AppManagermentRestaurant (Golden Plate) là ứng dụng quản lý nhà hàng đa nền tảng (.NET MAUI). Ứng dụng hỗ trợ toàn bộ luồng nghiệp vụ từ phục vụ, thanh toán đến quản lý nhân sự và doanh thu.

1. Yêu cầu hệ thống (Thiết lập môi trường)
Để chạy và phát triển dự án, hệ thống cần đáp ứng các yêu cầu sau:

Công cụ phát triển:

IDE: Visual Studio 2026 (phiên bản 18.4 trở lên) đã cài đặt workload .NET Multi-platform App UI development.

SDK: .NET 10 SDK.

Môi trường đích (Target Frameworks):
Dự án hỗ trợ các nền tảng với phiên bản hệ điều hành tối thiểu:

Android: net10.0-android (API 21 / Android 5.0+).

iOS / macOS: net10.0-ios, net10.0-maccatalyst (iOS 15.0+ / macOS 15.0+).

Windows: net10.0-windows10.0.19041.0 (Windows 10 Build 17763+).

2. Thư viện sử dụng (Dependencies)
Dự án sử dụng các NuGet packages chính sau:

Microsoft.Maui.Controls: Thư viện lõi .NET MAUI.

CommunityToolkit.Maui (v9.0.0): Cung cấp các UI components và tiện ích mở rộng.

Microsoft.Extensions.Logging.Debug (v10.0.0): Hỗ trợ ghi log hệ thống.

3. Hướng dẫn cài đặt & Chạy ứng dụng
Bước 1: Tải dự án và cài đặt các gói phụ thuộc
Mở terminal tại thư mục gốc dự án:

Bash
# Tải các thư viện (restore packages)
dotnet restore
Bước 2: Khởi chạy ứng dụng
Mở file solution (.sln) bằng Visual Studio 2026, chọn Target Platform và nhấn F5. Hoặc dùng lệnh:

Chạy trên Windows: dotnet build -f net10.0-windows10.0.19041.0

Chạy trên Android: dotnet build -f net10.0-android

Lưu ý: Dự án sử dụng XAML Source Generation để tối ưu hiệu năng build. File font PlayfairDisplay-Regular.ttf cần có trong Resources/Fonts/.

4. Tài khoản Demo (Mock Data)
Đăng nhập bằng các tài khoản sau (mật khẩu bất kỳ):

Quản lý: quan.manager@goldenplate.vn

Nhân viên: tuan.staff@goldenplate.vn

5. Các tính năng chính
👨‍🍳 Cho Nhân viên (Staff)
Sơ đồ bàn: Theo dõi trạng thái (Trống, Có khách, Đặt trước) theo từng khu vực. Hỗ trợ mở bàn, chuyển bàn, ghép đơn.

Đơn hàng & Món ăn: Tạo đơn, ghi chú yêu cầu, theo dõi tiến độ bếp (Chờ → Đang làm → Sẵn sàng).

Thanh toán: Xử lý bill qua Tiền mặt hoặc mã QR.

👔 Cho Quản lý (Manager)
Dashboard: Theo dõi doanh thu, biểu đồ hoạt động và nhân sự trong ngày.

Quản trị: Quản lý danh sách nhân viên, danh mục thực đơn, sơ đồ khu vực và cấu hình hệ thống.

Báo cáo: Thống kê hóa đơn, top món bán chạy và xuất dữ liệu doanh thu.