namespace QharonSzyne

[<RequireQualifiedAccess>]
module Scanning =

    open System
    open System.IO
    open System.Linq

    open Model

    type Message<'T> =
    | Content of 'T
    | EndOfInput
    | Reset

    type FileName = FileName of string

    let createControlActor reportTotal reportProgress outputTracks =
        MailboxProcessor.Start(fun inbox ->
            let reset = 0, List.empty<Track>

            let rec loop (total, tracks) =
                async {
                    let! message = inbox.Receive()

                    let newState =
                        match message with
                        | Choice1Of2 fileName ->
                            match fileName with
                            | Content(FileName _) -> total + 1, tracks
                            | EndOfInput -> reportTotal total; total, tracks
                            | Reset -> reset
                        | Choice2Of2 track ->
                            match track with
                            | Content track ->
                                let tracks = track :: tracks
                                reportProgress tracks.Length
                                total, tracks
                            | EndOfInput -> outputTracks tracks; total, tracks
                            | Reset -> reset

                    return! loop newState
                }

            loop reset)

    let getV2Comments (tag : TagLib.Id3v2.Tag) =
        tag.OfType<TagLib.Id3v2.CommentsFrame>()
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

    let getDuration (fileName : string) =
        use reader = new NAudio.Wave.MediaFoundationReader(fileName)

        reader.TotalTime

    let readTrack (fileInfo : FileInfo) =
        use stream = new FileStream(fileInfo.FullName, FileMode.Open)

        let duration = getDuration fileInfo.FullName

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

        {
            Number = byte tag.Track
            Title = string tag.Title
            Artist = string tag.FirstPerformer
            Album = string tag.Album
            Genres = List.ofArray tag.Genres
            Year = Some tag.Year
            Comments = comments
            Duration = duration
            FilePath = fileInfo.FullName
            FileSize = fileInfo.Length
            AddedOn = DateTime.UtcNow
            ModifiedOn = fileInfo.LastWriteTimeUtc
        }

    let createReadFileActor storeTrack signalScanningComplete getExistingTrack =
        let actor =
            MailboxProcessor.Start(fun inbox ->
                let rec loop() =
                    async {
                        let! message = inbox.Receive()

                        match message with
                        | Content(FileName fileName) ->
                            let fileInfo = FileInfo fileName

                            match getExistingTrack fileName with
                            | Some track ->
                                if (fileInfo.Length, fileInfo.LastWriteTimeUtc) = (track.FileSize, track.ModifiedOn)
                                then track
                                else { readTrack fileInfo with AddedOn = track.AddedOn }
                            | None -> readTrack fileInfo
                            |> storeTrack
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
                        |> Seq.iter (FileName >> Content >> processFileName)

                        EndOfInput |> processFileName

                        return! scan()
                    }

                scan())

        actor.Error.Add(fun e -> raise e)

        actor

    // TODO: Make cancelable
    let scan getExistingTrack reportTotal reportProgress outputTracks directory =
        let control = createControlActor reportTotal reportProgress outputTracks

        let storeTrack track = track |> Content |> Choice2Of2 |> control.Post
        let scanningCompleted() = EndOfInput |> Choice2Of2 |> control.Post

        let readFile = createReadFileActor storeTrack scanningCompleted getExistingTrack

        let processFileName fileName =
            fileName |> Choice1Of2 |> control.Post
            fileName |> readFile.Post

        let scanFileSystem = createScanFileSystemActor processFileName

        scanFileSystem.Post directory
