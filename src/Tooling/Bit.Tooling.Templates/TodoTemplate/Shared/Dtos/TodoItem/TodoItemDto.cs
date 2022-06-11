namespace TodoTemplate.Shared.Dtos.TodoItem;

public class TodoItemDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public DateTimeOffset Date { get; set; }
    public bool IsDone { get; set; }
    public bool IsInEditMode { get; set; }
}
