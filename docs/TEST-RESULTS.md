# Kết quả kiểm thử ngày 07/09/2026

Môi trường: Windows, .NET SDK 10.0.202, ASP.NET Core 10.0.6, SQLite và Microsoft Edge headless. Kết quả bên dưới do Codex chạy thực tế, không phải xác nhận sinh viên.

## Build

`dotnet build HeThongQLTV.slnx --no-restore -m:1`: thành công, 0 lỗi, 0 cảnh báo. Gói SQLitePCLRaw.bundle_e_sqlite3 được nâng lên 3.0.5 để thay phụ thuộc SQLite cũ có cảnh báo NU1903.

## Nghiệp vụ

`dotnet run --project tests/LibraryChecks/LibraryChecks.csproj --no-build --no-restore`: **20/20 đạt**.

- Seed 3 vai trò và mật khẩu băm đúng.
- Mượn thành công, giảm khả dụng và đặt hạn 14 ngày.
- Chặn hết sách, thẻ hết hạn, thẻ khóa, vượt hạn mức và trùng đầu sách đang mượn.
- Trả sách khôi phục khả dụng, tính đúng phạt; trả trùng không tăng tồn kho.
- Nợ phạt chặn mượn mới.
- Gia hạn tăng ngày/lượt; chặn quá hạn, vượt lượt và phiếu của độc giả khác.
- Chặn đặt trước sách còn sẵn hoặc đặt trước trùng.
- Có người đặt trước thì không gia hạn.
- FIFO đặt trước và chuyển trạng thái Đã nhận khi mượn.
- Khóa ngoại chặn xóa sách có phiếu; CHECK chặn tồn kho âm.

Mỗi tình huống dùng SQLite trong bộ nhớ riêng, không ghi vào CSDL demo.

## Trình duyệt

`node tests/browser-checks.cjs`: **26 kiểm tra đạt**, mã thoát 0.

- 11 trang của Admin phản hồi HTTP 200.
- Mobile 390 × 844 không tràn ngang toàn trang.
- Thêm, tìm, sửa và xóa sách kiểm thử.
- Thêm, tìm, sửa và xóa độc giả kiểm thử.
- Chặn Admin tự khóa tài khoản.
- Trang 404 thân thiện, đúng mã HTTP.
- POST thiếu CSRF token bị HTTP 400.
- Xuất CSV thành công, nội dung tiếng Việt UTF-8.
- Độc giả bị 403 khi mở Members, Users, Books/Edit, Loans/Create, Reports.
- Độc giả chỉ xem phiếu của mình.
- Thủ thư xem được độc giả nhưng không được quản lý tài khoản.
- Không có lỗi JavaScript được trình duyệt báo.

## Kiểm tra hình ảnh

Đã mở và kiểm tra ảnh dashboard và danh mục mobile trong `docs/screenshots/`. Mã phiếu được hiển thị dạng PM0016; menu mobile ẩn mặc định, mở qua nút menu; danh mục hai cột trên điện thoại. Có ảnh danh mục desktop kèm theo.

## Các lỗi đã phát hiện và sửa

1. NuGet PackageSourceMapping không xét nguồn online: thêm NuGet.Config riêng cho dự án.
2. SQLite dependency có cảnh báo bảo mật: cập nhật bundle 3.0.5, build lại hết cảnh báo.
3. Razor hiểu chuỗi PM@... là văn bản: dùng biểu thức Razor tường minh cho mã định danh.
4. Trường phụ của sách bị coi là bắt buộc: dùng Required tường minh và chuẩn hóa trường phụ rỗng trước khi lưu.
5. StatusCodePages thiếu dấu hỏi trước query string: sửa đường dẫn để lỗi 400/404 không gây exception.
6. Ảnh mobile chụp trong lúc sidebar chuyển động: chụp khi animation bị vô hiệu hóa và kiểm tra hình cuối.

## Phạm vi chưa kiểm chứng

Chưa kiểm thử tải đồng thời lớn, triển khai Internet, phục hồi dữ liệu sản xuất hay API AI. Các bài kiểm thử hiện tại phục vụ phạm vi KT2 và dữ liệu demo. Chuẩn bị migration, cấu hình triển khai và kiểm thử tải nếu mở rộng sử dụng thực tế.
