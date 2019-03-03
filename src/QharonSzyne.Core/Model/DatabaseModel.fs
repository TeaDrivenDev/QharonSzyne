namespace QharonSzyne.Core

module DatabaseModel =
    open System

    open SharedModel

    [<CLIMutable>]
    type Metadata =
        {
            Id : int
            Version : int
            CreatedOn : DateTime
            Complete : bool
        }

    [<CLIMutable>]
    type MediaFile =
        {
            Id : int
            Number : byte
            Title : string
            AlbumArtist : string
            Artist : string
            Album : string
            Year : uint32
            Genres : string list
            Comments : Comment list
            Duration : TimeSpan
            FilePath : string
            FileSize : int64
            AddedOn : DateTime
            ModifiedOn : DateTime
        }

    [<CLIMutable>]
    type Track =
        {
            Id : int
            TrackId : Guid
            Number : byte
            Title : string
            AlbumArtist : string
            Artist : string
            Album : string
            Year : uint32
            Comments : Comment list
            Duration : TimeSpan
            FilePath : string
            FileSize : int64
            AddedOn : DateTime
            ModifiedOn : DateTime
        }
        //with
        //static member fromMediaFile (mediaFile : MediaFile) =
        //    {
        //        Track.Id = 0
        //        Number = mediaFile.Number
        //        Title = mediaFile.Title
        //        AlbumArtist = mediaFile.AlbumArtist
        //        Artist = mediaFile.Artist
        //        Album = mediaFile.Album
        //        Year = mediaFile.Year
        //        Comments = mediaFile.Comments
        //        Duration = mediaFile.Duration
        //        FilePath = mediaFile.FilePath
        //        FileSize = mediaFile.FileSize
        //        AddedOn = mediaFile.AddedOn
        //        ModifiedOn = mediaFile.ModifiedOn
        //    }

    [<CLIMutable>]
    type Artist =
        {
            Id : int
            Name : string
            Location : string
            Releases : Release list
            Genres : string list
        }

    and [<CLIMutable>]
    Release =
        {
            Id : int
            ArtistId : Guid
            ReleaseId : Guid
            Location : string
            Title : string
            ReleaseType : ReleaseType
            ReleaseTypeStatus : ReleaseTypeStatus
            Genres : string list
            Year : uint32
            Tracks : Track list
            AddedOn : DateTime
            //Versions : ReleaseVersion list
        }

    and [<CLIMutable>]
    ReleaseVersion =
        {
            Name : string
        }
