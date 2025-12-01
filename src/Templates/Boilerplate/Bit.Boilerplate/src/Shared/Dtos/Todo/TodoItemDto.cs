//+:cnd:noEmit
namespace Boilerplate.Shared.Dtos.Todo;

[DtoResourceType(typeof(AppStrings))]
public partial class TodoItemDto
//#if (offlineDb == true)
    : BaseDtoTableData
//#endif
{

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Title))]
    public string? Title { get; set; }

    public bool IsDone { get; set; }

    [JsonIgnore, NotMapped]
    public bool IsInEditMode { get; set; }
}
