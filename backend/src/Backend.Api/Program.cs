using Backend.Application.Abstractions;
using Backend.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "FrontendCorsPolicy";
var frontendUrl = builder.Configuration["Frontend:BaseUrl"] ?? "https://localhost:7102";
const string frontendHttpUrl = "http://localhost:5102";

builder.Services.AddInfrastructure();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy.WithOrigins(frontendUrl, frontendHttpUrl)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
await app.Services.SeedDataAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(frontendCorsPolicy);

app.MapGet("/api/todos", async (ITodoRepository repository, CancellationToken cancellationToken) =>
{
    var items = await repository.GetAllAsync(cancellationToken);
    return Results.Ok(items);
})
.WithName("GetTodoItems");

app.Run();
