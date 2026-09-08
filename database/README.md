# Thiết kế dữ liệu

Ứng dụng dùng EF Core SQLite với 5 bảng, tự tạo bằng EnsureCreated cho bản demo mới. EnsureCreated không tự nâng cấp lược đồ CSDL cũ; khi thay đổi model trong giai đoạn phát triển cần tạo CSDL demo mới hoặc bổ sung migration trước sử dụng thật.

| Bảng | Nội dung và quan hệ |
| --- | --- |
| Books | Sách, tác giả, thể loại, NXB, số bản, vị trí, mô tả. CHECK Quantity >= 0. |
| Members | Họ tên, email duy nhất, loại thẻ, ngày hết hạn, trạng thái. |
| Users | Username duy nhất, PasswordHash, vai trò, Active, FK MemberId duy nhất và nullable. |
| Loans | FK BookId, MemberId; ngày mượn/hạn trả/ngày trả; số lần gia hạn; tiền phạt nguyên đồng; trạng thái thanh toán. |
| Reservations | FK BookId, MemberId; thời gian đăng ký và trạng thái Đang chờ/Đã nhận/Đã hủy. |

Khóa ngoại dùng RESTRICT, giữ lịch sử. Số lượng khả dụng tính từ phiếu đang mượn. Service bao bọc các thao tác kiểm tra rồi ghi mượn/trả/gia hạn/đặt trước trong transaction SQLite để tránh hai yêu cầu đồng thời lấy cùng bản sách. Validation bổ sung ở controller ngăn sửa tổng số bản thấp hơn số đang mượn.

Không lưu mật khẩu rõ; dữ liệu demo được seed bằng PasswordHasher. CSDL chạy thực tế nằm ở HeThongQLTV/database/library.db và không nằm trong Git.
