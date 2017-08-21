# Tips for Speeding up Visual Studio environment

In this article, we want to increase efficiency of visual studio and do some tricks that could boost visual studio performance,
If you feel like your IDE is slow, it's time to change some configuration to make your visual studio go faster. let's talk about the tweaks.


#### Visual Studio And Windows Defender

Windows defender is consuming cpu time as same as visual studio, so we excude visual studio to reduce this time
we need to exclude devenv,msbuild,dotnet,npm,nuget, etc.

You can exclude this process manually or use this PowerShell script to exclude them together 

Note that you should run this script with administrator privileges

```powershell

# Requires -RunAsAdministrator
# Visual Studio & tools

Add-MpPreference -ExclusionPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\devenv.exe"
Add-MpPreference -ExclusionPath "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
Add-MpPreference -ExclusionPath "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
Add-MpPreference -ExclusionPath "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
Add-MpPreference -ExclusionPath "C:\Program Files\dotnet\dotnet.exe"

# Node.js

Add-MpPreference -ExclusionPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Web\External\node.exe"
Add-MpPreference -ExclusionPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\ServiceHub\Hosts\ServiceHub.Host.Node.x86\ServiceHub.Host.Node.x86.exe"
Add-MpPreference -ExclusionPath "C:\Program Files\nodejs\node.exe"

# Visual Studio folders

Add-MpPreference -ExclusionPath "C:\Program Files (x86)\Microsoft Visual Studio 10.0"
Add-MpPreference -ExclusionPath "C:\Program Files (x86)\Microsoft Visual Studio 14.0"
Add-MpPreference -ExclusionPath "C:\Program Files (x86)\Microsoft Visual Studio"
Add-MpPreference -ExclusionPath "C:\Windows\assembly"
Add-MpPreference -ExclusionPath "C:\Windows\Microsoft.NET"
Add-MpPreference -ExclusionPath "C:\Program Files (x86)\MSBuild"
Add-MpPreference -ExclusionPath "C:\Program Files\dotnet"
Add-MpPreference -ExclusionPath "C:\Program Files (x86)\Microsoft SDKs"
Add-MpPreference -ExclusionPath "C:\Program Files\Microsoft SDKs"
Add-MpPreference -ExclusionPath "C:\Program Files (x86)\Common Files\Microsoft Shared\MSEnv"
Add-MpPreference -ExclusionPath "C:\Program Files (x86)\Microsoft Office"

# Cache folders
Add-MpPreference -ExclusionPath "C:\ProgramData\Microsoft\VisualStudio\Packages"
Add-MpPreference -ExclusionPath "C:\Program Files (x86)\Microsoft SDKs\NuGetPackages"
Add-MpPreference -ExclusionPath "C:\Windows\Microsoft.NET\Framework\v4.0.30319\Temporary ASP.NET Files"
Add-MpPreference -ExclusionPath "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\Temporary ASP.NET Files"

Add-MpPreference -ExclusionPath "C:\Users\$env:UserName\AppData\Local\Microsoft\VisualStudio"
Add-MpPreference -ExclusionPath "C:\Users\$env:UserName\AppData\Local\Microsoft\WebsiteCache"
Add-MpPreference -ExclusionPath "C:\Users\$env:UserName\AppData\Local\Jetbrains"
Add-MpPreference -ExclusionPath "C:\Users\$env:UserName\AppData\Roaming\Microsoft\VisualStudio"
Add-MpPreference -ExclusionPath "C:\Users\$env:UserName\AppData\Roaming\JetBrains"
Add-MpPreference -ExclusionPath "C:\Users\$env:UserName\AppData\Roaming\npm"
Add-MpPreference -ExclusionPath "C:\Users\$env:UserName\AppData\Roaming\npm-cache"

```

#### Visual Studio Settings

We provide a recommended settings to improve visual studio responsiveness and reduce build time and environment performance,
this settings apply to Visual Studio 2017.

Environment -> General
* Uncheck "Automatically adjust visual experience based on client performance"
* Uncheck "Enable rich client visual experience"
* Check "Use hardware graphics acceleration if available"

Environment -> Startup
* Set "At startup" to "Show empty environment at startup"
* Uncheck "Download content every"

Environment -> Synchronized settings (ignore if you are not logged in with Micorosft account)
* Uncheck "Synchronize settings across devices when signed into Visual Studio"

Projects and Solutions -> Web Package Management
* Set "Restore on Project Open" to false (for Bower)
* Set "Restore on Save" to false (for Bower)
* Set "Restore on Project Open" to false (for NPM)
* Set "Restore on Save" to false (for NPM)

Text Editor -> General
* Uncheck "Track changes"

Text Editor -> All Languages -> Scroll Bars
* Uncheck "Show annotations over vertical scroll bar"

Text Editor -> All Languages -> CodeLens
* Uncheck "Enable CodeLens"

Text Editor -> C# -> Advanced
* Uncheck "Enable full solution analysis"

Debugging
* Uncheck "Suppress JIT optimization on module load (Managed only)"
* Uncheck "Enable Edit and Continue"
* Uncheck "Enable JavaScript debugging for ASP.NET (Chrome and IE)"

Debugging -> Just-In-Time
* Uncheck "Script"

IntelliTrace
* Uncheck "Enable IntelliTrace"

Disable Browser link

![](/assets/browser-link.png)



Feedback and questions are welcome in the comments below.