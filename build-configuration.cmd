@echo off

%FAKE% %NYX% "target=clean" -st

IF NOT [%1]==[] (set RELEASE_NUGETKEY="%1")

SET RELEASE_NOTES=RELEASE_NOTES.md
SET SUMMARY="Build script which only purpose is to provide out of the box solution for building .NET projects, creating nuget packages and publishing packages"
SET DESCRIPTION="Build script which only purpose is to provide out of the box solution for building .NET projects, creating nuget packages and publishing packages"

%FAKE% %NYX% appName=Elders.Nyx appReleaseNotes=%RELEASE_NOTES% appSummary=%SUMMARY% appDescription=%DESCRIPTION% nugetPackageName=Nyx nugetkey=%RELEASE_NUGETKEY% nugetserver=%RELEASE_TARGETSOURCE% appType=file