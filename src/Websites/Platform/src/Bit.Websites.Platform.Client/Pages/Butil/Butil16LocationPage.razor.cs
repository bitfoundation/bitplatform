namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil16LocationPage
{
    private string? newHref;
    private string? currentHref;

    private string? newProtocol;
    private string? currentProtocol;

    private string? newHost;
    private string? currentHost;

    private string? newHostname;
    private string? currentHostname;

    private string? newPort;
    private string? currentPort;

    private string? newPathname;
    private string? currentPathname;

    private string? newSearch;
    private string? currentSearch;

    private string? newHash;
    private string? currentHash;

    private string? currentOrigin;

    private string? newAssign;

    private string? newReplace;


    private async Task GetHref()
    {
        currentHref = await location.GetHref();
    }

    private async Task SetHref()
    {
        await location.SetHref(newHref!);
    }

    private async Task GetProtocol()
    {
        currentProtocol = await location.GetProtocol();
    }

    private async Task SetProtocol()
    {
        await location.SetProtocol(newProtocol!);
    }

    private async Task GetHost()
    {
        currentHost = await location.GetHost();
    }

    private async Task SetHost()
    {
        await location.SetHost(newHost!);
    }

    private async Task GetHostname()
    {
        currentHostname = await location.GetHostname();
    }

    private async Task SetHostname()
    {
        await location.SetHostname(newHostname!);
    }

    private async Task GetPort()
    {
        currentPort = await location.GetPort();
    }

    private async Task SetPort()
    {
        await location.SetPort(newPort!);
    }

    private async Task GetPathname()
    {
        currentPathname = await location.GetPathname();
    }

    private async Task SetPathname()
    {
        await location.SetPathname(newPathname!);
    }

    private async Task GetSearch()
    {
        currentSearch = await location.GetSearch();
    }

    private async Task SetSearch()
    {
        await location.SetSearch(newSearch!);
    }

    private async Task GetHash()
    {
        currentHash = await location.GetHash();
    }

    private async Task SetHash()
    {
        await location.SetHash(newHash!);
    }

    private async Task GetOrigin()
    {
        currentOrigin = await location.GetOrigin();
    }

    private async Task SetAssign()
    {
        await location.Assign(newAssign!);
    }

    private async Task SetReplace()
    {
        await location.Replace(newReplace!);
    }


    private string hrefExampleCode =
@"@inject Bit.Butil.Location location

<BitTextField @bind-Value=""newHref"" />

<BitButton OnClick=""@SetHref"">SetHref</BitButton>

<BitButton OnClick=""@GetHref"">GetHref</BitButton>

<div>Href: @currentHref</div>

@code {
    private string? newHref;
    private string? currentHref;

    private async Task GetHref()
    {
        currentHref = await location.GetHref();
    }

    private async Task SetHref()
    {
        await location.SetHref(newHref!);
    }
}";
    private string protocolExampleCode =
@"@inject Bit.Butil.Location location

<BitTextField @bind-Value=""newProtocol"" />

<BitButton OnClick=""@SetProtocol"">SetProtocol</BitButton>

<BitButton OnClick=""@GetProtocol"">GetProtocol</BitButton>

<div>Protocol: @currentProtocol</div>

@code {
    private string? newProtocol;
    private string? currentProtocol;

    private async Task GetProtocol()
    {
        currentProtocol = await location.GetProtocol();
    }

    private async Task SetProtocol()
    {
        await location.SetProtocol(newProtocol!);
    }
}";
    private string hostExampleCode =
@"@inject Bit.Butil.Location location

<BitTextField @bind-Value=""newHost"" />

<BitButton OnClick=""@SetHost"">SetHost</BitButton>

<BitButton OnClick=""@GetHost"">GetHost</BitButton>

<div>Host: @currentHost</div>

@code {
    private string? newHost;
    private string? currentHost;

    private async Task GetHost()
    {
        currentHost = await location.GetHost();
    }

    private async Task SetHost()
    {
        await location.SetHost(newHost!);
    }
}";
    private string hostnameExampleCode =
@"@inject Bit.Butil.Location location

<BitTextField @bind-Value=""newHostname"" />

<BitButton OnClick=""@SetHostname"">SetHostname</BitButton>

<BitButton OnClick=""@GetHostname"">GetHostname</BitButton>

<div>Hostname: @currentHostname</div>

@code {
    private string? newHostname;
    private string? currentHostname;

    private async Task GetHostname()
    {
        currentHostname = await location.GetHostname();
    }

    private async Task SetHostname()
    {
        await location.SetHostname(newHostname!);
    }
}";
    private string portExampleCode =
@"@inject Bit.Butil.Location location

<BitTextField @bind-Value=""newPort"" />

<BitButton OnClick=""@SetPort"">SetPort</BitButton>

<BitButton OnClick=""@GetPort"">GetPort</BitButton>

<div>Port: @currentPort</div>

@code {
    private string? newPort;
    private string? currentPort;

    private async Task GetPort()
    {
        currentPort = await location.GetPort();
    }

    private async Task SetPort()
    {
        await location.SetPort(newPort!);
    }
}";
    private string pathnameExampleCode =
@"@inject Bit.Butil.Location location

<BitTextField @bind-Value=""newPathname"" />

<BitButton OnClick=""@SetPathname"">SetPathname</BitButton>

<BitButton OnClick=""@GetPathname"">GetPathname</BitButton>

<div>Pathname: @currentPathname</div>

@code {
    private string? newPathname;
    private string? currentPathname;

    private async Task GetPathname()
    {
        currentPathname = await location.GetPathname();
    }

    private async Task SetPathname()
    {
        await location.SetPathname(newPathname!);
    }
}";
    private string searchExampleCode =
@"@inject Bit.Butil.Location location

<BitTextField @bind-Value=""newSearch"" />

<BitButton OnClick=""@SetSearch"">SetSearch</BitButton>

<BitButton OnClick=""@GetSearch"">GetSearch</BitButton>

<div>Search: @currentSearch</div>

@code {
    private string? newSearch;
    private string? currentSearch;

    private async Task GetSearch()
    {
        currentSearch = await location.GetSearch();
    }

    private async Task SetSearch()
    {
        await location.SetSearch(newSearch!);
    }
}";
    private string hashExampleCode =
@"@inject Bit.Butil.Location location

<BitTextField @bind-Value=""newHash"" />

<BitButton OnClick=""@SetHash"">SetHash</BitButton>

<BitButton OnClick=""@GetHash"">GetHash</BitButton>

<div>Hash: @currentHash</div>

@code {
    private string? newHash;
    private string? currentHash;

    private async Task GetHash()
    {
        currentHash = await location.GetHash();
    }

    private async Task SetHash()
    {
        await location.SetHash(newHash!);
    }
}";
    private string originExampleCode =
@"@inject Bit.Butil.Location location

<BitButton OnClick=""@GetOrigin"">GetOrigin</BitButton>

<div>Origin: @currentOrigin</div>

@code {
    private string? currentOrigin;

    private async Task GetOrigin()
    {
        currentOrigin = await location.GetOrigin();
    }
}";
    private string assignExampleCode =
@"@inject Bit.Butil.Location location

<BitTextField @bind-Value=""newAssign"" />

<BitButton OnClick=""@SetAssign"">Assign</BitButton>

@code {
    private string? newAssign;

    private async Task SetAssign()
    {
        await location.Assign(newAssign!);
    }
}";
    private string reloadExampleCode =
@"@inject Bit.Butil.Location location

<BitButton OnClick=""@(() => location.Reload())"">Reload</BitButton>";
    private string replaceExampleCode =
@"@inject Bit.Butil.Location location

<BitTextField @bind-Value=""newReplace"" />

<BitButton OnClick=""@SetReplace"">Replace</BitButton>

@code {
    private string? newReplace;

    private async Task SetReplace()
    {
        await location.Replace(newReplace!);
    }
}";
}
