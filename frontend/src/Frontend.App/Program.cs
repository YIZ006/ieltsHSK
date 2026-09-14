using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Frontend.App;
using Frontend.App.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
var backendApiBaseUrl = builder.Configuration["BackendApi:BaseUrl"] ?? "http://localhost:5101/";

// Luôn tự động đồng bộ theo IP hoặc localhost của trình duyệt:
// - Nếu truy cập bằng IP LAN (ví dụ http://192.168.1.203:5102) -> Backend API tự đổi sang http://192.168.1.203:5101/
// - Nếu truy cập bằng localhost -> Backend API tự trỏ về http://localhost:5101/
if (Uri.TryCreate(builder.HostEnvironment.BaseAddress, UriKind.Absolute, out var hostUri))
{
    if (hostUri.Host != "localhost" && hostUri.Host != "127.0.0.1")
    {
        backendApiBaseUrl = $"{hostUri.Scheme}://{hostUri.Host}:5101/";
    }
    else
    {
        backendApiBaseUrl = "http://localhost:5101/";
    }
    builder.Configuration["BackendApi:BaseUrl"] = backendApiBaseUrl;
}

if (!backendApiBaseUrl.EndsWith('/'))
{
    backendApiBaseUrl += "/";
    builder.Configuration["BackendApi:BaseUrl"] = backendApiBaseUrl;
}

builder.Services.AddScoped(_ =>
{
    return new BackendApiClient(new HttpClient { BaseAddress = new Uri(backendApiBaseUrl) });
});

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<CookieStorageService>();
builder.Services.AddTransient(sp =>
{
    var tokenService = sp.GetRequiredService<TokenRefreshService>();
    var cookieStorage = sp.GetRequiredService<CookieStorageService>();
    return new AuthHeaderHandler(tokenService, cookieStorage);
});
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    return new AdminUserService(httpClient);
});
builder.Services.AddScoped<ExamSessionService>();
builder.Services.AddScoped<ExamHeaderService>();
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    var authStateProvider = sp.GetRequiredService<AuthenticationStateProvider>();
    return new ExamCheckpointService(localStorage, authStateProvider, httpClient);
});
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    return new NavigationService(httpClient);
});
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    return new FriendshipService(httpClient);
});
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    return new StreakService(localStorage, httpClient);
});
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    return new ProfileService(localStorage, httpClient);
});
builder.Services.AddScoped<ToeicAchievementService>();
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    return new NotificationService(httpClient, localStorage);
});
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    var streakService = sp.GetRequiredService<StreakService>();
    return new ToeicStudyTrackerService(localStorage, streakService, httpClient);
});
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    return new ToeicVocabularyService(httpClient, localStorage);
});
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    return new UserGameProgressService(httpClient, localStorage);
});
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    var cookieStorage = sp.GetRequiredService<CookieStorageService>();
    return new ExamSubmissionService(localStorage, httpClient, cookieStorage);
});
builder.Services.AddScoped<GrammarStructureService>();

builder.Services.AddAuthorizationCore();

// TokenRefreshService: quản lý access token + refresh token qua Cookie, tự gia hạn phiên đăng nhập.
// Dùng HttpClient "trần" (không qua AuthHeaderHandler) để tránh vòng lặp refresh.
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient { BaseAddress = new Uri(backendApiBaseUrl) };
    var cookieStorage = sp.GetRequiredService<CookieStorageService>();
    return new TokenRefreshService(httpClient, cookieStorage);
});

// Đăng ký 1 instance duy nhất cho cả kiểu cụ thể và kiểu AuthenticationStateProvider
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());

builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient { BaseAddress = new Uri(backendApiBaseUrl) };
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    var authStateProvider = sp.GetRequiredService<CustomAuthStateProvider>();
    var tokenRefreshService = sp.GetRequiredService<TokenRefreshService>();
    var cookieStorage = sp.GetRequiredService<CookieStorageService>();
    return new AuthService(httpClient, localStorage, authStateProvider, tokenRefreshService, cookieStorage);
});

builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    return new IeltsService(httpClient);
});

// ExamService: dùng BaseAddress của frontend để load được relative path (wwwroot/sample-data)
// Khi URL là đường dẫn tuyệt đối (http/https) thì HttpClient vẫn gọi thẳng được
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    return new ExamService(httpClient);
});

builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    return new MockTestService(httpClient);
});

// AnswerKeyService: same base URL as frontend (loads .answers.json from wwwroot or absolute R2 URL)
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    return new AnswerKeyService(httpClient);
});

// ToeicService: load đề thi TOEIC từ wwwroot/sample-data hoặc Cloudflare R2
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    return new ToeicService(httpClient);
});

// ToeicBuilderService: upload ảnh/audio per câu, lưu đề thi JSON lên Cloudflare R2
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    return new ToeicBuilderService(httpClient);
});

// StoryService: quản lý và đọc truyện tiếng Anh (Graded Readers)
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    return new StoryService(httpClient);
});

// HskService: tải dữ liệu HSK (gắn JWT tự động qua AuthHeaderHandler)
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<AuthHeaderHandler>())
    {
        BaseAddress = new Uri(backendApiBaseUrl)
    };
    return new HskService(httpClient, sp.GetRequiredService<ILocalStorageService>());
});

// SpeakAlongService: IELTS Nói Theo (Shadowing)
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    return new SpeakAlongService(httpClient, sp.GetRequiredService<IJSRuntime>(), backendApiBaseUrl);
});

await builder.Build().RunAsync();
