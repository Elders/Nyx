#load "./build/parameters.cake";
#load "./build/os.cake";

//#addin "nuget:https://www.nuget.org/api/v2?package=Cake.Git&version=0.19.0"
#addin "nuget:https://www.nuget.org/api/v2?package=Cake.SemVer&version=3.0.0"
#addin "nuget:https://www.nuget.org/api/v2?package=semver&version=2.0.4"

#tool "nuget:https://www.nuget.org/api/v2?package=GitVersion.CommandLine&version=3.6.1"
#tool "nuget:?package=WiX"

BuildParameters parameters = BuildParameters.GetParameters(Context);

Setup(context =>
{
    parameters.Initialize(context);

    Information("================================================================================================");
    Information("Building version {1} of {0} ({2}, {3}) using version {4} of Cake. (IsApp: {5})",
        parameters.Project,
        parameters.Version.SemVersion,
        parameters.Configuration,
        parameters.Target,
        parameters.Version.CakeVersion,
        parameters.IsApp,
        parameters.IsTagged);

    Information("------------------------------------------------------------------------------------------------");
    Information("   GitVersion version:\t{0}", parameters.Version.SemVersion);
    Information(" ReleaseNotes version:\t{0}", parameters.ReleaseNotes.SemanticVersion);
    Information("Last released version:\t{0}", parameters.RepositoryPaths.Directories.LastReleasedVersion);
    Information("================================================================================================");
});

var target = Argument("target", "Default");

Task("Clean").Does(() => CleanDirectories(parameters.Paths.Directories.ToClean));

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        var projects = GetFiles("./src/**/*.csproj");
        foreach(var project in projects)
        {
            DotNetCoreRestore(project.FullPath, new DotNetCoreRestoreSettings());
        }
    });

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .WithCriteria(() => parameters.IsNetFull == false)
    .Does(() =>
    {
        var path = MakeAbsolute(parameters.RepositoryPaths.Directories.CsProjFile);
        DotNetCoreBuild(path.FullPath, new DotNetCoreBuildSettings()
        {
            Configuration = parameters.Configuration,
            OutputDirectory = parameters.Paths.Directories.ArtifactsBinNetCoreApp,
            ArgumentCustomization = args => args
                .Append("/p:Version={0}", parameters.Version.SemVersion)
                .Append("/p:AssemblyVersion={0}", parameters.Version.Version)
                .Append("/p:FileVersion={0}", parameters.Version.Version)
                .Append("/p:SourceLinkCreate=true")
        });
    });

Task("Publish-As-Msi")
    .IsDependentOn("Build")
    .WithCriteria(() => parameters.IsMsi)
    .Does(() =>
    {
        var path = MakeAbsolute(parameters.RepositoryPaths.Directories.CsProjFile);
        DotNetCorePublish(path.FullPath, new DotNetCorePublishSettings()
        {
            Configuration = parameters.Configuration,
            OutputDirectory = parameters.Paths.Directories.ArtifactsBinNetCoreAppPublishTemp,
            ArgumentCustomization = args => args
                .Append("/p:Version={0}", parameters.Version.SemVersion)
                .Append("/p:AssemblyVersion={0}", parameters.Version.Version)
                .Append("/p:FileVersion={0}", parameters.Version.Version)
                .Append("/p:SourceLinkCreate=true")
        });


        Information("================================================================================================");
        Information("Published bits path: {0}", parameters.Paths.Directories.ArtifactsBinNetCoreAppPublishTemp.FullPath);
        var filePath = File("App.wxs");
        WiXHeat(parameters.Paths.Directories.ArtifactsBinNetCoreAppPublishTemp.FullPath, filePath, WiXHarvestType.Dir, new HeatSettings {
            ArgumentCustomization = args => args.Append("-var var.Source"),
            ComponentGroupName = "References",
            Verbose = true,
            NoLogo = true,
            SuppressRootDirectory = true,
            GenerateGuid = true,
            GenerateGuidWithoutBraces = true,
            SuppressCom = true,
            SuppressRegistry = true,
            Transform = parameters.RepositoryPaths.Directories.WixHeatTransformXslt.FullPath
        });

        WiXCandle("App.wxs", new CandleSettings {
            ArgumentCustomization = args => args
                .Append("-dSource=" + parameters.Paths.Directories.ArtifactsBinNetCoreAppPublishTemp.FullPath)
                .Append("-ext WixUtilExtension"),
            Architecture = Architecture.X64,
            Verbose = true
        });

        WiXLight("App.wixobj", new LightSettings {
            ArgumentCustomization = args => args
                .Append("-dSource=" + parameters.Paths.Directories.ArtifactsBinNetCoreAppPublishTemp.FullPath)
                .Append("-ext WixUIExtension")
                .Append("-ext WixUtilExtension"),
            RawArguments = "-pedantic -v",
            OutputFile = parameters.Paths.Directories.ArtifactsBinNetCoreAppPublish + "/" + parameters.Project + ".msi"
        });

        DeleteDirectory(parameters.Paths.Directories.ArtifactsBinNetCoreAppPublishTemp.FullPath, recursive:true);
    });

Task("Publish-As-App")
    .IsDependentOn("Build")
    .WithCriteria(() => parameters.IsApp)
    .Does(() =>
    {
        var path = MakeAbsolute(parameters.RepositoryPaths.Directories.CsProjFile);
        DotNetCorePublish(path.FullPath, new DotNetCorePublishSettings()
        {
            Configuration = parameters.Configuration,
            OutputDirectory = parameters.Paths.Directories.ArtifactsBinNetCoreAppPublish,
            ArgumentCustomization = args => args
                .Append("/p:Version={0}", parameters.Version.SemVersion)
                .Append("/p:AssemblyVersion={0}", parameters.Version.Version)
                .Append("/p:FileVersion={0}", parameters.Version.Version)
                .Append("/p:SourceLinkCreate=true")
        });
    });

Task("Build-Image")
    .IsDependentOn("Publish-As-App")
    .WithCriteria(context => parameters.CanRelease)
    .WithCriteria(context => parameters.CanPublishDocker)
    .Does(context => 
    {
        var dockerRepo = EnvironmentVariable("DOCKER_REPO");
        string tag = parameters.NugetPackageName + "@" + parameters.Version.SemVersion;
        string repoTag = dockerRepo + ":" + parameters.Version.SemVersion;
        string dockercmd = "docker build ../docker/" + dockerRepo + " --tag " + repoTag;
        
        OS.ExecuteCommand(context, "cp -R " + parameters.Paths.Directories.ArtifactsBinNetCoreAppPublish + "/* ../docker/" + dockerRepo + "/contents/");
        OS.ExecuteCommand(context, dockercmd);
        OS.ExecuteCommand(context, "docker push " + repoTag);
        OS.ExecuteCommand(context, "git tag " + tag);
        OS.ExecuteCommand(context, "git push --tags");
        
    });
    

Task("Publish")
    .IsDependentOn("Publish-As-App")
    .IsDependentOn("Publish-As-Msi")
    .IsDependentOn("Build-Image")
    .Does(() =>
    {
        Information("Publishing...");
    });

Task("Create-Lib-NuGet-Packages")
    .WithCriteria(() => parameters.IsLib)
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetCorePack(parameters.RepositoryPaths.Directories.CsProjFile.ToString(), new DotNetCorePackSettings {
            Configuration = parameters.Configuration,
            OutputDirectory = parameters.Paths.Directories.NugetRoot,
            NoBuild = false,
            ArgumentCustomization = args => args
                .Append("/p:Version={0}", parameters.Version.SemVersion)
                .Append("/p:AssemblyVersion={0}", parameters.Version.Version)
                .Append("/p:FileVersion={0}", parameters.Version.Version)
                .Append("/p:SourceLinkCreate=true")
        });
    });

Task("Create-App-NuGet-Packages")
    .IsDependentOn("Publish")
    .WithCriteria(() => parameters.IsApp || parameters.IsMsi)
    .Does(() =>
    {
        var deployment = MakeAbsolute(parameters.RepositoryPaths.Directories.DeploymentPath).FullPath;
        var files = new DirectoryInfo(deployment).GetFiles().Select(f=> new NuSpecContent {Source = f.FullName, Target = "tools"}).ToList();
        files.Add(new NuSpecContent {Source = "**", Target = "content"});

        NuGetPack(new NuGetPackSettings {
                                     Id                      = parameters.NugetPackageName,
                                     Version                 = parameters.Version.SemVersion.ToString(),
                                     Title                   = parameters.NugetPackageName,
                                     Description             = "The description of the package",
                                     Authors                 = new[] {"John Doe"},
                                     RequireLicenseAcceptance= false,
                                     Symbols                 = false,
                                     NoPackageAnalysis       = true,
                                     Files                   = files.ToArray(),
                                     BasePath                = parameters.Paths.Directories.ArtifactsBinNetCoreAppPublish,
                                     OutputDirectory         = parameters.Paths.Directories.NugetRoot
                                 });
    });

Task("Pack")
    .IsDependentOn("Create-Lib-NuGet-Packages")
    .IsDependentOn("Create-App-NuGet-Packages")
    .Does(() =>
    {
        Information("Packing...");
    });



Task("Release")
    .IsDependentOn("Pack")
    .WithCriteria(context => parameters.CanRelease)
    .WithCriteria(context => parameters.CanPublishNuGet)
    .Does(context =>
    {
        var apiUrl = EnvironmentVariable("nugetserver");
        if(string.IsNullOrEmpty(apiUrl))
            apiUrl = "https://www.nuget.org/api/v2/package";

        var password = EnvironmentVariable("RELEASE_NUGET_PASSWORD");

        var userName = EnvironmentVariable("RELEASE_NUGET_USERNAME");

        var sourceName = EnvironmentVariable("RELEASE_NUGET_SOURCENAME");

        var apiKey = EnvironmentVariable("RELEASE_NUGETKEY");

        var pkg = parameters.Packages.All.First();
    
        if (string.IsNullOrEmpty(userName) == false && string.IsNullOrEmpty(password) == false && string.IsNullOrEmpty(sourceName) == false && string.IsNullOrEmpty(apiUrl) == false)
        {
            if(NuGetHasSource(apiUrl))
            {
                NuGetRemoveSource(sourceName, apiUrl);
            }

            NuGetAddSource(sourceName, apiUrl, new NuGetSourcesSettings
                {  
                    UserName = userName,
                    Password = password,
                });

            NuGetPush(pkg.PackagePath, new NuGetPushSettings {
                ApiKey = apiKey,
                Source = sourceName
            });
        }
        else if(string.IsNullOrEmpty(apiKey) == false)
        {
            NuGetPush(pkg.PackagePath, new NuGetPushSettings {
                ApiKey = apiKey,
                Source = apiUrl
            });
        }
        else
        {
            throw new InvalidOperationException("Could not find credentials to push to NuGet feed.");
        }

        string tag = parameters.NugetPackageName + "@" + parameters.Version.SemVersion;
        OS.ExecuteCommand(context, "git tag " + tag);
        OS.ExecuteCommand(context, "git push --tags");
    });

Task("Default").IsDependentOn("Release");

RunTarget(target);
