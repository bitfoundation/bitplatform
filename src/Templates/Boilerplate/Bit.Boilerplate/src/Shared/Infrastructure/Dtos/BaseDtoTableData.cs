namespace Boilerplate.Shared.Infrastructure.Dtos;

/// <summary>
/// CommunityToolkit.Datasync compatible base class for DTOs for client app offline database sync.
/// </summary>
public partial class BaseDtoTableData : ITableData
{
    public string Id { get; set; } = default!;

    public bool Deleted { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public byte[] Version { get; set; } = [];

    public bool Equals(ITableData? other) => other is not null 
        && Id == other.Id 
        && other.Version?.SequenceEqual(Version) is true;
}
