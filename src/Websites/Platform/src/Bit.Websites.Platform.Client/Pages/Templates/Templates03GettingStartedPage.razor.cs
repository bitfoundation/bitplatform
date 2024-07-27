namespace Bit.Websites.Platform.Client.Pages.Templates;

public partial class Templates03GettingStartedPage
{
    private bool isAdditionalSelected;
    private bool isVSSelected;
    private bool isVSCodeSelected;

    Dictionary<CommandGroup, List<(string text, string command)>> _commandGroups = new()
    {
        [CommandGroup.Core] = new()
        {
            (text:"Enable long paths in the file system by updating the registry",
            command:@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\FileSystem"" /v LongPathsEnabled /t REG_DWORD /d 1 /f"),

            (text:"Enable windows developer mode",
            command:@"reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock"" /v AllowDevelopmentWithoutDevLicense /t REG_DWORD /d 1 /f"),

            (text:"Install .NET SDK",
            command:@"winget install Microsoft.DotNet.SDK.8 -v 8.0.303 --accept-source-agreements --accept-package-agreements"),

            (text:"Install Node.js",
            command:@"winget install OpenJS.NodeJS --accept-source-agreements --accept-package-agreements"),

            (text:"Install MAUI workloads and experimental tools",
            command:@"""%ProgramFiles%\dotnet\dotnet"" workload install wasm-tools wasm-experimental"),

            (text:"Install the Bit.Boilerplate project template",
            command:@"""%ProgramFiles%\dotnet\dotnet"" new install Bit.Boilerplate"),
        },

        [CommandGroup.Additional] = new()
        {
            (text:"Install MAUI workloads and experimental tools",
            command:@"""%ProgramFiles%\dotnet\dotnet"" workload install maui")
        },

        [CommandGroup.VS] = new()
        {
            (text:"Install Visual Studio 2022 Community Edition",
            command:@"winget install --id Microsoft.VisualStudio.2022.Community --exact --silent --custom ""--add Microsoft.VisualStudio.Workload.NetCrossPlat --add Microsoft.VisualStudio.Workload.NetWeb --add Component.Android.SDK.MAUI"" --accept-source-agreements --accept-package-agreements --disable-interactivity")
        },

        [CommandGroup.AdditionalVS] = new()
        {
            (text:"Install Visual Studio 2022 Community Edition",
            command:@"winget install --id Microsoft.VisualStudio.2022.Community --exact --silent --custom ""--add Microsoft.VisualStudio.Workload.NetWeb"" --accept-source-agreements --accept-package-agreements --disable-interactivity")
        },

        [CommandGroup.VSCode] = new()
        {
            (text:"Install Visual Studio Code",
            command:@"winget install -e --id Microsoft.VisualStudioCode --accept-source-agreements --accept-package-agreements"),

            (text:"Install the C# Dev Kit extension for Visual Studio Code",
            command:@"""%LocalAppData%\Programs\Microsoft VS Code\bin\code"" --install-extension ms-dotnettools.csdevkit"),

            (text:"Install the Blazor WASM Companion extension for Visual Studio Code",
            command:@"""%LocalAppData%\Programs\Microsoft VS Code\bin\code"" --install-extension ms-dotnettools.blazorwasm-companion"),

            (text:"Install the Live Sass Compiler extension for Visual Studio Code",
            command:@"""%LocalAppData%\Programs\Microsoft VS Code\bin\code"" --install-extension glenn2223.live-sass"),

            (text:"Install the ASP.NET Core Razor IntelliSense for Visual Studio Code",
            command:@"""%LocalAppData%\Programs\Microsoft VS Code\bin\code"" --install-extension kevin-chatham.aspnetcorerazor-html-css-class-completion"),

            (text:"Install the .NET MAUI extension for Visual Studio Code",
            command:@"""%LocalAppData%\Programs\Microsoft VS Code\bin\code"" --install-extension ms-dotnettools.dotnet-maui"),

            (text:"Install the SQLite3 Editor extension for Visual Studio Code",
            command:@"""%LocalAppData%\Programs\Microsoft VS Code\bin\code"" --install-extension yy0931.vscode-sqlite3-editor"),

            (text:"Install the RESX Editor extension for Visual Studio Code",
            command:@"""%LocalAppData%\Programs\Microsoft VS Code\bin\code"" --install-extension DominicVonk.vscode-resx-editor"),
        }
    };

    private enum CommandGroup
    {
        Core,
        Additional,
        AdditionalVS,
        VS,
        VSCode,
    }

    private List<(string text, string command)> GetCommandsInfo()
    {
        List<(string text, string command)> groups = [.. _commandGroups[CommandGroup.Core]];

        if (isVSSelected && isAdditionalSelected)
        {
            groups.AddRange(_commandGroups[CommandGroup.Additional]);
            groups.AddRange(_commandGroups[CommandGroup.AdditionalVS]);
        }
        else if (isVSSelected)
            groups.AddRange(_commandGroups[CommandGroup.VS]);
        else if (isAdditionalSelected)
            groups.AddRange(_commandGroups[CommandGroup.Additional]);

        if (isVSCodeSelected)
            groups.AddRange(_commandGroups[CommandGroup.VSCode]);

        return groups;
    }

    private string GetCommands()
    {
        return string.Join(" && ", GetCommandsInfo().Select(c => c.command));
    }
}
