//+:cnd:noEmit
using System.Text.Json;
using System.Text.Json.Nodes;
using Boilerplate.Server.Api.Features.PersonalData;

namespace Boilerplate.Server.Api.Features.Todo;

/// <summary>
/// Content the user wrote. The only section of the export whose rows are not about the account but authored through it.
/// </summary>
public partial class TodoItemsPersonalDataSource : IPersonalDataSource
{
    [AutoInject] private AppDbContext dbContext = default!;

    public string Key => "todoItems";

    public int Order => 60;

    public string Purpose => "Storing the to-do items you create, and syncing them to your devices.";

    public string Retention => "For as long as the account exists.";

    //#if (offlineDb == true)
    public string? Notes => "An item you deleted keeps its row, marked as deleted, so devices that were offline learn about the deletion when they next sync. Its title is still stored until the account is deleted.";
    //#endif

    public PersonalDataErasure Erasure => PersonalDataErasure.CascadeFromUser;

    public async Task<JsonNode?> Export(Guid userId, CancellationToken cancellationToken)
    {
        var todoItems = await dbContext.TodoItems
            .AsNoTracking()
            .Where(todoItem => todoItem.UserId == userId)
            .OrderBy(todoItem => todoItem.UpdatedAt)
            .Select(todoItem => new
            {
                todoItem.Id,
                todoItem.Title,
                todoItem.IsDone,
                todoItem.UpdatedAt,
                //#if (offlineDb == true)
                todoItem.Deleted,
                //#endif
            })
            .ToArrayAsync(cancellationToken);

        return JsonSerializer.SerializeToNode(todoItems, IPersonalDataSource.SerializerOptions);
    }
}
