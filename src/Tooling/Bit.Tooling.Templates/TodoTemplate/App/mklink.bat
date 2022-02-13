rd Components
rd Extensions
rd Pages
rd Services
rd Shared

mklink /j "Components" "../Web/Components"
mklink /j "Extensions" "../Web/Extensions"
mklink /j "Pages" "../Web/Pages"
mklink /j "Services" "../Web/Services"
mklink /j "Shared" "../Web/Shared"

del compilerconfig.json
del compilerconfig.json.defaults
del _Imports.razor
del Main.razor
del appsettings.json

mklink /h "compilerconfig.json" "../Web/compilerconfig.json"
mklink /h "compilerconfig.json.defaults" "../Web/compilerconfig.json.defaults"
mklink /h "_Imports.razor" "../Web/_Imports.razor"
mklink /h "Main.razor" "../Web/App.razor"
mklink /h "appsettings.json" "../Web/appsettings.json"

cd wwwroot

rd images
rd scripts
rd styles

mklink /j "images" "../../Web/wwwroot/images"
mklink /j "scripts" "../../Web/wwwroot/scripts"
mklink /j "styles" "../../Web/wwwroot/styles"

del service-worker.js
mklink /h "service-worker.js" "../../Web/wwwroot/service-worker.js"