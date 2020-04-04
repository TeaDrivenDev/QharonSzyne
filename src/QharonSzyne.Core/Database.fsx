#r "System.Collections"
#r "System.Linq.Expressions"
#r "System.Reflection"
#r "System.Runtime"

#I @"..\..\packages"

#r @"LiteDB\lib\net40\LiteDB.dll"
#r @"LiteDB.FSharp\lib\net45\LiteDB.FSharp.dll"
#r @"Newtonsoft.Json\lib\net45\Newtonsoft.Json.dll"
#r @"System.Reactive\lib\net46\System.Reactive.dll"

open System
open System.IO

#r "System.IO"

open LiteDB
open LiteDB.FSharp

#load "Prelude.fs"

[<CLIMutable>]
type Release =
    {
        //[<BsonId>]
        Id : ObjectId
        //Artist : Artist
        Title : string
        Year : uint16
    }

and [<CLIMutable>] Artist =
    {
        //[<BsonId>]
        Id: ObjectId
        Name : string
        Releases : Release list
    }

let applicationDataPath = @"D:\Development\Staging\QharonSzyne"

let databaseFileName = "database.db"

BsonMapper.Global <- FSharpBsonMapper()

BsonMapper.Global.Entity<Artist>()
    .DbRef(fun x -> x.Releases)

let connection = new LiteDatabase(Path.Combine(applicationDataPath, databaseFileName), BsonMapper.Global)

let artists = connection.GetCollection<Artist>()
let releases = connection.GetCollection<Release>()

let reign = { Id = ObjectId.Empty; (*Artist = slayer;*) Title = "Reign In Blood"; Year = 1986us }
let seasons = { Id = ObjectId.Empty;(* Artist = slayer;*) Title = "Seasons In The Abyss"; Year = 1990us }
let slayer = { Id = ObjectId.Empty; Name = "Slayer"; Releases = [ reign; seasons ] }

releases.Insert [| reign; seasons |]
artists.Insert slayer

let query =
    artists.Include(fun x -> x.Releases).FindAll()

query
|> Seq.iter (fun x ->
    printfn "%s" x.Name

    x.Releases
    |> Seq.iter (fun r ->
        printfn "\t%s" r.Title))
