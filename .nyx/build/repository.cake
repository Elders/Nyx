public class RepositoryPaths
{
    public RepositoryDirectories Directories { get; private set; }

    public static RepositoryPaths GetPaths(ICakeContext context, BuildParameters parameters)
    {
        if (context == null) throw new ArgumentNullException("context");

        var srcDir = (DirectoryPath)context.Directory("../src");
        var csProjPath = srcDir.Combine(parameters.Project);
        var csProjFile = csProjPath.Combine(parameters.Project + ".csproj");

        string globPattern = parameters.NugetPackageName + "@" + "*[0-9].*[0-9].*[0-9]*";
        var lastGitTag = ExecuteCommand(context, "git describe --tags --match " + globPattern);

        var lastGitTagVersion = lastGitTag.Replace(parameters.NugetPackageName + "@", string.Empty);
        var lastReleasedVersion = context.ParseSemVer(lastGitTagVersion, true);

        return new RepositoryPaths
        {
            Directories = new RepositoryDirectories(csProjPath, csProjFile, lastGitTag, lastReleasedVersion)
        };
    }

    static string ExecuteCommand(ICakeContext context, string command)
    {
        string output = "";
        context.Information(command);
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
        process.OutputDataReceived += ((object sender, System.Diagnostics.DataReceivedEventArgs e) =>
        {
            if (!String.IsNullOrEmpty(e.Data))
                output = e.Data;
        });

        process.BeginOutputReadLine();

        process.ErrorDataReceived += ((object sender, System.Diagnostics.DataReceivedEventArgs e) =>
        {
            if (!String.IsNullOrEmpty(e.Data))
                context.Information("[error]" + e.Data);
        });
        process.BeginErrorReadLine();
        process.WaitForExit();

        context.Information("ExitCode: " + process.ExitCode);
        process.Close();

        context.Information("Output is: " + output);
        return output;
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
