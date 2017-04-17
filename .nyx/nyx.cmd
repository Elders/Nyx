@echo off

@powershell -NoProfile -ExecutionPolicy unrestricted -Command "New-Item -ItemType directory -Path .nyx\ -Force; (New-Object System.Net.WebClient).DownloadFile('https://raw.githubusercontent.com/Elders/Nyx/master/.nyx/get-dependencies.cmd','.nyx\get-dependencies.cmd')"
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "New-Item -ItemType directory -Path .nyx\ -Force; (New-Object System.Net.WebClient).DownloadFile('https://raw.githubusercontent.com/Elders/Nyx/master/.nyx/get-pandora.cli.cmd','.nyx\get-pandora.cli.cmd')"
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "New-Item -ItemType directory -Path .nyx\ -Force; (New-Object System.Net.WebClient).DownloadFile('https://raw.githubusercontent.com/Elders/Nyx/master/.nyx/get-deploy-dependencies.cmd','.nyx\get-deploy-dependencies.cmd')"
