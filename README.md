# Lá Library — Hệ thống quản lý thư viện ASP.NET Core

Website thực hiện nghiệp vụ quản lý thư viện theo tài liệu “Hệ thống quản lý thư viện” và 10 tiêu chí Bài kiểm tra kỹ năng số 2. Dùng ASP.NET Core MVC (.NET 10), Razor, Entity Framework Core và SQLite. Giao diện tiếng Việt, responsive, tài nguyên giao diện lưu nội bộ, không phụ thuộc CDN.

## Chạy website

Yêu cầu: .NET SDK 10 và kết nối Internet ở lần restore đầu tiên.

```powershell
dotnet restore HeThongQLTV.slnx
dotnet run --project HeThongQLTV/HeThongQLTV.csproj --launch-profile http
```

Mở http://localhost:5256. Trong Visual Studio, mở `HeThongQLTV.slnx`, chọn dự án HeThongQLTV và profile `http`, nhấn F5.

SQLite tự tạo tại `HeThongQLTV/database/library.db`. Khi chạy profile Development lần đầu, hệ thống tạo 12 đầu sách, 6 độc giả, 16 phiếu mượn và 3 tài khoản. Tên sách phục vụ demo; mô tả, số lượng, vị trí, năm xuất bản và thông tin người dùng là dữ liệu minh họa. Dữ liệu được giữ lại qua những lần chạy tiếp theo.

| Tên đăng nhập | Mật khẩu demo | Vai trò |
| --- | --- | --- |
| admin | ThuVien@123 | Quản trị viên |
| thuthu | ThuVien@123 | Thủ thư |
| docgia | ThuVien@123 | Độc giả Nguyễn Minh Anh |

Các tài khoản này dùng để demo cục bộ. Production không tự seed trừ khi đặt `SeedDemo=true`. Bản này hướng tới bài kiểm tra và chạy thử cục bộ; trước triển khai thật cần cấu hình HTTPS, tài khoản quản trị riêng, sao lưu và quy trình migration.

## Chức năng

- Đăng nhập bằng cookie; mật khẩu băm bằng ASP.NET PasswordHasher; kiểm tra trạng thái và vai trò tài khoản trên mỗi yêu cầu.
- Admin: toàn bộ nghiệp vụ, tạo tài khoản, đổi vai trò, liên kết thẻ độc giả, đặt lại mật khẩu, khóa/mở tài khoản. Không được tự khóa hoặc hạ quyền chính mình.
- Thủ thư: CRUD sách/độc giả; tìm kiếm, lọc; mượn, trả, gia hạn, thu phạt; quản lý hàng đặt trước; xem dashboard và xuất CSV.
- Độc giả: tìm sách, xem chi tiết, đặt trước sách hết bản sẵn có, hủy yêu cầu của mình, xem và gia hạn phiếu của mình.
- Lưu lịch sử giao dịch; chặn xóa sách/độc giả đã có dữ liệu liên kết. Các thao tác thay đổi dữ liệu dùng POST và chống CSRF.
- Dashboard tính dữ liệu thực từ CSDL: tồn kho, số độc giả, đang mượn, quá hạn, biểu đồ 7 ngày, phân bố thể loại. Báo cáo lọc theo ngày mượn, xếp hạng sách và độc giả; CSV UTF-8 có BOM và chống công thức spreadsheet.

## Cấu trúc

```text
HeThongQLTV/
  Controllers/       HTTP, phân quyền và kiểm tra biểu mẫu
  Models/            Thực thể, validation và view model
  Data/              DbContext, quan hệ và dữ liệu mẫu
  Services/          Nghiệp vụ mượn, trả, gia hạn, đặt trước
  Views/             Frontend Razor MVC
  wwwroot/css,js/    Giao diện responsive và xác nhận thao tác
  database/          SQLite runtime, không commit dữ liệu
  appsettings.json   Cấu hình CSDL và quy định thư viện
  Properties/        Profile chạy Visual Studio/dotnet
config/              Hướng dẫn cấu hình
database/           Mô tả lược đồ dữ liệu (ở thư mục gốc)
docs/               Đối chiếu tiêu chí, nhật ký AI, hướng dẫn demo
tests/              Kiểm thử nghiệp vụ và trình duyệt
```

## Quy định demo và điểm khác tài liệu

Tài liệu gốc còn một số quy định chưa thống nhất. Bản này chọn rõ:

- Tối đa 5 cuốn đang mượn; thời hạn 14 ngày; gia hạn tối đa 2 lần, mỗi lần 7 ngày; phạt quá hạn 2.000 đồng/ngày. Đổi trong `LibraryRules` của `appsettings.json` hoặc biến môi trường.
- Chỉ đặt trước khi hết bản sẵn có; FIFO theo thời gian và mã đăng ký. Khi sách được trả, người đứng đầu được ưu tiên mượn; yêu cầu chuyển sang “Đã nhận” trong cùng giao dịch. Có thể hủy hàng chờ; chưa tự hết hạn giữ chỗ.
- Một phiếu ứng với một bản sách, hỗ trợ trả và gia hạn riêng từng cuốn. Số bản khả dụng được tính bằng tổng kho trừ các phiếu chưa trả; không duy trì bộ đếm tồn kho thứ hai.
- Tác giả/thể loại/NXB là trường dữ liệu của sách trong phạm vi KT2, chưa có màn hình danh mục riêng. Không triển khai quản lý từng mã bản sách, phạt hỏng/mất hoặc gửi email.
- Đã bổ sung chatbot AI cho tra cứu, tóm tắt mô tả và gợi ý sách; xem mục Chatbot Lá AI bên dưới.
- Các phần còn thiếu của tài liệu (quy trình sao lưu/khôi phục giao diện, thông báo, đăng ký độc giả công khai) không nằm trong luồng demo hiện tại. Tài khoản do Admin tạo; sao lưu SQLite theo hướng dẫn bên dưới.

## Cấu hình

Xem `.env.example` và `config/README.md`. ASP.NET Core **không tự đọc `.env`**: dùng biến môi trường của shell hoặc `appsettings.Development.json`. Ví dụ:

```powershell
$env:LibraryRules__MaxLoans = "5"
dotnet run --project HeThongQLTV/HeThongQLTV.csproj --launch-profile http
```

Giữ số ngày/giới hạn lớn hơn 0 và mức phạt không âm. File CSDL dùng đường dẫn tương đối với content root của ứng dụng.

Sao lưu demo: dừng website để SQLite đóng kết nối, sao chép file `library.db` sang vị trí lưu trữ riêng. Không sao chép riêng file chính khi ứng dụng còn hoạt động ở chế độ WAL. Không đưa dữ liệu cá nhân hoặc file database vào Git.

## Kiểm thử

```powershell
dotnet build HeThongQLTV.slnx
dotnet run --project tests/LibraryChecks/LibraryChecks.csproj
```

Bộ kiểm thử console dùng CSDL SQLite trong bộ nhớ riêng cho từng tình huống, không thay đổi dữ liệu demo.

Kiểm thử trình duyệt: cài Node.js, Microsoft Edge và `playwright` trong môi trường kiểm thử; chạy website rồi chạy `node tests/browser-checks.cjs`. Có thể đặt `PLAYWRIGHT_MODULE` trỏ tới module Playwright và `BASE_URL` để đổi địa chỉ. Script tạo, sửa và xóa một đầu sách kiểm thử; nên chạy trên CSDL demo. Ảnh được lưu trong `docs/screenshots/`.

Xem `docs/TEST-RESULTS.md` cho kết quả thực tế, `docs/KT2-CHECKLIST.md` cho đối chiếu tiêu chí, `docs/DEMO.md` cho kịch bản trình bày. Nhật ký `docs/AI-LOG.md` có phần sinh viên cần tự kiểm tra và điền, không xác nhận thay người học.

## Chatbot Lá AI

Chỉ tài khoản **Độc giả (DocGia)** được sử dụng chatbot. Sau khi đăng nhập bằng tài khoản Độc giả, nhấn **Chat** ở góc dưới bên phải. Admin và Thủ thư không thấy chatbot và bị từ chối khi gọi trực tiếp API chat. Hỗ trợ tìm sách bằng câu tự nhiên, tóm tắt mô tả sách và gợi ý sách cùng thể loại; có ngữ cảnh 8 lượt gần nhất trong trang, liên kết mã sách, phóng to, thu nhỏ và bắt đầu lại. Lịch sử không lưu qua tải lại trang.

- Development tự đọc riêng `OPENAI_API_KEY` từ `.env.local` ở thư mục gốc repo (đã bỏ qua trong Git). Biến môi trường được ưu tiên. Production dùng biến môi trường `OPENAI_API_KEY`.
- Model mặc định `gpt-4.1-mini`; thay bằng `OpenAI__Model` trong môi trường nếu cần. Khóa chỉ được dùng ở máy chủ.
- Tích hợp [OpenAI Responses API](https://developers.openai.com/api/docs/guides/text), không lưu response trên API (`store: false`). Câu hỏi, tối đa 8 lượt trước và danh mục sách được gửi để tạo câu trả lời; không gửi dữ liệu độc giả hoặc phiếu mượn.
- Hiện thư viện chỉ lưu mô tả, chưa có toàn văn. AI được yêu cầu nói rõ giới hạn nguồn; dữ liệu mô tả demo không đủ để tạo bản tóm tắt nội dung đáng tin cậy. Cập nhật mô tả thật trong màn hình sửa sách để có kết quả hữu ích.
- Phiên bản này gửi tối đa 200 đầu sách làm ngữ cảnh; vượt giới hạn sẽ thông báo dùng danh mục. Yêu cầu đăng nhập, CSRF, tối đa 1.500 ký tự/câu hỏi và 10 yêu cầu/phút/tài khoản.
- Kiểm thử trình duyệt: `node tests/chat-checks.cjs` (mặc định localhost:5257; đổi bằng BASE_URL). Đặt `CHAT_LIVE_TEST=1` để chạy thêm ba câu hỏi qua API thật, có sử dụng hạn mức API.
