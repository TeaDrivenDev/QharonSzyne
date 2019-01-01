namespace QharonSzyne.Core

module Model =

    open System

    [<CLIMutable>]
    type Metadata =
        {
            Id : int
            Name : string
            Value : string
        }

    type Comment =
        {
            CommentDescriptor : string
            Content : string
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
            MediaFile : MediaFile
        }

    [<CLIMutable>]
    type Artist =
        {
            Id : int
            Name : string
            Location : string
            Releases : Release list
            Genres : string list
        }

    and ReleaseType =
        | Album
        | EP
        | Single
        | Demo
        | Live
        | Compilation
        | Custom of name:string

    and [<CLIMutable>]
    Release =
        {
            Id : int
            Artist : string
            Location : string
            Title : string
            ReleaseType : ReleaseType
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
