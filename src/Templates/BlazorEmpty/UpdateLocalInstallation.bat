dotnet pack -c Release -p:ReleaseVersion=0.0.0 -p:PackageVersion=0.0.0 && cd bin\Release && dotnet new uninstall Bit.BlazorEmpty && dotnet new install .\Bit.BlazorEmpty.0.0.0.nupkg && cd ..\..