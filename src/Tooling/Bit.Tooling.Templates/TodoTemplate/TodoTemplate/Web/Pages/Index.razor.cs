using System.Net.Http.Json;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class Index
{
    public List<RoleDto> Roles { get; set; } = new();

    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var response = await HttpClient.GetAsync("Role");

        Roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>();

        await base.OnInitializedAsync();
    }
}
