using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil25PermissionsPage
{
    private string? isSupported;

    private string selectedPermission = "geolocation";

    private string? queryResult;

    private static readonly string[] permissionNames =
    [
        "geolocation",
        "notifications",
        "camera",
        "microphone",
        "clipboard-read",
        "clipboard-write",
        "push",
        "midi",
        "background-sync",
        "persistent-storage",
    ];


    private async Task CheckSupported()
    {
        isSupported = (await permissions.IsSupported()).ToString();
    }

    private async Task QuerySelected()
    {
        var state = await permissions.Query(selectedPermission);
        queryResult = $"{selectedPermission} → {state}";
    }

    private async Task QueryAll()
    {
        var result = "";
        foreach (var name in permissionNames)
        {
            var state = await permissions.Query(name);
            result += $"{name} → {state}\n";
        }
        queryResult = result;
    }


    private string isSupportedExampleCode =
@"@inject Bit.Butil.Permissions permissions

<BitButton OnClick=""@CheckSupported"">IsSupported</BitButton>

<div>Is supported: @isSupported</div>

@code {
    private string? isSupported;

    private async Task CheckSupported()
    {
        isSupported = (await permissions.IsSupported()).ToString();
    }
}";

    private string queryExampleCode =
@"@inject Bit.Butil.Permissions permissions

<select @bind=""selectedPermission"">
    @foreach (var name in permissionNames)
    {
        <option value=""@name"">@name</option>
    }
</select>

<BitButton OnClick=""@QuerySelected"">Query selected</BitButton>
<BitButton OnClick=""@QueryAll"">Query all common</BitButton>

<div>@queryResult</div>

@code {
    private string selectedPermission = ""geolocation"";
    private string? queryResult;

    private static readonly string[] permissionNames =
    [
        ""geolocation"", ""notifications"", ""camera"", ""microphone"",
        ""clipboard-read"", ""clipboard-write"", ""push"", ""midi"",
        ""background-sync"", ""persistent-storage"",
    ];

    private async Task QuerySelected()
    {
        var state = await permissions.Query(selectedPermission);
        queryResult = $""{selectedPermission} → {state}"";
    }

    private async Task QueryAll()
    {
        var result = """";
        foreach (var name in permissionNames)
        {
            var state = await permissions.Query(name);
            result += $""{name} → {state}\n"";
        }
        queryResult = result;
    }
}";
}
