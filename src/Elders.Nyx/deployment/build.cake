#load "./build/parameters.cake";

#addin "Cake.Git"
#addin "Cake.SemVer"

#tool "nuget:https://www.nuget.org/api/v2?package=GitVersion.CommandLine&version=4.0.0-beta0012"

BuildParameters parameters = BuildParameters.GetParameters(Context);

Setup(context =>
{
    parameters.Initialize(context);

    Information("Building version {1} of {0} ({2}, {3}) using version {4} of Cake. (IsTagged: {5})",
        parameters.Project,
        parameters.Version.SemVersion,
        parameters.Configuration,
        parameters.Target,
        parameters.Version.CakeVersion,
        parameters.IsTagged);

    Information("========================================");
    Information("   GitVersion version:\t{0}", parameters.Version.SemVersion);
    Information(" ReleaseNotes version:\t{0}", parameters.ReleaseNotes.SemanticVersion);
    Information("Last released version:\t{0}", parameters.RepositoryPaths.Directories.LastReleasedVersion);
    Information("========================================");

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
    .Does(() =>
{
    // Build the solution.
    var path = MakeAbsolute(parameters.RepositoryPaths.Directories.CsProjFile);
    DotNetCoreBuild(path.FullPath, new DotNetCoreBuildSettings()
    {
        Configuration = parameters.Configuration,
        OutputDirectory = parameters.Paths.Directories.ArtifactsBin,
        ArgumentCustomization = args => args
            .Append("/p:Version={0}", parameters.Version.SemVersion)
            .Append("/p:AssemblyVersion={0}", parameters.Version.Version)
            .Append("/p:FileVersion={0}", parameters.Version.Version)
    });
});

Task("Create-NuGet-Packages")
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
    });
});

Task("Release")
    .IsDependentOn("Create-NuGet-Packages")
    .WithCriteria(() => parameters.CanRelease)
    .WithCriteria(() => parameters.CanPublishNuGet)
    .Does(() =>
{
    var apiKey = EnvironmentVariable("RELEASE_NUGETKEY");
    if(string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("Could not resolve NuGet API key.");

    var apiUrl = EnvironmentVariable("nugetserver");
    if(string.IsNullOrEmpty(apiUrl))
        apiUrl = "https://www.nuget.org/api/v2/package";

    var pkg = parameters.Packages.All.First();
    NuGetPush(pkg.PackagePath, new NuGetPushSettings {
        ApiKey = apiKey,
        Source = apiUrl
    });

    string tag = parameters.NugetPackageName + "@" + parameters.Version.SemVersion;
    GitTag("../.", tag);
    ExecuteCommand("git push origin release-1.2.0");
});

Task("Default").IsDependentOn("Release");

RunTarget(target);

void ExecuteCommand(string command)
 {
     Information(command);
     int exitCode;
     System.Diagnostics.ProcessStartInfo processInfo;
     System.Diagnostics.Process process;

     processInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c " + command);
     processInfo.CreateNoWindow = true;
     processInfo.UseShellExecute = false;
     // *** Redirect the output ***
     processInfo.RedirectStandardError = true;
     processInfo.RedirectStandardOutput = true;

     process = System.Diagnostics.Process.Start(processInfo);
     process.OutputDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) =>
            Information("[output]" + e.Data);
     process.BeginOutputReadLine();

     process.ErrorDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) =>
            Information("[error]" + e.Data);
     process.BeginErrorReadLine();
     process.WaitForExit();

     Information("ExitCode: " + process.ExitCode);
     process.Close();
  }
