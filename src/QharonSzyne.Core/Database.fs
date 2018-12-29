﻿namespace QharonSzyne.Core

[<RequireQualifiedAccess>]
module Database =

    open System
    open System.IO

    open SQLite.Net
    open SQLite.Net.Attributes

    open Newtonsoft.Json

    [<CLIMutable>]
    type Metadata =
        {
            [<PrimaryKey>]
            Name : string
            [<NotNull>]
            Value : string
        }

    [<CLIMutable>]
    type Track =
        {
            [<PrimaryKey; AutoIncrement>]
            TrackId : uint32
            Number : byte
            Title : string
            Artist : string
            Album : string
            Year : int
            Genres : string
            Comments : string
            Duration : TimeSpan
            [<NotNull>]
            FilePath : string
            FileSize : int64
            AddedOn : DateTime
            ModifiedOn : DateTime
        }

    [<CLIMutable>]
    type Comment =
        {
            CommentDescriptor : string
            Content : string
        }

    let serializer = JsonSerializer.CreateDefault()

    let toDatabaseComments (comments : Model.Comment list) =
        use textWriter = new StringWriter()

        comments
        |> List.map (fun c ->
            {
                CommentDescriptor = c.CommentDescriptor
                Content = c.Content
            })
        |> List.toArray
        |> asSnd textWriter
        |> serializer.Serialize

        textWriter.ToString()

    let toDatabaseGenres (genres : string list) =
        use textWriter = new StringWriter()
        serializer.Serialize(textWriter, List.toArray genres)
        textWriter.ToString()

    let toDatabaseTrack (track : Model.Track) =
        {
            TrackId = 0u
            Number = track.Number
            Title = track.Title
            Artist = track.Artist
            Album = track.Album
            Year = track.Year |> Option.defaultValue 0u |> int
            Genres = track.Genres |> toDatabaseGenres
            Comments = track.Comments |> toDatabaseComments
            Duration = track.Duration
            FilePath = track.FilePath
            FileSize = track.FileSize
            AddedOn = track.AddedOn
            ModifiedOn = track.ModifiedOn
        }

    let fromDatabaseComments comments : Model.Comment list =
        use textReader = new StringReader(comments)
        use reader = new JsonTextReader(textReader)
        serializer.Deserialize<Comment []> reader
        |> Array.map (fun c ->
            {
                Model.Comment.CommentDescriptor = c.CommentDescriptor
                Model.Comment.Content = c.Content
            })
        |> Array.toList

    let fromDatabaseGenres genres =
        use textReader = new StringReader(genres)
        use reader = new JsonTextReader(textReader)
        serializer.Deserialize<string []> reader
        |> Array.toList

    let fromDatabaseTrack track : Model.Track =
        {
            Number = track.Number
            Title = track.Title
            Artist = track.Artist
            Album = track.Album
            Year = track.Year |> uint32 |> Some
            Genres = track.Genres |> fromDatabaseGenres
            Comments = track.Comments |> fromDatabaseComments
            Duration = track.Duration
            FilePath = track.FilePath
            FileSize = track.FileSize
            AddedOn = track.AddedOn
            ModifiedOn = track.ModifiedOn
        }

    [<Literal>]
    let TracksDatabaseFileName = "tracks.db"

    [<Literal>]
    let TracksTempDatabaseFileName = "tracks_temp.db"

    type ConnectionMode = OpenExisting | ReplaceOrCreateIfMissing

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
                new SQLiteConnection(Platform.Win32.SQLitePlatformWin32(), databaseFilePath)
                |> Some
            | false -> None

    let createTracksDatabase libraryPath =
        createConnection
            ReplaceOrCreateIfMissing
            (Path.Combine(libraryPath, TracksTempDatabaseFileName))
        |> function
            | Some connection ->
                connection.CreateTable<Metadata>() |> ignore

                [
                    { Name = "Version"; Value = "1.0" }
                    { Name = "CreatedOn"; Value = DateTime.UtcNow.ToString "s" }
                    { Name = "Complete"; Value = false.ToString() }
                ]
                |> connection.InsertAll
                |> ignore

                connection.CreateTable<Track>() |> ignore

                connection
            | None -> failwith "Could not create tracks database"

    type ITracksDatabase =
        abstract member Create : libraryName:string * tracks:Model.Track list -> unit

        abstract member Read : libraryName:string -> Model.Track list option

    type SqliteTracksDatabase(applicationDataPath : string) =
        interface ITracksDatabase with
            member __.Create(libraryName : string, tracks : Model.Track list): unit =
                let libraryPath = Path.Combine(applicationDataPath, libraryName)

                let newTracksDatabasePath =
                    use connection = createTracksDatabase libraryPath

                    tracks
                    |> List.map toDatabaseTrack
                    |> (fun tracks -> tracks, true)
                    |> connection.InsertAll
                    |> ignore

                    let complete =
                        connection.Table<Metadata>().Where(fun m -> m.Name = "Complete")
                        |> Seq.head

                    let isDone = { complete with Value = true.ToString() }
                    connection.Update isDone |> ignore

                    connection.DatabasePath

                let tracksDatabasePath = Path.Combine(libraryPath, TracksDatabaseFileName)

                if File.Exists tracksDatabasePath
                then File.Delete tracksDatabasePath

                File.Move(newTracksDatabasePath, tracksDatabasePath)

            member __.Read(libraryName : string) =
                let databasePath =
                    Path.Combine(applicationDataPath, libraryName, TracksDatabaseFileName)

                createConnection OpenExisting databasePath
                |> Option.map (fun connection ->
                    use connection = connection

                    connection.Table<Track>()
                    |> Seq.map fromDatabaseTrack
                    |> Seq.toList)
