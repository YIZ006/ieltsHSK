using Backend.Domain.Entities;

namespace Backend.Application.Abstractions;

public interface ITodoRepository
{
    Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);
}
