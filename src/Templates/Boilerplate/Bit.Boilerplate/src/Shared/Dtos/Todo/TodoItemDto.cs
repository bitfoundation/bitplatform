//+:cnd:noEmit
namespace Boilerplate.Shared.Dtos.Todo;

[DtoResourceType(typeof(AppStrings))]
public partial class TodoItemDto
//#if (offlineDb == true)
    : ITableData
//#endif
{
    public string Id { get; set; } = default!;

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Title))]
    public string? Title { get; set; }

    //#if (offlineDb == true)
    public bool Deleted { get; set; }
    //#endif

    public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsDone { get; set; }

    public byte[] Version { get; set; } = [];

    [JsonIgnore, NotMapped]
    public bool IsInEditMode { get; set; }

    //#if (offlineDb == true)
    public bool Equals(ITableData? other) => other is not null && Id == other.Id && other.Version.SequenceEqual(Version);
    //#endif
}
