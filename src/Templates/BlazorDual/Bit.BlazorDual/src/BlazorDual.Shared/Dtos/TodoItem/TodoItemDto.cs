namespace BlazorDual.Shared.Dtos.TodoItem;

[DtoResourceType(typeof(AppStrings))]
public class TodoItemDto
{
    public int Id { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Title))]
    public string? Title { get; set; }

    public DateTimeOffset Date { get; set; }

    public bool IsDone { get; set; }

    [JsonIgnore]
    public bool IsUnderEdit { get; set; }
}
