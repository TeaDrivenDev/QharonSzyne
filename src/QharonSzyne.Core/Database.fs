namespace QharonSzyne

[<RequireQualifiedAccess>]
module Database =

    open System
    open System.IO

    open SQLite.Net
    open SQLite.Net.Attributes
    open SQLiteNetExtensions.Attributes

    [<CLIMutable>]
    type Metadata =
        {
            [<SQLite.Net.Attributes.PrimaryKey>]
            Name : string
            [<SQLite.Net.Attributes.NotNull>]
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
            //[<OneToMany(CascadeOperations = CascadeOperation.All)>]
            //Genres : Genre []
            //[<OneToMany(CascadeOperations = CascadeOperation.All)>]
            //Comments : Comment []
            Duration : TimeSpan
            [<NotNull>]
            FilePath : string
            FileSize : int64
            AddedOn : DateTime
            ModifiedOn : DateTime
        }

    //and [<CLIMutable>] Comment =
    //    {
    //        [<PrimaryKey; AutoIncrement>]
    //        CommentId : uint32
    //        [<ForeignKey(typeof<Track>); NotNull>]
    //        TrackId : uint32
    //        CommentDescriptor : string
    //        Content : string
    //    }

    //and [<CLIMutable>] Genre =
    //    {
    //        [<PrimaryKey; AutoIncrement>]
    //        GenreId : uint32
    //        [<ForeignKey(typeof<Track>); NotNull>]
    //        TrackId : uint32
    //        [<NotNull>]
    //        Name : string
    //    }

    //let toDatabaseComment (comment : Model.Comment) =
    //    {
    //        CommentId = 0u
    //        TrackId = 0u
    //        CommentDescriptor = comment.CommentDescriptor
    //        Content = comment.Content
    //    }

    //let toDatabaseGenre genre =
    //    {
    //        GenreId = 0u
    //        TrackId = 0u
    //        Name = genre
    //    }

    let toDatabaseTrack (track : Model.Track) =
        {
            TrackId = 0u
            Number = track.Number
            Title = track.Title
            Artist = track.Artist
            Album = track.Album
            Year = defaultArg track.Year 0u |> int
            //Genres = track.Genres |> List.map toDatabaseGenre |> List.toArray
            //Comments = track.Comments |> List.map toDatabaseComment |> List.toArray
            Duration = track.Duration
            FilePath = track.FilePath
            FileSize = track.FileSize
            AddedOn = track.AddedOn
            ModifiedOn = track.ModifiedOn
        }

    //let fromDatabaseComment comment : Model.Comment =
    //    {
    //        CommentDescriptor = comment.CommentDescriptor
    //        Content = comment.Content
    //    }

    let fromDatabaseGenre genre = genre.Name

    let fromDatabaseTrack track : Model.Track =
        {
            Number = track.Number
            Title = track.Title
            Artist = track.Artist
            Album = track.Album
            Year = track.Year |> uint32 |> Some
            //Genres = track.Genres |> Seq.map fromDatabaseGenre |> Seq.toList
            //Comments = track.Comments |> Seq.map fromDatabaseComment |> Seq.toList
            Duration = track.Duration
            FilePath = track.FilePath
            FileSize = track.FileSize
            AddedOn = track.AddedOn
            ModifiedOn = track.ModifiedOn
        }

    let createConnection deleteExistingDatabase applicationDataPath =
        let databaseDirectory = Path.Combine(applicationDataPath, "database")
        let databaseFile = Path.Combine(databaseDirectory, "tracks.db")

        if deleteExistingDatabase then
            if not <| Directory.Exists databaseDirectory
            then Directory.CreateDirectory databaseDirectory |> ignore
            else File.Delete databaseFile

        new SQLiteConnection(Platform.Win32.SQLitePlatformWin32(), databaseFile)

    let createTracksDatabase applicationDataPath =
        use connection = createConnection true applicationDataPath

        connection.CreateTable<Metadata>() |> ignore

        [
            { Name = "Version"; Value = "1.0" }
            { Name = "CreatedOn"; Value = DateTime.UtcNow.ToString "s" }
            { Name = "Complete"; Value = false.ToString() }
        ]
        |> connection.InsertAll
        |> ignore

        connection.CreateTable<Track>() |> ignore
        //connection.CreateTable<Comment>() |> ignore
        //connection.CreateTable<Genre>() |> ignore
