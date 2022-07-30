if not exist "Pages/." mklink /j "Pages" "../Web/Pages"
if not exist "Models/." mklink /j "Models" "../Web/Models"
if not exist "Shared/." mklink /j "Shared" "../Web/Shared"
if not exist "Styles/." mklink /j "Styles" "../Web/Styles"
if not exist "Scripts/." mklink /j "Scripts" "../Web/Scripts"
if not exist "Services/." mklink /j "Services" "../Web/Services"
if not exist "Components/." mklink /j "Components" "../Web/Components"
if not exist "Extensions/." mklink /j "Extensions" "../Web/Extensions"

if not exist Main.razor mklink /h "Main.razor" "../Web/App.razor"
if not exist tsconfig.json mklink /h "tsconfig.json" "../Web/tsconfig.json"
if not exist _Imports.razor mklink /h "_Imports.razor" "../Web/_Imports.razor"
if not exist appsettings.json mklink /h "appsettings.json" "../Web/appsettings.json"
if not exist compilerconfig.json mklink /h "compilerconfig.json" "../Web/compilerconfig.json"
if not exist compilerconfig.json.defaults mklink /h "compilerconfig.json.defaults" "../Web/compilerconfig.json.defaults"

cd wwwroot

if not exist "images/." mklink /j "images" "../../Web/wwwroot/images"
if not exist "fonts/." mklink /j "fonts" "../../Web/wwwroot/fonts"