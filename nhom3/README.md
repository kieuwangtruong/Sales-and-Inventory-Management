# Nhóm 3 — User/Auth Service & Report Service

## Tổng quan

Nhóm 3 phụ trách **2 microservices**:

| Service | Port | Trách nhiệm |
|---------|------|------------|
| **UserAuth Service** | 5001 | Cấp JWT token, quản lý tài khoản, phân quyền |
| **Report Service** | 5003 | Báo cáo doanh thu, thống kê tổng hợp (Admin only) |

---

## Kiến trúc tuân thủ quy tắc

### Database per service
- `UserDB` → chỉ UserAuth Service dùng
- `ReportDB` → chỉ Report Service dùng
- **Không share database, không foreign key chéo service**

### JWT — 1 service duy nhất cấp token
- **UserAuth Service** là nơi DUY NHẤT cấp `access_token` và `refresh_token`
- **Report Service** và các service khác chỉ **validate** token bằng shared secret (cùng `Jwt__Key`)
- Không service nào khác được gọi `/api/auth/login`

### Giao tiếp đồng bộ (REST)
- Report Service gọi Order Service / Product Service **qua API Gateway**
- Forward Bearer token trong header để Gateway xác thực

---

## Cấu trúc thư mục

```
nhom3/
├── UserAuthService/
│   ├── Controllers/
│   │   ├── AuthController.cs      # login, refresh, logout, /me
│   │   └── UserController.cs      # CRUD user (Admin only)
│   ├── Data/
│   │   └── UserDbContext.cs       # EF Core + Seeder
│   ├── DTOs/
│   │   └── AuthDTOs.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Models/
│   │   └── UserModels.cs          # User, Role, RefreshToken
│   ├── Services/
│   │   ├── AuthService.cs         # login, refresh, logout
│   │   ├── JwtTokenService.cs     # tạo/validate token
│   │   └── UserService.cs         # CRUD tài khoản
│   ├── appsettings.json
│   ├── Dockerfile
│   └── Program.cs
│
├── ReportService/
│   ├── Controllers/
│   │   └── ReportController.cs    # /revenue, /dashboard
│   ├── Data/
│   │   └── ReportDbContext.cs
│   ├── Models/
│   │   └── ReportModels.cs
│   ├── Services/
│   │   └── ReportAggregatorService.cs
│   ├── appsettings.json
│   ├── Dockerfile
│   └── Program.cs
│
└── docker/
    └── docker-compose.yml
```

---

## API Endpoints

### UserAuth Service (port 5001)

#### Auth
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| POST | `/api/auth/login` | ❌ | Đăng nhập → access token + refresh token |
| POST | `/api/auth/refresh` | Bearer (expired OK) | Gia hạn token |
| POST | `/api/auth/logout` | Bearer | Đăng xuất |
| GET | `/api/auth/me` | Bearer | Thông tin user hiện tại |

#### Users (Admin only)
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/users` | Admin | Danh sách user |
| GET | `/api/users/{id}` | Admin / chính user | Chi tiết user |
| POST | `/api/users` | Admin | Tạo user mới |
| PUT | `/api/users/{id}` | Admin | Cập nhật user |
| POST | `/api/users/change-password` | Bearer | Đổi mật khẩu |
| DELETE | `/api/users/{id}` | Admin | Vô hiệu hoá |

### Report Service (port 5003)

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/reports/revenue?from=&to=` | Admin | Báo cáo doanh thu |
| GET | `/api/reports/dashboard` | Admin | Thống kê dashboard |

---

## Chạy dự án

### 1. Toàn bộ hệ thống (từ thư mục nhom3/)

```bash
docker-compose -f docker/docker-compose.yml up --build
```

### 2. Chỉ UserAuth Service (dev local)

```bash
cd UserAuthService
dotnet restore
dotnet ef database update    # tạo DB
dotnet run
# → http://localhost:5001/swagger
```

### 3. Migration

```bash
cd UserAuthService
dotnet ef migrations add InitialCreate
dotnet ef database update

cd ../ReportService
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Tài khoản mặc định

| Username | Password | Role |
|----------|----------|------|
| `admin` | `Admin@123` | Admin |

Tạo thêm tài khoản Sales/Warehouse qua POST `/api/users` (đăng nhập Admin trước).

---

## JWT Flow

```
[Client]     POST /api/auth/login
               ↓
[UserAuth]   Verify password → tạo access_token (60 phút) + refresh_token (7 ngày)
               ↓
[Client]     Gọi API khác với: Authorization: Bearer <access_token>
               ↓
[Gateway]    Validate JWT bằng shared secret → forward request
               ↓
[Client]     Khi access_token hết hạn → POST /api/auth/refresh
```

---

## Shared Secret

Tất cả service trong hệ thống dùng cùng giá trị `Jwt__Key` và `Jwt__Issuer`.
**Thay đổi giá trị này trước khi deploy production!**

```
Jwt__Key    = "SuperSecretKey_ThayDoiKhiDeploy_MinLength32Chars!"
Jwt__Issuer = "RetailSystem"
```

Trong Docker, truyền qua biến môi trường — không hardcode trong code.

---

## Swagger UI

- UserAuth Service: http://localhost:5001/swagger
- Report Service: http://localhost:5003/swagger

Nhấn **Authorize** → nhập `Bearer <token>` để test các endpoint cần auth.
