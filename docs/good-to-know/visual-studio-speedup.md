# Tips for Speeding up Visual Studio environment

In this article, we want to increase efficiency of visual studio and do some tricks that could boost visual studio performance,
If you feel like your IDE is slow, it's time to change some configuration to make your visual studio go faster. let's talk about the tweaks.

### Upgrade to SSD

First of all, use SSD drive rather than HDD.

#### Visual Studio and Windows Defender

Anti-Virtus is consuming cpu time as same as Visual Studio, so we exclude visual studio to reduce this time
we need to exclude devenv,msbuild,dotnet,npm,nuget, etc.

Here you can see a list of folders to be added to Windows Defender exclusion list. If you're not using Windows Defender, then use this directories list to configure your preferred Anti-Virus software.

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

Also add your **project directory** to Anti-virus exclusion list.


#### Windows Search Index

Windows Indexing Service pull disk I/O to 100% when you build project or installing packages,
you can disable indexing service at all or just exclude your project directory from indexing 
**Note that by disabling this feature you can still search but without indexing** ,it will be slower a little bit

Open Control Panel and go to Indexing Options.

![](/assets/Control-panel.png)

 Click the ‘Modify’ button

![](/assets/Indexing-Option.png)

 in the ‘Indexed Locations’ window, navigate to the folder you want to exclude from search. Uncheck the location and click ‘Ok’.

#### Visual Studio Settings

We provide a recommended setting to improve visual studio responsiveness and reduce build/debug time and environment performance.

Not all these configurations are applicable to your Visual Studio based on your Visual Studio's version and workloads/components you've installed.


Tools -> Options -> Environment -> General
* Uncheck "Automatically adjust visual experience based on client performance"
* Uncheck "Enable rich client visual experience"
* Check "Use hardware graphics acceleration if available"


Tools -> Options -> Environment -> Startup
* Set "At startup" to "Show empty environment at startup"
* Uncheck "Download content every"


Tools -> Options -> Environment -> Synchronized settings (ignore if you are not logged in with Microsoft account)
* Uncheck "Synchronize settings across devices when signed into Visual Studio"


Tools -> Options -> Projects and Solutions -> Web Package Management
* Set "Restore on Project Open" to false (for Bower)
* Set "Restore on Save" to false (for Bower)
* Set "Restore on Project Open" to false (for NPM)
* Set "Restore on Save" to false (for NPM)

Tools -> Options -> Text Editor -> General
* Uncheck "Track changes"


Tools -> Options -> Text Editor -> All Languages -> Scroll Bars
* Uncheck "Show annotations over vertical scroll bar"


Tools -> Options -> Text Editor -> All Languages -> CodeLens
* Uncheck "Enable CodeLens"


Tools -> Options -> Text Editor -> C# -> Advanced
* Uncheck "Enable full solution analysis"


Tools -> Options -> Debugging
* Uncheck "Suppress JIT optimization on module load (Managed only)"
* Uncheck "Enable Edit and Continue"
* Uncheck "Enable JavaScript debugging for ASP.NET (Chrome and IE)"
* Uncheck "Enable Diagnostic Tools while debugging"

Tools -> Options -> Debugging -> Just-In-Time
* Uncheck "Script"


Tools -> Options -> IntelliTrace
* Uncheck "Enable IntelliTrace"

Disable Browser link

![](/assets/browser-link.png)




[Refrence](http://medium.com/burak-tasci/tweaking-the-environment-to-speed-up-visual-studio-79cd1920fed9)

Feedback and questions are welcome in the comments below.
