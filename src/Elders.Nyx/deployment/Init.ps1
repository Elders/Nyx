param($installPath, $toolsPath, $package)

$nuget = $Env:LocalAppData + "\NuGet\NuGet.exe"
Write-Host "Using NuGet from: " $nuget

Write-Host "Installing dependencies in " $toolsPath

Write-Host "Downloading latest version of FAKE..."
& $nuget install FAKE -OutputDirectory $toolsPath -Version 4.4.4
	
Write-Host "Downloading latest version of Nuget.Core..."
& $nuget install Nuget.Core -OutputDirectory $toolsPath -Version 2.8.6