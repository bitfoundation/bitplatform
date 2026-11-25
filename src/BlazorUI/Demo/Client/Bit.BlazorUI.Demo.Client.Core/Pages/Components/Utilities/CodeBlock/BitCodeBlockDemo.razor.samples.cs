namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.CodeBlock;

public partial class BitCodeBlockDemo
{
    private readonly string example1RazorCode = @"
<BitCodeBlock Code=""@basicCode"" />

<BitCodeBlock>dotnet tool install --global Bit.Cli</BitCodeBlock>";
    private readonly string example1CsharpCode = @"
private readonly string basicCode = ""dotnet new install Bit.Boilerplate"";";

    private readonly string example2RazorCode = @"
<BitCodeBlock Code=""@headerCode"" Title=""Program.cs"" Language=""C#"" />

<BitCodeBlock Code=""@jsonCode"" Language=""json"" />";
    private readonly string example2CsharpCode = @"
private readonly string headerCode = """"""
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBitBlazorUIServices();

var app = builder.Build();
app.Run();
"""""";

private readonly string jsonCode = """"""{ ""name"": ""bit BlazorUI"", ""free"": true }"""""";";

    private readonly string example3RazorCode = @"
<BitCodeBlock Terminal Code=""@terminalCode"" />

<BitCodeBlock Terminal Title=""PowerShell"" Code=""@powershellCode"" Style=""--bit-CodeBlock-prompt: 'PS> '"" />";
    private readonly string example3CsharpCode = @"
private readonly string terminalCode = """"""
dotnet tool install --global Bit.Cli
bit new MyFirstProject
"""""";

private readonly string powershellCode = """"""
Set-Location .\MyFirstProject
dotnet run
"""""";";

    private readonly string example4RazorCode = @"
<BitCodeBlock Code=""@longCode"" Title=""appsettings.json"" MaxHeight=""10rem"" />";
    private readonly string example4CsharpCode = @"
private readonly string longCode = """"""
{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""AllowedHosts"": ""*"",
  ""ConnectionStrings"": {
    ""SqliteConnectionString"": ""Data Source=App_Data/AppDb.db;Mode=ReadWriteCreate;Cache=Shared""
  },
  ""Identity"": {
    ""Issuer"": ""MyFirstProject"",
    ""Audience"": ""MyFirstProject"",
    ""BearerTokenExpiration"": ""0.00:05:00"",
    ""RefreshTokenExpiration"": ""14.00:00:00""
  }
}
"""""";";

    private readonly string example5RazorCode = @"
<BitCodeBlock Code=""@basicCode"" CopyOnClick OnCopy=""() => copyCount++"" CopyText=""Copy the command"" CopiedText=""Copied to the clipboard"" />
<div>Copied @copyCount time(s).</div>

<BitCodeBlock Code=""@basicCode"" ShowCopyButton=""false"" />";
    private readonly string example5CsharpCode = @"
private int copyCount;

private readonly string basicCode = ""dotnet new install Bit.Boilerplate"";";

    private readonly string example6RazorCode = @"
<BitCodeBlock Code=""@highlightedCode"" Language=""C#""><span style=""color: var(--bit-clr-pri-fg)"">var</span> greeting = <span style=""color: var(--bit-clr-suc-fg)"">""Hello, world!""</span>;</BitCodeBlock>";
    private readonly string example6CsharpCode = @"
private readonly string highlightedCode = ""var greeting = \""Hello, world!\"";"";";

    private readonly string example7RazorCode = @"
<style>
    .custom-root {
        border-radius: 1rem;
        box-shadow: var(--bit-shd-card);
    }

    .custom-header {
        font-weight: 600;
        color: var(--bit-clr-pri-text);
        background-color: var(--bit-clr-pri);
    }

    .custom-copy {
        color: var(--bit-clr-pri-text);
    }
</style>

<BitCodeBlock Code=""@basicCode""
              Style=""--bit-CodeBlock-background: var(--bit-clr-pri-light); --bit-CodeBlock-border-color: var(--bit-clr-pri); --bit-CodeBlock-radius: 1rem;"" />

<BitCodeBlock Code=""@basicCode""
              Title=""Install""
              Classes=""@(new() { Root = ""custom-root"", Header = ""custom-header"", CopyButton = ""custom-copy"" })"" />

<BitCodeBlock Code=""@basicCode""
              Styles=""@(new() { Pre = ""font-size: 1rem; padding: 1.5rem;"", CopyButton = ""color: var(--bit-clr-pri);"" })"" />";
    private readonly string example7CsharpCode = @"
private readonly string basicCode = ""dotnet new install Bit.Boilerplate"";";

    private readonly string example8RazorCode = @"
<div dir=""rtl"">
    <BitCodeBlock Dir=""BitDir.Rtl"" Title=""نصب"" Code=""@basicCode"" CopyText=""کپی"" CopiedText=""کپی شد"" />
</div>";
    private readonly string example8CsharpCode = @"
private readonly string basicCode = ""dotnet new install Bit.Boilerplate"";";
}
