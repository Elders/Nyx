@echo off

SETLOCAL

SET NUGET=%LocalAppData%\NuGet\NuGet.exe

echo Downloading latest version of FAKE...
%NUGET% "install" "FAKE" "-OutputDirectory" "$toolsPath" "-Version" "4.4.4"
	
echo Downloading latest version of Nuget.Core...
%NUGET% "install" "Nuget.Core" "-OutputDirectory" "$toolsPath" "-Version" "2.8.6"