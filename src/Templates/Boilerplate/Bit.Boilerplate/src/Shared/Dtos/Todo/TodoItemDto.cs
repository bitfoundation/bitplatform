namespace Boilerplate.Shared.Dtos.Todo;

[DtoResourceType(typeof(AppStrings))]
public partial class TodoItemDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Title))]
    public string? Title { get; set; }

    public DateTimeOffset Date { get; set; }

    public bool IsDone { get; set; }

    [JsonIgnore]
    public bool IsInEditMode { get; set; }
}
