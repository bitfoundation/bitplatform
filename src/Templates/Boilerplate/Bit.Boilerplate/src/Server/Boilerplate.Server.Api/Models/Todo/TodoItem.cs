//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;
//#if (offlineDb == true)
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
//#endif

namespace Boilerplate.Server.Api.Models.Todo;

public partial class TodoItem
//#if (offlineDb == true)
    : BaseEntityTableData
//#endif
{
    //#if (offlineDb != true)
    //#if (IsInsideProjectTemplate == true)
    /*
    //#endif
    public new string Id { get; set; }
    public new DateTimeOffset? UpdatedAt { get; set; }
    //#if (IsInsideProjectTemplate == true)
    */
    //#endif
    //#endif

    [Required]
    public string? Title { get; set; }
    public bool IsDone { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
    public Guid UserId { get; set; }
}
