using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Models.Todo;

public partial class TodoItem
{
    public Guid Id { get; set; }

    [Required]
    public string? Title { get; set; }
    public DateTimeOffset Date { get; set; }
    public bool IsDone { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
    public Guid UserId { get; set; }
}
