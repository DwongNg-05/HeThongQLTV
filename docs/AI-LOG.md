# Nhật ký sử dụng AI hỗ trợ lập trình

Ngày thực hiện: 07/09/2026. Công cụ: Codex. Đây là bản ghi tóm tắt phiên làm việc thực tế, không phải bản chép nguyên văn toàn bộ hội thoại.

## Yêu cầu thực tế

Người dùng gửi tài liệu “Hệ thống quản lý thư viện.docx”, sau đó yêu cầu: “hãy tạo cho toi trang web từ tài liệu đã gửi và đúng với các tiêu chí trong bài kiểm tra kĩ năng số 2 này bằng ASP.NET”, kèm ảnh 10 tiêu chí.

## Phản hồi và phần mã được AI hỗ trợ

| Giai đoạn | Nội dung phản hồi/triển khai | Tệp tiêu biểu |
| --- | --- | --- |
| Phân tích | Nhận diện mẫu ASP.NET Core MVC .NET 10, phân quyền 3 vai trò; chỉ rõ quy định chưa thống nhất | README.md, docs/KT2-CHECKLIST.md |
| Dữ liệu | Đề xuất SQLite/EF Core, seed minh họa, FK và ràng buộc số lượng | Data/LibraryDb.cs, Models/Library.cs |
| Nghiệp vụ | Kiểm tra điều kiện mượn, transaction, chống trả trùng, phạt theo ngày, FIFO đặt trước | Services/CirculationService.cs |
| Backend | Controller CRUD, phân quyền, cookie, bảo vệ CSRF, xuất CSV | Program.cs, Controllers/ |
| Giao diện | Thiết kế Lá Library xanh–kem, dashboard, trang danh mục, biểu mẫu và mobile | Views/, wwwroot/css/site.css |
| Kiểm thử | Tạo kiểm thử nghiệp vụ SQLite riêng và browser flows; ghi lại kết quả chạy | tests/, docs/TEST-RESULTS.md |
| Hiệu chỉnh | Sửa nguồn NuGet bị PackageSourceMapping chặn; cập nhật SQLite khi build phát hiện cảnh báo lỗ hổng | NuGet.Config, HeThongQLTV.csproj |

AI đưa ra các quyết định demo: 5 cuốn/14 ngày, 2 lần gia hạn × 7 ngày, 2.000đ/ngày; chỉ đặt trước sách hết bản. Các giá trị này không được coi là quy định đã được giảng viên xác nhận.

## Phần sinh viên tự kiểm tra và chỉnh sửa

**Chưa được sinh viên xác nhận tại thời điểm tạo dự án.** Người học tự chạy và ghi đúng trải nghiệm thực tế; không ký nhận các thao tác chưa làm.

| Phần kiểm tra | Kết quả sinh viên | Chỉnh sửa thực tế và lý do | Ngày/người kiểm tra |
| --- | --- | --- | --- |
| Đăng nhập và phân quyền | Chưa điền | Chưa điền | Chưa điền |
| Thêm/sửa/xóa sách, độc giả | Chưa điền | Chưa điền | Chưa điền |
| Mượn/trả/quá hạn/thu phạt | Chưa điền | Chưa điền | Chưa điền |
| Gia hạn và đặt trước | Chưa điền | Chưa điền | Chưa điền |
| Kiểm tra truy vấn và CSDL | Chưa điền | Chưa điền | Chưa điền |
| Giao diện và báo cáo | Chưa điền | Chưa điền | Chưa điền |

Lưu ảnh chụp phần đã tự kiểm tra, trích đoạn code trước/sau nếu có sửa, và commit tương ứng. Có thể đính kèm bản xuất hội thoại Codex làm minh chứng prompt nguyên văn nếu giảng viên yêu cầu.

## Hiệu chỉnh qua kiểm thử thực tế

Codex phát hiện và sửa biểu thức mã phiếu Razor, trường sách phụ bị bắt buộc và query của trang lỗi thiếu dấu hỏi. Kết quả cuối: build 0 lỗi/0 cảnh báo, 20/20 nghiệp vụ và 26 kiểm tra trình duyệt đạt. Chi tiết ở TEST-RESULTS.md. Đây là kiểm chứng của công cụ, không thay thế phần tự kiểm tra của sinh viên.
