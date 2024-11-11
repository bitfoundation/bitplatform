namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil09StoragePage
{
    private string? localLength;
    private string? sessionLength;

    private double keyIndex;
    private string? localKey;
    private string? sessionKey;

    private string? currentLocalItemKey;
    private string? currentSessionItemKey;
    private string? currentLocalItem;
    private string? currentSessionItem;

    private string? newLocalItemKey;
    private string? newSessionItemKey;

    private string? localItemKey;
    private string? sessionItemKey;


    private async Task GetLength()
    {
        var localResult = await localStorage.GetLength();
        localLength = localResult.ToString();

        var sessionResult = await sessionStorage.GetLength();
        sessionLength = sessionResult.ToString();
    }

    private async Task GetKey()
    {
        localKey = await localStorage.GetKey((int)keyIndex);

        sessionKey = await sessionStorage.GetKey((int)keyIndex);
    }

    private async Task GetItem()
    {
        currentLocalItem = await localStorage.GetItem(currentLocalItemKey);

        currentSessionItem = await sessionStorage.GetItem(currentSessionItemKey);
    }

    private async Task SetItem()
    {
        await localStorage.SetItem(newLocalItemKey, newLocalItemKey);

        await sessionStorage.SetItem(newSessionItemKey, newSessionItemKey);
    }

    private async Task RemoveItem()
    {
        await localStorage.RemoveItem(localItemKey);

        await sessionStorage.RemoveItem(sessionItemKey);
    }

    private async Task Clear()
    {
        await localStorage.Clear();

        await sessionStorage.Clear();
    }


    private string getLengthExampleCode =
@"@inject Bit.Butil.LocalStorage localStorage
@inject Bit.Butil.SessionStorage sessionStorage

<BitButton OnClick=""@GetLength"">GetLength</BitButton>

<div>LocalStorage length: @localLength</div>

<div>SessionStorage length: @sessionLength</div>

@code {
    private string? localLength;
    private string? sessionLength;

    private async Task GetLength()
    {
        var localResult = await localStorage.GetLength();
        localLength = localResult.ToString();

        var sessionResult = await sessionStorage.GetLength();
        sessionLength = sessionResult.ToString();
    }
}";
    private string getKeyExampleCode =
@"@inject Bit.Butil.LocalStorage localStorage
@inject Bit.Butil.SessionStorage sessionStorage

<BitSpinButton @bind-Value=""keyIndex"" />

<BitButton OnClick=""@GetKey"">GetKey</BitButton>

<div>LocalStorage key: @localKey</div>

<div>SessionStorage key: @sessionKey</div>

@code {
    private double keyIndex;
    private string? localKey;
    private string? sessionKey;

    private async Task GetKey()
    {
        localKey = await localStorage.GetKey((int)keyIndex);

        sessionKey = await sessionStorage.GetKey((int)keyIndex);
    }
}";
    private string getItemExampleCode =
@"@inject Bit.Butil.LocalStorage localStorage
@inject Bit.Butil.SessionStorage sessionStorage

<BitTextField @bind-Value=""currentLocalItemKey"" Label=""LocalStorage key"" />

<BitTextField @bind-Value=""currentSessionItemKey"" Label=""SessionStorage key"" />

<BitButton OnClick=""@GetItem"">GetItem</BitButton>

<div>LocalStorage item: @currentLocalItem</div>

<div>SessionStorage item: @currentSessionItem</div>

@code {
    private string? currentLocalItemKey;
    private string? currentSessionItemKey;
    private string? currentLocalItem;
    private string? currentSessionItem;

    private async Task GetItem()
    {
        currentLocalItem = await localStorage.GetItem(currentLocalItemKey);

        currentSessionItem = await sessionStorage.GetItem(currentSessionItemKey);
    }
}";
    private string setItemExampleCode =
@"@inject Bit.Butil.LocalStorage localStorage
@inject Bit.Butil.SessionStorage sessionStorage

<BitTextField @bind-Value=""newLocalItemKey"" Label=""LocalStorage key"" />

<BitTextField @bind-Value=""newSessionItemKey"" Label=""SessionStorage key"" />

<BitButton OnClick=""@SetItem"">SetItem</BitButton>

@code {
    private string? newLocalItemKey;
    private string? newSessionItemKey;

    private async Task SetItem()
    {
        await localStorage.SetItem(newLocalItemKey, newLocalItemKey);

        await sessionStorage.SetItem(newSessionItemKey, newSessionItemKey);
    }
}";
    private string removeItemExampleCode =
@"@inject Bit.Butil.LocalStorage localStorage
@inject Bit.Butil.SessionStorage sessionStorage

<BitTextField @bind-Value=""localItemKey"" Label=""LocalStorage key"" />

<BitTextField @bind-Value=""sessionItemKey"" Label=""SessionStorage key"" />

<BitButton OnClick=""@RemoveItem"">RemoveItem</BitButton>

@code {
    private string? localItemKey;
    private string? sessionItemKey;

    private async Task RemoveItem()
    {
        await localStorage.RemoveItem(localItemKey);

        await sessionStorage.RemoveItem(sessionItemKey);
    }
}";
    private string clearItemExampleCode =
@"@inject Bit.Butil.LocalStorage localStorage
@inject Bit.Butil.SessionStorage sessionStorage

<BitButton OnClick=""@Clear"">Clear</BitButton>

@code {
    private async Task Clear()
    {
        await localStorage.Clear();

        await sessionStorage.Clear();
    }
}";
}
