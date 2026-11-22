# FitGym

Fit_Gym là sản phẩm quản lý và ứng dụng hội viên dành cho đơn vị gym vừa và nhỏ. Mỗi khách hàng được cài ứng dụng và SQL database riêng, có thể bắt đầu với một cơ sở. Solution dùng .NET 9, ASP.NET Core MVC, Web API, EF Core và SQL Server; ứng dụng hội viên dùng Flutter.

## Cấu trúc

- `src/FitGym.Web`: giao diện MVC cho vận hành phòng gym.
- `src/FitGym.Api`: Web API; `/api/health` kiểm tra kết nối cơ sở dữ liệu.
- `src/FitGym.Application`: thực thể và hợp đồng nghiệp vụ, bao gồm hồ sơ đơn vị/cơ sở.
- `src/FitGym.Infrastructure`: Identity, EF Core, DbContext và migrations.
- `mobile/fit_gym_mobile`: ứng dụng Flutter dành cho hội viên.
- `tests`: các bài kiểm thử nghiệp vụ, Web và API.

## Cơ sở dữ liệu phát triển

Máy hiện tại dùng SQL Server Express `localhost\SQLEXPRESS` và cơ sở dữ liệu `FitGymDev`, xác thực Windows. Chuỗi kết nối nằm trong `appsettings.Development.json` của Web và API. Công cụ EF Core có thể dùng biến môi trường `FITGYM_CONNECTION_STRING` khi cần kết nối nơi khác.

Từ thư mục gốc, chạy migration bằng:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/FitGym.Infrastructure/FitGym.Infrastructure.csproj --startup-project src/FitGym.Api/FitGym.Api.csproj --context FitGymDbContext
```

Các bảng tài khoản và vai trò logic của ERD được triển khai bằng ASP.NET Core Identity. Các bảng nghiệp vụ dùng tên tiếng Việt không dấu như trong ERD. Migration chứa các khóa chính, khóa ngoại, chỉ mục duy nhất và một số ràng buộc dữ liệu. Các quy tắc liên bảng như thời hạn gói, trạng thái check-in và tồn kho sẽ được xử lý ở lớp nghiệp vụ khi xây dựng chức năng.

## Chạy ứng dụng

```powershell
dotnet build FitGym.sln
dotnet run --project src/FitGym.Web --launch-profile http
```

Mở `http://localhost:5057/Account/Login` để dùng giao diện nhân viên/quản lý. Ở terminal thứ hai, chạy API cho ứng dụng di động và cổng vào:

```powershell
dotnet run --project src/FitGym.Api --launch-profile http
```

`http://localhost:5202/api/health` chỉ trả trạng thái API và kết nối database dưới dạng JSON; đây không phải trang giao diện Web.

Ở thư mục `mobile/fit_gym_mobile`:

```powershell
flutter pub get
flutter test
flutter analyze
flutter run
```

Web hiện có đăng nhập/phân quyền, hội viên, gói tập, đăng ký/gia hạn, tra cứu thanh toán, check-in hỗ trợ, kho, thiết bị/bảo trì và nền tảng dữ liệu tập luyện. Flutter có đăng nhập và xem dữ liệu hội viên qua API. QR hiện chỉ chứa định danh giao dịch và số tiền; chưa kết nối cổng thanh toán, không tự thu tiền, xác nhận thanh toán hoặc kích hoạt gói. Kết nối thiết bị nhận diện khuôn mặt và phân tích động tác bằng AI chưa được triển khai. Hệ thống không điều khiển cửa ra vào.

Web có **Thanh toán gói tập** (`/Payments`) để tra cứu giao dịch SQL theo ngày Việt Nam/phương thức/trạng thái; cùng một token báo giá đăng ký gửi lặp/đồng thời chỉ được ghi một giao dịch. Thanh toán tiền mặt tại quầy được nhân viên xác nhận khi lập đăng ký; giao dịch QR hiện luôn ở trạng thái chờ. Giá tại bản cài đặt phát triển hiện là 300.000 VND và 850.000 VND, không phải dữ liệu mặc định cho mọi khách hàng.

Quản lý có **Báo cáo vận hành** (`/Reports`) theo khoảng ngày: doanh thu và số giao dịch chỉ tính khoản đã thanh toán thành công, yêu cầu QR đang chờ và lượt check-in thành công hiển thị riêng. Số liệu được đọc từ SQL, không sinh dữ liệu minh họa.

## Thiết lập đơn vị sử dụng

Quản lý mở **Thông tin đơn vị** (`/Organization`) để nhập tên đơn vị, tên/địa chỉ cơ sở đầu tiên và liên hệ tùy chọn; sau đó có thể tải logo PNG/JPEG tối đa 512 KiB. Không tự tạo chi nhánh, hội viên hay giá gói mẫu. Web và Flutter đọc cùng nhận diện từ database của đơn vị. Khi chưa cấu hình, tên hiển thị là tên sản phẩm Fit_Gym.

Chưa mở cơ sở thứ hai: việc phân quyền và khóa ngoại cơ sở ở các bảng vận hành vẫn cần triển khai trước. Mỗi đơn vị gym dự kiến có bản cài đặt và SQL database riêng.

## Bắt đầu vận hành và cấp tài khoản hội viên

1. Quản lý thiết lập thông tin đơn vị/cơ sở, sau đó hoàn thiện hồ sơ nhân viên của mình qua cảnh báo trên Tổng quan hoặc **Tài khoản nhân viên → Hoàn thiện hồ sơ**. Nhập họ tên, chức vụ, email và ngày vào làm thực tế; không có hồ sơ nhân viên tự tạo bằng dữ liệu giả.
2. Quản lý tạo gói tập; nhân viên tạo hồ sơ hội viên, đăng ký gói và xác nhận tiền mặt tại quầy theo luồng hiện có.
3. Trong chi tiết hội viên, chọn **Cấp tài khoản app**, nhập tên đăng nhập và mật khẩu theo chính sách, bàn giao trực tiếp cho hội viên. Tên đăng nhập dùng chữ không dấu/số/dấu chấm/gạch dưới/gạch ngang. Tài khoản app không có quyền nhân viên/quản lý. Có thể tra cứu tên đăng nhập ngay tại hồ sơ; chỉ quản lý được đặt lại mật khẩu app.
4. Flutter kết nối API của đúng bản cài đặt bằng `--dart-define=FITGYM_API_URL=...`. Gói hiện tại chỉ hiển thị kỳ đã thanh toán, có hiệu lực và nằm trong thời hạn; kỳ QR tương lai nằm trong lịch sử thanh toán, không che gói đang dùng.

Trang đăng nhập Web cho chọn Nhân viên hoặc Quản lý; backend kiểm tra vai trò thật của tài khoản trước khi cấp phiên. Mỗi tài khoản có mật khẩu riêng. Khi quên mật khẩu, nhân viên liên hệ quản lý để đặt lại tại **Tài khoản & nhân viên**; quản lý liên hệ đơn vị triển khai để xác minh và khôi phục quyền truy cập. Màn quản trị tài khoản chỉ dành cho quản lý và hỗ trợ tạo tài khoản, cập nhật hồ sơ/vai trò/trạng thái, đặt lại mật khẩu.

Tổng quan tự tải lại số liệu mỗi 60 giây, có nút **Làm mới dữ liệu**, giữ bộ lọc ngày lịch sử và chỉ chuyển sang ngày Việt Nam mới khi theo dõi hôm nay. Mất kết nối giữ snapshot gần nhất kèm thông báo. Web/API kiểm tra trạng thái tài khoản, SecurityStamp và vai trò; đổi mật khẩu hoặc đổi quyền làm phiên cũ không còn được chấp nhận. Luồng refresh token/lưu token lâu dài trên Mobile chưa triển khai.
