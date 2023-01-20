namespace TodoTemplate.Shared.Dtos.TodoItem;

[DtoResourceType(typeof(AppStrings))]
public class TodoItemDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? Title { get; set; }

    public DateTimeOffset Date { get; set; }

    public bool IsDone { get; set; }

    [JsonIgnore]
    public bool IsUnderEdit { get; set; }
}
