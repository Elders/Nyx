#I @"../../FAKE/tools/"
#r @"../../FAKE/tools/FakeLib.dll"
#r @"../../FAKE/tools/Newtonsoft.Json.dll"
#r @"../../FAKE/tools/Fake.Deploy.Lib.dll"
#r @"../../Nuget.Core/lib/net40-Client/NuGet.Core.dll"

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
open Newtonsoft.Json
open Newtonsoft.Json.Linq

type System.String with member x.endswith (comp:System.StringComparison) str = x.EndsWith(str, comp)

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  BEGIN EDIT

let appName = getBuildParamOrDefault "appName" ""
let solution = getBuildParamOrDefault "solution" appName
let conventionAppType = match appName.ToLowerInvariant() with
                          | EndsWith "msi" -> "msi"
                          | EndsWith "cli" -> "cli"
                          | EndsWith "tests" -> "tests"
                          | _ -> "lib"
let appType = getBuildParamOrDefault "appType" conventionAppType
let target = getBuildParamOrDefault "target" "Build"

//  END EDIT
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

let sourceDir = "./src"
let appDir = sourceDir @@ appName
let assemblyInfoFile = appDir @@ "Properties/AssemblyInfo.cs"
let deploymentDir = appDir @@ "deployment"
let buildDir  = @"./bin/Release" @@ appName
let toolDir = buildDir @@ "_tools" @@ appName
let defaultReleaseNotes = sourceDir @@ appName @@ @"RELEASE_NOTES.md"
let releaseNotes = getBuildParamOrDefault "appReleaseNotes" defaultReleaseNotes
let nuget = environVar "NUGET"
let nugetWorkDir = buildDir @@ "_tools" @@ appName @@ "Release"
let testResultDir = "./bin/tests" @@ appName
let gitversion = environVar "GITVERSION"
let mspec = environVar "MSPEC"

type Version = {
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

let mutable gitVer = Unchecked.defaultof<Version>

Target "Clean" (fun _ -> CleanDirs [buildDir; nugetWorkDir;])

Target "UpdateProjectInfo" (fun _ ->
    let result = ExecProcessAndReturnMessages (fun info ->
        info.FileName <- gitversion
        info.WorkingDirectory <- "."
        info.Arguments <- "/output json") (TimeSpan.FromMinutes 5.0)

    if result.ExitCode <> 0 then failwithf "'GitVersion.exe' returned with a non-zero exit code"

    let jsonResult = System.String.Concat(result.Messages)

    jsonResult |> deserialize<Version> |> fun ver -> gitVer <- ver

    let project = appDir @@ "project.json" |> deserializeFile<JObject>

    project.Property("version").Value.Replace(JToken.Parse("\"" + gitVer.NuGetVersionV2 + "\""))

    let mutable isRelease = (target, "Release") |> String.Equals
    
    if isRelease |> not then
                            isRelease <- (target, "ReleaseLocal") |> String.Equals

    if isRelease then
                    let release = LoadReleaseNotes releaseNotes
                    let isNOTValid = (gitVer.NuGetVersionV2, release.NugetVersion) |> String.Equals |> not

                    if isNOTValid then
                                    Console.ForegroundColor <- ConsoleColor.Red
                                    printfn "Unable to find release notes for version '%s'" gitVer.NuGetVersionV2
                                    Console.ForegroundColor <- ConsoleColor.White
                                    Environment.Exit(1)

                    project.Property("releaseNotes").Value.Replace(JToken.Parse("\"" + String.concat "; " release.Notes + "\""))

    File.WriteAllText(appDir @@ "project.json", serialize(project))
)

Target "Build" (fun _ ->
    let appBuildFile = sourceDir @@ appName @@ "build.cmd"
    if File.Exists appBuildFile then
                                    let result = ExecProcess (fun info ->
                                            info.FileName <- "cmd"
                                            info.WorkingDirectory <- sourceDir @@ appName
                                            info.Arguments <- "/c build.cmd") (TimeSpan.FromMinutes 5.0)
                                    if result <> 0 then failwithf "'build.cmd' returned with a non-zero exit code"

    sourceDir @@ solution + ".sln" |> fun dir -> !!dir |> MSBuildRelease toolDir "Build" |> Log "Build-Output: "
)

Target "RunTests" (fun _ ->
    let isTests = (appType, "tests") |> String.Equals

    if isTests then
                    CreateDir testResultDir
                    let result = ExecProcess (fun info ->
                        info.FileName <- mspec
                        info.WorkingDirectory <- "."
                        info.Arguments <- "--html " + testResultDir @@ "index.html " + buildDir @@ appName + ".dll") (TimeSpan.FromMinutes 5.0)
                    if result <> 0 then failwithf "'mspec-clr4.exe' returned with a non-zero exit code"
)

Target "ReleaseLocal" (fun _ ->
   printfn "Release local"
)

Target "PublishNuget" (fun _ ->
    let nugetAccessKey = getBuildParamOrDefault "nugetkey" ""
    let nugetDoPublish = nugetAccessKey.Equals "" |> not
    let nugetPublishUrl = getBuildParamOrDefault "nugetserver" "https://nuget.org"
    let nugetPackageName = "./_tools" @@ appName @@ "Release" @@ appName + "." + gitVer.NuGetVersionV2 + ".nupkg"
    Console.WriteLine nugetPackageName
    Console.WriteLine buildDir
    let args = "push " + nugetPackageName + " " + nugetAccessKey
    let result = 
        ExecProcess (fun info -> 
            info.FileName <- nuget
            info.WorkingDirectory <- buildDir
            info.Arguments <- args) (TimeSpan.FromMinutes 5.0)
    if result <> 0 then failwithf "Error during NuGet push. %s %s" nuget args
)

Target "Release" (fun _ ->
    Console.WriteLine("Released")
)

"Clean"
    ==> "UpdateProjectInfo"
    ==> "Build"
    ==> "RunTests"
    ==> "ReleaseLocal"
    ==> "PublishNuget"
    ==> "Release"

RunParameterTargetOrDefault "target" "Build"
