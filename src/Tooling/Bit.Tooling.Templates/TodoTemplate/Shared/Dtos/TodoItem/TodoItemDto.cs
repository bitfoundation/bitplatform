namespace TodoTemplate.Shared.Dtos.TodoItem;

public class TodoItemDto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "please fill title input")]
    public string? Title { get; set; }
    [Required(ErrorMessage = "please choose date")]
    public DateTimeOffset Date { get; set; }
    public bool IsDone { get; set; }

    public bool IsInEditMode { get; set; }
}
