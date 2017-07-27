public class BuildPaths
{
    public BuildDirectories Directories { get; private set; }

    public static BuildPaths GetPaths(ICakeContext context, string project, string configuration, string semVersion)
    {
        if (context == null) throw new ArgumentNullException("context");
        if (string.IsNullOrEmpty(configuration)) throw new ArgumentNullException("configuration");
        if (string.IsNullOrEmpty(semVersion)) throw new ArgumentNullException("semVersion");

        var binDir = (DirectoryPath)context.Directory("../.bin");
        var projectDir = binDir.Combine(project);
        var buildDir = (DirectoryPath)(projectDir + context.Directory("-") + context.Directory(configuration));
        var artifactsDir = (DirectoryPath)(buildDir.Combine("artifacts-") + context.Directory(semVersion));
        var artifactsBinDir = artifactsDir.Combine("bin");
        var artifactsBinNet45 = artifactsBinDir.Combine("net45");
        var artifactsBinNetCoreApp10 = artifactsBinDir.Combine("netcoreapp1.0");
        var testResultsDir = artifactsDir.Combine("test-results");
        var nugetRoot = artifactsDir.Combine("nuget");

        // Directories
        var buildDirectories = new BuildDirectories(
            artifactsDir,
            testResultsDir,
            nugetRoot,
            artifactsBinDir,
            artifactsBinNet45,
            artifactsBinNetCoreApp10);

        return new BuildPaths
        {
            Directories = buildDirectories
        };
    }
}

public class BuildDirectories
{
    public DirectoryPath Artifacts { get; private set; }
    public DirectoryPath TestResults { get; private set; }
    public DirectoryPath NugetRoot { get; private set; }
    public DirectoryPath ArtifactsBin { get; private set; }
    public DirectoryPath ArtifactsBinNet45 { get; private set; }
    public DirectoryPath ArtifactsBinNetCoreApp10 { get; private set; }
    public ICollection<DirectoryPath> ToClean { get; private set; }

    public BuildDirectories(
        DirectoryPath artifactsDir,
        DirectoryPath testResultsDir,
        DirectoryPath nugetRoot,
        DirectoryPath artifactsBinDir,
        DirectoryPath artifactsBinNet45,
        DirectoryPath artifactsBinNetCoreApp10
        )
    {
        Artifacts = artifactsDir;
        TestResults = testResultsDir;
        NugetRoot = nugetRoot;
        ArtifactsBin = artifactsBinDir;
        ArtifactsBinNet45 = artifactsBinNet45;
        ArtifactsBinNetCoreApp10 = artifactsBinNetCoreApp10;
        ToClean = new[] {
            Artifacts,
            TestResults,
            NugetRoot,
            ArtifactsBin,
            ArtifactsBinNet45,
            ArtifactsBinNetCoreApp10
        };
    }
}
