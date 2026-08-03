using Backend.Application.Abstractions;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("BackendDb"));
        services.AddScoped<ITodoRepository, TodoRepository>();
        return services;
    }

    public static async Task SeedDataAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!dbContext.TodoItems.Any())
        {
            await dbContext.TodoItems.AddRangeAsync(
                new TodoItem { Title = "Complete IELTS speaking practice", IsCompleted = false },
                new TodoItem { Title = "Review HSK vocabulary list", IsCompleted = true });
            await dbContext.SaveChangesAsync();
        }
    }
}
