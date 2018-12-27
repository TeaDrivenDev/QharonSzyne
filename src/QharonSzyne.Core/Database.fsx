#r "System.Collections"
#r "System.Linq.Expressions"
#r "System.Reflection"
#r "System.Runtime"

#I @"..\..\packages"

#r @"SQLite.Net-PCL\lib\portable-win8+net45+wp8+wpa81+MonoAndroid1+MonoTouch1\SQLite.Net.dll"
#r @"SQLite.Net-PCL\lib\net4\SQLite.Net.Platform.Win32.dll"
#r @"sqlite-net-pcl\lib\netstandard1.1\SQLite-net.dll"
#r @"SqLiteNetExtensions\lib\netstandard1.1\SQLiteNetExtensions.dll"
//#r @"Newtonsoft.Json\lib\net45\Newtonsoft.Json.dll"

open System
open System.IO
open SQLiteNetExtensions.Extensions

#r "System.IO"

#r @"TagLib.Portable\lib\portable-net45+win+wpa81+wp8+MonoAndroid10+xamarinios10+MonoTouch10\TagLib.Portable.dll"
#r @"NAudio\lib\net35\NAudio.dll"

#load "Model.fs"
#load "Scanning.fs"
#load "Database.fs"

open QharonSzyne

let interopPathRelative = @"..\..\packages\System.Data.SQLite.Core\build\net46"
let interopPath = DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, interopPathRelative)).FullName
Environment.SetEnvironmentVariable("Path", Environment.GetEnvironmentVariable("Path") + ";" + interopPath)

let applicationDataPath = @"D:\Development\Staging\QharonSzyne"

Database.createTracksDatabase applicationDataPath

let directory = @"E:\Recompress\Progressive Metal\Cynic"

let addToDatabase (createConnection : unit -> SQLite.Net.SQLiteConnection) tracks =
    use connection = createConnection()

    try
        tracks
        |> List.map Database.toDatabaseTrack
        |> (fun tracks -> tracks, true)
        |> connection.InsertAll
        |> ignore
    with
    | ex -> printfn "%s\n%s" ex.Message ex.StackTrace

Scanning.scan
    (fun _ -> None)
    (printfn "%i tracks found")
    (fun tracks ->
        printfn "Adding %i tracks to database" tracks.Length
        addToDatabase (fun _ -> Database.createConnection false applicationDataPath) tracks

        use connection = Database.createConnection false applicationDataPath
        let complete =
            connection.Table<Database.Metadata>().Where(fun m -> m.Name = "Complete")
            |> Seq.head

        let isDone = { complete with Value = true.ToString() }
        connection.Update isDone |> ignore

        printfn "Done")
    directory
