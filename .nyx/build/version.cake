public class BuildVersion
{
    public string Version { get; private set; }
    public Semver.SemVersion SemVersion { get; private set; }
    public string DotNetAsterix { get; private set; }
    public string Milestone { get; private set; }
    public string CakeVersion { get; private set; }

    public static BuildVersion Calculate(ICakeContext context, RepositoryPaths repositoryPaths)
    {
        if (context == null) throw new ArgumentNullException("context");
        if (repositoryPaths == null) throw new ArgumentNullException("repositoryPaths");

        string version = null;
        Semver.SemVersion semVersion = null;
        string milestone = null;

        if(System.IO.File.Exists(repositoryPaths.Directories.CsProjPath.Combine("gitversion.yml").ToString()) == false)
        {
            context.Information("Creating gitversion.yml...");
        }

        context.Information("Calculating Semantic Version...");

        if(context.IsRunningOnUnix())
        {
            string[] lines = {
            "<configuration>",
            "   <dllmap os=\"linux\" cpu=\"x86-64\" wordsize=\"64\" dll=\"git2-381caf5\" target=\"/usr/lib/x86_64-linux-gnu/libgit2.so.24\" />",
            "   <dllmap os=\"osx\" cpu=\"x86,x86-64\" dll=\"git2-381caf5\" target=\"lib/osx/libgit2-381caf5.dylib\" />",
            "</configuration>"};
            System.IO.File.WriteAllLines(@"/Elders/tools/GitVersion.CommandLine.3.6.1/tools/LibGit2Sharp.dll.config", lines);
        }

        GitVersion assertedVersions = context.GitVersion(new GitVersionSettings
        {
            OutputType = GitVersionOutput.Json,
            WorkingDirectory = repositoryPaths.Directories.CsProjPath
        });

        version = $"{assertedVersions.Major}.0.0.0";
        semVersion = context.ParseSemVer(assertedVersions.NuGetVersion, true);
        milestone = string.Concat("v", version);

        context.Information("Calculated Semantic Version: {0}", semVersion);

        if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(semVersion.ToString()))
        {
            context.Information("Fetching verson from first SolutionInfo...");
            version = ReadSolutionInfoVersion(context);
            semVersion = context.ParseSemVer(version, true);
            milestone = string.Concat("v", version);
        }

        var cakeVersion = typeof(ICakeContext).Assembly.GetName().Version.ToString();

        return new BuildVersion
        {
            Version = version,
            SemVersion = semVersion,
            DotNetAsterix = semVersion.Prerelease,
            Milestone = milestone,
            CakeVersion = cakeVersion
        };
    }

    public static string ReadSolutionInfoVersion(ICakeContext context)
    {
        var solutionInfo = context.ParseAssemblyInfo("./src/SolutionInfo.cs");
        if (!string.IsNullOrEmpty(solutionInfo.AssemblyVersion))
        {
            return solutionInfo.AssemblyVersion;
        }
        throw new CakeException("Could not parse version.");
    }
}
