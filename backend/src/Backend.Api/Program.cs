using Backend.Api;
using Backend.Api.Endpoints;
using Backend.Application.Abstractions;
using Backend.Application.DTOs;
using Backend.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Giới hạn kích thước body upload tối đa 50MB (audio, ảnh, excel) để chống tấn công DoS
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024;
});

// Cấu hình CORS an toàn: Không dùng wildcard tự do khi bật AllowCredentials()
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        var allowedOriginsConfig = builder.Configuration["Cors:AllowedOrigins"] 
            ?? builder.Configuration["Frontend:BaseUrl"];
        var configuredOrigins = (allowedOriginsConfig ?? "")
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;

            // Trong môi trường Development: cho phép toàn bộ localhost và 127.0.0.1 trên mọi cổng
            if (builder.Environment.IsDevelopment())
            {
                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                        uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // Kiểm tra danh sách origin được cấu hình (ví dụ domain production)
            return configuredOrigins.Any(allowed => string.Equals(allowed, origin, StringComparison.OrdinalIgnoreCase));
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// Rate Limiting cho các endpoint xác thực (tối đa 15 request/phút) chống brute-force và DDoS
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 15;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var jwtKey = jwtSettings["Key"] ?? Environment.GetEnvironmentVariable("JWT_SECRET");
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("Cấu hình Jwt:Key không được để trống.");
        }
        if (!builder.Environment.IsDevelopment() && jwtKey.Contains("SuperSecretKeyThatIsAtLeast32BytesLongForHSK"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[SECURITY ALERT] Jwt:Key đang sử dụng khóa mẫu mặc định trong môi trường Non-Development! Hãy thiết lập biến môi trường Jwt__Key.");
            Console.ResetColor();
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        // Chặn token của tài khoản bị khoá/xoá ngay cả khi JWT còn hạn
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                    ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    context.Fail("Invalid token subject.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices
                    .GetRequiredService<Backend.Infrastructure.Persistence.AppDbContext>();
                var role = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                    ?? context.Principal?.FindFirst("role")?.Value;
                var isAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)
                    || context.Principal?.FindFirst("admin_id") != null;

                var cache = context.HttpContext.RequestServices.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                var cacheKey = isAdmin ? $"auth_admin_active_{userId}" : $"auth_user_active_{userId}";

                if (cache != null && cache.TryGetValue(cacheKey, out bool isActive))
                {
                    if (!isActive)
                    {
                        context.Fail(isAdmin ? "Admin account is disabled or no longer exists." : "Account is disabled or no longer exists.");
                    }
                    return;
                }

                if (isAdmin)
                {
                    var admin = await dbContext.Admins.FindAsync(new object[] { userId }, context.HttpContext.RequestAborted);
                    if (admin == null || !admin.IsActive)
                    {
                        cache?.Set(cacheKey, false, TimeSpan.FromSeconds(60));
                        context.Fail("Admin account is disabled or no longer exists.");
                        return;
                    }
                    cache?.Set(cacheKey, true, TimeSpan.FromSeconds(60));
                }
                else
                {
                    var user = await dbContext.Users.FindAsync(new object[] { userId }, context.HttpContext.RequestAborted);
                    if (user == null || !user.IsActive)
                    {
                        cache?.Set(cacheKey, false, TimeSpan.FromSeconds(60));
                        context.Fail("Account is disabled or no longer exists.");
                        return;
                    }
                    cache?.Set(cacheKey, true, TimeSpan.FromSeconds(60));
                }
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Khởi chạy kết nối DB, nạp trước model EF Core, kết nối Redis và kiểm tra seed ngầm
// để Kestrel mở cổng 5101 NGAY LẬP TỨC (< 100ms) mà không chặn các request đầu tiên từ client.
_ = Task.Run(async () =>
{
    try
    {
        // 1. Kích hoạt mở sẵn kết nối (pre-warm) tới PostgreSQL & nạp model EF Core
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Backend.Infrastructure.Persistence.AppDbContext>();
        await dbContext.Database.CanConnectAsync();

        // 2. Kích hoạt kết nối trước tới Redis nếu có bật
        _ = app.Services.GetService<StackExchange.Redis.IConnectionMultiplexer>();

        // 3. Đồng bộ migration và kiểm tra dữ liệu khởi tạo
        await app.Services.SeedDataAsync();
        Console.WriteLine("[Startup] Database connection warmed up & seed data verified.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup Warning] Background initialization note: {ex.Message}");
    }
});

// Swagger: Chỉ bật trong môi trường Development hoặc khi có cấu hình EnableSwagger = true
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwagger", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Security Headers Middleware: Bảo vệ chống MIME sniffing, clickjacking, và rò rỉ referrer
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    // Cho phép microphone phục vụ chức năng luyện nói IELTS Speaking, chặn các quyền không cần thiết
    context.Response.Headers.Append("Permissions-Policy", "microphone=(self), camera=(), geolocation=()");
    await next();
});

app.UseCors(frontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Root health check endpoint for Render & container monitoring
app.MapGet("/", () => Results.Ok(new { status = "healthy", app = "IELTS & HSK Backend API", timestamp = DateTime.UtcNow }));

// ─── MODULAR ENDPOINT REGISTRATIONS ───
// TUYỆT ĐỐI KHÔNG VIẾT ENDPOINT TRỰC TIẾP VÀO FILE NÀY!
// Mọi endpoint mới phải được định nghĩa trong Backend.Api.Endpoints.<Domain>Endpoints.cs
app.MapAuthEndpoints();
app.MapAiEndpoints();
app.MapIeltsEndpoints();
app.MapHskEndpoints();
app.MapToeicEndpoints();
app.MapExamSubmissionEndpoints();
app.MapStoryEndpoints();
app.MapGrammarEndpoints();
app.MapAdminEndpoints();

app.Run();
