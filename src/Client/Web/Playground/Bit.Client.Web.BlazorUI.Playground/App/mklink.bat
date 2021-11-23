rd Components
rd Extensions
rd Models
rd Pages
rd Scripts
rd Services
rd Shared
rd Styles
mklink /j "Components" "../Web/Components"
mklink /j "Extensions" "../Web/Extensions"
mklink /j "Models" "../Web/Models"
mklink /j "Pages" "../Web/Pages"
mklink /j "Scripts" "../Web/Scripts"
mklink /j "Services" "../Web/Services"
mklink /j "Shared" "../Web/Shared"
mklink /j "Styles" "../Web/Styles"
cd wwwroot
rd fonts
rd images
rd scripts
rd styles
mklink /j "fonts" "../../Web/wwwroot/fonts"
mklink /j "images" "../../Web/wwwroot/images"
mklink /j "scripts" "../../Web/wwwroot/scripts"
mklink /j "styles" "../../Web/wwwroot/styles"