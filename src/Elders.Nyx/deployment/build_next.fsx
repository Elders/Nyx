#I @"../../FAKE/tools/"
#r @"../../FAKE/tools/FakeLib.dll"
#r @"../../FAKE/tools/Fake.Deploy.Lib.dll"
#r @"../../FAKE/tools/ICSharpCode.SharpZipLib.dll"

open System
open System.Collections.Generic
open System.IO
open Fake
open Fake.Git
open Fake.FSharpFormatting
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.ProcessHelper
open Fake.FileHelper
open Fake.Json
open System.Text.RegularExpressions
open ICSharpCode.SharpZipLib.Zip
open ICSharpCode.SharpZipLib.Core

type GitVersion = {
    Major : int
    Minor : string
    Patch : string
    PreReleaseTag : string
    PreReleaseTagWithDash : string
    BuildMetaData : string
    BuildMetaDataPadded : string
    FullBuildMetaData : string
    MajorMinorPatch : string
    SemVer : string
    LegacySemVer : string
    LegacySemVerPadded : string
    AssemblySemVer : string
    FullSemVer : string
    InformationalVersion : string
    BranchName : string
    Sha : string
    NuGetVersionV2 : string
    NuGetVersion : string
    CommitDate : string
}

type AppInfo(name, summary, description, authors) =
    let copyright = "Copyright © " + DateTime.UtcNow.Year.ToString()
    member this.Name = name
    member this.Sumarry = summary
    member this.Description = description
    member this.Authors = authors
    member this.UpdateAssemblyInfo(assemblyInfoFile, getGitVersion) =
        CreateCSharpAssemblyInfo assemblyInfoFile
           [Attribute.Title name
            Attribute.Description description
            Attribute.ComVisible false
            Attribute.Product name
            Attribute.Copyright copyright
            Attribute.Version getGitVersion.AssemblySemVer
            Attribute.FileVersion (getGitVersion.MajorMinorPatch + ".0")
            Attribute.InformationalVersion getGitVersion.InformationalVersion]

type System.String with member x.endswith (comp:System.StringComparison) str = x.EndsWith(str, comp)

type Repository(appInfo:AppInfo, appType, sourceDir, releaseNotes) =
    let appName = appInfo.Name
    let getReleaseNotes =
        printfn "[nyx] Loading release notes from %s" releaseNotes
        LoadReleaseNotes releaseNotes

    let getGitVersion =
        let gitversionconfig = sourceDir @@ appName @@ "gitversion.yml"
        let shouldCreateGitVersionConfig = gitversionconfig |> File.Exists |> not
        if shouldCreateGitVersionConfig then
            printfn "[nyx] gitversion.yml file was not found. Automatically creating %s" gitversionconfig
            let gitVersionConfigContent = "tag-prefix: '" + getBuildParamOrDefault "nugetPackageName" appName + "@'"
            WriteStringToFile false gitversionconfig gitVersionConfigContent
        let workingDir = sourceDir @@ appName
        printfn "[nyx] GitVersion working dir is %s" workingDir
        let gitversion = environVar "GITVERSION"
        let result = ExecProcessAndReturnMessages (fun info ->
            info.FileName <- gitversion
            info.WorkingDirectory <- workingDir
            info.Arguments <- "/output json") (TimeSpan.FromMinutes 5.0)
        if result.ExitCode <> 0 then failwithf "'GitVersion.exe' returned with a non-zero exit code"
        let jsonResult = System.String.Concat(result.Messages)
        jsonResult |> deserialize<GitVersion>

    let appDir = sourceDir @@ appInfo.Name
    let assemblyInfoFile = appDir @@ "Properties/AssemblyInfo.cs"


    member this.AppInfo = appInfo
    member this.AppName = appName
    member this.AppType = appType
    member this.SourceDir = sourceDir
    member this.AppDir = appDir
    member this.AssemblyInfoFile = this.AppDir @@ "Properties/AssemblyInfo.cs"
    member this.ReleaseNotes = getReleaseNotes
    member this.GitVersion = getGitVersion
    member this.DeploymentDir = this.AppDir @@ "deployment"

type Artifacts(appName, artifactsDir) =
    member this.BuildDir  = artifactsDir @@ appName
    member this.WebsiteDir = this.BuildDir @@ "_publishedWebsites" @@ appName
    member this.MsiDir = this.BuildDir @@ "_publishedMsi" @@ appName
    member this.ToolDir = this.BuildDir @@ "_tools" @@ appName
    member this.IntTestsDir = this.BuildDir @@ "_publishedTests" @@ appName
    member this.Clean() = CleanDirs [this.BuildDir;]

type RepositoryFactory() =
    let appInfo = new AppInfo(
                    getBuildParamOrDefault "appName" "",
                    getBuildParamOrDefault "appSummary" "",
                    getBuildParamOrDefault "appDescription" "",
                    ["Elders";])

    let conventionAppType = match appInfo.Name.ToLower() with
                              | EndsWith "msi" -> "msi"
                              | EndsWith "cli" -> "cli"
                              | EndsWith "tests" -> "tests"
                              | _ -> "lib"

    let appType = getBuildParamOrDefault "appType" conventionAppType
    let sourceDir = ".\\src"
    let appDir = sourceDir @@ appInfo.Name
    let releaseNotes =
        let appReleaseNotesPath = appDir @@ appInfo.Name + ".rn.md"
        let legacyReleaseNotesPath = appDir @@ "RELEASE_NOTES.md"
        let activeReleaseNotesPath = match File.Exists legacyReleaseNotesPath with
                                        | true -> legacyReleaseNotesPath
                                        | _ -> appReleaseNotesPath
        let releaseNotesPath = getBuildParamOrDefault "appReleaseNotes" activeReleaseNotesPath

        let defaultReleaseNotesContent = "#### 0.1.0 - " + System.DateTime.Now.ToString("dd.MM.yyyy") + "
* Initial Release"
        let shouldCreateReleaseNotesFile = releaseNotesPath |> File.Exists |> not
        if shouldCreateReleaseNotesFile then
            printfn "[nyx] Release Notes file was not found. Automatically creating %s" releaseNotesPath
            WriteStringToFile false releaseNotesPath defaultReleaseNotesContent
        releaseNotesPath
    member this.GetRepository = new Repository(appInfo, appType, sourceDir, releaseNotes)

type EldersNuget(repository:Repository) =
    let nuget = environVar "NUGET"
    let nugetPackageName = getBuildParamOrDefault "nugetPackageName" repository.AppName
    let nugetWorkDir = "./bin/nuget" @@ repository.AppName
    let nugetLibDir = nugetWorkDir @@ "lib" @@ "net45-full"
    let nugetToolsDir = nugetWorkDir @@ "tools"
    let nugetContentDir = nugetWorkDir @@ "content"
    let canPublishPackage gitVersion =
        let version = gitVersion.NuGetVersionV2
        let prerelease = if version.Contains("beta") then " -prerelease" else ""
        let args = "/c " + nuget + " list packageid:" + nugetPackageName + prerelease;
        let result = ExecProcessAndReturnMessages (fun info ->
                                info.FileName <- "cmd"
                                info.Arguments <- args) (TimeSpan.FromMinutes 20.0)
        if result.ExitCode <> 0 then failwithf "%s returned with a non-zero exit code" args
        printfn "[nyx] Found %i packages." result.Messages.Count
        Console.WriteLine "------------------------"

        result.Messages
        |> Seq.map(fun split -> Regex.Split(split, " "))
        |> Seq.map(fun q ->
            printfn "[nyx] %s %s" q.[0] q.[1]
            if (q.[0].Equals(nugetPackageName, StringComparison.OrdinalIgnoreCase))
                then (q.[0].Equals(nugetPackageName, StringComparison.OrdinalIgnoreCase) && SemVerHelper.parse(q.[1]) < SemVerHelper.parse(version)) || (q.[0].Equals("No", StringComparison.OrdinalIgnoreCase) && q.[1].Equals("packages", StringComparison.OrdinalIgnoreCase))
                else true
            )
        |> Seq.tryFind(fun e -> e.Equals(true))

    let getNugetPackageDependencies =
        let nugetPackagesFile = repository.AppDir @@ "packages.config"
        match File.Exists nugetPackagesFile && repository.AppType.Equals "cli" |> not with
        | true -> getDependencies nugetPackagesFile
        | _ -> []

    let getNuspecFile =
        let nuspecFile = repository.AppDir @@ nugetPackageName + ".nuspec"
        let defaultNuspec = "<?xml version=\"1.0\" encoding=\"utf-8\"?>
<package xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">
  <metadata xmlns=\"http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd\">
    <description>@description@</description>
    <id>@project@</id>
    <version>@build.number@</version>
    <authors>@authors@</authors>
    <language>en-US</language>
    <summary>@summary@</summary>
    <releaseNotes>@releaseNotes@</releaseNotes>
    @dependencies@
  </metadata>
</package>"
        let shouldCreateNuspecFile = nuspecFile |> File.Exists |> not
        if shouldCreateNuspecFile then WriteStringToFile false nuspecFile defaultNuspec
        nuspecFile

    let getFiles (zipFilePath : string) =
        let zipFile = new ZipFile(zipFilePath)
        seq {
            for ze in zipFile do
                let entry = ze :?> ZipEntry
                yield entry.Name
        }
        |> Seq.map(fun file -> "\\" + filename file)

    let prepareNugetPackage(artifacts:Artifacts) =
        //Exclude libraries which are part of the packages.config file only when lib nuget package is created
        let dependencyFiles = getNugetPackageDependencies
                              |> Seq.map(fun (name,ver) -> name + "." + ver)
                              |> Seq.collect(fun pkgName -> !! ("./src/packages/*/" + pkgName + ".nupkg"))
                              |> Seq.collect(fun pkg -> getFiles(pkg))
                              |> fun gga -> Collections.Set(gga)
                              |> Set.toSeq
        printfn "[nyx] Nuget dependencies:"
        dependencyFiles |> Seq.iter(fun file -> printfn "[nyx] %s" file)
        let excludePaths (pathsToExclude : string seq) (path: string) = pathsToExclude |> Seq.exists (path.endswith StringComparison.OrdinalIgnoreCase)|> not
        let exclude = fun file -> (excludePaths dependencyFiles file) && (FileHelper.hasExt ".pdb" file |> not)
        let onlyDll = fun file -> (FileHelper.hasExt ".pdb" file |> not)
        let buildDirList = Directory.GetDirectories artifacts.BuildDir
        buildDirList
        |> Seq.iter(fun dir ->
            if dir.ToLower().Contains "_published" then CopyDir nugetContentDir (dir @@ repository.AppName) onlyDll
            if dir.ToLower().Contains "_tools" then CopyDir nugetToolsDir (dir @@ repository.AppName) allFiles)
        let hasNonLibArtifacts = buildDirList |> Seq.exists(fun dir -> dir.ToLower().Contains "_published" || dir.ToLower().Contains "_tools")
        if hasNonLibArtifacts |> not
        then CopyDir nugetLibDir artifacts.BuildDir exclude
        if Directory.Exists repository.DeploymentDir
        then CopyDir nugetToolsDir repository.DeploymentDir allFiles

    let createNuget release =
        let nugetAccessKey = getBuildParamOrDefault "nugetkey" ""
        let nugetPublishUrl = getBuildParamOrDefault "nugetserver" "https://www.nuget.org/api/v2/package"
        if release then printfn "[nyx] Pushing %s to %s ..." nugetPackageName nugetPublishUrl

        NuGet (fun app ->
            {app with
                NoPackageAnalysis = true
                Authors = repository.AppInfo.Authors
                Project = nugetPackageName
                Description = repository.AppInfo.Description
                Version = repository.GitVersion.NuGetVersionV2
                Summary = repository.AppInfo.Sumarry
                ReleaseNotes = repository.ReleaseNotes.Notes |> toLines
                Dependencies = getNugetPackageDependencies
                AccessKey = nugetAccessKey
                Publish = release
                PublishUrl = nugetPublishUrl
                ToolPath = nuget
                OutputPath = nugetWorkDir
                WorkingDir = nugetWorkDir
            }) getNuspecFile

    member this.PackageName = nugetPackageName
    member this.CanPublishPackage gitversion = canPublishPackage gitversion
    member this.Clean() = CleanDirs [nugetWorkDir;]
    member this.CreateNuget artifacts release =
        prepareNugetPackage artifacts
        createNuget release

let ErrorAndExit(message:string) =
    Console.ForegroundColor <- ConsoleColor.Red
    Console.ForegroundColor <- ConsoleColor.White
    Environment.Exit(1)

type Release(repository:Repository, nuget:EldersNuget) =
    let isValidRelease = (repository.ReleaseNotes.NugetVersion, repository.GitVersion.NuGetVersionV2) |> String.Equals

    let canRelease =
        printfn "[nyx] GitVer: %s  |  ReleaseNotesVer: %s" repository.GitVersion.NuGetVersionV2 repository.ReleaseNotes.NugetVersion
        let mutable canRelease = false

        if isValidRelease then
            let canPublishNuget = (nuget.CanPublishPackage repository.GitVersion).IsSome
            printfn "[nyx] Can publish nuget => %b" canPublishNuget
            let nugetAccessKey = getBuildParamOrDefault "nugetkey" ""
            canRelease <- nugetAccessKey.Equals "" |> not && canPublishNuget
            printfn "[nyx] Can release => %b" canRelease
            if canRelease |> not then
                if repository.GitVersion.NuGetVersionV2.Equals repository.ReleaseNotes.NugetVersion |> not then
                    if nugetAccessKey.Equals "" then
                        ErrorAndExit "[nyx] Unable to release because nuget access key is missing"
                    if canPublishNuget |> not then
                        ErrorAndExit "[nyx] Unable to release because this version is already released or it is lower than the currently release version"
        else
            printfn "[nyx] Regular build without release. Package will not be published. If you want to publish a release both versions should match => GitVer: %s  |  ReleaseNotesVer: %s" repository.GitVersion.NuGetVersionV2 repository.ReleaseNotes.NugetVersion
        canRelease

    member this.IsValidRelease = isValidRelease
    member this.CanRelease = canRelease

type Tests(appInfo:AppInfo) =
    let testResultDir = "./bin/tests" @@ appInfo.Name
    let mspec = environVar "MSPEC"

    member this.TestResultDir = testResultDir
    member this.MSpec = mspec

Target "Clean" (fun _ ->
    CleanDirs [@"./bin"]
)

Target "RestoreNugetPackages" (fun _ ->
    let nuget = environVar "NUGET"
    let packagesDir = @"./src/packages"
    !! "./src/*/packages.config"
    |> Seq.iter (RestorePackage (fun p ->
        { p with
            ToolPath = nuget
            OutputPath = packagesDir }))
)

Target "RestoreBowerPackages" (fun _ ->
    !! "./src/*/package.json"
    |> Seq.iter (fun config ->
        config.Replace("package.json", "")
        |> fun cfgDir ->
            printf "[nyx] Bower working dir: %s" cfgDir
            let result = ExecProcess (fun info ->
                            info.FileName <- "cmd"
                            info.WorkingDirectory <- cfgDir
                            info.Arguments <- "/c npm install") (TimeSpan.FromMinutes 20.0)
            if result <> 0 then failwithf "'npm install' returned with a non-zero exit code")
)

Target "Build" (fun _ ->
    let repositoryFactory = new RepositoryFactory()
    let repository = repositoryFactory.GetRepository
    let artifacts = new Artifacts(repository.AppName, @"./bin/Release")
    let eldersNuget = new EldersNuget(repository)
    let release = new Release(repository, eldersNuget)

    let buildDir = artifacts.BuildDir
    let appName = repository.AppName
    let sourceDir = repository.SourceDir
    let msiDir = artifacts.MsiDir
    let toolDir = artifacts.ToolDir
    let intTestsDir = artifacts.IntTestsDir

    printfn "[nyx] Creating build artifacts directory..."
    CreateDir buildDir

    repository.AppInfo.UpdateAssemblyInfo(repository.AssemblyInfoFile, repository.GitVersion)

    let appBuildFile = sourceDir @@ appName @@ "build.cmd"
    if File.Exists appBuildFile
    then
        let result = ExecProcess (fun info ->
                info.FileName <- "cmd"
                info.WorkingDirectory <- sourceDir @@ appName
                info.Arguments <- "/c build.cmd") (TimeSpan.FromMinutes 5.0)
        if result <> 0 then ErrorAndExit "'build.cmd' returned with a non-zero exit code"

    match repository.AppType with
    | "msi" -> sourceDir @@ appName + ".sln" |> fun dir -> !!dir |> MSBuildRelease msiDir "Build" |> Log "Build-Output: "
    | "cli" -> sourceDir @@ appName @@ appName + ".csproj" |> fun dir -> !!dir |> MSBuildRelease toolDir "Build" |> Log "Build-Output: "
    | "int-tests" -> sourceDir @@ appName @@ appName + ".csproj" |> fun dir -> !!dir |> MSBuildRelease intTestsDir "Build" |> Log "Build-Output: "
    | _ -> sourceDir @@ appName @@ appName + ".csproj" |> fun dir -> !!dir |> MSBuildRelease buildDir "Build" |> Log "Build-Output: "
)

Target "RunTests" (fun _ ->
    let repositoryFactory = new RepositoryFactory()
    let repository = repositoryFactory.GetRepository
    let artifacts = new Artifacts(repository.AppName, @"./bin/Release")
    let eldersNuget = new EldersNuget(repository)
    let release = new Release(repository, eldersNuget)
    let tests = new Tests(repository.AppInfo)

    let isTests = (repository.AppType, "tests") |> String.Equals
    if isTests then
                    CreateDir tests.TestResultDir
                    let result = ExecProcess (fun info ->
                        info.FileName <- tests.MSpec
                        info.WorkingDirectory <- "."
                        info.Arguments <- "--html " + tests.TestResultDir @@ "index.html " + artifacts.BuildDir @@ repository.AppName + ".dll") (TimeSpan.FromMinutes 5.0)
                    if result <> 0 then failwithf "'mspec-clr4.exe' returned with a non-zero exit code"
)

Target "CreateNuget" (fun _ ->
    let repositoryFactory = new RepositoryFactory()
    let repository = repositoryFactory.GetRepository
    let artifacts = new Artifacts(repository.AppName, @"./bin/Release")
    let eldersNuget = new EldersNuget(repository)
    let release = new Release(repository, eldersNuget)

    release.CanRelease |> eldersNuget.CreateNuget artifacts
)

Target "ReleaseLocal" (fun _ -> printfn "[nyx] Release local")

Target "Release" (fun _ ->
    let repositoryFactory = new RepositoryFactory()
    let repository = repositoryFactory.GetRepository
    let artifacts = new Artifacts(repository.AppName, @"./bin/Release")
    let eldersNuget = new EldersNuget(repository)
    let release = new Release(repository, eldersNuget)

    if release.CanRelease
    then
        StageAll ""
        let notes = String.concat "; " repository.ReleaseNotes.Notes
        Commit "" (sprintf "%s" notes)
        Branches.push ""

        let tag = eldersNuget.PackageName + "@" + repository.GitVersion.NuGetVersionV2;
        printfn "[nyx] Assign version %s as git tag" tag
        Branches.tag "" tag
        Branches.pushTag "" "origin" tag
    else
        printfn "[nyx] NOT RELEASED!"
)

"Build"
    ==> "RunTests"
    ==> "CreateNuget"
    ==> "ReleaseLocal"
    ==> "Release"

RunParameterTargetOrDefault "target" "Release"
