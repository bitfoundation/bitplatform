using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Client.Core.Components.Pages.Settings.Account;

public partial class AccountSection
{
    [CascadingParameter] public UserDto? CurrentUser { get; set; }
}
