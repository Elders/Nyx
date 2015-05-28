#I @"../../FAKE/tools/"
#r @"../../FAKE/tools/FakeLib.dll"
#r @"../../Nuget.Core/lib/net40-Client/NuGet.Core.dll"

open System
open System.IO
open Fake
open Fake.Git
open Fake.FSharpFormatting
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.ProcessHelper

type System.String with member x.endswith (comp:System.StringComparison) str = x.EndsWith(str, comp)

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  BEGIN EDIT

let appName = getBuildParamOrDefault "appName" ""
let appType = getBuildParamOrDefault "appType" ""
let appSummary = getBuildParamOrDefault "appSummary" ""
let appDescription = getBuildParamOrDefault "appDescription" ""
let appAuthors = ["Nikolai Mynkow"; "Simeon Dimov";]

//  END EDIT
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

let sourceDir = "./src"
let deploymentDir = sourceDir @@ appName @@ "deployment"
let buildDir  = @"./bin/Release" @@ appName
let websiteDir = buildDir @@ "_PublishedWebsites" @@ appName
let msiDir = buildDir @@ "_PublishedMsi" @@ appName

let releaseNotes = sourceDir @@ appName @@ @"RELEASE_NOTES.md"
let release = LoadReleaseNotes releaseNotes

let nuget = environVar "NUGET"
let nugetWorkDir = "./bin/nuget" @@ appName
let nugetLibDir = nugetWorkDir @@ "lib" @@ "net45-full"
let nugetToolsDir = nugetWorkDir @@ "tools"
let nugetContentDir = nugetWorkDir @@ "content"

Target "Clean" (fun _ -> CleanDirs [buildDir; nugetWorkDir;])

Target "RestoreNugetPackages" (fun _ ->
  let packagesDir = @"./src/packages"
  let nugetSources = environVarOrDefault "NUGET_SOURCES" "https://nuget.org"
  !! "./src/*/packages.config"
  |> Seq.iter (RestorePackage (fun p ->
      { p with
          Sources = nugetSources :: p.Sources
          ToolPath = nuget
          OutputPath = packagesDir }))
)

Target "RestoreBowerPackages" (fun _ ->
    !! "./src/*/package.config"
    |> Seq.iter (fun config ->
        config.Replace("package.config", "")
        |> fun cfgDir ->
            printf "Bower working dir: %s" cfgDir
            let result = ExecProcess (fun info ->
                            info.FileName <- "cmd"
                            info.WorkingDirectory <- cfgDir
                            info.Arguments <- "/c npm install") (TimeSpan.FromMinutes 20.0)
            if result <> 0 then failwithf "'npm install' returned with a non-zero exit code")
)

Target "Build" (fun _ ->
    let appBuildFile = sourceDir @@ appName @@ "build.cmd"
    if File.Exists appBuildFile then
                                    let result = ExecProcess (fun info ->
                                            info.FileName <- "cmd"
                                            info.WorkingDirectory <- sourceDir @@ appName
                                            info.Arguments <- "/c build.cmd") (TimeSpan.FromMinutes 5.0)
                                    if result <> 0 then failwithf "'build.cmd' returned with a non-zero exit code"

    match appType with
    | "msi" -> sourceDir @@ appName + ".sln" |> fun dir -> !!dir |> MSBuildRelease msiDir "Build" |> Log "Build-Output: "
    | _ -> sourceDir @@ appName @@ appName + ".csproj" |> fun dir -> !!dir |> MSBuildRelease buildDir "Build" |> Log "Build-Output: "
)

Target "PrepareNuGet" (fun _ ->
    //  Exclude libraries which are part of the packages.config file only when nuget package is created.
    let nugetPackagesFile = sourceDir @@ appName @@ "packages.config"
    let dependencies = match File.Exists nugetPackagesFile with
                        | true -> getDependencies nugetPackagesFile
                        | _ -> []
    let dependencyFiles = dependencies
                          |> Seq.map(fun (name,ver) -> name + "." + ver)
                          |> Seq.collect(fun pkgName -> !! ("./src/packages/*/" + pkgName + ".nupkg"))
                          |> Seq.collect(fun pkg -> global.NuGet.ZipPackage(pkg).GetFiles())
                          |> Seq.map(fun file -> "\\" + filename file.Path)
                          |> fun gga -> Collections.Set(gga)
                          |> Set.toList

    let excludePaths (pathsToExclude : string list) (path: string) = pathsToExclude |> List.exists (path.endswith StringComparison.OrdinalIgnoreCase)|> not
    let exclude = excludePaths dependencyFiles

    Directory.GetDirectories buildDir
    |> Seq.iter(fun dir ->
        if dir.ToLowerInvariant().Contains "_published"
            then CopyDir nugetContentDir (dir @@ appName) allFiles
            else CopyDir nugetLibDir buildDir exclude)

    if Directory.Exists deploymentDir then CopyDir nugetToolsDir deploymentDir allFiles
)

Target "CreateNuget" (fun _ ->
    let nugetPackagesFile = sourceDir @@ appName @@ "packages.config"
    let dependencies = match File.Exists nugetPackagesFile with
                        | true -> getDependencies nugetPackagesFile
                        | _ -> []

    let nugetAccessKey = getBuildParamOrDefault "nugetkey" ""
    let nugetPackageName = getBuildParamOrDefault "nugetPackageName" appName
    let nuspecFile = sourceDir @@ appName @@ nugetPackageName + ".nuspec"
    let nugetDoPublish = nugetAccessKey.Equals "" |> not
    let nugetPublishUrl = getBuildParamOrDefault "nugetserver" "https://nuget.org"

    //  Create/Publish the nuget package
    NuGet (fun app ->
        {app with
            NoPackageAnalysis = true
            Authors = appAuthors
            Project = nugetPackageName
            Description = appDescription
            Version = release.NugetVersion
            Summary = appSummary
            ReleaseNotes = release.Notes |> toLines
            Dependencies = dependencies
            AccessKey = nugetAccessKey
            Publish = nugetDoPublish
            PublishUrl = nugetPublishUrl
            ToolPath = nuget
            OutputPath = nugetWorkDir
            WorkingDir = nugetWorkDir
        }) nuspecFile
)

Target "ReleaseLocal" (fun _ ->
   printfn "Release local"
)

Target "Release" (fun _ ->
    StageAll ""
    let notes = String.concat "; " release.Notes
    Commit "" (sprintf "%s" notes)
    Branches.push ""

    Branches.tag "" release.NugetVersion
    Branches.pushTag "" "origin" release.NugetVersion
)

"Clean"
    ==> "RestoreNugetPackages"
    ==> "RestoreBowerPackages"
    ==> "Build"
    ==> "PrepareNuGet"
    ==> "CreateNuget"
    ==> "ReleaseLocal"
    ==> "Release"

RunParameterTargetOrDefault "target" "Build"
