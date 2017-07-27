public class BuildReleaseNotes
{
    public Semver.SemVersion SemanticVersion { get; private set; }

    public static BuildReleaseNotes LoadReleaseNotes(ICakeContext context, BuildParameters parameters)
    {
        var rn = new BuildReleaseNotes();
        try
        {
            context.Information("Loading Release Notes...");
            var last = context.ParseReleaseNotes("C:/_/elders/Multithreading.Scheduler/src/Elders.Multithreading.Scheduler/Elders.Multithreading.Scheduler.rn.md");

            string pattern = @"(?<Version>\d+(\s*\.\s*\d+){0,3})(?<Release>-[a-z][0-9a-z-]*)";
            var regex = new System.Text.RegularExpressions.Regex(pattern);
            var result = regex.Match(last.RawVersionLine);
            if(result.Success)
            {
                rn.SemanticVersion = context.ParseSemVer(result.Value, true);
            }
            else
            {
                context.Information("Error loading Release Notes version!");
                context.Information("Last Release Notes {0}", last);
                context.Information("RawVersionLine: {0}", last.RawVersionLine);
            }
            return rn;
        }
        catch(Exception ex)
        {
            context.Error(ex);
            return rn;
        }
    }
}
