
## Create a nuget package

    dotnet pack .\TodoTemplate.nuget.csproj

## Install the nuget package

    dotnet new -i .\bin\debug\Bit.Tooling.Templates.TodoTemplate


## Create solution base on template

    dotnet new  Bit.TodoTemplate

## unistall package 
    dotnet new --uninstall Bit.Tooling.Templates.TodoTemplate
