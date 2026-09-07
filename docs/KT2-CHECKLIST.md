# Đối chiếu Bài kiểm tra kỹ năng số 2

| Tiêu chí | Phần triển khai | Cách chứng minh |
| --- | --- | --- |
| 1. Cấu trúc hợp lý | MVC Controllers/Models/Views, Data, Services, wwwroot, config, database, docs, tests | Mở cây thư mục và README |
| 2. Đăng nhập/phân quyền | Cookie auth, PasswordHasher, Authorize theo vai trò, kiểm tra tài khoản bị khóa | Đăng nhập 3 vai trò; DocGia truy cập /Users bị 403 |
| 3. CRUD nghiệp vụ | Sách, độc giả; tài khoản thêm/sửa/khóa; phiếu mượn và trả | Thêm sửa xóa sách chưa giao dịch, chặn xóa sách có lịch sử |
| 4. Tìm kiếm/lọc/sắp xếp | Sách theo tên/tác giả/ISBN, thể loại, còn/hết; A–Z/mới/số lượng; phân trang | Dùng bộ lọc danh mục; tìm độc giả và phiếu |
| 5. Thống kê/báo cáo | Dashboard, biểu đồ 7 ngày, top sách/độc giả, báo cáo theo kỳ, CSV | So sánh dữ liệu trước/sau mượn và tải CSV |
| 6. Giao diện rõ ràng | Tiếng Việt, sidebar theo quyền, responsive, toast trạng thái, validation, xác nhận xóa/trả/thu tiền | Kiểm tra desktop và mobile |
| 7. CSDL ổn định | EF Core SQLite, FK RESTRICT, unique, CHECK, transaction, seed | Khởi động lại, dữ liệu giữ nguyên; kiểm thử nghiệp vụ |
| 8. Xử lý lỗi | ModelState, business errors, CSRF, 403/404 và exception page không lộ stack trace | Nhập sai, trả trùng, hết sách, thẻ hết hạn |
| 9. Minh chứng AI | AI-LOG.md ghi yêu cầu, phản hồi tóm tắt, các phần code và bảng tự kiểm chứng | Sinh viên tự chạy test, điền kết quả và chỉnh sửa thực tế |
| 10. Mã nguồn/tài liệu | README, .env.example, NuGet.Config, .gitignore, hướng dẫn demo, Git | Chạy theo README, xem lịch sử commit |

Không thể xác nhận thay sinh viên phần người học đã kiểm tra/chỉnh sửa. Phần này được để trống trung thực trong nhật ký AI.
