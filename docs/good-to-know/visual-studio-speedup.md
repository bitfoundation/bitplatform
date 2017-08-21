# tips for Speeding up Visual Studio environment


in this article we want to increase efficiency of visual studio and do some tricks that could boost visual studio performance,
If you feel like your IDE is slow, it's time to change some configuration to make your visual studio go faster. let's talk about the tweaks,



#### Visual Studio And Windows Defender
windows defender is consuming cpu time as same as visual studio , so we excude visual studio to reduce this time
we need to exclude devenv,msbuild,dotnet,npm,nuget and etc.
you can exclude this process manually or use this PowerShell script to exclude theme together 

**note that you should run this script with administrator privilege**
```

#Requires -RunAsAdministrator
# visual studio & tools

 Add-MpPreference -ExclusionPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\devenv.exe"
Add-MpPreference -ExclusionPath "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
Add-MpPreference -ExclusionPath "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
Add-MpPreference -ExclusionPath "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
Add-MpPreference -ExclusionPath "C:\Program Files\dotnet\dotnet.exe"
# Node.js

Add-MpPreference -ExclusionPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Web\External\node.exe"
Add-MpPreference -ExclusionPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\ServiceHub\Hosts\ServiceHub.Host.Node.x86\ServiceHub.Host.Node.x86.exe"
Add-MpPreference -ExclusionPath "C:\Program Files\nodejs\node.exe"

# visual studio folders

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
# cache folders
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
we provide a recommended settings to improve visual studio responsiveness and reduce build time  and environment performance,
this settings apply to visual studio 2017 professional.

Environment -> Startup
* Set "At startup" to "Show empty environment at startup"
* Uncheck "Download content every"

Environment -> General
* Uncheck "Automatically adjust visual experience based on client performance"
* Uncheck "Enable rich client visual experience"
* Check "Use hardware graphics acceleration if available"


Text Editor -> All Languages -> Scroll Bars
* Uncheck "Show annotations over vertical scroll bar"
Text Editor -> All Languages -> CodeLens
* Uncheck "Enable CodeLens"



Projects and Solutions -> Web Package Management
* Set "Restore on Project Open" to false (for Bower)
* Set "Restore on Save" to false (for Bower)
* Set "Restore on Project Open" to false (for NPM)
* Set "Restore on Save" to false (for NPM)


Text Editor -> General
* Uncheck "Track changes"




Feedback and questions are welcome in the comments below.