using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Templates;

public partial class Templates03GettingStartedPage
{
    [Inject] private Clipboard clipboard { get; set; } = default!;

    private bool installVs;
    private bool installVsCode;
    private bool enableCrossPlatform;
    private bool enableVirtualization;
    private string devOS = "Windows";
    private string copyButtonText = "Copy commands";
    private bool showCrossPlatform;

    private List<(string text, string command)> GetSelectedComands()
    {
        List<(string text, string command)> selectedCommandGroups = [.. commandGroups[CommandGroup.Core]];
        if (enableVirtualization)
            selectedCommandGroups.AddRange(commandGroups[CommandGroup.Virtualization]);
        if (enableCrossPlatform)
            selectedCommandGroups.AddRange(commandGroups[CommandGroup.Additional]);
        if (installVsCode)
            selectedCommandGroups.AddRange(commandGroups[CommandGroup.VSCode]);
        if (installVs)
        {
            if (enableCrossPlatform)
                selectedCommandGroups.AddRange(commandGroups[CommandGroup.AdditionalVS]);
            else
                selectedCommandGroups.AddRange(commandGroups[CommandGroup.VS]);
        }

        return selectedCommandGroups;
    }

    private string GetReadyToRunSelectedCommands()
    {
        return string.Join(" ", GetSelectedComands().Select(c => $"{c.text} {c.command}"));
    }

    private string GetDisplayableSelectedCommands()
    {
        return string.Join($"{Environment.NewLine}", GetSelectedComands().Select(c => $"{c.text}{Environment.NewLine}{c.command}{Environment.NewLine}"));
    }

    private async Task CopyCommandsToClipboard()
    {
        var commands = GetReadyToRunSelectedCommands();
        await clipboard.WriteText(commands);
        copyButtonText = "Now paste commands in Windows PowerShell";
    }

    private Dictionary<CommandGroup, List<(string text, string command)>> commandGroups = new()
    {
        [CommandGroup.Core] =
        [
            (text:"echo 'Set execution policy';",
            command:"Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force;"),

            (text:"echo 'Install - Update winget';",
            command:"$ProgressPreference = 'SilentlyContinue'; Install-PackageProvider -Name \"NuGet\" -Force; Set-PSRepository -Name \"PSGallery\" -InstallationPolicy Trusted; Install-Script winget-install -Force; winget-install -Force;"),

            (text:@"echo 'Install .NET SDK https://dotnet.microsoft.com/en-us/download';",
            command:"winget install Microsoft.DotNet.SDK.8 --accept-source-agreements --accept-package-agreements;"),

            (text:@"echo 'Discover installed .NET SDK';",
            command:"$env:Path = [System.Environment]::GetEnvironmentVariable(\"Path\",\"Machine\") + \";\" + [System.Environment]::GetEnvironmentVariable(\"Path\",\"User\");"),

            (text:@"echo 'Install Node.js https://nodejs.org/en/download/package-manager/all#windows-1';",
            command:"winget install OpenJS.NodeJS --accept-source-agreements --accept-package-agreements;"),

            (text:@"echo 'Install WebAssembly workloads https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot#net-webassembly-build-tools';",
            command:"dotnet nuget add source \"https://api.nuget.org/v3/index.json\" --name \"nuget.org\"; dotnet workload install wasm-tools;"),

            (text:@"echo 'Install the Bit.Boilerplate project template https://www.nuget.org/packages/Boilerplate.Templates';",
            command:"dotnet new install Bit.Boilerplate::8.12.0-pre-01;"),
        ],

        [CommandGroup.Additional] =
        [
            (text:@"echo 'Install MAUI workloads https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation?tabs=visual-studio-code#install-net-and-net-maui-workloads';",
            command:"dotnet workload install maui;"),

            (text:@"echo 'Enable long paths files in Windows https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation';",
            command:@"New-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem"" -Name ""LongPathsEnabled"" -Value 1 -PropertyType DWORD -Force;"),

            (text:@"echo 'Enable Windows developer mode https://learn.microsoft.com/en-us/windows/apps/get-started/developer-mode-features-and-debugging#use-regedit-to-enable-your-device';",
            command:@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock"" -Name ""AllowDevelopmentWithoutDevLicense"" -Value 1 -PropertyType DWORD -Force;"),
        ],

        [CommandGroup.VS] =
        [
            (text:@"echo 'Install Visual Studio 2022 Community Edition https://visualstudio.microsoft.com/downloads/';",
            command:"winget install --id Microsoft.VisualStudio.2022.Community --exact --silent --custom \"--add Microsoft.VisualStudio.Workload.NetWeb\" --accept-source-agreements --accept-package-agreements --disable-interactivity;")
        ],

        [CommandGroup.AdditionalVS] =
        [
            (text:@"echo 'Install Visual Studio 2022 Community Edition https://visualstudio.microsoft.com/downloads/';",
            command:"winget install --id Microsoft.VisualStudio.2022.Community --exact --silent --custom \"--add Microsoft.VisualStudio.Workload.NetCrossPlat --add Microsoft.VisualStudio.Workload.NetWeb --add Component.Android.SDK.MAUI\" --accept-source-agreements --accept-package-agreements --disable-interactivity;")
        ],

        [CommandGroup.VSCode] =
        [
            (text:@"echo 'Install Visual Studio Code https://code.visualstudio.com/download';",
            command:"winget install -e --id Microsoft.VisualStudioCode --scope machine --accept-source-agreements --accept-package-agreements;"),

            (text:@"echo 'Discover installed Visual Studio Code';",
            command:"$env:Path = [System.Environment]::GetEnvironmentVariable(\"Path\",\"Machine\") + \";\" + [System.Environment]::GetEnvironmentVariable(\"Path\",\"User\");"),

            (text:@"echo 'Install the C# Dev Kit extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit';",
            command:"code --install-extension ms-dotnettools.csdevkit;"),

            (text:@"echo 'Install Dev Containers extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers';",
            command:"code --install-extension ms-vscode-remote.remote-containers;"),

            (text:@"echo 'Install the Blazor WASM Companion extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.blazorwasm-companion';",
            command:"code --install-extension ms-dotnettools.blazorwasm-companion;"),

            (text:@"echo 'Install the Live Sass Compiler extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=glenn2223.live-sass';",
            command:"code --install-extension glenn2223.live-sass;"),

            (text:@"echo 'Install the ASP.NET Core Razor IntelliSense for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=kevin-chatham.aspnetcorerazor-html-css-class-completion';",
            command:"code --install-extension kevin-chatham.aspnetcorerazor-html-css-class-completion;"),

            (text:@"echo 'Install the .NET MAUI extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui';",
            command:"code --install-extension ms-dotnettools.dotnet-maui;"),

            (text:@"echo 'Install the RESX Editor extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=DominicVonk.vscode-resx-editor';",
            command:"code --install-extension DominicVonk.vscode-resx-editor;"),
        ],
        [CommandGroup.Virtualization] =
        [
            (text:@"echo 'Install WSL';",
            command:"wsl --install;"),

            (text:@"echo 'Install HyperV';",
            command:"Enable-WindowsOptionalFeature -FeatureName Microsoft-Hyper-V-All -Online -NoRestart;"),

            (text:@"echo 'Install HyperVisior Platform';",
            command:"Enable-WindowsOptionalFeature -FeatureName HypervisorPlatform -Online -NoRestart;"),

            (text:@"echo 'Install Virtual Machine Platform';",
            command:"Enable-WindowsOptionalFeature -FeatureName VirtualMachinePlatform -Online -NoRestart;"),

            (text:@"echo 'Install Docker Desktop';",
            command:"winget install Docker.DockerDesktop --accept-source-agreements --accept-package-agreements;"),
        ]
    };

    private enum CommandGroup
    {
        Core,
        Additional,
        AdditionalVS,
        VS,
        VSCode,
        Virtualization
    }
}
