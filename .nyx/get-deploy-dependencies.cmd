@echo off

SET ROOT=%ALLUSERSPROFILE%\Elders
SET NUGET=%ROOT%\NuGet\NuGet.exe
SET FAKE=bin\FAKE\tools\Fake.exe

echo Downloading FAKE...
%NUGET% "install" "FAKE" "-OutputDirectory" "bin" "-ExcludeVersion" "-Version" "4.50.0"

for /f %%i in ("%0") do set curpath=%%~dpi
cd /d %curpath%

xcopy ..\content . /D /Y /I /s
