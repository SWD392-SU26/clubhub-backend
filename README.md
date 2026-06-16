# ClubHub API

Hệ thống quản lý câu lạc bộ sinh viên — ASP.NET Core 8 Web API, SQL Server, Code First.

## 🚀 Cách chạy

### 1. Cấu hình connection string

Mở `src/ClubHub.API/appsettings.json` và sửa:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=ClubHubDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 2. Chạy Migration (tạo database)

```bash
cd src/ClubHub.API
dotnet ef database update
```

### 3. Chạy API

```bash
dotnet run
```

Swagger UI sẽ mở tại: `http://localhost:5000` (hoặc port được cấu hình)

---

## 📁 Cấu trúc project

```
src/ClubHub.API/
├── Controllers/          # API Controllers
│   ├── AuthController.cs
│   ├── ClubController.cs
│   ├── MembershipController.cs
│   ├── EventController.cs
│   ├── FeedbackController.cs
│   ├── PointController.cs
│   ├── ProposalController.cs
│   └── UniversityAdminController.cs
├── Data/
│   ├── AppDbContext.cs   # EF Core DbContext
│   └── Migrations/       # Code-first migrations
├── DTOs/                 # Data Transfer Objects
│   ├── Auth/
│   ├── Club/
│   ├── Common/
│   ├── Event/
│   ├── Feedback/
│   ├── Membership/
│   ├── Point/
│   └── Proposal/
├── Entities/             # Domain entities
│   ├── User.cs
│   ├── Club.cs
│   ├── ClubMember.cs
│   ├── ClubProposal.cs
│   ├── Event.cs
│   ├── EventRegistration.cs
│   ├── Feedback.cs
│   ├── Notification.cs
│   └── PointTransaction.cs
├── Enums/                # Domain enumerations
├── Middlewares/
│   └── ExceptionMiddleware.cs
└── Services/
    ├── Interfaces/       # Service interfaces + implementations
    └── Implementations/
```

---

## 📋 API Endpoints

### 🔐 Auth (`/api/auth`)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/register` | Đăng ký |
| POST | `/login` | Đăng nhập |
| POST | `/refresh-token` | Làm mới token |
| POST | `/logout` | Đăng xuất |
| PUT | `/change-password` | Đổi mật khẩu |
| POST | `/forgot-password` | Quên mật khẩu |
| POST | `/reset-password` | Reset mật khẩu |
| GET | `/me` | Xem profile |
| PUT | `/me` | Cập nhật profile |

### 🏛️ Club (`/api/clubs`)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/` | Danh sách CLB (filter + phân trang) |
| GET | `/{clubId}` | Chi tiết CLB |
| GET | `/my-clubs` | CLB của tôi |
| PUT | `/{clubId}` | Cập nhật CLB |

### 👥 Membership (`/api/clubs/{clubId}/members`)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/join` | Gửi đơn tham gia |
| DELETE | `/leave` | Rời CLB |
| GET | `/` | Danh sách thành viên |
| GET | `/pending` | Đơn chờ duyệt |
| PUT | `/requests/{id}/review` | Duyệt/từ chối đơn |
| PUT | `/assign-role` | Gán vai trò |
| DELETE | `/{memberId}` | Xóa thành viên |
| PUT | `/transfer-admin` | Chuyển quyền chủ nhiệm |

### 📅 Events
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/clubs/{clubId}/events` | Sự kiện của CLB |
| GET | `/api/events/{eventId}` | Chi tiết sự kiện |
| POST | `/api/clubs/{clubId}/events` | Tạo sự kiện |
| PUT | `/api/events/{eventId}` | Cập nhật sự kiện |
| DELETE | `/api/events/{eventId}` | Hủy sự kiện |
| POST | `/api/events/{eventId}/register` | Đăng ký sự kiện |
| DELETE | `/api/events/{eventId}/register` | Hủy đăng ký |
| POST | `/api/events/{eventId}/checkin/{userId}` | Check-in |
| GET | `/api/my-events` | Sự kiện của tôi |

### ⭐ Feedback (`/api/events/{eventId}/feedback`)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/` | Gửi feedback (+2 điểm) |
| GET | `/` | Xem feedback sự kiện |

### 🏆 Points (`/api/clubs/{clubId}/points`)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/me` | Điểm của tôi trong CLB |
| GET | `/leaderboard` | Bảng xếp hạng |

### 📄 Proposals (`/api/proposals`)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/` | Nộp hồ sơ thành lập CLB |
| GET | `/my` | Hồ sơ của tôi |
| GET | `/{id}` | Chi tiết hồ sơ |
| GET | `/` | Tất cả hồ sơ (Admin) |
| PUT | `/{id}/review` | Duyệt/từ chối (Admin) |
| PUT | `/{id}/request-revision` | Yêu cầu bổ sung (Admin) |

### 🎓 University Admin (`/api/admin`)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/clubs` | Tất cả CLB |
| POST | `/clubs` | Tạo CLB trực tiếp |
| PUT | `/clubs/{clubId}/hide` | Ẩn CLB |
| PUT | `/clubs/{clubId}/lock` | Khóa CLB |
| DELETE | `/clubs/{clubId}` | Xóa mềm CLB |
| DELETE | `/clubs/{clubId}/hard` | Xóa cứng CLB |

---

## 🎯 Hệ thống điểm thi đua

| Hành động | Điểm |
|-----------|------|
| Check-in sự kiện | +10 |
| Tham gia hoạt động nội bộ | +5 |
| Hỗ trợ tổ chức sự kiện | +15 |
| Gửi feedback sau sự kiện | +2 |
| Vắng không lý do | -5 |

---

## 🔑 Phân quyền

| Role | Mô tả |
|------|-------|
| `Student` | Sinh viên - xem CLB, gửi đơn |
| `UniversityAdmin` | Admin trường - quản lý toàn bộ |
| `ClubRole.Member` | Thành viên CLB |
| `ClubRole.VicePresident` | Phó chủ nhiệm |
| `ClubRole.President` | Chủ nhiệm |
| `ClubRole.ClubAdmin` | Admin CLB |
