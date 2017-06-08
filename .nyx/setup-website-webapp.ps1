param(
    [Parameter(mandatory=$true)]
    [string]$Company,

    [Parameter(mandatory=$true)]
    [string]$App,

    [Parameter(mandatory=$true)]
    [string]$Tenant,

    [Parameter(mandatory=$true)]
    [string]$AppHost,

    [Parameter(mandatory=$true)]
    [string]$Website,

    [string]$AppPool = ($Website + '_' + $AppHost),

    [Parameter(mandatory=$true)]
    [string]$WebsiteSource
    )
$cd = @{
       AllNodes = @(
       @{
           NodeName = 'localhost'
           Company = $Company
           App = $App
           Tenant = $Tenant
           Host = $AppHost
           WebsiteSource = $WebsiteSource
           Website = $Website
           AppPool = $AppPool.Replace("/", "_")
           WebsitePath  = [io.path]::combine([environment]::getfolderpath('CommonApplicationData'), $Company, $App, $Tenant, $AppHost.Replace("/", "\"))
       })
}
Configuration EldersWebApp
{
    Import-DscResource -ModuleName PSDesiredStateConfiguration, xWebAdministration, xCertificate, xComputerManagement
    Node $AllNodes.NodeName
    {
        # Configure the server to automatically corret configuration drift including reboots if needed.
        LocalConfigurationManager
        {
            ConfigurationMode = 'ApplyAndAutoCorrect'
            RebootNodeifNeeded = $node.RebootNodeifNeeded
        }

        Script CleanWebsiteDirectory
        {
            GetScript =
            {
                #needs to return hashtable.
            }
            SetScript =
            {
                $dest = $using:node.WebsitePath
                Remove-Item -Path $dest -Recurse -Force
            }
            TestScript =
            {
                $dest = $using:node.WebsitePath
                -Not(Test-Path -Path $dest)
            }
        }

        File WebsiteDirectory
        {
            SourcePath = $node.WebsiteSource
            DestinationPath = $node.WebsitePath
            Ensure = 'Present'
            Recurse = $true
            Type = 'Directory'
            Checksum = "SHA-256"
            Force = $true
            MatchSource = $true
            DependsOn = '[Script]CleanWebsiteDirectory'
        }

        xWebAppPool pool
        {
            Ensure                  = 'Present'
            Name                    = $node.AppPool
            State                   = 'Started'
            autoStart               = $true
            startMode               = 'AlwaysRunning'
            managedRuntimeVersion   = 'v4.0'
            managedPipelineMode     = 'Integrated'
            identityType            = 'NetworkService'
        }
        xWebApplication NewWebApplication 
        { 
            Name = $node.Host
            Website = $node.Website
            WebAppPool = $node.AppPool
            PhysicalPath = $node.WebsitePath
            Ensure = 'Present'
        }
    }
}

EldersWebApp -ConfigurationData $cd
Start-DscConfiguration -Path EldersWebApp -Wait -Verbose -Force
