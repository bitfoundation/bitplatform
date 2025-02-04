using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Templates;

public partial class Templates03GettingStartedPage
{
    [Inject] private Clipboard clipboard { get; set; } = default!;

    private bool installVs;
    private bool installVsCode;
    private bool showCrossPlatform;
    private bool enableCrossPlatform;
    private string devOS = "Windows";
    private bool enableVirtualization;
    private string dotnetVersion = "net9.0";
    private string copyButtonText = "Copy commands";

    private List<(string text, string command)> GetSelectedCommands()
    {
        List<(string text, string command)> result =
        [
            (text:"echo 'Set execution policy';",
            command:"Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force;"),

            (text:"echo 'Install - Update winget';",
            command:"$ProgressPreference = 'SilentlyContinue'; Install-PackageProvider -Name \"NuGet\" -Force; Set-PSRepository -Name \"PSGallery\" -InstallationPolicy Trusted; Install-Script winget-install -Force; winget-install -Force;"),

            (text:@"echo 'Install .NET SDK https://dotnet.microsoft.com/en-us/download';",
            command: $"winget install Microsoft.DotNet.SDK.{(dotnetVersion is "net9.0" ? "9" : "8")} --accept-source-agreements --accept-package-agreements;"),

            (text:@"echo 'Discover installed .NET SDK';",
            command:"$env:Path = [System.Environment]::GetEnvironmentVariable(\"Path\",\"Machine\") + \";\" + [System.Environment]::GetEnvironmentVariable(\"Path\",\"User\");"),

            (text:@"echo 'Install Node.js https://nodejs.org/en/download/package-manager/all#windows-1';",
            command:"winget install OpenJS.NodeJS --accept-source-agreements --accept-package-agreements;"),

            (text:@"echo 'Install WebAssembly workloads https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot#net-webassembly-build-tools';",
            command:"dotnet nuget add source \"https://api.nuget.org/v3/index.json\" --name \"nuget.org\"; dotnet workload install wasm-tools;"),

            (text:@"echo 'Install the Bit.Boilerplate project template https://www.nuget.org/packages/Boilerplate.Templates';",
            command:"dotnet new install Bit.Boilerplate::9.4.0-pre-03;")
        ];

        if (enableVirtualization)
        {
            result.AddRange([
                (text:@"echo 'Install WSL';",
                command:"wsl --install;"),

                (text:@"echo 'Install HyperV';",
                command:"Enable-WindowsOptionalFeature -FeatureName Microsoft-Hyper-V-All -Online -NoRestart;"),

                (text:@"echo 'Install HyperVisior Platform';",
                command:"Enable-WindowsOptionalFeature -FeatureName HypervisorPlatform -Online -NoRestart;"),

                (text:@"echo 'Install Virtual Machine Platform';",
                command:"Enable-WindowsOptionalFeature -FeatureName VirtualMachinePlatform -Online -NoRestart;"),

                (text:@"echo 'Install Docker Desktop';",
                command:"winget install Docker.DockerDesktop --accept-source-agreements --accept-package-agreements;")]
            );
        }

        if (enableCrossPlatform)
        {
            result.AddRange([
                (text:@"echo 'Install MAUI workloads https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation?tabs=visual-studio-code#install-net-and-net-maui-workloads';",
                command:"dotnet workload install maui;"),

                (text:@"echo 'Enable long paths files in Windows https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation';",
                command:@"New-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem"" -Name ""LongPathsEnabled"" -Value 1 -PropertyType DWORD -Force;"),

                (text:@"echo 'Enable Windows developer mode https://learn.microsoft.com/en-us/windows/apps/get-started/developer-mode-features-and-debugging#use-regedit-to-enable-your-device';",
                command:@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock"" -Name ""AllowDevelopmentWithoutDevLicense"" -Value 1 -PropertyType DWORD -Force;")]
            );
        }

        if (installVsCode)
        {
            result.AddRange([
                (text: @"echo 'Install Visual Studio Code https://code.visualstudio.com/download';",
                command: "winget install -e --id Microsoft.VisualStudioCode --scope machine --accept-source-agreements --accept-package-agreements;"),

                (text: @"echo 'Discover installed Visual Studio Code';",
                command: "$env:Path = [System.Environment]::GetEnvironmentVariable(\"Path\",\"Machine\") + \";\" + [System.Environment]::GetEnvironmentVariable(\"Path\",\"User\");"),

                (text: @"echo 'Install the C# Dev Kit extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit';",
                command: "code --install-extension ms-dotnettools.csdevkit;"),

                (text: @"echo 'Install Dev Containers extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers';",
                command: "code --install-extension ms-vscode-remote.remote-containers;"),

                (text: @"echo 'Install the Blazor WASM Companion extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.blazorwasm-companion';",
                command: "code --install-extension ms-dotnettools.blazorwasm-companion;"),

                (text: @"echo 'Install the Live Sass Compiler extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=glenn2223.live-sass';",
                command: "code --install-extension glenn2223.live-sass;"),

                (text: @"echo 'Install the ASP.NET Core Razor IntelliSense for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=kevin-chatham.aspnetcorerazor-html-css-class-completion';",
                command: "code --install-extension kevin-chatham.aspnetcorerazor-html-css-class-completion;"),

                (text: @"echo 'Install the .NET MAUI extension for Visual Studio Code https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui';",
                command: "code --install-extension ms-dotnettools.dotnet-maui;")]
            );
        }

        if (installVs)
        {
            result.Add((text: @"echo 'Install Visual Studio 2022 Community Edition https://visualstudio.microsoft.com/downloads/';",
                command: $"winget install --id Microsoft.VisualStudio.2022.Community --exact --silent --custom \"--add Microsoft.VisualStudio.Workload.NetWeb{(enableCrossPlatform ? " --add Microsoft.VisualStudio.Workload.NetCrossPlat --add Component.Android.SDK.MAUI" : "")}\" --accept-source-agreements --accept-package-agreements --disable-interactivity;"));
        }

        return result;
    }

    private string GetReadyToRunSelectedCommands()
    {
        return string.Join(" ", GetSelectedCommands().Select(c => $"{c.text} {c.command}"));
    }

    private string GetDisplayableSelectedCommands()
    {
        return string.Join($"{Environment.NewLine}", GetSelectedCommands().Select(c => $"{c.text}{Environment.NewLine}{c.command}{Environment.NewLine}"));
    }

    private async Task CopyCommandsToClipboard()
    {
        var commands = GetReadyToRunSelectedCommands();
        await clipboard.WriteText(commands);
        copyButtonText = "Now paste commands in Windows PowerShell";
    }
}
