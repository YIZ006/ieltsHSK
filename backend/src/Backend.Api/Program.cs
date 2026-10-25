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

// Helper trích xuất địa chỉ IP của Client an toàn (chống giả mạo header CF-Connecting-IP và X-Forwarded-For)
static string ResolveClientIp(HttpContext context) => ClientIpResolver.ResolveClientIp(context);

// Rate Limiting phân vùng độc lập theo Client IP và User ID chống brute-force và spam
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }
        else
        {
            context.HttpContext.Response.Headers.RetryAfter = "60";
        }
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Quá nhiều yêu cầu từ địa chỉ IP của bạn. Vui lòng chờ ít phút trước khi thử lại.",
            retryAfterSeconds = 60
        }, token);
    };

    // Phân vùng "auth": Mỗi Client IP độc lập nhận tối đa 5 request / phút (lần thứ 6 sẽ bị chặn ngay)
    options.AddPolicy("auth", context =>
    {
        var clientIp = ResolveClientIp(context);
        return RateLimitPartition.GetFixedWindowLimiter(
            $"auth_ip_{clientIp}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // Phân vùng "ai": Mỗi User ID (hoặc IP nếu ẩn danh) tối đa 10 request / phút
    options.AddPolicy("ai", context =>
    {
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var partitionKey = !string.IsNullOrWhiteSpace(userId) 
            ? $"ai_user_{userId}" 
            : $"ai_ip_{ResolveClientIp(context)}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // Phân vùng "submission": Mỗi User ID (hoặc IP nếu ẩn danh) tối đa 10 request / phút chống flood nộp bài thi
    options.AddPolicy("submission", context =>
    {
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        var partitionKey = !string.IsNullOrWhiteSpace(userId) 
            ? $"sub_user_{userId}" 
            : $"sub_ip_{ResolveClientIp(context)}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // Phân vùng "user-mutation": Mỗi User ID (hoặc IP nếu ẩn danh) tối đa 30 request / phút cho các thao tác thay đổi trạng thái người dùng
    options.AddPolicy("user-mutation", context =>
    {
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        var partitionKey = !string.IsNullOrWhiteSpace(userId) 
            ? $"user_mutation_{userId}" 
            : $"user_mutation_ip_{ResolveClientIp(context)}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
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
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["token"].FirstOrDefault()
                    ?? context.Request.Query["access_token"].FirstOrDefault();
                if (!string.IsNullOrEmpty(accessToken) && string.IsNullOrEmpty(context.Token))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
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

// Readiness tracking flag
var isAppReady = false;
var startupError = string.Empty;

// Khởi tạo Database và Seed Data đồng bộ trước khi Kestrel nhận lưu lượng
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<Backend.Infrastructure.Persistence.AppDbContext>();
    await dbContext.Database.CanConnectAsync();

    // Khởi tạo kết nối Redis ngầm
    _ = app.Services.GetService<StackExchange.Redis.IConnectionMultiplexer>();

    // Đồng bộ migration và kiểm tra dữ liệu khởi tạo
    await app.Services.SeedDataAsync();
    isAppReady = true;
    Console.WriteLine("[Startup] Database connection verified & seed data ready.");
}
catch (Exception ex)
{
    startupError = ex.Message;
    Console.WriteLine($"[Startup Warning] Database initialization deferred: {ex.Message}");
    // Ứng dụng vẫn chạy để phản hồi health check và báo 503 Service Unavailable khi chưa sẵn sàng
}

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

// ─── HEALTH CHECKS & READINESS GATE ───

// Liveness Probe: Xác nhận tiến trình Kestrel đang chạy
app.MapGet("/health/live", () => Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow }));

// Readiness Probe: Chứng minh Database & Redis đã kết nối và sẵn sàng nhận traffic
app.MapGet("/health/ready", async (Backend.Infrastructure.Persistence.AppDbContext db, IServiceProvider sp) =>
{
    if (!isAppReady)
    {
        return Results.Json(new { status = "unhealthy", reason = startupError }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    try
    {
        var dbOk = await db.Database.CanConnectAsync();
        if (!dbOk)
        {
            return Results.Json(new { status = "unhealthy", reason = "Database connection failed" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var redis = sp.GetService<StackExchange.Redis.IConnectionMultiplexer>();
        var redisStatus = redis != null ? (redis.IsConnected ? "connected" : "disconnected") : "disabled";

        return Results.Ok(new
        {
            status = "ready",
            database = "connected",
            redis = redisStatus,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "unhealthy", error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

// Root endpoint: Thông tin tổng quan hệ thống
app.MapGet("/", () => Results.Ok(new
{
    status = isAppReady ? "healthy" : "initializing",
    app = "IELTS & HSK Backend API",
    version = "1.0.0",
    timestamp = DateTime.UtcNow
}));

// ─── MODULAR ENDPOINT REGISTRATIONS ───
// TUYỆT ĐỐI KHÔNG VIẾT ENDPOINT TRỰC TIẾP VÀO FILE NÀY!
// Mọi endpoint mới phải được định nghĩa trong Backend.Api.Endpoints.<Domain>Endpoints.cs
app.MapAuthEndpoints();
app.MapAiEndpoints();
app.MapIeltsEndpoints();
app.MapHskEndpoints();
app.MapExamScheduleEndpoints();
app.MapExamSubmissionEndpoints();
app.MapStoryEndpoints();
app.MapGrammarEndpoints();
app.MapAdminEndpoints();

app.Run();

public static class ClientIpResolver
{
    private static readonly System.Net.IPNetwork[] CloudflareNetworks = new[]
    {
        System.Net.IPNetwork.Parse("173.245.48.0/20"),
        System.Net.IPNetwork.Parse("103.21.244.0/22"),
        System.Net.IPNetwork.Parse("103.22.200.0/22"),
        System.Net.IPNetwork.Parse("103.31.4.0/22"),
        System.Net.IPNetwork.Parse("141.101.64.0/18"),
        System.Net.IPNetwork.Parse("108.162.192.0/18"),
        System.Net.IPNetwork.Parse("190.93.240.0/20"),
        System.Net.IPNetwork.Parse("188.114.96.0/20"),
        System.Net.IPNetwork.Parse("197.234.240.0/22"),
        System.Net.IPNetwork.Parse("198.41.128.0/17"),
        System.Net.IPNetwork.Parse("162.158.0.0/15"),
        System.Net.IPNetwork.Parse("104.16.0.0/13"),
        System.Net.IPNetwork.Parse("104.24.0.0/14"),
        System.Net.IPNetwork.Parse("172.64.0.0/13"),
        System.Net.IPNetwork.Parse("131.0.72.0/22"),
        System.Net.IPNetwork.Parse("2400:cb00::/32"),
        System.Net.IPNetwork.Parse("2606:4700::/32"),
        System.Net.IPNetwork.Parse("2803:f800::/32"),
        System.Net.IPNetwork.Parse("2405:b500::/32"),
        System.Net.IPNetwork.Parse("2405:8100::/32"),
        System.Net.IPNetwork.Parse("2a06:98c0::/29"),
        System.Net.IPNetwork.Parse("2c0f:f248::/32")
    };

    public static bool IsCloudflareIp(System.Net.IPAddress? ip)
    {
        if (ip == null) return false;
        var checkIp = ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;
        for (int i = 0; i < CloudflareNetworks.Length; i++)
        {
            if (CloudflareNetworks[i].BaseAddress.AddressFamily == checkIp.AddressFamily &&
                CloudflareNetworks[i].Contains(checkIp))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsTrustedProxyOrLoopback(System.Net.IPAddress? ip)
    {
        if (ip == null) return false;
        if (System.Net.IPAddress.IsLoopback(ip)) return true;
        var ipStr = ip.ToString();
        if (ipStr == "127.0.0.1" || ipStr == "::1") return true;
        return IsCloudflareIp(ip);
    }

    public static string ResolveClientIp(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        var isTrustedProxy = IsTrustedProxyOrLoopback(remoteIp);

        // 1. Chỉ tin cậy CF-Connecting-IP nếu kết nối đến từ Cloudflare Edge hoặc loopback
        if (isTrustedProxy)
        {
            var cfIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(cfIp) && System.Net.IPAddress.TryParse(cfIp.Trim(), out var parsedCfIp))
            {
                return parsedCfIp.ToString();
            }
        }

        // 2. Chỉ tin cậy X-Forwarded-For nếu kết nối đến từ local proxy/loopback
        if (remoteIp == null || System.Net.IPAddress.IsLoopback(remoteIp))
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                var clientCandidate = forwardedFor.Split(',').LastOrDefault()?.Trim();
                if (!string.IsNullOrWhiteSpace(clientCandidate) && System.Net.IPAddress.TryParse(clientCandidate, out var parsedForwarded))
                {
                    return parsedForwarded.ToString();
                }
            }
        }

        // 3. Nếu là external IP không thuộc Cloudflare / proxy tin cậy, tuyệt đối không dùng CF-Connecting-IP; dùng remote IP thực tế từ socket connection
        return remoteIp?.ToString() ?? "unknown-client";
    }
}

public partial class Program
{
    public static string ResolveClientIp(HttpContext context) => ClientIpResolver.ResolveClientIp(context);
    public static bool IsCloudflareIp(System.Net.IPAddress? ip) => ClientIpResolver.IsCloudflareIp(ip);
}
