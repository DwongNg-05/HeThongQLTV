# Cấu hình

Nguồn cấu hình theo thứ tự ghi đè của ASP.NET Core: appsettings.json, appsettings theo môi trường, biến môi trường, tham số dòng lệnh. Dùng hai dấu gạch dưới để biểu diễn cấp con trong biến môi trường.

| Khóa | Mặc định | Ý nghĩa |
| --- | --- | --- |
| ConnectionStrings:Library | Data Source=database/library.db | CSDL SQLite |
| LibraryRules:MaxLoans | 5 | Giới hạn đang mượn |
| LibraryRules:LoanDays | 14 | Hạn mượn ngày |
| LibraryRules:RenewalDays | 7 | Số ngày thêm khi gia hạn |
| LibraryRules:MaxRenewals | 2 | Giới hạn gia hạn |
| LibraryRules:FinePerDay | 2000 | Tiền phạt mỗi ngày |
| SeedDemo | false | Ngoài Development chỉ seed khi true |

Không cần API key cho phạm vi KT2. Không commit .env hay file database. Profile http phục vụ demo localhost; profile https dùng development certificate của .NET.
