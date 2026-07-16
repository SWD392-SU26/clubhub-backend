# ClubHub Backend

ClubHub Backend is an ASP.NET Core Web API for managing university student clubs, club memberships, club creation proposals, events, feedback, announcements, and club point rankings.

## Tech Stack

- ASP.NET Core 8 Web API
- Entity Framework Core 8
- SQL Server with code-first migrations
- JWT authentication with refresh tokens
- BCrypt password hashing
- Swagger/OpenAPI via Swashbuckle
- xUnit + EF Core InMemory for service tests

The repository already used a .NET/SQL Server backend stack, so this implementation continues that stack instead of replacing it with Spring Boot/PostgreSQL.

## Run Locally

1. Configure the database connection in `src/ClubHub.API/appsettings.json`, or set environment variables:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost;Database=ClubHubDb;Trusted_Connection=True;TrustServerCertificate=True;"
$env:Jwt__Key="replace-with-a-long-secure-secret"
$env:Jwt__Issuer="ClubHub.API"
$env:Jwt__Audience="ClubHub.Client"
$env:Jwt__ExpiryMinutes="60"
```

2. Apply migrations:

```powershell
dotnet ef database update --project src/ClubHub.API/ClubHub.API.csproj
```

3. Run the API:

```powershell
dotnet run --project src/ClubHub.API/ClubHub.API.csproj
```

Swagger is available at the application root in Development, for example `https://localhost:5001/` or the URL printed by `dotnet run`.

## Seed Accounts

Development startup applies migrations and seeds data when the database has no users.

All seeded accounts use password `ClubHub@123`.

| Role | Email | Username |
| --- | --- | --- |
| University Admin | `admin@clubhub.local` | `university.admin` |
| Student | `minhanh@student.clubhub.local` | `minhanh` |
| Student | `baolong@student.clubhub.local` | `baolong` |
| Student | `giahan@student.clubhub.local` | `giahan` |

Seed data also includes clubs, approved and pending club memberships, events, feedback, point transactions, announcements, audit history, and club proposals.
It also includes sample notifications and one draft event for testing private event detail authorization.

## Main Endpoints

Authentication:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `PUT /api/auth/change-password`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `GET /api/auth/me`
- `PUT /api/auth/me`

Clubs and membership:

- `GET /api/clubs`
- `GET /api/clubs/{clubId}`
- `GET /api/clubs/my-clubs`
- `GET /api/clubs/managed`
- `GET /api/clubs/{clubId}/statistics`
- `PUT /api/clubs/{clubId}`
- `POST /api/clubs/{clubId}/members/join`
- `POST /api/clubs/{clubId}/join-requests`
- `DELETE /api/clubs/{clubId}/members/leave`
- `GET /api/clubs/{clubId}/members`
- `GET /api/clubs/{clubId}/members/pending`
- `GET /api/clubs/{clubId}/join-requests`
- `PUT /api/clubs/{clubId}/members/requests/{membershipId}/review`
- `PUT /api/clubs/{clubId}/join-requests/{membershipId}/review`
- `PUT /api/clubs/{clubId}/members/assign-role`
- `PUT /api/clubs/{clubId}/members/transfer-admin`

Proposals:

- `POST /api/club-proposals`
- `GET /api/club-proposals/my`
- `GET /api/club-proposals/{id}`
- `PUT /api/club-proposals/{id}`
- `PUT /api/club-proposals/{id}/resubmit`
- `GET /api/admin/club-proposals`
- `PUT /api/admin/club-proposals/{id}/review`
- `PUT /api/admin/club-proposals/{id}/request-info`

Events, feedback, and points:

- `GET /api/events`
- `GET /api/events/upcoming`
- `GET /api/clubs/{clubId}/events`
- `POST /api/clubs/{clubId}/events`
- `GET /api/events/{eventId}`
- `PUT /api/events/{eventId}`
- `DELETE /api/events/{eventId}`
- `POST /api/events/{eventId}/register`
- `POST /api/events/{eventId}/checkin/{userId}`
- `POST /api/events/{eventId}/feedback`
- `GET /api/clubs/{clubId}/points/me`
- `GET /api/clubs/{clubId}/points/leaderboard`
- `GET /api/clubs/{clubId}/points/ranking`
- `POST /api/clubs/{clubId}/points/transactions`

Notifications, uploads, announcements, audit, and university admin:

- `GET /api/notifications`
- `GET /api/notifications/unread-count`
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/read-all`
- `POST /api/uploads`
- `POST /api/uploads/avatar`
- `POST /api/uploads/logo`
- `POST /api/uploads/cover`
- `POST /api/uploads/proposal-file`
- `POST /api/uploads/founder-id`
- `GET /api/clubs/{clubId}/announcements`
- `POST /api/clubs/{clubId}/announcements`
- `GET /api/clubs/{clubId}/activity-history`
- `GET /api/clubs/{clubId}/members/{memberUserId}/audit-log`
- `GET /api/admin/clubs`
- `POST /api/admin/clubs`
- `PUT /api/admin/clubs/{clubId}/hide`
- `PUT /api/admin/clubs/{clubId}/lock`
- `DELETE /api/admin/clubs/{clubId}`
- `PUT /api/admin/clubs/{clubId}/transfer-manager`
- `GET /api/admin/statistics`
- `GET /api/admin/clubs/{clubId}/statistics`

## Authorization Notes

- Public APIs include register, login, forgot/reset password, public club listing, and club detail.
- Public event APIs expose only non-draft, non-cancelled events. Draft event details require club membership/management or university admin.
- Student APIs require a valid JWT.
- University admin APIs require the `UniversityAdmin` global role.
- Club management APIs require `ClubAdmin`, `President`, or `VicePresident` membership in that club. University admins are also allowed for oversight operations.
- Join request approval rejects self-review.

## Tests

Run all tests:

```powershell
dotnet test ClubHub.slnx
```

Current focused coverage verifies:

- register/login
- club listing with search/category filter
- join request creation and approval
- club proposal approval creating a club and president membership
- point transaction ranking
- notifications unread/read flow
- public/upcoming event listing
- draft event detail authorization
- proposal edit/resubmit
- club statistics permission

## Notes

- Forgot password currently stores a reset token but does not send email; email delivery should be integrated with the university mail provider.
- File uploads are stored locally under `wwwroot/uploads/{purpose}` and return a URL that can be saved into existing URL fields.
- Upload image endpoints accept `jpg`, `jpeg`, `png`, and `webp` up to 5MB. Proposal files also accept `pdf`, `doc`, and `docx` up to 10MB.

## Manual Test Examples

Login first and authorize Swagger with `Bearer <accessToken>`.

Notification flow:

```text
GET /api/notifications/unread-count
GET /api/notifications
PUT /api/notifications/{id}/read
PUT /api/notifications/read-all
```

Public events:

```text
GET /api/events?page=1&pageSize=10
GET /api/events/upcoming?category=Technology
GET /api/events/30000000-0000-0000-0000-000000000001
```

Draft event detail authorization:

```text
GET /api/events/30000000-0000-0000-0000-000000000003
```

Guests and non-members should get not found. `minhanh`, `baolong`, and `university.admin` can view it.

Upload with multipart form-data:

```text
POST /api/uploads/avatar
form field: file = your-image.png
```

Proposal revision flow:

```text
PUT /api/admin/club-proposals/{id}/request-info
PUT /api/club-proposals/{id}
PUT /api/club-proposals/{id}/resubmit
```
