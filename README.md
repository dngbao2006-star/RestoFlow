# 📋 THE GOLDEN PLATE — TÀI LIỆU ĐẶC TẢ HỆ THỐNG

### Dành cho tái thiết kế bằng C# .NET MAUI

> Phiên bản: 1.0 | Ngày: 08/04/2026 | Nền tảng gốc: React 18 + TypeScript + Tailwind CSS v4

---

## MỤC LỤC

1. [Kiến trúc hệ thống tổng quan](#1-kiến-trúc-hệ-thống-tổng-quan)
2. [Hệ thống thiết kế (Design System)](#2-hệ-thống-thiết-kế-design-system)
3. [Mô hình dữ liệu (Data Models)](#3-mô-hình-dữ-liệu-data-models)
4. [Luồng điều hướng (Navigation Flow)](#4-luồng-điều-hướng-navigation-flow)
5. [Trang Đăng nhập (LoginPage)](#5-trang-đăng-nhập-loginpage)
6. [Layout chung (AppLayout)](#6-layout-chung-applayout)
7. [STAFF — Sơ đồ bàn (TableManagement)](#7-staff--sơ-đồ-bàn-tablemanagement)
8. [STAFF — Đặt món (OrderCreation)](#8-staff--đặt-món-ordercreation)
9. [STAFF — Trạng thái món (DishStatus)](#9-staff--trạng-thái-món-dishstatus)
10. [STAFF — Thanh toán (Payment)](#10-staff--thanh-toán-payment)
11. [STAFF — Lịch sử đơn hàng (OrderHistory)](#11-staff--lịch-sử-đơn-hàng-orderhistory)
12. [STAFF — Chat nội bộ (Chat)](#12-staff--chat-nội-bộ-chat)
13. [STAFF — Tài khoản (AccountManagement)](#13-staff--tài-khoản-accountmanagement)
14. [MANAGER — Dashboard](#14-manager--dashboard)
15. [MANAGER — Quản lý nhân sự (HRManagement)](#15-manager--quản-lý-nhân-sự-hrmanagement)
16. [MANAGER — Quản lý thực đơn (MenuManagement)](#16-manager--quản-lý-thực-đơn-menumanagement)
17. [MANAGER — Quản lý sơ đồ bàn (LayoutManagement)](#17-manager--quản-lý-sơ-đồ-bàn-layoutmanagement)
18. [MANAGER — Doanh thu & Báo cáo (Revenue)](#18-manager--doanh-thu--báo-cáo-revenue)
19. [MANAGER — Quản lý hóa đơn (InvoiceManagement)](#19-manager--quản-lý-hóa-đơn-invoicemanagement)
20. [MANAGER — Thông báo Broadcast (NotificationsManagement)](#20-manager--thông-báo-broadcast-notificationsmanagement)
21. [MANAGER — Cấu hình hệ thống (SystemConfig)](#21-manager--cấu-hình-hệ-thống-systemconfig)
22. [Các lỗi logic cần sửa khi tái thiết kế](#22-các-lỗi-logic-cần-sửa-khi-tái-thiết-kế)

---

## 1. KIẾN TRÚC HỆ THỐNG TỔNG QUAN

### 1.1 Sơ đồ kiến trúc tổng thể

```
┌─────────────────────────────────────────────────────────────┐
│                    THE GOLDEN PLATE APP                      │
├─────────────────────────────────────────────────────────────┤
│  PRESENTATION LAYER (MAUI Pages + Views)                     │
│  ┌──────────────┐  ┌──────────────────────────────────────┐ │
│  │  Auth Pages  │  │         AppShell (Layout)            │ │
│  │  - LoginPage │  │  ┌─────────────┐ ┌────────────────┐  │ │
│  └──────────────┘  │  │  Sidebar    │ │  TopBar        │  │ │
│                    │  │  Navigation │ │  + Notif       │  │ │
│                    │  └─────────────┘ └────────────────┘  │ │
│                    │  ┌──────────────────────────────────┐  │ │
│                    │  │         Page Content Area        │  │ │
│                    │  └──────────────────────────────────┘  │ │
│                    └──────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│  BUSINESS LOGIC LAYER (ViewModels / Services)                │
│  ┌────────────┐ ┌───────────┐ ┌──────────┐ ┌────────────┐  │
│  │  AuthVM    │ │  OrderVM  │ │  TableVM │ │  ChatVM    │  │
│  └────────────┘ └───────────┘ └──────────┘ └────────────┘  │
├─────────────────────────────────────────────────────────────┤
│  DATA LAYER (Repository / Local DB / API)                    │
│  ┌─────────────┐ ┌────────────┐ ┌──────────────────────┐   │
│  │  SQLite DB  │ │  API Client│ │  SignalR (Real-time)  │   │
│  └─────────────┘ └────────────┘ └──────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 State Management trong MAUI

Trong React gốc dùng **Context API** (giống Provider Pattern). Trong MAUI nên dùng:

- **MVVM Pattern**: ViewModel + INotifyPropertyChanged cho mỗi trang
- **Singleton Services**: `AppStateService` giữ trạng thái toàn cục (user hiện tại, orders, tables, notifications, chatMessages)
- **ObservableCollection<T>**: cho các danh sách cần cập nhật UI tự động (orders, tables, notifications)
- **Dependency Injection**: đăng ký Services trong `MauiProgram.cs`

### 1.3 Các Service cần thiết

| Service               | Trách nhiệm                             |
| --------------------- | --------------------------------------- |
| `AuthService`         | Đăng nhập, đăng xuất, kiểm tra phiên    |
| `TableService`        | Quản lý trạng thái bàn, CRUD bàn        |
| `OrderService`        | Tạo đơn, cập nhật món, gộp/chuyển đơn   |
| `PaymentService`      | Thanh toán, tính giảm giá, xuất hóa đơn |
| `MenuService`         | CRUD thực đơn, quản lý tồn kho          |
| `ChatService`         | Gửi/nhận tin nhắn nội bộ (SignalR)      |
| `NotificationService` | Quản lý thông báo, push notification    |
| `ReportService`       | Tổng hợp doanh thu, báo cáo             |
| `StaffService`        | Quản lý nhân sự, phân quyền             |

### 1.4 Vai trò người dùng

| Vai trò                     | Màn hình được truy cập                                                                        |
| --------------------------- | --------------------------------------------------------------------------------------------- |
| `staff` (Nhân viên phục vụ) | Sơ đồ bàn, Đặt món, Trạng thái món, Thanh toán, Chat, Lịch sử đơn, Tài khoản                  |
| `manager` (Quản lý)         | Dashboard, Nhân sự, Thực đơn, Sơ đồ bàn (edit), Doanh thu, Hóa đơn, Chat, Thông báo, Cấu hình |

---

## 2. HỆ THỐNG THIẾT KẾ (DESIGN SYSTEM)

### 2.1 Bảng màu (Color Palette)

```
PRIMARY COLORS:
  Navy chính:       #1B3A6B  → Nút chính, sidebar, highlight
  Navy nhạt:        #4A90D9  → Accent, link, icon phụ

BACKGROUND COLORS:
  Nền tổng thể:     #F5F0E8  → Background toàn trang
  Nền thẻ/Card:     #FDFAF6  → Card, panel, modal
  Nền ô nhập liệu:  #EDE8DC  → Input, button phụ, chip

TEXT COLORS:
  Chữ chính:        #1A1209  → Tiêu đề, nội dung quan trọng
  Chữ đậm phụ:      #3D2C1E  → Label form
  Chữ trung bình:   #4A3B2E  → Text body bình thường
  Chữ mờ:           #7B6A57  → Placeholder, metadata, mô tả
  Chữ rất mờ:       #C5B8A8  → Timestamp, gợi ý nhỏ

BORDER COLOR:
  Viền chuẩn:       rgba(139, 110, 80, 0.15)  (1.5px)
  Viền form:        rgba(139, 110, 80, 0.20)  (1.5px)
  Viền đậm:         rgba(139, 110, 80, 0.25)  (2px)

STATUS COLORS:
  Thành công/Xanh:  #22C55E (nền) / #15803D (chữ) / rgba(34,197,94,0.12) (badge bg)
  Cảnh báo/Vàng:   #F59E0B (nền) / #92400E (chữ) / rgba(245,158,11,0.12) (badge bg)
  Lỗi/Đỏ:          #EF4444 (nền) / #B91C1C (chữ) / rgba(239,68,68,0.12) (badge bg)
  Thông tin/Xanh:  #4A90D9 (nền) / #1D4ED8 (chữ) / rgba(74,144,217,0.12) (badge bg)
  Xám trung tính:  #6B7280 (nền) / #374151 (chữ) / rgba(107,114,128,0.12) (badge bg)
  Hủy bỏ/Đỏ nhạt: #d4183d
  Loading/Xanh nhạt: #7B9CC8

SIDEBAR:
  Nền sidebar:      #1B3A6B
  Item active:      rgba(255,255,255,0.15)
  Item hover:       rgba(255,255,255,0.08)
  Chữ active:       #FFFFFF
  Chữ inactive:     rgba(255,255,255,0.65)
  Viền sidebar:     rgba(255,255,255,0.10)

OVERLAY:
  Modal backdrop:   rgba(0, 0, 0, 0.4)
```

### 2.2 Typography

| Element              | Font Size     | PX      | Weight  | Font Family          |
| -------------------- | ------------- | ------- | ------- | -------------------- |
| h1 (Tiêu đề trang)   | 1.5rem        | 24px    | 500     | **Playfair Display** |
| h2 (Tiêu đề phụ)     | 1.25rem       | 20px    | 500     | Inter                |
| h3 (Tiêu đề section) | 1.125rem      | 18px    | 500     | Inter                |
| h4 (Tiêu đề nhỏ)     | 1rem          | 16px    | 500     | Inter                |
| Body / p             | 1rem          | 16px    | 400     | Inter                |
| Label form           | 0.875rem      | 14px    | 500     | Inter                |
| Button text          | 1rem          | 16px    | 600     | Inter                |
| Input text           | 0.9rem        | 14.4px  | 400     | Inter                |
| Text phụ             | 0.875rem      | 14px    | 400     | Inter                |
| Text nhỏ             | 0.8rem        | 12.8px  | 400     | Inter                |
| Text metadata        | 0.78rem       | 12.5px  | 400     | Inter                |
| Badge / label        | 0.75rem       | 12px    | 400–600 | Inter                |
| Text rất nhỏ         | 0.72rem       | 11.5px  | 400     | Inter                |
| Text nhỏ nhất        | 0.68rem       | 10.9px  | 400     | Inter                |
| Số liệu lớn          | 1.5rem        | 24px    | 700     | Inter                |
| Số liệu vừa          | 1.3rem–1.4rem | 20–22px | 700     | Inter                |

> **Font import**: Google Fonts — `Inter` (300/400/500/600/700) + `Playfair Display` (400/500/600/700)

### 2.3 Khoảng cách & Bo góc (Spacing & Border Radius)

```
BORDER RADIUS:
  Bo nhỏ (badge, chip):   rounded-lg  = 8px
  Bo vừa (input, button): rounded-xl  = 12px
  Bo lớn (card, modal):   rounded-2xl = 16px
  Bo tròn (avatar):       rounded-full = 9999px

PADDING:
  Card/Panel:        p-4  = 16px  /  p-5  = 20px  /  p-6  = 24px
  Input:             py-2.5 = 10px / px-4 = 16px  (với icon: pl-10 = 40px)
  Button phụ:        px-3 = 12px  / py-1.5 = 6px
  Button chính:      px-4 = 16px  / py-2.5 = 10px → py-3 = 12px
  Button lớn:        w-full / py-3.5 = 14px
  Modal padding:     p-5 = 20px  /  p-6 = 24px
  Nav item:          px-3 = 12px / py-2.5 = 10px

GAP (khoảng cách giữa các phần tử):
  Khoảng nhỏ:       gap-1 = 4px / gap-1.5 = 6px / gap-2 = 8px
  Khoảng vừa:       gap-3 = 12px / gap-4 = 16px
  Khoảng lớn:       gap-6 = 24px
```

### 2.4 Kích thước thành phần chuẩn

```
ICON SIZES:
  Icon nhỏ (badge):      11–12px
  Icon vừa (inline):     14–16px
  Icon button phụ:       16–18px
  Icon button chính:     18–20px
  Icon lớn (header):     20–22px
  Icon hero (thành công): 32–48px

BUTTON SIZES (Height):
  Nút nhỏ:              h-9  = 36px  (icon-only action buttons)
  Nút vừa:              py-2.5 → ~40px total
  Nút chuẩn:            py-3   → ~44px total
  Nút lớn (CTA):        py-3.5 → ~48px total
  Nút icon round:       w-10 h-10 = 40×40px

AVATAR SIZES:
  Rất nhỏ (chat):       w-7 h-7  = 28×28px
  Nhỏ (nav):            w-9 h-9  = 36×36px
  Vừa (danh sách):      w-10 h-10 = 40×40px / w-12 h-12 = 48×48px
  Lớn (profile banner): w-16 h-16 = 64×64px

ICON CONTAINER SIZES:
  Nhỏ:    w-8 h-8  = 32×32px
  Vừa:    w-9 h-9  = 36×36px  /  w-10 h-10 = 40×40px
  Lớn:    w-11 h-11 = 44×44px / w-12 h-12 = 48×48px
  Hero:   w-16 h-16 = 64×64px / w-20 h-20 = 80×80px / w-24 h-24 = 96×96px

CARD IMAGE SIZES:
  Thẻ menu (grid):      h-28 = 112px (chiều cao ảnh)
  Ảnh item trong giỏ:  w-10 h-10 = 40×40px
  Ảnh item trong list:  w-12 h-12 = 48×48px
  Ảnh item trong order: w-8 h-8   = 32×32px

SHADOWS:
  Card hover:    hover:shadow-md
  Modal/Dropdown: shadow-xl
  Sidebar:       boxShadow: '4px 0 20px rgba(0,0,0,0.15)'
```

### 2.5 Các pattern tái sử dụng

**Badge trạng thái** (Status Badge):

```
Kích thước: px-2 py-0.5 → padding 8px × 2px
Bo góc: rounded-lg = 8px  hoặc  rounded-full = 9999px
Font: 0.75rem / 12px / weight 600
```

**Thẻ thống kê** (Stat Card):

```
Kích thước: p-4 (16px padding mọi phía)
Bo góc: rounded-2xl = 16px
Viền: 1.5px solid rgba(139,110,80,0.15)
Nền: #FDFAF6
Icon container: w-11 h-11 (44×44px), rounded-xl
Số liệu: font-size 1.5rem, font-weight 700, color #1A1209
Label: font-size 0.78rem, color #7B6A57
```

**Input chuẩn**:

```
Nền: #EDE8DC (trong modal) hoặc #FDFAF6 (ngoài modal)
Viền: 1.5px solid rgba(139,110,80,0.20)
Bo góc: rounded-xl = 12px
Padding: py-3 (12px) / pl-10 (40px nếu có icon) / pr-4 (16px)
Font: 0.9rem / 14.4px
Outline: none (không viền focus mặc định)
```

**Modal**:

```
Backdrop: fixed inset-0, background rgba(0,0,0,0.4), z-index 50
Hộp modal: rounded-2xl, overflow-hidden, background #FDFAF6
Chiều rộng chuẩn: max-w-md (448px)
Chiều rộng rộng: max-w-2xl (672px)
Header modal: background #1B3A6B, padding p-5, chữ trắng
Body modal: p-5 hoặc p-6
```

---

## 3. MÔ HÌNH DỮ LIỆU (DATA MODELS)

### 3.1 Table (Bàn)

```csharp
public class Table {
    string Id;                // "t1", "t2"...
    int Number;               // Số bàn (hiển thị)
    string Floor;             // "Tầng 1" | "Tầng 2" | "Khu vườn"
    TableStatus Status;       // available | occupied | reserved | needs_clearing
    int Capacity;             // Sức chứa (số người)
    string? CurrentOrderId;   // ID đơn hàng đang chạy (NULL nếu trống)
    string? OccupiedSince;    // Giờ có khách (VD: "12:30")
    string? ReservedFor;      // Tên khách đặt trước
    string? ReservedAt;       // Giờ hẹn (VD: "18:00")
}

enum TableStatus { Available, Occupied, Reserved, NeedsClearing }
```

### 3.2 Order (Đơn hàng)

```csharp
public class Order {
    string Id;                // "o-1234567890"
    string TableId;           // Liên kết bàn
    int TableNumber;          // Số bàn (cache)
    List<OrderItem> Items;    // Danh sách món
    decimal Total;            // Tổng tiền (VNĐ)
    OrderStatus Status;       // pending|confirmed|preparing|ready|served|paid
    DateTime CreatedAt;
    string ServerName;        // Tên nhân viên phục vụ
    string ServerId;          // ID nhân viên phục vụ
    decimal? Discount;        // Số tiền giảm giá
    string? DiscountCode;     // Mã giảm giá đã dùng
    bool? Paid;
    string? PaymentMethod;    // "QR Code" | "Cash"
}

enum OrderStatus { Pending, Confirmed, Preparing, Ready, Served, Paid }
```

### 3.3 OrderItem (Món trong đơn)

```csharp
public class OrderItem {
    string Id;                // "oi-1234567890-m1"
    string MenuItemId;        // Liên kết món ăn
    string Name;              // Cache tên món
    decimal Price;            // Cache giá tại thời điểm đặt
    int Quantity;
    string? Notes;            // Ghi chú đặc biệt
    DishStatus Status;        // pending|preparing|ready|served
    string Image;             // URL ảnh
}

enum DishStatus { Pending, Preparing, Ready, Served }
```

### 3.4 MenuItem (Món ăn)

```csharp
public class MenuItem {
    string Id;
    string Name;
    string Category;          // "Khai vị"|"Món chính"|"Súp"|"Hải sản"|"Tráng miệng"|"Đồ uống"
    decimal Price;
    string Description;
    string Image;
    bool Available;
    bool OutOfStock;
}
```

### 3.5 Staff (Nhân viên)

```csharp
public class Staff {
    string Id;
    string Name;
    StaffRole Role;           // staff | manager
    string Email;
    string Phone;
    StaffStatus Status;       // active | inactive | locked
    DateTime? LastLogin;
    DateTime JoinDate;
    List<string> Permissions; // ["tables","orders","payment","chat"] hoặc ["all"]
}
```

### 3.6 ChatMessage (Tin nhắn)

```csharp
public class ChatMessage {
    string Id;
    string SenderId;
    string SenderName;
    StaffRole SenderRole;
    string Message;
    DateTime Timestamp;
    bool IsSystem;            // true = tin broadcast từ quản lý
}
```

### 3.7 Notification (Thông báo)

```csharp
public class Notification {
    string Id;
    NotificationType Type;    // dish_ready | order_update | system | chat
    string Title;
    string Message;
    DateTime Timestamp;
    bool Read;
    string? TableId;
    string? OrderId;
}
```

### 3.8 Mã giảm giá (DiscountCode)

```csharp
// Lưu trong DB hoặc config
public class DiscountCode {
    string Code;              // "WELCOME10"
    DiscountType Type;        // Percentage | FixedAmount
    decimal Value;            // 0.10 (10%) hoặc 50000 (50k VNĐ cố định)
    bool IsActive;
}
// Ví dụ hiện có: WELCOME10=10%, VIP20=20%, SAVE50K=50.000đ cố định
```

---

## 4. LUỒNG ĐIỀU HƯỚNG (NAVIGATION FLOW)

```
App Start
    │
    ▼
[LoginPage] ──── Đăng nhập thành công ────►
    │                                        │
    │               role = 'staff'           │  role = 'manager'
    │                    │                   │       │
    ▼                    ▼                   ▼       ▼
[Auth Guard]    [Staff Shell]           [Manager Shell]
                     │                       │
    ┌────────────────┤                       ├─────────────────┐
    │                │                       │                 │
  /tables          /orders               /management        /hr
  /dish-status     /payment              /menu              /layout
  /history         /chat                 /revenue           /invoices
  /account                               /notifications     /system
                                         /chat
```

**Trong MAUI**: Dùng `Shell` với `TabBar` hoặc `FlyoutPage` + `NavigationPage`

---

## 5. TRANG ĐĂNG NHẬP (LoginPage)

### 5.1 Bố cục (Layout)

```
[Màn hình đầy đủ - min-height: 100vh]
├── [Cột trái - 50% rộng - chỉ desktop/tablet landscape]
│   ├── Ảnh nền nhà hàng (full cover)
│   ├── Overlay gradient: linear-gradient(135deg, rgba(27,58,107,0.85), rgba(27,58,107,0.4))
│   └── Nội dung phía dưới (padding 48px):
│       ├── Logo icon (w=48, h=48, rounded-xl, bg=rgba(255,255,255,0.2)) + ChefHat icon 28px
│       ├── "The Golden Plate" - Playfair Display / 1.5rem / bold
│       ├── "Hệ thống quản lý nhà hàng" - 0.875rem / rgba(255,255,255,0.7)
│       ├── Tagline lớn: "Nâng tầm mỗi trải nghiệm ẩm thực"
│       │   → Playfair Display / 2.5rem / 600 / line-height 1.2
│       └── Mô tả: rgba(255,255,255,0.8) / 0.875rem / max-width 360px
│
└── [Cột phải - 50% rộng (tablet/desktop) hoặc 100% (mobile)]
    → background: #F5F0E8
    → Căn giữa dọc và ngang
    → Nội dung: max-width 448px, width 100%, padding 32px
```

### 5.2 Form đăng nhập

```
[Logo mobile - chỉ hiện khi < tablet]
  Icon 40×40px / rounded-xl / bg #1B3A6B
  "The Golden Plate" - Playfair / 1.3rem / bold / #1B3A6B

[Tiêu đề] "Chào mừng trở lại"
  Font: Playfair Display / 2rem / weight 600 / color #1A1209

[Mô tả] "Đăng nhập vào tài khoản để tiếp tục"
  Font: Inter / 1rem / color #7B6A57 / margin-bottom 32px

[ROLE SELECTOR - Switch Tab]
  Container: rounded-xl / bg #EDE8DC / padding 4px
  Hai nút:
    - "Nhân viên phục vụ" | "Quản lý"
    - Width: flex-1 (chiều rộng bằng nhau)
    - Height: py-2.5 = 10px top+bottom → tổng ~40px
    - Bo góc: rounded-lg = 8px
    - Trạng thái ACTIVE: bg #1B3A6B / chữ trắng / weight 600
    - Trạng thái INACTIVE: bg transparent / chữ #7B6A57 / weight 400
    - Margin-bottom: 24px

[INPUT EMAIL]
  Label: "Địa chỉ Email" / 0.875rem / #3D2C1E / margin-bottom 8px
  Input:
    - Chiều rộng: 100%
    - Chiều cao: py-3 (12px top+bottom) + line-height → ~48px
    - bg: #FDFAF6
    - border: 1.5px solid rgba(139,110,80,0.2)
    - border-radius: 12px
    - padding-left: 40px (chừa chỗ icon)
    - padding-right: 16px
    - font-size: 0.9rem / 14.4px
  Icon Mail 18px tại left:12px, vertical-center

[INPUT PASSWORD]
  Giống email, thêm:
  - type="password" (có nút toggle xem/ẩn)
  - Icon Lock 18px bên trái
  - Nút Eye/EyeOff 18px bên phải tại right:12px

[QUÊN MẬT KHẨU]
  Link text / right-align / font-size 0.875rem / color #1B3A6B
  Không có border, background

[ERROR BANNER]
  Hiển thị khi login sai:
  - bg: rgba(212,24,61,0.08)
  - border: 1px solid rgba(212,24,61,0.2)
  - rounded-xl = 12px / padding 12px
  - Icon AlertCircle 16px màu #d4183d + text 0.85rem #d4183d

[NÚT ĐĂNG NHẬP - CTA]
  Chiều rộng: 100% (w-full)
  Chiều cao: py-3.5 → ~52px tổng
  bg: #1B3A6B (active) / #7B9CC8 (loading)
  chữ: trắng / weight 600 / 1rem
  border-radius: 12px
  Khi loading: disabled + cursor not-allowed

[DEMO ACCOUNTS BOX]
  bg: #EDE8DC / rounded-xl = 12px / padding 16px / margin-top 24px
  Font: 0.78rem / #7B6A57 (label) / #4A3B2E (email)
```

### 5.3 Màn hình Quên mật khẩu

```
[Nút quay lại] ← icon ArrowLeft 18px + "Quay lại đăng nhập" 0.9rem #1B3A6B

[Tiêu đề] "Đặt lại mật khẩu" - Playfair / 1.8rem / 600

[Input Email] - giống form login

[Nút gửi] - full width / py-3.5 / bg #1B3A6B + icon Send

[SAU KHI GỬI - success state]
  Card: p-8 / rounded-2xl / bg #FDFAF6 / text-center
  Icon Mail 32px trong vòng tròn w-16 h-16 bg rgba(34,197,94,0.1)
  Tiêu đề "Email đã được gửi!" - h3 / #1A1209
  Mô tả + email bold - 0.9rem
  Nút "Quay lại" - px-6 py-2.5 / bg #1B3A6B
```

### 5.4 Logic xác thực

```
1. Kiểm tra email + role trong danh sách Staff
2. Nếu không tìm thấy → Hiện error "Email hoặc mật khẩu không đúng"
3. Nếu tìm thấy nhưng status = 'locked' → "Tài khoản đã bị khóa"
4. Nếu status = 'inactive' → "Tài khoản không hoạt động"
5. Nếu hợp lệ → setCurrentUser() → điều hướng theo role
   - staff  → /staff/tables
   - manager → /management
6. Fake loading 800ms để mô phỏng API call
```

---

## 6. LAYOUT CHUNG (AppLayout)

### 6.1 Cấu trúc tổng thể

```
[Màn hình - h-screen, overflow-hidden]
├── [SIDEBAR - bên trái, flex-shrink-0]
│   Chiều rộng MỞ:    260px
│   Chiều rộng THU:    72px
│   Transition: 300ms
│   Background: #1B3A6B
│   Box-shadow: 4px 0 20px rgba(0,0,0,0.15)
│
└── [NỘI DUNG CHÍNH - flex-1, overflow-hidden]
    ├── [TOPBAR - cố định trên, min-height 64px]
    │   background: #FDFAF6
    │   border-bottom: rgba(139,110,80,0.15)
    │
    └── [PAGE CONTENT - flex-1, overflow-y-auto]
```

### 6.2 Sidebar - Chi tiết

```
[LOGO SECTION] - min-height 64px / padding 16px / border-bottom rgba(255,255,255,0.1)
  Icon ChefHat: w=40 h=40 / rounded-xl / bg rgba(255,255,255,0.15) / ChefHat 22px white
  Tên "The Golden Plate": Playfair / 1rem / bold / white
  Phụ đề: 0.7rem / rgba(255,255,255,0.6) → "Cổng nhân viên" hoặc "Cổng quản lý"
  [CHỈ HIỆN KHI SIDEBAR MỞ]

[NÚT THU/MỞ]
  mx-3 mt-3 / p-2 / rounded-lg
  bg: rgba(255,255,255,0.08)
  Icon Menu 18px (thu gọn) hoặc X 18px (mở rộng) + text "Thu gọn" 0.8rem
  [Chữ "Thu gọn" chỉ hiện khi mở]

[THÔNG TIN USER - CHỈ KHI MỞ]
  mx-3 mt-3 / p-3 / rounded-xl / bg rgba(255,255,255,0.08)
  Avatar: w=36 h=36 / rounded-full / bg #4A90D9 (staff) hoặc #22C55E (manager)
  Chữ cái đầu tên: font 0.9rem / weight 700 / white
  Tên rút gọn (2 từ cuối): 0.85rem / weight 600 / white
  Vai trò: 0.72rem / rgba(255,255,255,0.5) / uppercase

[NAVIGATION ITEMS]
  padding: px-3 py-4 / space-y-1 (4px giữa các item)
  Mỗi item:
    - Padding: px-3 py-2.5 = 12px / 10px
    - Bo góc: rounded-xl = 12px
    - ACTIVE: bg rgba(255,255,255,0.15) / chữ trắng
    - INACTIVE: bg transparent / chữ rgba(255,255,255,0.65)
    - Icon: 20px / flex-shrink-0
    - Label: 0.875rem / weight 500 [chỉ khi mở]
    - Badge số: w=20 h=20 / rounded-full / bg #EF4444 / chữ trắng 0.7rem
      → Khi thu: w=16 h=16 / absolute top-1 right-1

[NAVIGATION STAFF - 7 items]
  1. Sơ đồ bàn     → LayoutGrid icon
  2. Đặt món       → ShoppingCart icon
  3. Trạng thái món→ UtensilsCrossed icon [badge: số thông báo chưa đọc]
  4. Thanh toán    → Receipt icon
  5. Nhắn tin      → MessageSquare icon [badge: tin chưa đọc]
  6. Lịch sử đơn  → History icon
  7. Tài khoản     → User icon

[NAVIGATION MANAGER - 9 items]
  1. Tổng quan           → BarChart3 icon
  2. Quản lý nhân sự     → Users icon
  3. Quản lý thực đơn    → BookOpen icon
  4. Quản lý sơ đồ       → Map icon
  5. Doanh thu & Báo cáo → BarChart3 icon
  6. Quản lý hóa đơn     → Receipt icon
  7. Nhắn tin            → MessageSquare icon [badge]
  8. Thông báo           → Bell icon
  9. Cấu hình hệ thống   → Settings icon

[ĐĂNG XUẤT]
  p-3 / border-top rgba(255,255,255,0.1)
  Nút full-width: px-3 py-2.5 / rounded-xl
  bg: rgba(239,68,68,0.15) / chữ #FCA5A5 / icon LogOut 20px
  [Label "Đăng xuất" 0.875rem chỉ khi mở]
```

### 6.3 TopBar - Chi tiết

```
Chiều cao: min-height 64px / px-6 = 24px hai bên
background: #FDFAF6
border-bottom: rgba(139,110,80,0.15)

[TRÁI] Ngày tháng hiện tại
  font: 0.8rem / #7B6A57
  Format: "Thứ Tư, 08 tháng 4 năm 2026"

[PHẢI] 2 button trong flex gap-3:

  [NÚT THÔNG BÁO]
    w=40 h=40 / rounded-xl / bg #EDE8DC
    Icon Bell 20px / color #1B3A6B
    Badge số: w=20 h=20 / rounded-full / bg #EF4444 / absolute -top-1 -right-1
              font 0.65rem / weight 700
    Click → mở dropdown thông báo

    [DROPDOWN THÔNG BÁO]
      absolute / right:0 / top:100%+8px / z-50
      w=320px / rounded-2xl / shadow-xl
      bg #FDFAF6 / border 1px rgba(139,110,80,0.15)
      max-height 288px / overflow-y-auto

      Header: p-4 / "Thông báo" / 0.95rem / #1A1209

      Mỗi item thông báo:
        - padding: p-4
        - border-bottom: rgba(139,110,80,0.1)
        - bg: transparent (đã đọc) / rgba(74,144,217,0.05) (chưa đọc)
        - Icon w=32 h=32 / rounded-full
          dish_ready: bg rgba(34,197,94,0.15) / CheckCircle #22C55E
          khác:       bg rgba(74,144,217,0.15) / Bell #4A90D9
        - Tiêu đề: 0.85rem / weight 600 / #1A1209
        - Nội dung: 0.78rem / #7B6A57 / line-height 1.4
        - Thời gian: 0.72rem / #C5B8A8

  [NÚT PROFILE]
    px-3 py-2 / rounded-xl / bg #EDE8DC
    Avatar w=28 h=28 / rounded-full / bg #1B3A6B (manager) hoặc #22C55E (staff)
    Tên (2 từ cuối): 0.85rem / weight 500 / #1A1209 / max-width 120px / truncate
    Icon ChevronDown 16px / #7B6A57

    [DROPDOWN PROFILE]
      absolute / right:0 / top:100%+8px / z-50
      w=224px / rounded-2xl / shadow-xl
      bg #FDFAF6 / border 1px rgba(139,110,80,0.15)

      Header: p-4 / border-bottom
        Tên: 0.9rem / weight 600 / #1A1209
        Email: 0.78rem / #7B6A57

      Body: p-2 (2 items)
        "Tài khoản" - NavLink → /staff/account hoặc /management/system
        "Đăng xuất" - Button / color #d4183d
        Mỗi item: px-3 py-2 / rounded-lg / 0.875rem
```

---

## 7. STAFF — SƠ ĐỒ BÀN (TableManagement)

### 7.1 Bố cục trang

```
[TRANG - padding: p-6 = 24px mọi phía]

[HEADER ROW - flex items-center justify-between / margin-bottom 24px]
  Trái:
    h1 "Sơ đồ bàn" - Playfair / 1.5rem / 500 / #1A1209
    p "Quản lý và xem trạng thái..." - 0.875rem / #7B6A57
  Phải:
    Số tổng bàn: 1.5rem / weight 700 / #1B3A6B
    "Tổng số bàn": 0.78rem / #7B6A57

[THẺ THỐNG KÊ - grid 2 cột (mobile) → 4 cột (≥md) / gap-3 / margin-bottom 24px]
  4 thẻ: Còn trống / Có khách / Đặt trước / Cần dọn dẹp
  Mỗi thẻ:
    - p-4 = 16px / rounded-2xl / cursor pointer
    - ACTIVE: bg = màu nhạt của trạng thái / border = màu chấm
    - INACTIVE: bg #FDFAF6 / border 1.5px rgba(139,110,80,0.15)
    - Chấm màu: w=12 h=12 / rounded-full
    - Label: 0.78rem / weight 600
    - Số đếm: 1.5rem / weight 700 / #1A1209

[FILTERS - flex column (mobile) → row (sm+) / gap-3 / margin-bottom 24px]
  [Ô TÌM KIẾM - flex-1]
    relative / icon Search 18px tại left:12px / py-2.5
    bg #FDFAF6 / border 1.5px / border-radius 12px / font 0.9rem
    placeholder: "Tìm theo số bàn hoặc khu vực..."
  [NÚT LỌC KHU VỰC - flex gap-2 overflow-x-auto]
    "Tất cả" / "Tầng 1" / "Tầng 2" / "Khu vườn"
    Mỗi nút: px-4 py-2.5 / rounded-xl / 0.875rem
    ACTIVE: bg #1B3A6B / chữ trắng / border #1B3A6B
    INACTIVE: bg #FDFAF6 / chữ #4A3B2E / border rgba(139,110,80,0.2)

[CÁC KHU VỰC BÀN - lặp theo floor]
  margin-bottom 32px mỗi khu
  Header khu vực:
    flex items-center gap-3:
    - h3 tên khu: 1rem / #1B3A6B
    - Badge "X bàn": px-2.5 py-0.5 / rounded-full / bg #EDE8DC / text-xs / #7B6A57
    - Đường kẻ ngang: flex-1 / h-px / bg rgba(139,110,80,0.2)
  Grid bàn: grid 2 cột → 3 → 4 → 5 → 6 / gap-3
```

### 7.2 Thẻ bàn (Table Card)

```
[THẺ BÀN - button]
  Kích thước: p-4 = 16px / rounded-2xl
  background: #FDFAF6
  border: 2px solid [màu chấm của trạng thái]
  cursor: default (reserved) / pointer (các loại khác)
  hover: shadow-md + translate-y(-2px) [chỉ nếu clickable]
  transition: 200ms

  [ROW 1 - flex justify-between]
    Số bàn "T5": 1.1rem / weight 700 / #1A1209
    Chấm màu: w=10 h=10 / rounded-full

  [ROW 2 - margin-bottom 8px]
    Badge trạng thái: px-2 py-0.5 / rounded-lg / font-size 0.75rem / weight 600
    bg + color theo trạng thái

  [ROW 3 - flex items-center gap-1]
    Icon Users 12px + "X chỗ": 0.72rem / #7B6A57

  [ROW 4 - conditional]
    Nếu occupied: Icon Clock 11px + "Từ HH:MM" / 0.70rem / #7B6A57
    Nếu reserved:
      Icon Calendar 11px + giờ hẹn / 0.68rem / #92400E
      Tên khách: 0.68rem / #7B6A57
    Nếu needs_clearing:
      Icon CheckCircle 11px + "Nhấn để dọn" / 0.68rem / #6B7280

  [THÔNG TIN ĐƠN HÀNG - conditional nếu có currentOrderId]
    border-top: 1px rgba(139,110,80,0.15) / margin-top 8px / padding-top 8px
    "X items": 0.7rem / weight 600 / #1B3A6B
    Tổng tiền: 0.7rem / #7B6A57

BẢNG MÀU TRẠNG THÁI BÀN:
  available:      dot #22C55E / label #15803D / bg rgba(34,197,94,0.12)
  occupied:       dot #EF4444 / label #B91C1C / bg rgba(239,68,68,0.12)
  reserved:       dot #F59E0B / label #92400E / bg rgba(245,158,11,0.12)
  needs_clearing: dot #6B7280 / label #374151 / bg rgba(107,114,128,0.12)
```

### 7.3 Logic xử lý khi nhấn bàn

```
handleTableClick(table):
  if status == 'available':
    confirm("Xếp khách vào Bàn X?")
    if confirmed: navigate → /staff/orders?tableId=X&tableNumber=Y
  else if status == 'needs_clearing':
    confirm("Đánh dấu Bàn X là sẵn sàng?")
    if confirmed: table.status = 'available'
  else if status == 'occupied':
    navigate → /staff/orders?tableId=X&tableNumber=Y  [không cần confirm]
  else if status == 'reserved':
    → Không làm gì
```

---

## 8. STAFF — ĐẶT MÓN (OrderCreation)

### 8.1 Bố cục trang (layout 2 cột)

```
[TRANG - flex / h-full / bg #F5F0E8]

[CỘT TRÁI - flex-1 / overflow-hidden / flex-col]
  Header menu
  Danh mục
  Lưới món ăn

[CỘT PHẢI - w-72 = 288px / flex-col / border-left]
  Tiêu đề tóm tắt
  Danh sách giỏ hàng
  Footer tổng tiền + nút gửi
```

### 8.2 Cột trái — Header

```
[HEADER - p-4 / border-bottom / bg #FDFAF6]
  [ROW 1 - flex justify-between]
    Trái:
      h2 "Bàn X — Đặt món": 1.25rem / 500 / #1A1209
      [Nếu có đơn cũ] Badge "Cập nhật đơn hiện tại":
        px-2 py-0.5 / rounded-full / bg rgba(74,144,217,0.12) / text-xs / #1B3A6B
    Phải (2 nút):
      Nút "Chuyển bàn":
        px-3 py-1.5 / rounded-lg / bg #EDE8DC
        border 1px rgba(139,110,80,0.2) / text-sm / #3D2C1E
        Icon ArrowLeftRight 15px
      Nút "Ghép bàn":
        Tương tự / Icon GitMerge 15px

  [ROW 2 - Ô tìm kiếm]
    relative / icon Search 17px tại left:10px
    w-full / rounded-xl / pl-9 pr-4 py-2 / text-sm
    bg #EDE8DC / color #1A1209
```

### 8.3 Cột trái — Danh mục

```
[DANH MỤC - flex gap-2 / px-4 py-3 / overflow-x-auto / border-bottom / bg #FDFAF6]
  Mỗi nút danh mục:
    px-3 py-1.5 / rounded-lg / text-sm / whitespace-nowrap
    ACTIVE: bg #1B3A6B / chữ trắng / weight 600
    INACTIVE: bg #EDE8DC / chữ #4A3B2E

Danh mục: "Tất cả" / "Khai vị" / "Món chính" / "Súp" / "Hải sản" / "Tráng miệng" / "Đồ uống"
```

### 8.4 Cột trái — Lưới món ăn

```
[LƯỚI - p-4 / overflow-y-auto / flex-1]
  grid: 2 cột → 3 (md) → 4 (lg) / gap-3

[THẺ MÓN ĂN]
  rounded-2xl / overflow-hidden
  NORMAL:    border 1.5px rgba(139,110,80,0.15) / bg #FDFAF6
  SELECTED:  border 2px #1B3A6B / bg #FDFAF6
  HẾT MÓN:  opacity 0.6

  [ẢNH MÓN]
    w-full / h-28 = 112px / object-cover / relative
    [Nếu hết hàng] Overlay đen rgba(0,0,0,0.5) + badge "HẾT MÓN":
      bg #EF4444 / text-xs / text-white / font-bold / px-2 py-1 / rounded-lg
    [Nếu đã thêm vào giỏ] Badge số lượng:
      absolute top-2 right-2
      w=24 h=24 / rounded-full / bg #1B3A6B / chữ trắng / text-xs / weight-bold

  [NỘI DUNG - p-3]
    Tên món: 0.85rem / weight 600 / #1A1209 / margin-bottom 2px
    Giá: 0.85rem / weight 700 / #1B3A6B

    [NÚT ĐIỀU CHỈNH - flex gap-1 / margin-top 8px]
    Khi CHƯA CÓ trong giỏ:
      1 nút full-width: py-1.5 / rounded-lg / bg #1B3A6B / text-white / 0.8rem
      Icon Plus 14px + "Thêm"
      [Hết hàng: bg #EDE8DC / chữ #7B6A57 / disabled]

    Khi ĐÃ CÓ trong giỏ: 3 nút flex
      Nút "−": flex-1 / py-1 / rounded-lg / bg #EDE8DC / Icon Minus 14px / #4A3B2E
      Nút Ghi chú: flex-1 / py-1 / rounded-lg
        Không có note: bg #EDE8DC / Icon StickyNote 14px / #4A3B2E
        Có note: bg rgba(245,158,11,0.15) / Icon StickyNote 14px / #F59E0B
      Nút "+": flex-1 / py-1 / rounded-lg / bg #1B3A6B / Icon Plus 14px / white
```

### 8.5 Cột phải — Tóm tắt đơn hàng

```
[HEADER - p-4 / border-bottom / bg #FDFAF6]
  flex items-center gap-2
  Icon ShoppingCart 20px / #1B3A6B
  h3 "Tóm tắt đơn hàng": 1.125rem / 500 / #1A1209
  [Nếu có items] Badge tổng số lượng:
    ml-auto / w=24 h=24 / rounded-full / bg #1B3A6B / chữ trắng / text-xs

[BODY - flex-1 / overflow-y-auto / p-3 / space-y-2]

  [NẾU CÓ ĐƠN CŨ - hiện "Đã gọi trước"]
    Label "Đã gọi trước:": text-xs / #7B6A57 / margin-bottom 8px
    Mỗi món cũ:
      flex items-center gap-2 / p-2 / rounded-xl / bg #EDE8DC
      Ảnh: w=32 h=32 / rounded-lg / object-cover
      Tên: 0.78rem / weight 500 / truncate / #4A3B2E
      Số lượng "×2": 0.72rem / #7B6A57
      Badge trạng thái (served/ready/preparing): text-xs

    Đường kẻ ngang
    Label "Món mới thêm:": text-xs / #7B6A57

  [NẾU GIỎ TRỐNG]
    Căn giữa / h-32
    Icon ShoppingCart 32px / #C5B8A8
    "Chọn món từ thực đơn": 0.85rem / #7B6A57

  [MỖI MÓN TRONG GIỎ - p-3 / rounded-xl / bg #EDE8DC]
    flex items-start gap-2:
    Ảnh: w=40 h=40 / rounded-lg / object-cover / flex-shrink-0
    Thông tin:
      Tên: 0.82rem / weight 600 / #1A1209
      Giá × Số lượng: 0.78rem / #1B3A6B
      [Nếu có ghi chú] "📝 ghi chú": 0.72rem / #F59E0B / margin-top 2px
    Phải:
      Thành tiền: 0.82rem / weight 700 / #1A1209
      Nút Trash2 14px / #EF4444 / no-bg

[FOOTER - p-4 / border-top - chỉ hiện khi giỏ có hàng]
  flex justify-between:
    "Tạm tính": 0.875rem / #7B6A57
    Số tiền: 1rem / weight 700 / #1A1209

  [NÚT GỬI LÊN BẾP]
    w-full / py-3 = 12px / rounded-xl
    bg: #1B3A6B / chữ trắng / weight 600
    flex items-center justify-center gap-2
    Icon Send 18px + "Gửi lên bếp"
```

### 8.6 Modal Ghi chú

```
[OVERLAY] fixed inset-0 / z-50 / bg rgba(0,0,0,0.4) / flex center

[HỘP MODAL] w-320px / rounded-2xl / p-6 / bg #FDFAF6
  h3 "Thêm ghi chú": 1.125rem / 500 / #1A1209
  Tên món: 0.85rem / #7B6A57 / margin-bottom 16px
  Textarea:
    w-full / rounded-xl / p-3 / rows=3 / resize-none / text-sm
    bg #EDE8DC / border 1px rgba(139,110,80,0.2) / #1A1209
    placeholder: "VD: Không cay, ít đường, không hành..."
  2 nút (flex gap-2 / margin-top 16px):
    "Hủy":      flex-1 / py-2.5 / rounded-xl / bg #EDE8DC / #4A3B2E
    "Lưu ghi chú": flex-1 / py-2.5 / rounded-xl / bg #1B3A6B / white / weight 600
```

### 8.7 Modal Chuyển bàn

```
[HỘP MODAL] w-384px / rounded-2xl / p-6 / bg #FDFAF6
  h3 "Chuyển bàn": 1.125rem / 500 / #1A1209
  Mô tả: 0.85rem / #7B6A57 / margin-bottom 24px

  [LƯỚI BÀN - grid 3 cột / gap-2 / max-h-192px / overflow-y-auto]
    Mỗi nút bàn: p-3 / rounded-xl / text-center / 0.85rem
    SELECTED: bg #1B3A6B / chữ trắng
    NORMAL:   bg #EDE8DC / #4A3B2E
    "TXX" bold + "🟢/🔴 X chỗ" 0.70rem opacity 0.8

  2 nút hành động:
    "Hủy": flex-1 / py-2.5 / bg #EDE8DC
    "Xác nhận chuyển":
      flex-1 / py-2.5 / bg #1B3A6B (có chọn) / #C5B8A8 (chưa chọn)
      disabled khi chưa chọn bàn
```

### 8.8 Modal Ghép bàn

```
Tương tự modal chuyển bàn nhưng:
  - Tiêu đề: "Ghép bàn"
  - Mô tả: "Ghép hóa đơn bàn khác vào Bàn X"
  - Chỉ hiển thị bàn có trạng thái 'occupied'
  - Mỗi bàn chỉ hiện "🔴 Có khách" (không hiện sức chứa)
```

### 8.9 Màn hình xác nhận thành công

```
[FULL PAGE CENTER]
  Vòng tròn: w=80 h=80 / rounded-full / bg rgba(34,197,94,0.1)
  Icon Send 36px / #22C55E
  h2 "Đã gửi đơn lên bếp!": Playfair / 1.25rem / #1A1209
  p "Đơn hàng Bàn X đã được xác nhận.": 1rem / #7B6A57
  → Auto-navigate sau 1500ms
```

---

## 9. STAFF — TRẠNG THÁI MÓN (DishStatus)

### 9.1 Bố cục trang

```
[TRANG - p-6]

[HEADER ROW - flex justify-between / margin-bottom 24px]
  Trái:
    h1 "Trạng thái món": Playfair / 1.5rem / 500 / #1A1209
    p mô tả: 0.875rem / #7B6A57
  Phải [chỉ khi có món ready]:
    flex items-center gap-2 / px-4 py-2.5 / rounded-xl
    bg rgba(34,197,94,0.1) / border 1.5px rgba(34,197,94,0.3)
    Icon Bell 18px / #22C55E
    "X món đã sẵn sàng phục vụ!": 0.9rem / weight 600 / #15803D

[THẺ THỐNG KÊ - grid 2 cột (mobile) → 4 cột (md) / gap-3 / margin-bottom 24px]
  4 trạng thái: pending / preparing / ready / served
  Cách hiển thị giống bảng thống kê bàn

BẢNG MÀU TRẠNG THÁI MÓN:
  pending:   icon Clock / label #92400E / bg rgba(245,158,11,0.12)
  preparing: icon ChefHat / label #1D4ED8 / bg rgba(74,144,217,0.12)
  ready:     icon Bell / label #15803D / bg rgba(34,197,94,0.12)
  served:    icon CheckCircle / label #6B7280 / bg rgba(107,114,128,0.1)
```

### 9.2 Banner món sẵn sàng

```
[BANNER - rounded-2xl / p-4 / margin-bottom 24px]
  bg rgba(34,197,94,0.08) / border 1.5px rgba(34,197,94,0.25)
  [Chỉ hiện khi readyDishes.length > 0]

  Header: flex items-center gap-2 / margin-bottom 12px
    Icon Bell 18px / #22C55E
    h3 "Thông báo bếp — Món đã sẵn sàng phục vụ": 1rem / #15803D

  Mỗi món sẵn sàng:
    flex items-center gap-3 / p-3 / rounded-xl / bg rgba(255,255,255,0.7)
    Ảnh: w=48 h=48 / rounded-xl / object-cover
    Tên món: 0.9rem / weight 600 / #1A1209
    "Bàn X · ×Y": 0.8rem / #7B6A57
    NÚT "Đã phục vụ":
      flex items-center gap-1.5 / px-3 py-2 / rounded-xl
      bg #22C55E / chữ trắng / text-sm / font-bold
      Icon CheckCircle 15px
```

### 9.3 Danh sách đơn theo bàn

```
Mỗi đơn hàng: rounded-2xl / overflow-hidden / bg #FDFAF6 / border 1.5px

  [HEADER ĐƠN - px-4 py-3 / border-bottom / bg #EDE8DC]
    flex justify-between:
    Trái: Icon Utensils 16px / #1B3A6B + "Bàn X" weight 600 + "• HH:MM" 0.8rem
    Phải: "X món · tổng tiền" 0.8rem / #7B6A57

  [BODY - p-3 / space-y-2]
    Mỗi món: flex items-center gap-3 / p-3 / rounded-xl / bg #F5F0E8
      Ảnh: w=48 h=48 / rounded-xl / flex-shrink-0
      Nội dung:
        flex items-center gap-2:
          Tên: 0.875rem / weight 600 / #1A1209
          "×Y": text-xs
        [Nếu có ghi chú] "📝 ghi chú": 0.75rem / #F59E0B
        Badge trạng thái: px-2 py-0.5 / rounded-lg / text-xs / weight 600
      [Nếu status = 'ready'] NÚT "Đã phục vụ":
        px-3 py-1.5 / rounded-xl / text-xs / font-bold
        bg #22C55E / chữ trắng / Icon CheckCircle 13px
```

---

## 10. STAFF — THANH TOÁN (Payment)

### 10.1 Bố cục trang

```
[TRANG - p-6 / max-width 1024px / căn giữa]

[HEADER - margin-bottom 24px]
  h1 "Thanh toán": Playfair / 1.5rem / 500 / #1A1209
  p mô tả: 0.875rem / #7B6A57

[CHỌN BÀN - margin-bottom 24px]
  label "Chọn bàn": display-block / 0.875rem / #3D2C1E / margin-bottom 8px
  Select dropdown:
    w-full / rounded-xl / px-4 py-3 / appearance-none
    bg #FDFAF6 / border 1.5px / #1A1209 / font 0.9rem
    Icon ChevronDown 18px / absolute right:12px / #7B6A57 / pointer-events-none

[NỘI DUNG 2 CỘT - grid 1 cột (mobile) → 2 cột (lg) / gap-6]
  Cột trái: Hóa đơn
  Cột phải: Phương thức thanh toán
```

### 10.2 Hóa đơn (Bill)

```
[CARD HÓA ĐƠN - rounded-2xl / overflow-hidden / bg #FDFAF6 / border 1.5px]

  [HEADER - p-4 / border-bottom / bg #1B3A6B]
    flex items-center gap-2:
    Icon Receipt 18px / white
    h3 "Hóa đơn — Bàn X": 1.125rem / 500 / white
    p "HH:MM · Nhân viên: Tên": 0.8rem / rgba(255,255,255,0.7) / margin-top 2px

  [BODY - p-4]
    [Thông tin nhà hàng - text-center / border-bottom dashed / margin-bottom 16px]
      Tên NHÀ HÀNG: Playfair / 1rem / weight 700 / #1B3A6B
      Địa chỉ: 0.75rem / #7B6A57
      Điện thoại: 0.75rem / #7B6A57

    [DANH SÁCH MÓN - space-y-2 / margin-bottom 16px]
      Mỗi món: flex items-center gap-3
        Tên + (đơn giá × số lượng): left / 0.875rem (tên) + 0.78rem (giá×qty)
        Thành tiền: right / 0.875rem / weight 600

    [TỔNG TIỀN - border-top / space-y-2 / padding-top 12px]
      "Tạm tính" + số tiền: 0.875rem
      [Nếu có giảm giá] "Giảm giá" + "-số tiền": 0.875rem / #22C55E
      [Đường kẻ ngang]
      "Tổng cộng" / weight 700 + Số tiền / 1.1rem / weight 700 / #1B3A6B

    [MÃ GIẢM GIÁ - border-top / margin-top 16px / padding-top 16px]
      label "Mã giảm giá": 0.8rem
      flex gap-2:
        Input: flex-1 / rounded-xl / px-3 py-2 / bg #EDE8DC / text-sm
               placeholder "Nhập mã..."
        Nút "Áp dụng": px-3 py-2 / rounded-xl / text-sm / bg #1B3A6B / weight 600
      [Nếu lỗi] text đỏ 0.78rem / #EF4444
      [Nếu OK] text xanh "✓ Đã áp dụng" 0.78rem / #22C55E
      hint "Thử: WELCOME10, VIP20, SAVE50K": 0.72rem / #C5B8A8

    [GHI CHÚ CUỐI] text-center / 0.75rem / italic / #7B6A57
```

### 10.3 Phương thức thanh toán

```
[CỘT PHẢI]
  h3 "Phương thức thanh toán": 1.125rem / 500 / #1A1209 / margin-bottom 16px

  2 NÚT PHƯƠNG THỨC (space-y-3):
    Mỗi nút: w-full / p-4 / rounded-2xl / text-left / flex items-center gap-4
    ACTIVE: bg rgba(27,58,107,0.08) / border 2px #1B3A6B
    INACTIVE: bg #FDFAF6 / border 2px rgba(139,110,80,0.2)

    Icon container: w=48 h=48 / rounded-xl
      ACTIVE: bg #1B3A6B / icon white
      INACTIVE: bg #EDE8DC / icon #4A3B2E
    Icon: QrCode 24px (QR) / Banknote 24px (tiền mặt)
    Text:
      Tên: 1rem / weight 600 / #1A1209
      Mô tả: 0.8rem / #7B6A57
    [Khi ACTIVE] Icon CheckCircle 20px / ml-auto / #1B3A6B

  [KHỐI QR CODE - chỉ khi chọn QR - rounded-2xl / p-6 / text-center]
    bg #FDFAF6 / border 1.5px
    "Quét mã để thanh toán": weight 600 / #1B3A6B / margin-bottom 16px
    SVG QR Code: 160×160px / border trắng 12px / rounded 8px / bg white
    Tên ngân hàng: 0.9rem / weight 600 / #1A1209
    Số TK / Tên TK: 0.85rem / #7B6A57
    Số tiền: 1.1rem / weight 700 / #1B3A6B
    Nội dung chuyển khoản: 0.75rem / #C5B8A8

  [NÚT XÁC NHẬN THANH TOÁN]
    w-full / py-4 / rounded-2xl / flex center gap-2
    ACTIVE (đã chọn PT): bg #1B3A6B / weight 700 / 1rem
    DISABLED: bg #C5B8A8 / cursor not-allowed
    Icon CheckCircle 20px + "Xác nhận thanh toán — [tổng tiền]"
```

### 10.4 Màn hình thành công

```
[FULL PAGE CENTER - p-8]
  Vòng tròn: w=96 h=96 / rounded-full / bg rgba(34,197,94,0.1)
  Icon CheckCircle 48px / #22C55E
  h2 "Thanh toán thành công!": Playfair / 1.25rem / #1A1209
  p "Bàn đã được đánh dấu cần dọn dẹp.": 1rem / #7B6A57 / margin-bottom 32px
  Nút "Thanh toán bàn khác": px-6 py-3 / rounded-xl / bg #1B3A6B / weight 600
```

---

## 11. STAFF — LỊCH SỬ ĐƠN HÀNG (OrderHistory)

### 11.1 Bố cục

```
[TRANG - p-6]
[HEADER - flex justify-between / margin-bottom 24px]
  Trái: h1 + mô tả
  Phải:
    Tổng doanh thu: 1.3rem / weight 700 / #1B3A6B
    "Tổng phục vụ (X đơn)": 0.78rem / #7B6A57

[FILTERS - flex column → row / gap-3 / margin-bottom 24px]
  Ô tìm kiếm (flex-1): icon Search 17px / placeholder "Tìm theo số bàn hoặc nhân viên..."
  3 nút thời gian: "Hôm nay" / "Tuần này" / "Tháng này"
    px-4 py-2.5 / rounded-xl / text-sm
    ACTIVE: bg #1B3A6B / trắng / border #1B3A6B
    INACTIVE: bg #FDFAF6 / #4A3B2E / border rgba(139,110,80,0.2)

[DANH SÁCH ĐƠN - space-y-3]
```

### 11.2 Thẻ đơn hàng (accordion)

```
[THẺ - rounded-2xl / overflow-hidden / bg #FDFAF6 / border 1.5px]

  [HEADER NÚT - w-full / flex items-center gap-4 / p-4]
    Icon container: w=40 h=40 / rounded-xl / bg rgba(34,197,94,0.1)
    Icon CheckCircle 20px / #22C55E

    Thông tin:
      flex wrap gap-2:
        "Bàn X": weight 600 / #1A1209
        Badge "Đã thanh toán": rounded-full / text-xs / bg rgba(34,197,94,0.12) / #15803D
        [Nếu có giảm giá] Badge "-Xk giảm giá": bg rgba(245,158,11,0.12) / #92400E
        [Nếu có PT] Badge phương thức: bg #EDE8DC / #7B6A57
      "DD/MM/YYYY HH:MM · X món · Nhân viên": 0.8rem / #7B6A57

    Phải:
      Tổng tiền: 1rem / weight 700 / #1B3A6B
      Icon ChevronDown (mở) / ChevronRight (đóng)

  [EXPANDED BODY - px-4 pb-4 / border-top / animate height]
    Danh sách món (space-y-2 / margin-top 12px):
      Mỗi món: flex items-center gap-3 / p-2 / rounded-xl / bg #F5F0E8
        Ảnh: w=40 h=40 / rounded-lg / object-cover
        Tên + ghi chú: 0.875rem / weight 500 + 0.75rem / #F59E0B
        "×Y": 0.85rem / #7B6A57
        Thành tiền: 0.875rem / weight 600

    Tổng (border-top / margin-top 12px / padding-top 12px):
      [Nếu có giảm giá] "Giảm giá (CODE)": 0.85rem / #22C55E
      "Tổng đã thanh toán" + số tiền: weight 700 / #1B3A6B

    2 nút (flex gap-2 / margin-top 12px):
      "Xem hóa đơn": flex items-center gap-2 / px-4 py-2 / rounded-xl
        bg #EDE8DC / Icon Receipt 15px / #3D2C1E / text-sm
      "In hóa đơn": tương tự / Icon Download 15px
```

---

## 12. STAFF — CHAT NỘI BỘ (Chat)

### 12.1 Bố cục

```
[TRANG - flex-col / h-full]
  [HEADER - cố định trên]
  [MESSAGE AREA - flex-1 / overflow-y-auto]
  [INPUT AREA - cố định dưới]
```

### 12.2 Header chat

```
[HEADER - p-4 / border-bottom / bg #FDFAF6]
  flex items-center gap-3:
  Icon container: w=40 h=40 / rounded-xl / bg #EDE8DC
  Icon MessageSquare 20px / #1B3A6B
  h2 "Chat nội bộ": 1.25rem / 500 / #1A1209
  p "Liên lạc nội bộ trong nhóm": 0.8rem / #7B6A57

  ml-auto: flex items-center gap-2
    Chấm xanh: w=10 h=10 / rounded-full / bg #22C55E
    "3 thành viên đang trực tuyến": 0.8rem / #7B6A57
```

### 12.3 Vùng tin nhắn

```
[MESSAGES - p-4 / space-y-4 / overflow-y-auto / flex-1]

[TIN NHẮN HỆ THỐNG (isSystem = true)]
  flex justify-center:
  Viên thuốc: px-4 py-2 / rounded-full / max-w-md / text-center
  bg rgba(74,144,217,0.1) / border 1px rgba(74,144,217,0.2)
  "🔔 nội dung": 0.8rem / #1B3A6B
  "Tên · HH:MM": 0.72rem / #7B6A57

[TIN NHẮN THƯỜNG]
  flex gap-3:
  TIN CỦA MÌNH (isMe): flex-row-reverse (đảo chiều)
  TIN CỦA NGƯỜI KHÁC: flex-row

  Avatar: w=36 h=36 / rounded-full / align-self flex-end
    Manager: bg #1B3A6B / Staff: bg #4A90D9
    Chữ cái đầu: 0.9rem / weight 700 / white

  Bong bóng:
    max-width 320px (sm) → 448px (md)
    [Nếu người khác] Tên gửi: 0.78rem / weight 600 / #4A3B2E
      + Badge "QL" nếu manager: px-1.5 py-0.5 / bg #1B3A6B / white / 0.65rem

    Nội dung bubble: px-4 py-2.5 / rounded-2xl
    TIN CỦA MÌNH:
      bg #1B3A6B / chữ trắng
      border-bottom-right-radius: 4px (đuôi bong bóng)
    TIN NGƯỜI KHÁC:
      bg #FDFAF6 / #1A1209 / border 1px rgba(139,110,80,0.15)
      border-bottom-left-radius: 4px

    Nội dung text: 0.9rem / line-height 1.5
    Giờ gửi: 0.72rem / #C5B8A8
```

### 12.4 Input gửi tin nhắn

```
[INPUT AREA - p-4 / border-top / bg #FDFAF6]
  flex gap-3:
  Textarea:
    flex-1 / rounded-xl / px-4 py-3 / resize-none / rows=1
    bg #EDE8DC / border 1.5px rgba(139,110,80,0.2) / #1A1209 / 0.9rem
    placeholder "Nhập tin nhắn... (Enter để gửi)"
    Hành vi: Enter → gửi / Shift+Enter → xuống dòng

  Nút gửi:
    w=48 h=48 / rounded-xl / align-self flex-end
    ACTIVE (có text): bg #1B3A6B / cursor pointer
    DISABLED (trống): bg #C5B8A8 / cursor not-allowed
    Icon Send 20px / white

  Hint text: "Shift+Enter để xuống dòng · Enter để gửi"
    0.72rem / #C5B8A8 / margin-top 6px
```

---

## 13. STAFF — TÀI KHOẢN (AccountManagement)

### 13.1 Bố cục

```
[TRANG - p-6 / max-width 672px / căn giữa]
  h1 "Tài khoản của tôi": Playfair + mô tả phụ

[BANNER PROFILE - flex items-center gap-4 / p-5 / rounded-2xl / bg #1B3A6B / margin-bottom 24px]
  Avatar: w=64 h=64 / rounded-2xl / bg rgba(255,255,255,0.15)
    Chữ cái: 1.5rem / weight 700 / white
  Tên: h2 / 1.25rem / white
  Email: 0.85rem / rgba(255,255,255,0.7)
  flex gap-2 mt-2:
    Badge vai trò: px-2 py-0.5 / rounded-full / bg rgba(255,255,255,0.15) / text-xs
    Badge "Đang hoạt động": bg rgba(34,197,94,0.2) / #4ADE80 / text-xs + chấm xanh

[TABS - flex gap-1 / p-1 / rounded-xl / bg #EDE8DC / margin-bottom 24px]
  4 tab: Hồ sơ / Đổi mật khẩu / Xác minh Email / Nhật ký
  Mỗi tab: flex-1 / flex center / px-3 py-2 / rounded-lg / text-sm
  ACTIVE: bg #1B3A6B / chữ trắng
  INACTIVE: bg transparent / #7B6A57
  Icon 16px + Label (label ẩn trên mobile)
```

### 13.2 Tab Hồ s��

```
[CARD - rounded-2xl / p-6 / bg #FDFAF6 / border 1.5px]
  h3 "Thông tin cá nhân": 1.125rem / #1A1209 / margin-bottom 24px
  4 trường (space-y-4):
    Họ tên / Email / Số điện thoại / Chức vụ
    Mỗi trường: flex items-center gap-3 / p-3 / rounded-xl / bg #F5F0E8
      Icon 18px / #1B3A6B
      Label: 0.75rem / #7B6A57
      Giá trị: 1rem / weight 500 / #1A1209
  Ghi chú: p-3 / rounded-xl / bg #EDE8DC / 0.8rem / #7B6A57
```

### 13.3 Tab Đổi mật khẩu

```
Form 3 trường (space-y-4):
  Mật khẩu hiện tại / Mới / Xác nhận mới
  Mỗi trường:
    Label: 0.875rem / #3D2C1E
    Input: relative / icon Lock trái + nút Eye/EyeOff phải
      pl-10 pr-12 py-3 / rounded-xl / bg #EDE8DC / border 1.5px

[Nếu lỗi] Banner đỏ: flex gap-2 / p-3 / rounded-xl / bg rgba(212,24,61,0.08)
  Icon AlertCircle 15px + text 0.85rem / #d4183d

[Nếu thành công] Banner xanh: flex gap-2 / p-3 / rounded-xl / bg rgba(34,197,94,0.1)
  Icon CheckCircle 16px + "Đổi mật khẩu thành công!" / 0.875rem / #15803D

Nút "Cập nhật mật khẩu": w-full / py-3 / rounded-xl / bg #1B3A6B / weight 600
```

### 13.4 Tab Xác minh Email — 3 giai đoạn

```
GIAI ĐOẠN 1 (chưa gửi):
  Card email: p-4 / rounded-xl / bg #EDE8DC
    Icon Mail 16px + email: weight 600 / #1A1209
    Mô tả: 0.8rem / #7B6A57
  Nút "Gửi Email xác minh": w-full / py-3 / bg #1B3A6B + Icon Mail 18px

GIAI ĐOẠN 2 (đã gửi, chờ nhập mã):
  Card thông báo: p-4 / text-center / bg rgba(74,144,217,0.08) / border rgba(74,144,217,0.2)
    Icon Mail 24px / #4A90D9
    Tiêu đề: weight 600 + email bold
  Input OTP:
    w-full / rounded-xl / px-4 py-3 / text-center
    text-2xl (24px) / tracking-widest (letter-spacing rộng)
    bg #EDE8DC / maxLength=6
  Nút "Xác minh Email": active khi length=6 / bg #1B3A6B : #C5B8A8

GIAI ĐOẠN 3 (đã xác minh):
  Center: vòng tròn w=64 h=64 + Icon CheckCircle 32px / #22C55E
  "Email đã được xác minh!": h3 / #1A1209
  Mô tả: 0.875rem / #7B6A57
```

### 13.5 Tab Nhật ký hoạt động

```
[CARD - rounded-2xl / overflow-hidden / bg #FDFAF6 / border 1.5px]
  Header: p-4 / border-bottom / h3 "Hoạt động gần đây"
  Danh sách: divide-y

  Mỗi log: flex items-center gap-3 / p-4
    Icon container: w=36 h=36 / rounded-full / bg #EDE8DC
    Icon Clock 16px / #1B3A6B
    Tên hành động: 0.875rem / weight 500 / #1A1209
    Thiết bị: 0.78rem / #7B6A57
    Ngày giờ: 0.78rem / #C5B8A8 / text-right
```

---

## 14. MANAGER — DASHBOARD

### 14.1 Bố cục

```
[TRANG - p-6]
[WELCOME - margin-bottom 24px]
  h1 "Xin chào, Tên! 👋": Playfair / 1.5rem / #1A1209
  p mô tả: 0.875rem / #7B6A57

[KPI CARDS - grid 2 cột (mobile) → 4 cột (lg) / gap-4 / margin-bottom 24px]
  4 thẻ: Doanh thu / Đơn đang xử lý / Bàn đang có khách / Nhân viên đang trực

  Mỗi thẻ KPI:
    p-5 / rounded-2xl / bg #FDFAF6 / border 1.5px
    [ROW 1] flex justify-between / margin-bottom 12px:
      Icon container: w=44 h=44 / rounded-xl
      Badge % thay đổi: px-2 py-0.5 / rounded-full / text-xs
        Up: bg rgba(34,197,94,0.1) / #15803D
        Down: bg rgba(245,158,11,0.1) / #92400E
    Số liệu: 1.4rem / weight 700 / #1A1209
    Label: 0.78rem / #7B6A57 / margin-top 2px

[GRID 2 - grid 1 cột (mobile) → 3 cột (lg) / gap-6]
  Biểu đồ doanh thu: lg:col-span-2 / rounded-2xl / p-5
  Cảnh báo gần đây: 1 cột

[GRID 3 - grid 1 cột → 2 cột / gap-6 / margin-top 24px]
  Đơn hàng đang xử lý: 1 cột
  Tổng quan trạng thái bàn: 1 cột
```

### 14.2 Biểu đồ doanh thu

```
[CARD - lg:col-span-2 / rounded-2xl / p-5 / bg #FDFAF6 / border 1.5px]
  flex justify-between / margin-bottom 16px:
    h3 "Doanh thu hôm nay"
    "Theo giờ": 0.8rem / #7B6A57
  ResponsiveContainer: width 100% / height 220px
  BarChart:
    CartesianGrid: dashed / rgba(139,110,80,0.1) / không có đường dọc
    XAxis: fontSize 11 / #7B6A57 / không axis line / không tick line
    YAxis: fontSize 11 / #7B6A57 / format "1.5M"
    Tooltip: bg #FDFAF6 / border rgba(139,110,80,0.2) / rounded 12px
    Bar: fill #1B3A6B / borderRadius [6,6,0,0] (chỉ bo góc trên)
```

### 14.3 Cảnh báo gần đây

```
[CARD - rounded-2xl / overflow-hidden / bg #FDFAF6 / border 1.5px]
  Header: p-4 / border-bottom / h3 "Cảnh báo gần đây"
  Body: p-3 / space-y-2
  Mỗi cảnh báo: p-3 / rounded-xl / bg theo loại
    warning: rgba(245,158,11,0.1) / Icon AlertTriangle / #92400E
    info:    rgba(74,144,217,0.1) / Icon Clock / #1D4ED8
    success: rgba(34,197,94,0.1) / Icon CheckCircle / #15803D

    Nội dung: 0.82rem / #1A1209 / line-height 1.4
    Thời gian: 0.72rem / #C5B8A8
```

---

## 15. MANAGER — QUẢN LÝ NHÂN SỰ (HRManagement)

### 15.1 Bố cục

```
[HEADER - flex justify-between / margin-bottom 24px]
  h1 "Quản lý nhân sự" (Playfair) + mô tả phụ
  Nút "Thêm nhân viên":
    flex items-center gap-2 / px-4 py-2.5 / rounded-xl
    bg #1B3A6B / chữ trắng / weight 600 / Icon Plus 18px

[THỐNG KÊ - grid 3 cột / gap-4 / margin-bottom 24px]
  Tổng NV / Đang hoạt động / Bị khóa
  Mỗi thẻ: p-4 / rounded-2xl / bg #FDFAF6 / border 1.5px
    Số: 1.5rem / weight 700 / màu theo loại
    Label: 0.8rem / #7B6A57

[FILTERS - flex column → row / gap-3 / margin-bottom 24px]
  Ô tìm kiếm (flex-1)
  Select "Vai trò": rounded-xl / px-3 py-2.5 / 0.875rem
  Select "Trạng thái": tương tự

[DANH SÁCH - space-y-3]
```

### 15.2 Thẻ nhân viên

```
[THẺ - p-4 / rounded-2xl / flex items-center gap-4 / bg #FDFAF6 / border 1.5px]
  Avatar: w=48 h=48 / rounded-2xl
    Manager: bg #1B3A6B / Staff: bg #4A90D9
    Chữ cái đầu: 1.1rem / weight 700 / white

  Thông tin (flex-1):
    flex wrap gap-2:
      Tên: weight 600 / #1A1209
      Badge vai trò: px-2 py-0.5 / rounded-full / text-xs / bg #EDE8DC / #4A3B2E
      Badge trạng thái: text-xs / màu theo status
    Email + SĐT: 0.78rem / #7B6A57 / flex gap-4
    "Đăng nhập lần cuối: DD/MM/YYYY": 0.72rem / #C5B8A8

  Nhóm nút (flex gap-2 / flex-shrink-0):
    Nút Nhật ký: w=36 h=36 / rounded-xl / bg #EDE8DC / Icon Clock 16px
    Nút Khóa/Mở khóa: w=36 h=36 / rounded-xl
      Đang active: bg rgba(245,158,11,0.1) / Icon Lock 16px / #F59E0B
      Đã khóa: bg rgba(34,197,94,0.1) / Icon UserCheck 16px / #22C55E
    Nút Xóa: w=36 h=36 / rounded-xl / bg rgba(239,68,68,0.1) / Icon Trash2 16px / #EF4444
```

### 15.3 Modal Thêm nhân viên

```
[OVERLAY - fixed / z-50 / bg rgba(0,0,0,0.4)]
[MODAL - max-w-md / rounded-2xl / overflow-hidden / bg #FDFAF6]
  Header: bg #1B3A6B / p-5 / flex justify-between
    h3 "Thêm nhân viên mới" / white
    Nút X / rgba(255,255,255,0.7)

  Form: p-5 / space-y-4
    Họ tên (input + icon Shield)
    Email (input + icon Mail)
    SĐT (input + icon Phone)
    Vai trò (Select: Nhân viên / Quản lý)
    Mật khẩu khởi tạo (input password, minLength=6)

    Mỗi input: rounded-xl / pl-10 pr-4 py-3 / bg #EDE8DC / border 1.5px

    2 nút:
      "Hủy": flex-1 / py-3 / bg #EDE8DC / #4A3B2E
      "Tạo tài khoản": flex-1 / py-3 / bg #1B3A6B / white / weight 600
```

---

## 16. MANAGER — QUẢN LÝ THỰC ĐƠN (MenuManagement)

```
Trang gồm:
- Grid hiển thị tất cả món ăn
- Bộ lọc theo danh mục
- Toggle "Hết hàng" / "Còn hàng" cho từng món
- Modal thêm/sửa món: Tên, Danh mục, Giá, Mô tả, Ảnh URL
- Nút xóa món với confirm
- Badge "HẾT MÓN" overlay trên ảnh khi outOfStock=true
- Tìm kiếm theo tên món
```

---

## 17. MANAGER — QUẢN LÝ SƠ ĐỒ BÀN (LayoutManagement)

```
Trang gồm:
- Hiển thị tất cả bàn theo khu vực (read-only preview)
- Nút thêm bàn mới: chọn khu vực, số bàn, sức chứa
- Nút chỉnh sửa thông tin bàn
- Nút xóa bàn (chỉ khi status = available)
- Thống kê: tổng bàn, phân bổ theo khu vực
- Không thể xóa bàn đang có khách hoặc đang đặt trước
```

---

## 18. MANAGER — DOANH THU & BÁO CÁO (Revenue)

```
Trang gồm:
- 3 nút lọc: Hôm nay / Tuần này / Tháng này
- KPI top: Tổng doanh thu / Tổng đơn / Giá trị TB / Tỷ lệ tăng trưởng
- Biểu đồ đường (LineChart) hoặc cột (BarChart): doanh thu theo giờ/ngày/tháng
- Bảng Top 6 món bán chạy: tên / số đơn / doanh thu
  - Mỗi hàng có thanh progress bar tỷ lệ
- Pie chart: tỷ lệ doanh thu theo danh mục
```

### 18.1 Cấu hình Recharts (MAUI dùng LiveCharts2 hoặc Microcharts)

```
BarChart config:
  Bar color: #1B3A6B
  Bar border-radius: [6,6,0,0]
  Grid: stroke rgba(139,110,80,0.1) / dashed / no vertical
  XAxis: fontSize 11 / #7B6A57 / no axisLine / no tickLine
  YAxis: format "Xtr" hoặc "XM"
  Tooltip: rounded-12px / bg #FDFAF6

LineChart config:
  Line color: #1B3A6B / strokeWidth: 2.5
  Dot: fill #1B3A6B / r=4
  ActiveDot: r=6

PieChart config:
  Colors: ['#1B3A6B','#4A90D9','#22C55E','#F59E0B','#EF4444','#6B7280']
  InnerRadius: 60 / OuterRadius: 100 (donut)
```

---

## 19. MANAGER — QUẢN LÝ HÓA ĐƠN (InvoiceManagement)

```
Trang gồm:
- Tìm kiếm theo số bàn / nhân viên / mã đơn
- Lọc theo khoảng thời gian và phương thức thanh toán
- Danh sách tất cả đơn đã thanh toán (accordion)
- Mỗi đơn: số bàn, ngày giờ, nhân viên, tổng tiền, phương thức, mã giảm giá
- Expanded: chi tiết từng món + tổng sau giảm giá
- Nút "Xuất PDF" / "In hóa đơn"
- Thống kê tổng: tổng doanh thu / số hóa đơn / giá trị TB
```

---

## 20. MANAGER — THÔNG BÁO BROADCAST (NotificationsManagement)

```
Trang gồm:
[CỘT TRÁI] Form soạn thông báo:
  Textarea nội dung broadcast
  Chọn loại: Thông thường / Khẩn cấp / Thông tin bếp
  Nút "Gửi broadcast": gửi đến tất cả nhân viên + thêm vào chat là tin hệ thống

[CỘT PHẢI] Lịch sử thông báo:
  Danh sách các broadcast đã gửi (tên người gửi, nội dung, thời gian)
  Mỗi thông báo có badge loại + icon tương ứng

Thông báo broadcast xuất hiện trong Chat với isSystem=true
```

---

## 21. MANAGER — CẤU HÌNH HỆ THỐNG (SystemConfig)

```
Trang gồm (form chia thành các section):

[Section 1: Thông tin nhà hàng]
  - Tên nhà hàng
  - Địa chỉ
  - Số điện thoại / Email
  - Mã số thuế

[Section 2: Thanh toán]
  - Tên ngân hàng
  - Số tài khoản
  - Tên chủ tài khoản
  - Chi nhánh

[Section 3: Cấu hình in]
  - Ghi chú cuối hóa đơn
  - Thuế VAT (%)

Nút "Lưu cấu hình": w-full / py-3 / bg #1B3A6B

Mỗi section: rounded-2xl / p-6 / bg #FDFAF6 / border 1.5px / margin-bottom 24px
  Tiêu đề section: h3 / border-bottom / #1A1209
  Mỗi field: label + input (chuẩn như các form khác)
```

---

## 22. CÁC LỖI LOGIC CẦN SỬA KHI TÁI THIẾT KẾ

> Đây là danh sách đầy đủ các lỗi trong phiên bản React gốc cần được sửa ngay trong bản MAUI mới.

### 22.1 🔴 LỖI NGHIÊM TRỌNG

| #   | Mô tả lỗi                                                                                | Giải pháp trong MAUI                                                                                          |
| --- | ---------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| 1   | `Table.CurrentOrderId` không được gán khi tạo đơn mới → thẻ bàn không hiện thông tin đơn | Trong `CreateOrder()`, sau khi tạo đơn, cập nhật `table.CurrentOrderId = newOrder.Id`                         |
| 2   | Thêm món vào đơn cũ xóa mất các món `pending` trước đó                                   | Dùng `order.Items.AddRange(newItems)` thay vì lọc bỏ pending cũ                                               |
| 3   | Ghép bàn (Merge) không có logic thực                                                     | Implement: lấy tất cả items của bàn B, thêm vào đơn của bàn A, cộng total, bàn B → needs_clearing             |
| 4   | Sau thanh toán, đơn không xuất hiện trong Lịch sử                                        | Lịch sử phải đọc từ cùng một nguồn với orders (lọc `status == paid`)                                          |
| 5   | Chuyển bàn không cập nhật `Order.TableId` và `Order.TableNumber`                         | Trong `TransferTable()`, cập nhật cả order: `order.TableId = newTableId; order.TableNumber = newTable.Number` |

### 22.2 🟠 TÍNH NĂNG CẦN BỔ SUNG

| #   | Tính năng thiếu                                        | Giải pháp                                                              |
| --- | ------------------------------------------------------ | ---------------------------------------------------------------------- |
| 6   | Không có màn hình bếp                                  | Thêm trang "Kitchen Display" cho role `kitchen` hoặc tab trong Manager |
| 7   | `Order.Status` không tự đồng bộ với `OrderItem.Status` | Sau mỗi lần cập nhật item status, tự tính lại Order.Status             |
| 8   | DishStatus/Payment chỉ thấy đơn của mình               | Thêm option "Xem tất cả" cho manager, hoặc lọc theo ca làm việc        |
| 9   | Bàn `reserved` không có luồng check-in                 | Thêm nút "Check-in" → chuyển reserved → occupied → navigate đặt món    |
| 10  | Không tạo notification khi gửi đơn                     | Sau `CreateOrder()`, push notification `order_update`                  |
| 11  | Không tạo notification khi món ready                   | Sau `UpdateDishStatus(ready)`, push notification `dish_ready`          |
| 12  | `UnreadMessages` hardcode = 2                          | Tính theo tin nhắn mới kể từ lần đọc cuối của user                     |

### 22.3 🟡 LỖI NHỎ / EDGE CASE

| #   | Mô tả                                 | Giải pháp                                                                  |
| --- | ------------------------------------- | -------------------------------------------------------------------------- |
| 13  | `useEffect` cart thiếu dependency     | Trong MAUI: Load cart trong `OnNavigatedTo()` hoặc observe `ExistingOrder` |
| 14  | Nhân viên B sửa đơn của nhân viên A   | Thêm check `order.ServerId == currentUser.Id` hoặc log "chỉnh bởi: B"      |
| 15  | Chuyển bàn có thể mất `OccupiedSince` | Null-check và copy `occupiedSince` an toàn sang bàn mới                    |

### 22.4 Luồng Order Status cần implement đúng

```
Luồng đầy đủ cần có:

[Nhân viên] Thêm món → item.Status = PENDING
        ↓
[Nhân viên] Gửi lên bếp → Order.Status = CONFIRMED
        ↓
[Bếp] Nhận đơn → Order.Status = PREPARING, item.Status = PREPARING
        ↓
[Bếp] Món xong → item.Status = READY → tạo Notification dish_ready
        ↓
[Nhân viên] Nhận thông báo → đến lấy món
        ↓
[Nhân viên] Xác nhận phục vụ → item.Status = SERVED
        ↓
[Kiểm tra] Nếu TẤT CẢ items = SERVED → Order.Status = SERVED
        ↓
[Nhân viên] Thanh toán → Order.Status = PAID → Table.Status = NEEDS_CLEARING
        ↓
[Nhân viên] Dọn bàn xong → Table.Status = AVAILABLE
```

---

## PHỤ LỤC A — MAPPING MÀU SẮC MAUI

```csharp
// Trong App.xaml hoặc Styles.xaml
<Color x:Key="NavyPrimary">#1B3A6B</Color>
<Color x:Key="NavyLight">#4A90D9</Color>
<Color x:Key="BgMain">#F5F0E8</Color>
<Color x:Key="BgCard">#FDFAF6</Color>
<Color x:Key="BgInput">#EDE8DC</Color>
<Color x:Key="TextPrimary">#1A1209</Color>
<Color x:Key="TextSecondary">#7B6A57</Color>
<Color x:Key="TextMuted">#C5B8A8</Color>
<Color x:Key="TextLabel">#3D2C1E</Color>
<Color x:Key="BorderColor">#8B6E5026</Color>  <!-- rgba(139,110,80,0.15) -->
<Color x:Key="Success">#22C55E</Color>
<Color x:Key="SuccessDark">#15803D</Color>
<Color x:Key="Warning">#F59E0B</Color>
<Color x:Key="WarningDark">#92400E</Color>
<Color x:Key="Danger">#EF4444</Color>
<Color x:Key="DangerDark">#B91C1C</Color>
<Color x:Key="Info">#4A90D9</Color>
<Color x:Key="InfoDark">#1D4ED8</Color>
```

## PHỤ LỤC B — MAPPING ICON (Lucide → MAUI)

| Lucide Icon     | MAUI/SF Symbols / MaterialIcon  |
| --------------- | ------------------------------- |
| ChefHat         | `restaurant` (Material)         |
| LayoutGrid      | `grid_view`                     |
| ShoppingCart    | `shopping_cart`                 |
| UtensilsCrossed | `restaurant_menu`               |
| Receipt         | `receipt`                       |
| History         | `history`                       |
| MessageSquare   | `chat`                          |
| Bell            | `notifications`                 |
| Users           | `group`                         |
| BookOpen        | `menu_book`                     |
| Map             | `map`                           |
| BarChart3       | `bar_chart`                     |
| Settings        | `settings`                      |
| LogOut          | `logout`                        |
| Search          | `search`                        |
| Plus            | `add`                           |
| Minus           | `remove`                        |
| Trash2          | `delete`                        |
| Lock            | `lock`                          |
| Eye/EyeOff      | `visibility` / `visibility_off` |
| CheckCircle     | `check_circle`                  |
| AlertCircle     | `error`                         |
| Clock           | `schedule`                      |
| Send            | `send`                          |
| Download        | `download`                      |
| ArrowLeftRight  | `swap_horiz`                    |
| GitMerge        | `merge`                         |
| StickyNote      | `sticky_note_2`                 |
| QrCode          | `qr_code`                       |
| Banknote        | `payments`                      |
| TrendingUp      | `trending_up`                   |
| ArrowLeft       | `arrow_back`                    |
| Mail            | `email`                         |
| Phone           | `phone`                         |
| Shield          | `shield`                        |
| ChevronDown     | `expand_more`                   |
| ChevronRight    | `chevron_right`                 |
| X               | `close`                         |
| Menu            | `menu`                          |
| Calendar        | `calendar_today`                |
| UserCheck       | `how_to_reg`                    |
| UserX           | `person_off`                    |
| Star            | `star`                          |

---

_Tài liệu này được tạo từ phân tích source code React 18 + TypeScript của The Golden Plate v1.0_
_Tổng số trang: 21 | Tổng số component: 16 | Routes: 17_
