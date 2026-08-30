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

builder.Services.AddScoped(_ =>
{
    return new BackendApiClient(new HttpClient { BaseAddress = new Uri(backendApiBaseUrl) });
});

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddTransient<AuthHeaderHandler>();
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
builder.Services.AddScoped<ExamCheckpointService>();
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
    return new ExamSubmissionService(localStorage, httpClient);
});

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient { BaseAddress = new Uri(backendApiBaseUrl) };
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    var authStateProvider = sp.GetRequiredService<AuthenticationStateProvider>();
    return new AuthService(httpClient, localStorage, authStateProvider);
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
