# HƯỚNG DẪN KIẾN TRÚC BACKEND & QUY CHUẨN PHÁT TRIỂN DÀNH CHO AI AGENT & DEVELOPERS

- **Dự án**: Nền tảng luyện thi `ieltsHSK` (ASP.NET Core 9 Minimal API + Clean Architecture)
- **Mục đích**: Thiết lập tiêu chuẩn bất biến, loại bỏ triệt để việc viết code dồn vào `Program.cs`, đảm bảo hệ thống mở rộng theo nguyên lý SOLID và Clean Architecture.

---

## 1. NGUYÊN TẮC BẤT BIẾN: KHÔNG VIẾT ENDPOINT INLINE VÀO `Program.cs`

`Program.cs` là điểm khởi đầu (Entry Point) của ứng dụng, **CHỈ ĐƯỢC PHÉP** thực hiện 4 nhiệm vụ:
1. Đăng ký dịch vụ vào `builder.Services` (DI Container, DbContext, Auth, CORS, Caching, Swagger).
2. Thiết lập Middleware Pipeline (`app.UseCors()`, `app.UseAuthentication()`, `app.UseAuthorization()`, v.v.).
3. Khởi động tác vụ pre-warm nền (Pre-warm DB connection, seed data).
4. Gọi các phương thức mở rộng đăng ký Endpoint Modules:
   ```csharp
   app.MapAuthEndpoints();
   app.MapAiEndpoints();
   app.MapIeltsEndpoints();
   app.MapHskEndpoints();
   app.MapToeicEndpoints();
   app.MapExamSubmissionEndpoints();
   app.MapStoryEndpoints();
   app.MapGrammarEndpoints();
   app.MapAdminEndpoints();
   ```

> ⚠️ **CẢNH BÁO CHO AGENT**: Mọi PR hoặc commit tự ý viết thêm `app.MapGet(...)`, `app.MapPost(...)` trực tiếp vào `Program.cs` đều bị coi là vi phạm tiêu chuẩn kiến trúc dự án.

---

## 2. QUY CHUẨN CẤU TRÚC THƯ MỤC BACKEND

```
backend/src/
├── Backend.Domain/             # Thực thể CSDL (Entities), Enums, Value Objects (KHÔNG phụ thuộc vào bất kỳ layer nào)
│   └── Entities/               # User, MockTest, Story, HskVocabulary, etc.
├── Backend.Application/        # DTOs, Giao diện Abstractions, Business Logic, Dịch vụ dùng chung
│   ├── Abstractions/           # IAiGradingService, IAuthService, IR2StorageService, ICacheService
│   └── DTOs/                   # Mọi Request / Response DTOs PHẢI đặt ở đây
├── Backend.Infrastructure/     # Triển khai tầng ngoại vi (Database, External APIs, Cloudflare R2, Redis)
│   ├── Persistence/            # AppDbContext, Migrations
│   └── Services/               # AiGradingService, AuthService, R2StorageService, RedisCacheService
└── Backend.Api/                # Tầng trình diễn (API Layer)
    ├── Endpoints/              # MỌI API ENDPOINT PHẢI NẰM TẠI ĐÂY
    │   ├── AuthEndpoints.cs
    │   ├── AiEndpoints.cs
    │   ├── IeltsEndpoints.cs
    │   ├── HskEndpoints.cs
    │   ├── ToeicEndpoints.cs
    │   ├── ExamSubmissionEndpoints.cs
    │   ├── StoryEndpoints.cs
    │   ├── GrammarEndpoints.cs
    │   └── AdminEndpoints.cs
    ├── Common/                 # Helper, Utilities, Parser (VD: HskVocabCsvParser)
    └── Program.cs              # Entry point sạch sẽ (< 200 dòng)
```

---

## 3. MẪU THIẾT KẾ CHUẨN KHI TẠO ENDPOINT MỚI (ENDPOINT MODULE PATTERN)

Khi bổ sung tính năng hoặc API mới, Agent phải thực hiện theo mẫu sau:

### Bước 1: Khai báo DTOs trong `Backend.Application/DTOs/`
Tuyệt đối không khai báo `public record XxxRequest(...)` ở cuối `Program.cs`. Mọi DTO phải nằm trong thư mục `Backend.Application/DTOs/` tương ứng.

### Bước 2: Tạo Endpoint Extension Method trong `Backend.Api/Endpoints/`
Tạo một lớp tĩnh `public static class <Feature>Endpoints` và cung cấp phương thức mở rộng:

```csharp
using Backend.Application.DTOs;
using Backend.Application.Abstractions;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Endpoints;

public static class SampleFeatureEndpoints
{
    public static IEndpointRouteBuilder MapSampleFeatureEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sample-feature");
        // Nếu toàn bộ nhóm cần đăng nhập: group.RequireAuthorization();

        // 1. GET danh sách
        group.MapGet("/", async (AppDbContext dbContext, CancellationToken ct) =>
        {
            var data = await dbContext.SampleEntities.AsNoTracking().ToListAsync(ct);
            return Results.Ok(data);
        });

        // 2. POST tạo mới có phân quyền
        group.MapPost("/", [Microsoft.AspNetCore.Authorization.Authorize] async (
            CreateSampleRequest req,
            System.Security.Claims.ClaimsPrincipal user,
            AppDbContext dbContext,
            CancellationToken ct) =>
        {
            // Business logic hoặc gọi Application Service
            return Results.Created($"/api/sample-feature/1", new { id = 1 });
        });

        return app;
    }
}
```

### Bước 3: Đăng ký phương thức vào `Program.cs`
Mở `Program.cs` và chỉ thêm đúng một dòng gọi đăng ký:
```csharp
app.MapSampleFeatureEndpoints();
```

---

## 4. QUY TẮC BẢO MẬT & PHÂN QUYỀN (SECURITY CHECKLIST)

Mỗi khi thêm một endpoint mới, Agent phải tự rà soát:
1. **Endpoint có dữ liệu người dùng riêng biệt**: Bắt buộc có `[Microsoft.AspNetCore.Authorization.Authorize]`. Trích xuất User ID từ `ClaimsPrincipal`, không nhận `userId` tùy tiện từ query param để tránh lỗ hổng IDOR.
2. **Endpoint dành riêng cho Quản trị viên (Admin)**: Bắt buộc có `[Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]`.
3. **Endpoint gọi dịch vụ tốn phí (AI Grading, SMS, Mail)**:
   - Bắt buộc phải đăng nhập.
   - Bắt buộc kiểm tra hạn mức sử dụng (Quota/Credits) của người dùng trước khi gọi dịch vụ bên ngoài.
   - Bắt buộc gắn Rate Limiter để chống tấn công DoS làm cạn kiệt ngân sách.
4. **Endpoint có tham số tìm kiếm/bộ lọc**: Luôn có phân trang (`.Take(...)`, `.Skip(...)`) hoặc giới hạn tối đa số bản ghi (VD: `.Take(100)`), tuyệt đối không trả về toàn bộ hàng triệu bản ghi CSDL gây tràn RAM.

---

## 5. QUY TẮC CACHE & INVALIDATION
- Khi dùng `ICacheService` (Redis/Memory), luôn đặt tiền tố chuẩn theo domain:
  - IELTS: `ielts:...`
  - HSK: `hsk:...`
  - TOEIC: `toeic:...`
- Mọi thao tác ghi dữ liệu (POST, PUT, DELETE) phải thực hiện **xóa cache tương ứng (Cache Invalidation)** ngay sau khi `SaveChangesAsync()`.
