#I "..\..\packages"

#r "System.IO"

#r @"TagLib.Portable\lib\portable-net45+win+wpa81+wp8+MonoAndroid10+xamarinios10+MonoTouch10\TagLib.Portable.dll"
#r @"NAudio\lib\net35\NAudio.dll"

#load "Model.fs"
#load "Scanning.fs"

open System
open System.IO
open System.Linq

open QharonSzyne

let directory = @"F:\Music\Speed Metal"

Scanning.scan
    (fun _ -> None)
    (printfn "%i tracks found")
    ignore
    (fun tracks -> printfn "Total duration: %O" ((TimeSpan.Zero, tracks) ||> List.fold (fun acc track -> acc + track.Duration)))
    directory

let hellPatrol = @"F:\Music\Thrash Metal\Angelus Apatrida\[2012] The Call\11 - Hell Patrol.mp3"

let track =
    FileInfo hellPatrol
    |> Scanning.readTrack

let getTagLibFile (path : string) =
    use stream = new FileStream(path, FileMode.Open)

    (path, stream, null)
    |> TagLib.StreamFileAbstraction
    |> TagLib.File.Create

let getTag (path : string) =
    use file = getTagLibFile path

    file.GetTag TagLib.TagTypes.Id3v2 :?> TagLib.Id3v2.Tag

//let tag = getTag hellPatrol

//let comments = tag.OfType<TagLib.Id3v2.CommentsFrame>()

let getDurationMp3 (fileName : string) =
    let duration =
        use reader = new NAudio.Wave.Mp3FileReader(fileName)
        reader.TotalTime

    use file = getTagLibFile fileName

    file.Properties.AudioBitrate, duration.ToString()

let getDurationAudio (fileName : string) =
    let duration =
        use reader = new NAudio.Wave.AudioFileReader(fileName)
        reader.TotalTime

    use file = getTagLibFile fileName

    file.Properties.AudioBitrate, duration.ToString()

let getDurationMediaFoundation (fileName : string) =
    let duration =
        use reader = new NAudio.Wave.MediaFoundationReader(fileName)
        reader.TotalTime

    use file = getTagLibFile fileName

    file.Properties.AudioBitrate, duration.ToString()

#time "on"
getDurationMediaFoundation @"D:\Development\Staging\QharonSzyne\Testdata\Power Metal\Brainstorm\[2001] Metus Mortis\14 - Don't Wait For Me.mp3"
#time "off"

#time "on"
getDurationAudio @"D:\Development\Staging\QharonSzyne\Testdata\Power Metal\Brainstorm\[2001] Metus Mortis\14 - Don't Wait For Me.mp3"
#time "off"

#time "on"
getDurationMp3 @"D:\Development\Staging\QharonSzyne\Testdata\Power Metal\Brainstorm\[2001] Metus Mortis\14 - Don't Wait For Me.mp3"
#time "off"

let x = Unchecked.defaultof<TagLib.File>
