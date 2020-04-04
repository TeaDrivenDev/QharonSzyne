namespace QharonSzyne.Core

module DomainModel =
    open System

    open SharedModel

    type Track =
        {
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

    type Release =
        {
            Id : int
            Artist : string
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

    type Artist =
        {
            ArtistId : Guid
            Name : string
            Location : string
        }
