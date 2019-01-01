namespace QharonSzyne.Core

module Classification =

    open System
    open System.IO

    open Model

    let lastCommonBaseDirectory (paths : string list) =
        let rec findMinimumCommonItems current max (items : _ [] list) =
            if current <= max
            then
                items
                |> List.map (Array.item current)
                |> function
                    | [] -> failwith "Should not get here"
                    | [ single ] -> Some single
                    | head :: rest ->
                        if rest |> List.forall ((=) head)
                        then Some head
                        else None
                |> Option.map (fun here -> here :: findMinimumCommonItems (current + 1) max items)
                |> Option.defaultValue []
            else []

        let splitPaths =
            paths
            |> List.map (String.split [| Path.DirectorySeparatorChar |])

        let minimumLength =
            splitPaths
            |> List.minBy Array.length
            |> Array.length

        findMinimumCommonItems 0 (minimumLength - 1) splitPaths
        |> String.concat (string Path.DirectorySeparatorChar)

    type UnclassifiedRelease =
        {
            Location : string
            Artist : string
            Title : string
            Year : uint32
            Tracks : MediaFile list
        }

    let (|SpecifyingName|_|) part (name : string) =
        let regex = sprintf @"[([]%s[)\]]$" part
        if System.Text.RegularExpressions.Regex.IsMatch(name.ToLower(), regex) then Some name else None

    let (|Single|_|) = (|SpecifyingName|_|) "single"
    let (|EP|_|) = (|SpecifyingName|_|) "ep"
    let (|Demo|_|) = (|SpecifyingName|_|) "demo"

    let releaseTypeNaive (release : UnclassifiedRelease) =
        match release.Title with
        | Single _ -> Single
        | EP _ -> EP
        | Demo _ -> Demo
        | _ ->
            let count = release.Tracks |> List.length
            let duration = release.Tracks
                            |> List.fold (fun acc current -> acc + current.Duration) (TimeSpan.FromSeconds 0.)

            if count <= 7 then
                if count <= 4 && duration <= TimeSpan.FromMinutes 25. then Single
                else if duration <= TimeSpan.FromMinutes 30. then EP else Album
            else Album

    let thingsForArtist (tracks : MediaFile list) =
        let releases =
            tracks
            |> List.groupBy (fun track -> Path.GetDirectoryName track.FilePath)
            |> List.map (fun (location, tracks) ->
                let first = List.head tracks

                {
                    Location = location
                    Artist = first.AlbumArtist
                    Title = first.Album
                    Year = first.Year
                    Tracks = tracks
                })
            |> List.sortBy (fun release -> release.Year)

        let namesSet (tracks : MediaFile list) =
            tracks |> List.map (fun t -> t.Title) |> Set.ofList

        let floatCount set = set |> Set.count |> float

        let rec cl (knownTracks : MediaFile list) (releases : UnclassifiedRelease list) =
            match releases with
            | [] -> []
            | release :: tail ->

                let releaseType, knownTracks =
                    match release |> releaseTypeNaive with
                    | Album ->
                        let allNames = namesSet knownTracks
                        let namesHere = namesSet release.Tracks

                        let notInAll = namesHere - allNames

                        if Set.count allNames > Set.count namesHere
                            && floatCount notInAll / floatCount namesHere < 0.25
                        then Live, knownTracks
                        else Album, knownTracks @ release.Tracks
                    | other -> other, knownTracks @ release.Tracks

                {
                    Id = 0
                    Artist = release.Artist
                    Location = ""
                    Title = release.Title
                    Year = release.Year
                    Genres = []
                    ReleaseType = releaseType
                    Tracks = []
                    AddedOn = DateTime.Now
                } :: (cl knownTracks tail)

        cl [] releases

    type XArtist =
        {
            Name : string
            Genres : string list
            Releases : Release list
        }

    let things (tracks : MediaFile list) =
        let byArtistName =
            tracks |> List.groupBy(fun track -> track.Artist)

        let byArtistLocation =
            byArtistName
            |> List.collect (fun (artist, tracks) ->
                tracks
                |> List.groupBy (fun track ->
                    track.FilePath |> Path.GetDirectoryName |> Path.GetDirectoryName)
                |> List.map (fun (location, tracks) -> artist, location, tracks))

        byArtistLocation
        |> List.map (fun (artist, _, tracks) ->
            {
                Name = artist
                Genres = tracks |> List.collect (fun track -> track.Genres) |> List.distinct
                Releases = thingsForArtist tracks
            })
