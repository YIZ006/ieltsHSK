using System.Net.Http.Json;

namespace Frontend.App.Services;

public class BackendApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<TodoItemDto>> GetTodoItemsAsync(CancellationToken cancellationToken = default)
    {
        var items = await httpClient.GetFromJsonAsync<List<TodoItemDto>>("api/todos", cancellationToken);
        return items ?? [];
    }
}

public record TodoItemDto(int Id, string Title, bool IsCompleted);
