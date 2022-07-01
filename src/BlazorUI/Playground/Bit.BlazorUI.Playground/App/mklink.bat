rd Components
rd Extensions
rd Models
rd Pages
rd Scripts
rd Services
rd Shared
rd Styles
del compilerconfig.json
del compilerconfig.json.defaults
del _Imports.razor
del Main.razor
del appsettings.json
del tsconfig.json
mklink /j "Components" "../Web/Components"
mklink /j "Extensions" "../Web/Extensions"
mklink /j "Models" "../Web/Models"
mklink /j "Pages" "../Web/Pages"
mklink /j "Scripts" "../Web/Scripts"
mklink /j "Services" "../Web/Services"
mklink /j "Shared" "../Web/Shared"
mklink /j "Styles" "../Web/Styles"
mklink /h "compilerconfig.json" "../Web/compilerconfig.json"
mklink /h "compilerconfig.json.defaults" "../Web/compilerconfig.json.defaults"
mklink /h "_Imports.razor" "../Web/_Imports.razor"
mklink /h "Main.razor" "../Web/App.razor"
mklink /h "appsettings.json" "../Web/appsettings.json"
mklink /h "tsconfig.json" "../Web/tsconfig.json"
cd wwwroot
rd fonts
rd images
rd scripts
mklink /j "fonts" "../../Web/wwwroot/fonts"
mklink /j "images" "../../Web/wwwroot/images"
mklink /j "scripts" "../../Web/wwwroot/scripts"