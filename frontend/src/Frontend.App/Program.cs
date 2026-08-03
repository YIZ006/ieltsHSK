using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Frontend.App;
using Frontend.App.Configuration;
using Frontend.App.Services;
using Microsoft.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.Configure<BackendApiOptions>(builder.Configuration.GetSection("BackendApi"));
builder.Services.AddHttpClient<BackendApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<BackendApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

await builder.Build().RunAsync();
