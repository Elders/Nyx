@echo off

SETLOCAL
	
SET FAKE=%LocalAppData%\FAKE\tools\Fake.exe
SET NYX=%LocalAppData%\Nyx\tools\build.fsx

CALL prebuild.cmd

SET TARGET="Build"
IF NOT [%1]==[] (set TARGET="%1")

SET SUMMARY="Elders.Nyx"
SET DESCRIPTION="Elders.Nyx"

%FAKE% %NYX% "target=%TARGET%" appName=Elders.Nyx appType=file appSummary=%SUMMARY% appDescription=%DESCRIPTION%
