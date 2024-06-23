using Boilerplate.Server.Models.Identity;

namespace Boilerplate.Server.Models.Todo;

public class TodoItem
{
    public int Id { get; set; }

    [Required]
    public string? Title { get; set; }
    public DateTimeOffset Date { get; set; }
    public bool IsDone { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
    public int UserId { get; set; }
}
