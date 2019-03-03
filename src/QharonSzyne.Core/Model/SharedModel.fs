namespace QharonSzyne.Core

module SharedModel =

    type Comment =
        {
            CommentDescriptor : string
            Content : string
        }

    type ReleaseType =
        | Album
        | EP
        | Single
        | Demo
        | Live
        | Compilation
        | Custom of name:string

    type ReleaseTypeStatus = Tentative | Confirmed
