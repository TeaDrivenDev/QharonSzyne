System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__

#r "paket:
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Paket
nuget Fake.IO.FileSystem
//"

#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators

let solutionFile  = "QharonSzyne.sln"

let gitOwner = "TeaDrivenDev"
let gitHome = "https://github.com/" + gitOwner
let gitName = "QharonSzyne"
let gitRaw = Environment.environVarOrDefault "gitRaw" ("https://raw.github.com/" + gitOwner)

let outputDirectory = "bin"

let configuration = Environment.environVarOrDefault "Configuration" "Release"

Target.create "Restore" (fun _ -> Paket.restore id)

Target.create "Clean" (fun _ -> Shell.cleanDirs [ outputDirectory ])

Target.create "Build" (fun _ ->
    !! solutionFile
    |> MSBuild.run id "" "Rebuild" [ "Configuration", configuration ]
    |> ignore)

Target.create "All" ignore

"Restore"
==> "Build"
==> "All"

Target.runOrDefault "All"

printfn "Finished %s" (System.DateTime.Now.ToString "HH:mm:ss")
