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
        abstract member Create : libraryName:string * tracks:Model.MediaFile list -> unit

        abstract member Read : libraryName:string -> Model.MediaFile list option

    module LiteDB =
        open LiteDB
        open LiteDB.FSharp

        open Model

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

                    {
                        Id = 0
                        Version = 1
                        CreatedOn = DateTime.Now
                        Complete = false
                    }
                    |> metadata.Insert
                    |> ignore

                    connection.GetCollection<MediaFile>() |> ignore

                    connection, databaseFilePath
                | None -> failwith "Could not create tracks database"

        type LiteDbTracksDatabase(applicationDataPath : string) =
            interface ITracksDatabase with
                member __.Create(libraryName : string, tracks : Model.MediaFile list): unit =
                    let libraryPath = Path.Combine(applicationDataPath, libraryName)

                    let newTracksDatabasePath =
                        let (connection, newTracksDatabasePath) = createTracksDatabase libraryPath
                        use connection = connection

                        let tracksCollection = connection.GetCollection<MediaFile>()

                        tracks
                        |> tracksCollection.InsertBulk
                        |> ignore

                        let metadata = connection.GetCollection<Metadata>()

                        let metadataValue = metadata.FindOne(fun _ -> true)

                        { metadataValue with Complete = true }
                        |> metadata.Update
                        |> ignore

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

                        connection.GetCollection<MediaFile>().FindAll()
                        |> Seq.toList)
