using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class Index
{
    public RoleDto[]? Roles { get; set; }

    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    [Inject]
    public StateService StateService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Roles = await StateService.GetValue("roles", () => GetRoles());

        await base.OnInitializedAsync();
    }

    async Task<RoleDto[]> GetRoles()
    {
        var response = await HttpClient.GetAsync("Role");
        var roles = await response.Content.ReadFromJsonAsync<RoleDto[]>();
        return roles!;
    }
}
