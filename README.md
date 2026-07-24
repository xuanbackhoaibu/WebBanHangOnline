# WebBanHangOnline

Website bán hàng trực tuyến xây dựng bằng ASP.NET Core MVC, tập trung vào luồng mua sắm, quản trị sản phẩm/đơn hàng, phân quyền người dùng và tích hợp thanh toán.

## Công nghệ sử dụng

- Backend: C#, ASP.NET Core MVC, Razor Pages, ASP.NET Core Identity
- Database: SQL Server, Entity Framework Core, Migration/Seed data
- Realtime: SignalR cho chat/hỗ trợ khách hàng
- Thanh toán: VNPay, MoMo, COD, VietQR
- AI/API: Google Gemini API cho chatbot gợi ý sản phẩm
- Frontend: Razor View, Bootstrap, jQuery, CSS/JavaScript
- Công cụ: Visual Studio/Rider, .NET CLI, LibMan

## Chức năng chính

- Xem danh sách sản phẩm, chi tiết sản phẩm với SEO URL dạng `san-pham/{slug}-{id}`
- Quản lý danh mục, sản phẩm, biến thể sản phẩm, hình ảnh sản phẩm
- Giỏ hàng, yêu thích sản phẩm, đánh giá sản phẩm
- Đặt hàng, theo dõi đơn hàng, cập nhật trạng thái thanh toán
- Đăng ký, đăng nhập, phân quyền `Admin`, `User`, `Client`
- Khu vực admin quản lý sản phẩm, người dùng, đơn hàng, thông báo, hỗ trợ và báo cáo
- Chat hỗ trợ realtime bằng SignalR
- Chatbot hỗ trợ tư vấn/gợi ý sản phẩm
- Seed dữ liệu vai trò, tài khoản admin và danh mục mặc định

## Cấu trúc thư mục

```text
WebBanHangOnline/
  Areas/Admin/          màn hình và controller quản trị
  Areas/Identity/       đăng nhập, đăng ký, tài khoản
  Controllers/          controller phía khách hàng
  Data/                 DbContext, seed database
  Hubs/                 SignalR chat hub
  Models/               entity, view model, payment model
  Services/             service tích hợp thanh toán
  Views/                Razor views
  wwwroot/              CSS, JavaScript, ảnh, thư viện frontend
```

## Cách chạy project

Yêu cầu:

- .NET SDK 9
- SQL Server
- Visual Studio/Rider hoặc .NET CLI

Các bước chạy:

```bash
git clone https://github.com/xuanbackhoaibu/WebBanHangOnline.git
cd WebBanHangOnline
dotnet restore
```

Cập nhật chuỗi kết nối, Gemini API key và thông tin thanh toán trong `WebBanHangOnline/appsettings.Development.json`, biến môi trường hoặc user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=WebBanHangOnline;Trusted_Connection=True;TrustServerCertificate=True" --project WebBanHangOnline/WebBanHangOnline.csproj
dotnet user-secrets set "Gemini:ApiKey" "your-gemini-api-key" --project WebBanHangOnline/WebBanHangOnline.csproj
dotnet user-secrets set "VNPay:TmnCode" "your-vnpay-code" --project WebBanHangOnline/WebBanHangOnline.csproj
dotnet user-secrets set "VNPay:HashSecret" "your-vnpay-secret" --project WebBanHangOnline/WebBanHangOnline.csproj
```

Chạy ứng dụng:

```bash
dotnet run --project WebBanHangOnline/WebBanHangOnline.csproj
```

Ứng dụng sẽ tự chạy migration/seed dữ liệu khi khởi động.

## Tài khoản demo

Tài khoản admin được seed trong code:

```text
Email: admin@shop.com
Password: Admin@123
```

Nên đổi mật khẩu và cấu hình secret/API key bằng `dotnet user-secrets` hoặc biến môi trường trước khi public.

## Ảnh demo

Repo đã có ảnh sản phẩm và banner trong `WebBanHangOnline/wwwroot/images/`.

![Banner demo](WebBanHangOnline/wwwroot/images/banner2.jpg)

Khi có ảnh chụp màn hình giao diện, nên đặt vào `docs/screenshots/` và bổ sung:

- Trang chủ / danh sách sản phẩm
- Chi tiết sản phẩm
- Giỏ hàng / thanh toán
- Dashboard admin
- Chat hỗ trợ

## Điểm nổi bật khi trao đổi với nhà tuyển dụng

- Có đủ luồng end-to-end của một website thương mại điện tử
- Biết dùng Identity để xác thực/phân quyền
- Biết thiết kế entity quan hệ như sản phẩm, danh mục, biến thể, đơn hàng, đánh giá, wishlist
- Có tích hợp thanh toán và realtime chat
- Có tư duy mở rộng sang chatbot/API bên ngoài

## Tác giả

Trần Xuân Bắc

GitHub: https://github.com/xuanbackhoaibu
