:: This batch file can completely clean the source code and remove the extra files (even better than the Clean command of the Visual Studio).
:: Close the IDE (Visual Studio, and ...) before running it.
:: These commands work only on Windows.
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S bin') DO RMDIR /S /Q "%%G"
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S node_modules') DO RMDIR /S /Q "%%G"
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S Packages') DO RMDIR /S /Q "%%G"
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S .vs') DO RMDIR /S /Q "%%G"
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S TestResults') DO RMDIR /S /Q "%%G"
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S AppPackages') DO RMDIR /S /Q "%%G"
DEL /Q /F /S "Resource.designer.cs"
DEL /Q /F /S "*.csproj.user"
DEL /Q /F /S "*.razor.css"