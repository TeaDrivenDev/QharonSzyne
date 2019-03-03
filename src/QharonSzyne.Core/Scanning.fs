namespace QharonSzyne.Core

[<RequireQualifiedAccess>]
module Scanning =

    open System
    open System.IO
    open System.Linq

    open SharedModel
    open DatabaseModel

    type Message<'T> =
    | Content of 'T
    | EndOfInput
    | Reset

    type ScanningError =
    | CorruptFile of string

    type FileName = FileName of fileName:string * basePath:string

    type ControlMessage =
    | ScannedFileName of Message<FileName>
    | ReadTrack of Message<MediaFile>
    | ScanningError of ScanningError

    let createControlActor reportTotal reportProgress outputTracks =
        MailboxProcessor.Start(fun inbox ->
            let reset = 0, [], []

            let rec loop (total, tracks, errors) =
                async {
                    let! message = inbox.Receive()

                    let newState =
                        match message with
                        | ScannedFileName fileName ->
                            match fileName with
                            | Content(FileName _) -> total + 1, tracks, errors
                            | EndOfInput -> reportTotal total; total, tracks, errors
                            | Reset -> reset
                        | ReadTrack track ->
                            match track with
                            | Content track ->
                                let tracks = track :: tracks
                                reportProgress (tracks.Length + errors.Length)
                                total, tracks, errors
                            | EndOfInput -> outputTracks tracks; total, tracks, errors
                            | Reset -> reset
                        | ScanningError error ->
                            let errors = error :: errors
                            reportProgress (tracks.Length + errors.Length)
                            total, tracks, errors

                    return! loop newState
                }

            loop reset)

    let getV2Comments (tag : TagLib.Id3v2.Tag) =
        tag.OfType<TagLib.Id3v2.CommentsFrame>()
        |> Seq.filter (fun frame -> not <| String.IsNullOrWhiteSpace frame.Text)
        |> Seq.map (fun frame ->
            {
                CommentDescriptor = frame.Description
                Content = frame.Text
            })
        |> Seq.toList

    let getV1Comments (tag : TagLib.Id3v1.Tag) =
        [
            match tag.Comment with
            | null | "" -> ()
            | comment -> yield { CommentDescriptor = "ID3V1"; Content = comment }
        ]

    let getDuration useFallbackMethod (filePath : string) =
        let getDurationAudioFile (filePath : string) =
            use reader = new NAudio.Wave.AudioFileReader(filePath)

            reader.TotalTime

        let getDurationMediaFoundation (filePath : string) =
            use reader = new NAudio.Wave.MediaFoundationReader(filePath)

            reader.TotalTime

        // Some MP3 files have errors that, while perfectly playable, cause MediaFoundationReader to
        // report a very incorrect track duration (often off by tens of minutes). It is unclear what
        // exactly constitutes those errors, but what the affected files appear to have in common is
        // also reporting a (wrong) 32kbps bitrate. We use that as an indicator to fall back to
        // determining the length via AudioFileReader, which is much slower, but will return the
        // correct duration.
        // While not all files reporting a 32kbps bitrate actually require this, they are rare
        // enough not to cause this to impact overall scanning time in any noticeable way.
        if useFallbackMethod
        then
            try
                getDurationAudioFile filePath
            with
            | _ ->
                // AudioFileReader in turn is less resilient regarding other errors, so if it fails
                // to process a file, we fall back from the fallback.
                getDurationMediaFoundation filePath
        else
            getDurationMediaFoundation filePath

    let readTrack (fileInfo : FileInfo) relativePath : Result<MediaFile, ScanningError> =
        try
            let tag, comments, bitrate =
                use stream = new FileStream(fileInfo.FullName, FileMode.Open)
                use taglibFile =
                    TagLib.StreamFileAbstraction(fileInfo.FullName, stream, null)
                    |> TagLib.File.Create

                let tag, comments =
                    let v2Tag = taglibFile.GetTag TagLib.TagTypes.Id3v2
                    if not <| isNull v2Tag
                    then v2Tag, v2Tag :?> TagLib.Id3v2.Tag |> getV2Comments
                    else
                        let v1Tag = taglibFile.GetTag TagLib.TagTypes.Id3v1
                        if not <| isNull v1Tag
                        then v1Tag, v1Tag :?> TagLib.Id3v1.Tag |> getV1Comments
                        else TagLib.Id3v2.Tag() :> TagLib.Tag, []

                tag, comments, taglibFile.Properties.AudioBitrate

            let duration = getDuration (bitrate = 32) fileInfo.FullName

            let artist = string tag.FirstPerformer

            {
                MediaFile.Id = 0
                Number = byte tag.Track
                Title = string tag.Title
                AlbumArtist =
                    Option.ofObj tag.FirstAlbumArtist
                    |> Option.defaultValue artist
                Artist = artist
                Album = string tag.Album
                Genres = List.ofArray tag.Genres
                Year = tag.Year
                Comments = comments
                Duration = duration
                FilePath = relativePath
                FileSize = fileInfo.Length
                AddedOn = DateTime.UtcNow
                ModifiedOn = fileInfo.LastWriteTimeUtc
            }
            |> Ok
        with
        | :? TagLib.CorruptFileException as ex -> Error (CorruptFile fileInfo.FullName)

    // see https://weblog.west-wind.com/posts/2010/Dec/20/Finding-a-Relative-Path-in-NET
    let getRelativePath (basePath : string) fullPath =
            // Require trailing backslash for path
            let basePath =
                if not <| basePath.EndsWith "\\"
                then basePath + "\\"
                else basePath

            let baseUri = Uri basePath
            let fullUri = Uri fullPath

            let relativeUri = baseUri.MakeRelativeUri fullUri

            // Uri's use forward slashes so convert back to backward slashes
            relativeUri.ToString().Replace("/", "\\")
            |> Uri.UnescapeDataString

    let createReadFileActor reportError storeTrack signalScanningComplete (getExistingTrack : string -> MediaFile option) =
        let actor =
            MailboxProcessor.Start(fun inbox ->
                let rec loop() =
                    async {
                        let! message = inbox.Receive()

                        match message with
                        | Content(FileName (filePath, basePath)) ->
                            let fileInfo = FileInfo filePath

                            let relativePath =
                                filePath
                                |> getRelativePath basePath

                            match getExistingTrack relativePath with
                            | Some existingTrack ->
                                if (fileInfo.Length, fileInfo.LastWriteTimeUtc) = (existingTrack.FileSize, existingTrack.ModifiedOn)
                                then Ok existingTrack
                                else
                                    match readTrack fileInfo relativePath with
                                    | Ok track -> Ok { track with AddedOn = existingTrack.AddedOn }
                                    | Error _ as error -> error
                            | None -> readTrack fileInfo relativePath
                            |> function
                                | Ok track -> storeTrack track
                                | Error error -> reportError error
                        | EndOfInput -> signalScanningComplete()
                        | Reset -> ()

                        return! loop()
                    }

                loop())

        actor.Error.Add(fun e -> raise e)

        actor

    let createScanFileSystemActor processFileName =
        let actor =
            MailboxProcessor.Start(fun inbox ->
                let rec scan() =
                    async {
                        let! directory = inbox.Receive()

                        Directory.EnumerateFiles(directory, "*.mp3", SearchOption.AllDirectories)
                        |> Seq.iter (asFst directory >> FileName >> Content >> processFileName)

                        EndOfInput |> processFileName

                        return! scan()
                    }

                scan())

        actor.Error.Add(fun e -> raise e)

        actor

    // TODO: Make cancelable
    // TODO: Report recoverable errors
    // TODO: Report incomplete tags
    let scan getExistingTrack reportError reportTotal reportProgress outputTracks directory =
        let control = createControlActor reportTotal reportProgress outputTracks

        let storeTrack (track : MediaFile) = track |> Content |> ReadTrack |> control.Post
        let scanningCompleted () = EndOfInput |> ReadTrack |> control.Post

        let reportError error =
            reportError error
            control.Post (ScanningError error)

        let readFile = createReadFileActor reportError storeTrack scanningCompleted getExistingTrack

        let processFileName fileName =
            fileName |> ScannedFileName |> control.Post
            fileName |> readFile.Post

        let scanFileSystem = createScanFileSystemActor processFileName

        scanFileSystem.Post directory
