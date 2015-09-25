param($installPath, $toolsPath, $package)

$nuget = $Env:LocalAppData + "\NuGet\NuGet.exe"
Write-Host "Using NuGet from: " + $nuget

Write-Host "Downloading latest version of FAKE..."
& $nuget install FAKE -OutputDirectory D:\ -Version 4.4.4
	
Write-Host "Downloading latest version of Nuget.Core..."
& $nuget install Nuget.Core -OutputDirectory D:\ -Version 2.8.6