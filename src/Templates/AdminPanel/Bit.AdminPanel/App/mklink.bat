if not exist "Pages/." mklink /j "Pages" "../Web/Pages"
if not exist "Shared/." mklink /j "Shared" "../Web/Shared"
if not exist "Styles/." mklink /j "Styles" "../Web/Styles"
if not exist "Scripts/." mklink /j "Scripts" "../Web/Scripts"
if not exist "Services/." mklink /j "Services" "../Web/Services"
if not exist "Components/." mklink /j "Components" "../Web/Components"
if not exist "Extensions/." mklink /j "Extensions" "../Web/Extensions"

if not exist Main.razor mklink "Main.razor" "%cd%/../Web/App.razor"
if not exist tsconfig.json mklink "tsconfig.json" "%cd%/../Web/tsconfig.json"
if not exist _Imports.razor mklink "_Imports.razor" "%cd%/../Web/_Imports.razor"
if not exist appsettings.json mklink "appsettings.json" "%cd%/../Web/appsettings.json"
if not exist compilerconfig.json mklink "compilerconfig.json" "%cd%/../Web/compilerconfig.json"
if not exist compilerconfig.json.defaults mklink "compilerconfig.json.defaults" "%cd%/../Web/compilerconfig.json.defaults"

cd wwwroot

if not exist "images/." mklink /j "images" "../../Web/wwwroot/images"

if not exist service-worker.js mklink "service-worker.js" "%cd%/../../Web/wwwroot/service-worker.js"
if not exist service-worker.published.js mklink "service-worker.published.js" "%cd%/../../Web/wwwroot/service-worker.published.js"