
## Create a nuget package

    dotnet pack .\TodoTemplate.nuget.csproj

## Install the nuget package

    dotnet new -i .\Bit.Tooling.Templates.TodoTemplate


## Create solution base on template

    dotnet new  Bit.TodoTemplate

## unistall package 
    dotnet new --uninstall Bit.Tooling.Templates.TodoTemplate

     dotnet new --uninstall Bit.TodoTemplate


## junction file problem
     mklink run
     mklink problem on delate not empty directory use switch /S /Q 
        example rd  images /S /Q

        "postActions": [{
          "actionId": "3A7C4B45-1F5D-4A30-959A-51B88E82B5D2",
          "args": {
            "executable": "setup.cmd",
            "args": "",
            "redirectStandardOutput": false,
            "redirectStandardError": false
          },
          "manualInstructions": [{
             "text": "Run 'setup.cmd'"
          }],
          "continueOnError": false,
          "description ": "setups the project by calling setup.cmd"
        }]


          postActions (optional)

Defines an ordered list of actions to perform after template generation. The post action information is provided to the creation broker, to act on as appropriate.

See the Post Action Registry for existing post actions.

https://github.com/dotnet/templating/wiki/Post-Action-Registry


## Solve #if #else

"copyOnly": [ "Directory.Build.props" ]
https://github.com/dotnet/templating/wiki/Conditional-processing-and-comment-syntax


#if DEBUG
Comet.Reload.Init();
#endif
//-:cnd:noEmit
#if DEBUG
Xamarin.Calabash.Start();
#endif
//+:cnd:noEmit

