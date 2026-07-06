# ClubHub API

Hệ thống quản lý câu lạc bộ sinh viên bằng ASP.NET Core 8 Web API, SQL Server và EF Core Code First.

## Cách chạy

1. Mở `src/ClubHub.API/.env` hoặc `src/ClubHub.API/appsettings*.json` và cấu hình `ConnectionStrings__DefaultConnection` để trỏ tới SQL Server của bạn.
2. Từ thư mục gốc của solution, chạy migration:

```bash
dotnet ef database update --project ClubHub.Infrastructure --startup-project src/ClubHub.API
```

3. Chạy API:

```bash
dotnet run --project src/ClubHub.API
```

Khi chạy ở môi trường Development, Swagger được mở ở route gốc của ứng dụng. Ngoài Development, Swagger không được bật.

## Cấu trúc project

```
ClubHub.Domain/
├── Entities/
├── Enums/
├── Exceptions/
└── Interfaces/

ClubHub.Infrastructure/
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
├── Repositories/
└── Services/

ClubHub.Services/
├── DTOs/
├── Mappings/
├── Services/
└── Validators/

src/ClubHub.API/
├── Controllers/
├── Middlewares/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── .env
```

## API Endpoints

### Auth

Base route: `/`

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/register` | Đăng ký tài khoản |
| POST | `/login` | Đăng nhập |
| POST | `/refresh-token` | Làm mới access token |
| PUT | `/change-password` | Đổi mật khẩu |
| POST | `/forgot-password` | Gửi yêu cầu quên mật khẩu |
| POST | `/reset-password` | Reset mật khẩu bằng token |
| GET | `/me` | Xem profile cá nhân |
| PUT | `/me` | Cập nhật profile cá nhân |

### Clubs

Base route: `/api/clubs`

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/` | Danh sách CLB, hỗ trợ filter và phân trang |
| GET | `/{clubId}` | Chi tiết CLB |
| GET | `/my-clubs` | Danh sách CLB của tôi |
| PUT | `/{clubId}` | Cập nhật CLB |

### Membership

Base route: `/api/clubs/{clubId}/members`

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/join` | Gửi đơn tham gia CLB |
| DELETE | `/leave` | Rời CLB |
| GET | `/` | Danh sách thành viên |
| GET | `/pending` | Danh sách đơn chờ duyệt |
| PUT | `/requests/{membershipId}/review` | Duyệt hoặc từ chối đơn |
| PUT | `/assign-role` | Gán vai trò cho thành viên |
| DELETE | `/{memberId}` | Xóa thành viên khỏi CLB |
| PUT | `/transfer-admin` | Chuyển quyền chủ nhiệm |

Base route phụ: `/api/my-memberships`

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/` | Lịch sử membership của chính mình |

### Events

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/clubs/{clubId}/events` | Danh sách sự kiện của CLB |
| GET | `/api/events/{eventId}` | Chi tiết sự kiện |
| POST | `/api/clubs/{clubId}/events` | Tạo sự kiện |
| PUT | `/api/events/{eventId}` | Cập nhật sự kiện |
| DELETE | `/api/events/{eventId}` | Xóa sự kiện |
| POST | `/api/events/{eventId}/register` | Đăng ký sự kiện |
| DELETE | `/api/events/{eventId}/register` | Hủy đăng ký sự kiện |
| POST | `/api/events/{eventId}/checkin/{userId}` | Check-in thành viên |
| GET | `/api/events/{eventId}/registrations` | Danh sách người đăng ký |
| GET | `/api/my-events` | Danh sách sự kiện của tôi |

### Feedback

Base route: `/api/events/{eventId}/feedback`

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/` | Gửi feedback sau sự kiện |
| GET | `/` | Xem tổng hợp feedback |

### Points

Base route: `/api/clubs/{clubId}/points`

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/me` | Xem điểm thi đua của mình trong CLB |
| GET | `/leaderboard` | Bảng xếp hạng điểm thi đua |

### Proposals

Base route: `/api/proposals`

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/` | Nộp hồ sơ thành lập CLB |
| GET | `/my` | Xem hồ sơ của tôi |
| GET | `/{id}` | Xem chi tiết hồ sơ |
| GET | `/` | Xem tất cả hồ sơ, chỉ dành cho UniversityAdmin |
| PUT | `/{id}/review` | Duyệt hoặc từ chối hồ sơ |
| PUT | `/{id}/request-revision` | Yêu cầu bổ sung hồ sơ |

### University Admin

Base route: `/api/admin`

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/clubs` | Xem toàn bộ CLB |
| POST | `/clubs` | Tạo CLB trực tiếp |
| PUT | `/clubs/{clubId}/hide` | Ẩn CLB |
| PUT | `/clubs/{clubId}/lock` | Khóa CLB |
| DELETE | `/clubs/{clubId}` | Xóa mềm CLB |
| DELETE | `/clubs/{clubId}/hard` | Xóa cứng CLB |

## Phân quyền

| Role | Mô tả |
|------|-------|
| `Student` | Sinh viên |
| `UniversityAdmin` | Quản trị viên trường |
| `ClubRole.Member` | Thành viên CLB |
| `ClubRole.VicePresident` | Phó chủ nhiệm |
| `ClubRole.President` | Chủ nhiệm |
| `ClubRole.ClubAdmin` | Quản trị CLB |

## Ghi chú phát triển

- `Program.cs` tự động migrate database khi chạy ở Development.
- Tài khoản admin seed sẵn trong Development là `admin@gmail.com` với mật khẩu `12345`.
- API dùng CORS mở toàn bộ trong cấu hình hiện tại.
