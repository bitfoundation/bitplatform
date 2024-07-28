using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Templates;

public partial class Templates03GettingStartedPage
{
    [Inject] private Clipboard clipboard { get; set; } = default!;

    private bool isAdditionalSelected = true;
    private bool isVSSelected = true;
    private bool isVSCodeSelected = true;
    private string copyCommandsText = "Copy Commands";

    private Dictionary<CommandGroup, List<(string text, string command)>> commandGroups = new()
    {
        [CommandGroup.Core] =
        [
            (text:@"Enable long paths files in Windows https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation ::",
            command:@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\FileSystem"" /v LongPathsEnabled /t REG_DWORD /d 1 /f"),

            (text:@"Enable Windows developer mode https://learn.microsoft.com/en-us/windows/apps/get-started/developer-mode-features-and-debugging#use-regedit-to-enable-your-device ::",
            command:@"reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock"" /v AllowDevelopmentWithoutDevLicense /t REG_DWORD /d 1 /f"),

            (text:@"Install .NET SDK https://dotnet.microsoft.com/en-us/download ::",
            command:"winget install Microsoft.DotNet.SDK.8 -v 8.0.303 --accept-source-agreements --accept-package-agreements"),

            (text:@"Install Node.js https://nodejs.org/en/download/package-manager/all#windows-1 ::",
            command:"winget install OpenJS.NodeJS --accept-source-agreements --accept-package-agreements"),

            (text:@"Install WebAssembly workloads https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot#net-webassembly-build-tools ::",
            command:"\"%ProgramFiles%\\dotnet\\dotnet.exe\" workload install wasm-tools wasm-experimental"),

            (text:@"Install the Bit.Boilerplate project template https://www.nuget.org/packages/Boilerplate.Templates ::",
            command:"\"%ProgramFiles%\\dotnet\\dotnet.exe\" new install Bit.Boilerplate::8.10.0-pre-03"),
        ],

        [CommandGroup.Additional] =
        [
            (text:@"Install MAUI workloads https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation?tabs=visual-studio-code#install-net-and-net-maui-workloads ::",
            command:"\"%ProgramFiles%\\dotnet\\dotnet.exe\" workload install maui")
        ],

        [CommandGroup.VS] =
        [
            (text:@"Install Visual Studio 2022 Community Edition https://visualstudio.microsoft.com/downloads/ ::",
            command:"winget install --id Microsoft.VisualStudio.2022.Community --exact --silent --custom \"--add Microsoft.VisualStudio.Workload.NetWeb\" --accept-source-agreements --accept-package-agreements --disable-interactivity")
        ],

        [CommandGroup.AdditionalVS] =
        [
            (text:@"Install Visual Studio 2022 Community Edition https://visualstudio.microsoft.com/downloads/ ::",
            command:"winget install --id Microsoft.VisualStudio.2022.Community --exact --silent --custom \"--add Microsoft.VisualStudio.Workload.NetCrossPlat --add Microsoft.VisualStudio.Workload.NetWeb --add Component.Android.SDK.MAUI\" --accept-source-agreements --accept-package-agreements --disable-interactivity")
        ],

        [CommandGroup.VSCode] =
        [
            (text:@"Install Visual Studio Code https://code.visualstudio.com/download ::",
            command:"winget install -e --id Microsoft.VisualStudioCode --accept-source-agreements --accept-package-agreements"),

            (text:@"Install the C# Dev Kit extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit ::",
            command:"\"%LocalAppData%\\Programs\\Microsoft VS Code\\bin\\code.cmd\" --install-extension ms-dotnettools.csdevkit"),

            (text:@"Install the Blazor WASM Companion extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.blazorwasm-companion ::",
            command:"\"%LocalAppData%\\Programs\\Microsoft VS Code\\bin\\code.cmd\" --install-extension ms-dotnettools.blazorwasm-companion"),

            (text:@"Install the Live Sass Compiler extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=glenn2223.live-sass ::",
            command:"\"%LocalAppData%\\Programs\\Microsoft VS Code\\bin\\code.cmd\" --install-extension glenn2223.live-sass"),

            (text:@"Install the ASP.NET Core Razor IntelliSense for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=kevin-chatham.aspnetcorerazor-html-css-class-completion ::",
            command:"\"%LocalAppData%\\Programs\\Microsoft VS Code\\bin\\code.cmd\" --install-extension kevin-chatham.aspnetcorerazor-html-css-class-completion"),

            (text:@"Install the .NET MAUI extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui ::",
            command:"\"%LocalAppData%\\Programs\\Microsoft VS Code\\bin\\code.cmd\" --install-extension ms-dotnettools.dotnet-maui"),

            (text:@"Install the SQLite3 Editor extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=yy0931.vscode-sqlite3-editor ::",
            command:"\"%LocalAppData%\\Programs\\Microsoft VS Code\\bin\\code.cmd\" --install-extension yy0931.vscode-sqlite3-editor"),

            (text:@"Install the RESX Editor extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=DominicVonk.vscode-resx-editor ::",
            command:"\"%LocalAppData%\\Programs\\Microsoft VS Code\\bin\\code.cmd\" --install-extension DominicVonk.vscode-resx-editor"),
        ]
    };

    private enum CommandGroup
    {
        Core,
        Additional,
        AdditionalVS,
        VS,
        VSCode,
    }

    private List<(string text, string command)> GetSelectedComands()
    {
        List<(string text, string command)> selectedCommandGroups = [.. commandGroups[CommandGroup.Core]];

        if (isAdditionalSelected)
            selectedCommandGroups.AddRange(commandGroups[CommandGroup.Additional]);
        if (isVSCodeSelected)
            selectedCommandGroups.AddRange(commandGroups[CommandGroup.VSCode]);
        if (isVSSelected)
        {
            if (isAdditionalSelected)
                selectedCommandGroups.AddRange(commandGroups[CommandGroup.AdditionalVS]);
            else
                selectedCommandGroups.AddRange(commandGroups[CommandGroup.VS]);
        }

        selectedCommandGroups.Add(("Done", "echo Done!"));

        return selectedCommandGroups;
    }

    private string GetReadyToRunSelectedCommands()
    {
        return string.Join(" && ", GetSelectedComands().Select(c => c.command));
    }

    private string GetDisplayableSelectedCommands()
    {
        return string.Join(Environment.NewLine, GetSelectedComands().Select(c => $"{c.text}{Environment.NewLine}{c.command}{Environment.NewLine}"))
            .Replace("\"%ProgramFiles%\\dotnet\\dotnet.exe\"", "dotnet")
            .Replace("\"%LocalAppData%\\Programs\\Microsoft VS Code\\bin\\code.cmd\"", "code");
    }

    private async Task CopyCommandsToClipboard()
    {
        var commands = GetReadyToRunSelectedCommands();
        await clipboard.WriteText(commands);
        copyCommandsText = "Now paste commands in Windows CMD!";
    }
}
