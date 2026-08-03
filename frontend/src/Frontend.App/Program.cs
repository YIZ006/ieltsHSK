using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Frontend.App;
using Frontend.App.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped(_ =>
{
    var backendApiBaseUrl = builder.Configuration["BackendApi:BaseUrl"] ?? "https://localhost:7101/";
    return new BackendApiClient(new HttpClient { BaseAddress = new Uri(backendApiBaseUrl) });
});

await builder.Build().RunAsync();
