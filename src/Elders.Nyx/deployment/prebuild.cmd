@echo off

SETLOCAL

SET NUGET=%LocalAppData%\NuGet\NuGet.exe
SET NYX=%LocalAppData%\Nyx\tools

echo Downloading latest version of FAKE...
%NUGET% "install" "FAKE" "-OutputDirectory" "." "-ExcludeVersion" "-Version" "4.4.4"

echo Downloading latest version of NuGet.Core...
%NUGET% "install" "NuGet.Core" "-OutputDirectory" "." "-ExcludeVersion" "-Version" "2.8.6"