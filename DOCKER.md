# Triển khai bằng Docker

Yêu cầu: Docker Engine hoặc Docker Desktop chạy Linux containers, Docker Compose v2 và Internet để tải image/package. Không cần cài .NET trên máy chủ. Image dùng .NET 10 theo [hướng dẫn Microsoft](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-10.0).

## Chạy lần đầu

```sh
git clone https://github.com/DwongNg-05/HeThongQLTV.git
cd HeThongQLTV
cp .env.docker.example .env
```

PowerShell: thay lệnh `cp` bằng `Copy-Item .env.docker.example .env` nếu cần.

Sửa `.env`, đặt `ADMIN_PASSWORD` thành mật khẩu riêng ít nhất 12 ký tự. Giữ dấu nháy đơn quanh mật khẩu để ký tự `$` không bị Compose nội suy. Có thể đổi `ADMIN_USERNAME` trước lần chạy đầu.

```sh
docker compose up -d --build
docker compose ps
docker compose logs --tail=100 web
```

Mở http://localhost:8080, đăng nhập bằng tài khoản trong `.env`. CSDL mới chỉ có tài khoản quản trị, không có dữ liệu hoặc tài khoản demo. Tạo sách, độc giả và tài khoản từ giao diện quản trị. Tài khoản khởi tạo chỉ được tạo khi bảng người dùng trống; thay mật khẩu trong `.env` không đặt lại mật khẩu tài khoản đã tồn tại. Đặt lại mật khẩu bằng màn hình quản lý tài khoản.

Mặc định cổng chỉ mở trên máy chủ (`127.0.0.1`). Để truy cập qua IP máy chủ, đổi `APP_BIND_ADDRESS=0.0.0.0` và mở cổng `APP_PORT` trên firewall. Khi công khai Internet, đặt reverse proxy có HTTPS (Nginx/Caddy hoặc proxy của nền tảng) phía trước cổng này; đặt `ALLOWED_HOSTS` thành tên miền, nhiều tên cách nhau bằng `;`. Không nhập tên miền kèm `https://` hoặc đường dẫn. Cấu hình mặc định cung cấp HTTP nội bộ, không tự cấp chứng chỉ TLS.

## Dữ liệu và cập nhật

- Volume `hethongqltv_library-data` chứa SQLite; `hethongqltv_library-keys` giữ khóa cookie qua các lần tạo lại container. Tiến trình ứng dụng chạy bằng người dùng không phải root.
- Dùng một instance với SQLite; không tăng số replica dùng chung CSDL.
- `.env`, CSDL và khóa cục bộ bị loại khỏi Docker build context. Bảo vệ quyền truy cập `.env`, Docker và các bản sao lưu; volume khóa chứa khóa không mã hóa trên đĩa.
- Docker Compose không tự đưa mọi biến trong `.env` vào ứng dụng. Muốn thêm cấu hình, khai báo biến đó trong `environment` của `compose.yaml`. Chatbot cần cấu hình API riêng; cấu hình Docker mặc định không có khóa API.

```sh
git pull --ff-only
docker compose up -d --build
```

`docker compose down` giữ volume. **Không chạy `docker compose down -v` nếu cần giữ dữ liệu.** Sao lưu trước cập nhật. Ứng dụng đang dùng `EnsureCreated`, chưa có migration tự động cho thay đổi lược đồ CSDL.

## Sao lưu và khôi phục

Chạy trong thư mục repo. Dừng ứng dụng trước khi sao chép để SQLite nhất quán; sao lưu toàn bộ thư mục kể cả file WAL nếu có.

```sh
mkdir backup
docker compose stop web
docker compose cp web:/app/database backup/database
docker compose cp web:/app/keys backup/keys
docker compose start web
```

Dùng thư mục sao lưu mới cho mỗi lần và lưu bản sao ở nơi khác máy chủ. Nếu sao chép thất bại, chạy `docker compose start web` để mở lại hệ thống.

Khôi phục vào volume mới/trống (không trộn với CSDL đang tồn tại):

```sh
docker compose create web
docker compose cp backup/database/. web:/app/database
docker compose cp backup/keys/. web:/app/keys
docker compose run --rm --no-deps --user root --entrypoint sh web -c 'chown -R app:app /app/database /app/keys'
docker compose up -d
```

Nếu khôi phục vào hệ thống đang chạy, dừng và sao lưu hệ thống đó trước; đảm bảo volume đích trống bằng quy trình quản trị dữ liệu của bạn. Không xóa volume đang chứa bản dữ liệu duy nhất.

## Kiểm tra sau triển khai

Kiểm tra trang đăng nhập, đăng nhập quản trị, tạo thử một đầu sách rồi chạy `docker compose restart web` và xác nhận sách vẫn còn. Kiểm tra lại sau `docker compose up -d --build` để xác nhận volume hoạt động.

Nếu container thoát: xem `docker compose logs --tail=100 web`; kiểm tra mật khẩu khởi tạo đủ dài, quyền ghi volume và cổng máy chủ chưa bị chiếm.
