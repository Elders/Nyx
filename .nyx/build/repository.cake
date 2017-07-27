public class RepositoryPaths
{
    public RepositoryDirectories Directories { get; private set; }

    public static RepositoryPaths GetPaths(ICakeContext context, BuildParameters parameters)
    {
        if (context == null) throw new ArgumentNullException("context");

        var srcDir = (DirectoryPath)context.Directory("../src");
        var csProjPath = srcDir.Combine(parameters.Project);
        var csProjFile = csProjPath.Combine(parameters.Project + ".csproj");
        var lastGitTag = context.GitDescribe("../.", false, GitDescribeStrategy.Tags);
        var lastGitTagVersion = lastGitTag.Replace(parameters.NugetPackageName + "@", string.Empty);

        var lastReleasedVersion = context.ParseSemVer(lastGitTagVersion, true);

        return new RepositoryPaths
        {
            Directories = new RepositoryDirectories(csProjPath, csProjFile, lastGitTag, lastReleasedVersion)
        };
    }
}

public class RepositoryDirectories
{
    public DirectoryPath CsProjPath { get; private set; }
    public DirectoryPath CsProjFile { get; private set; }
    public string LastGitTag { get; private set; }
    public Semver.SemVersion LastReleasedVersion { get; private set; }

    public RepositoryDirectories(
        DirectoryPath csProjPath,
        DirectoryPath csProjFile,
        string lastGitTag,
        Semver.SemVersion lastReleasedVersion)
    {
        CsProjPath = csProjPath;
        CsProjFile = csProjFile;
        LastGitTag = lastGitTag;
        LastReleasedVersion = lastReleasedVersion;
    }
}
