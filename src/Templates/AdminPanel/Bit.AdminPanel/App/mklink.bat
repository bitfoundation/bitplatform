rd Pages /S /Q
rd Shared /S /Q
rd Styles /S /Q
rd Scripts /S /Q
rd Services /S /Q
rd Components /S /Q
rd Extensions /S /Q

mklink /j "Pages" "../Web/Pages"
mklink /j "Shared" "../Web/Shared"
mklink /j "Styles" "../Web/Styles"
mklink /j "Scripts" "../Web/Scripts"
mklink /j "Services" "../Web/Services"
mklink /j "Components" "../Web/Components"
mklink /j "Extensions" "../Web/Extensions"


del Main.razor
del tsconfig.json
del _Imports.razor
del appsettings.json
del compilerconfig.json
del compilerconfig.json.defaults

mklink /h "Main.razor" "../Web/App.razor"
mklink /h "tsconfig.json" "../Web/tsconfig.json"
mklink /h "_Imports.razor" "../Web/_Imports.razor"
mklink /h "appsettings.json" "../Web/appsettings.json"
mklink /h "compilerconfig.json" "../Web/compilerconfig.json"
mklink /h "compilerconfig.json.defaults" "../Web/compilerconfig.json.defaults"

cd wwwroot

rd images /S /Q

mklink /j "images" "../../Web/wwwroot/images"

del service-worker.js
del service-worker.published.js

mklink /h "service-worker.js" "../../Web/wwwroot/service-worker.js"
mklink /h "service-worker.published.js" "../../Web/wwwroot/service-worker.published.js"