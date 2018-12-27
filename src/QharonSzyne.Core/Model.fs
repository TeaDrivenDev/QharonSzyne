namespace QharonSzyne

module Model =

    open System

    type Track =
        {
            Number : byte
            Title : string
            Artist : string
            Album : string
            Year : uint32 option
            //Genres : string list
            //Comments : Comment list
            Duration : TimeSpan
            FilePath : string
            FileSize : int64
            AddedOn : DateTime
            ModifiedOn : DateTime
        }

    and Comment =
        {
            CommentDescriptor : string
            Content : string
        }
