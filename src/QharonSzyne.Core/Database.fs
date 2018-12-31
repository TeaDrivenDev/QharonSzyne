namespace QharonSzyne.Core

[<RequireQualifiedAccess>]
module Database =

    open System
    open System.IO

    let newtonSoftJsonDummyReference =
        Newtonsoft.Json.ConstructorHandling.AllowNonPublicDefaultConstructor

    [<Literal>]
    let TracksDatabaseFileName = "tracks.db"

    [<Literal>]
    let TracksTempDatabaseFileName = "tracks_temp.db"

    type ConnectionMode = OpenExisting | ReplaceOrCreateIfMissing

    type ITracksDatabase =
        abstract member Create : libraryName:string * tracks:Model.Track list -> unit

        abstract member Read : libraryName:string -> Model.Track list option

    module LiteDB =
        open LiteDB
        open LiteDB.FSharp

        open Model

        [<CLIMutable>]
        type Metadata =
            {
                Id : int
                Name : string
                Value : string
            }

        let createConnection connectionMode databaseFilePath =
            let exists = File.Exists databaseFilePath

            match connectionMode with
            | OpenExisting -> exists
            | ReplaceOrCreateIfMissing ->
                if exists
                then File.Delete databaseFilePath
                else Directory.CreateDirectory(Path.GetDirectoryName databaseFilePath) |> ignore

                true
            |> function
                | true ->
                    Some (new LiteDatabase(databaseFilePath, FSharpBsonMapper()), databaseFilePath)
                | false -> None

        let createTracksDatabase libraryPath =
            createConnection
                ReplaceOrCreateIfMissing
                (Path.Combine(libraryPath, TracksTempDatabaseFileName))
            |> function
                | Some (connection, databaseFilePath) ->
                    let metadata = connection.GetCollection<Metadata>()

                    [
                        { Id = 0; Name = "Version"; Value = "1.0" }
                        { Id = 0; Name = "CreatedOn"; Value = DateTime.UtcNow.ToString "s" }
                        { Id = 0; Name = "Complete"; Value = false.ToString() }
                    ]
                    |> metadata.InsertBulk
                    |> ignore

                    connection.GetCollection<Track>() |> ignore

                    connection, databaseFilePath
                | None -> failwith "Could not create tracks database"

        type LiteDbTracksDatabase(applicationDataPath : string) =
            interface ITracksDatabase with
                member __.Create(libraryName : string, tracks : Model.Track list): unit =
                    let libraryPath = Path.Combine(applicationDataPath, libraryName)

                    let newTracksDatabasePath =
                        let (connection, newTracksDatabasePath) = createTracksDatabase libraryPath
                        use connection = connection

                        let tracksCollection = connection.GetCollection<Track>()

                        tracks
                        |> tracksCollection.InsertBulk
                        |> ignore

                        let metadata = connection.GetCollection<Metadata>()

                        let complete = metadata.FindOne(fun m -> m.Name = "Complete")
                        let isDone = { complete with Value = true.ToString() }
                        metadata.Update isDone |> ignore

                        newTracksDatabasePath

                    let tracksDatabasePath = Path.Combine(libraryPath, TracksDatabaseFileName)

                    if File.Exists tracksDatabasePath
                    then File.Delete tracksDatabasePath

                    File.Move(newTracksDatabasePath, tracksDatabasePath)

                member __.Read(libraryName : string) =
                    let databasePath =
                        Path.Combine(applicationDataPath, libraryName, TracksDatabaseFileName)

                    createConnection OpenExisting databasePath
                    |> Option.map (fun (connection, _) ->
                        use connection = connection

                        connection.GetCollection<Track>().FindAll()
                        |> Seq.toList)
