
using System.ComponentModel.DataAnnotations.Schema;
using TodoTemplate.Api.Models.Account;

namespace TodoTemplate.Api.Models.TodoItem;

public class TodoItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public DateTime Date { get; set; }
    public bool IsDone { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
    public int UserId { get; set; }
}
