rd Extensions
rd Pages
rd Contracts
rd Implementations
del compilerconfig.json
del compilerconfig.json.defaults
del _Imports.razor
del Main.razor
del appsettings.json
mklink /j "Extensions" "../Web/Extensions"
mklink /j "Pages" "../Web/Pages"
mklink /j "Contracts" "../Web/Contracts"
mklink /j "Implementations" "../Web/Implementations"
mklink /h "compilerconfig.json" "../Web/compilerconfig.json"
mklink /h "compilerconfig.json.defaults" "../Web/compilerconfig.json.defaults"
mklink /h "_Imports.razor" "../Web/_Imports.razor"
mklink /h "Main.razor" "../Web/App.razor"
mklink /h "appsettings.json" "../Web/appsettings.json"
cd wwwroot
rd styles
mklink /j "styles" "../../Web/wwwroot/styles"