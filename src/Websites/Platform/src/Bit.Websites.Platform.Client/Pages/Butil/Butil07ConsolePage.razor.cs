namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil07ConsolePage
{
    private string value = "Test";


    private async Task CreateGroupLogs()
    {
        await console.Log("This is the outer level");
        await console.Group();
        await console.Log("Level 2");
        await console.Group();
        await console.Log("Level 3");
        await console.Warn("More of level 3");
        await console.GroupCollapsed();
        await console.Log("Back to level 2");
        await console.GroupEnd();
        await console.Log("Back to the outer level");
    }


    private string assertExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Assert(false, ""This is a test assert:"", value))"">Assert</BitButton>

@code {
    private string value = ""Test"";
}";
    private string countExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Count(value))"">Count</BitButton>

@code {
    private string value = ""Test"";
}";
    private string countResetExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.CountReset(value))"">CountReset</BitButton>

@code {
    private string value = ""Test"";
}";
    private string debugExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Debug(value))"">Debug</BitButton>

@code {
    private string value = ""Test"";
}";
    private string dirExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Dir(value))"">Dir</BitButton>

@code {
    private string value = ""Test"";
}";
    private string dirxmlExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Dirxml(value))"">Dirxml</BitButton>

@code {
    private string value = ""Test"";
}";
    private string errorExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Error(""This is a test error:"", value))"">Error</BitButton>

@code {
    private string value = ""Test"";
}";
    private string groupExampleCode =
@"@inject Bit.Butil.Console console

<BitButton OnClick=""CreateGroupLogs"">Create group logs</BitButton>

@code {
    private async Task CreateGroupLogs()
    {
        await console.Log(""This is the outer level"");
        await console.Group();
        await console.Log(""Level 2"");
        await console.Group();
        await console.Log(""Level 3"");
        await console.Warn(""More of level 3"");
        await console.GroupCollapsed();
        await console.Log(""Back to level 2"");
        await console.GroupEnd();
        await console.Log(""Back to the outer level"");
    }
}";
    private string infoExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Info(""This is a test info:"", value))"">Info</BitButton>

@code {
    private string value = ""Test"";
}";
    private string logExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Log(""This is a test log:"", value))"">Log</BitButton>

@code {
    private string value = ""Test"";
}";
    private string warnExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Warn(""This is a test warn:"", value))"">Warn</BitButton>

@code {
    private string value = ""Test"";
}";
    private string tableExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Table(new {Name = ""Value"", Value = value }))"">Table</BitButton>

@code {
    private string value = ""Test"";
}";
    private string profileExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Profile(value))"">Profile</BitButton>

@code {
    private string value = ""Test"";
}";
    private string profileEndExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.ProfileEnd(value))"">ProfileEnd</BitButton>

@code {
    private string value = ""Test"";
}";
    private string timeExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Time(value))"">Time</BitButton>

@code {
    private string value = ""Test"";
}";
    private string timeLogExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.TimeLog(value))"">TimeLog</BitButton>

@code {
    private string value = ""Test"";
}";
    private string timeEndExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.TimeEnd(value))"">TimeEnd</BitButton>

@code {
    private string value = ""Test"";
}";
    private string timeStampExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.TimeStamp(value))"">TimeStamp</BitButton>

@code {
    private string value = ""Test"";
}";
    private string traceExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Trace(value))"">Trace</BitButton>

@code {
    private string value = ""Test"";
}";
    private string clearExampleCode =
@"@inject Bit.Butil.Console console

<BitButton OnClick=""@(() => console.Clear(value))"">Clear</BitButton>";
}
