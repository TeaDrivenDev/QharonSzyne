namespace QharonSzyne.Core.ViewModels

open System
open System.Collections.ObjectModel
open System.ComponentModel
open System.Linq
open System.Reactive.Concurrency
open System.Reactive.Disposables
open System.Windows

open FSharp.Control.Reactive
open FSharp.Quotations

open Reactive.Bindings

module Utilities =
    // see https://stackoverflow.com/a/48311816/236507
    let nameof (q : Expr) =
        match q with
        | Patterns.Let(_, _, DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _))) -> mi.Name
        | Patterns.PropertyGet(_, mi, _) -> mi.Name
        | DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _)) -> mi.Name
        | _ -> failwith "Unexpected format"

    let any<'R> : 'R = failwith "!"

    let addTo (compositeDisposable : CompositeDisposable) disposable =
        compositeDisposable.Add disposable
        disposable

    let withSubscribeAndDispose
        (compositeDisposable : CompositeDisposable)
        onNext
        (reactiveCommand : ReactiveCommand<_>) =
        reactiveCommand.WithSubscribe(Action<_> onNext, compositeDisposable.Add)

    let toReadOnlyReactiveProperty (observable : IObservable<_>) =
        observable.ToReadOnlyReactiveProperty()

open Utilities

open QharonSzyne.Core

// see http://www.fssnip.net/4Q/title/F-Quotations-with-INotifyPropertyChanged
type PropertyChangedBase() =
    let propertyChanged = new Event<_, _>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = propertyChanged.Publish

    abstract member OnPropertyChanged: string -> unit
    default x.OnPropertyChanged(propertyName : string) =
        propertyChanged.Trigger(x, new PropertyChangedEventArgs(propertyName))

    member x.OnPropertyChanged(expr : Expr) =
        x.OnPropertyChanged(nameof expr)

type Status =
    | Ready
    | Read of tracksInDatabase:int
    | Scanning
    | Error of Scanning.ScanningError
    | Storing
    | Done of filesFound:int

type ScannerViewModel(tracksDatabase : TracksDatabase.ITracksDatabase) =
    inherit PropertyChangedBase()

    let compositeDisposable = new CompositeDisposable()

    let sourceDirectory = new ReactiveProperty<_>("")
    let totalFiles = new ReactiveProperty<_>(0)
    let mutable scannedFiles = Unchecked.defaultof<ReadOnlyReactiveProperty<_>>
    let status = new ObservableCollection<_>()
    let mutable timeRemaining = Unchecked.defaultof<ReadOnlyReactiveProperty<_>>

    let updateExistingDatabase = new ReactiveProperty<_>(true)

    let statusSubject = new System.Reactive.Subjects.Subject<_>()

    let scannedFilesSubject = new System.Reactive.Subjects.Subject<_>()

    let mutable scanStartTime = DateTime.Now

    let library = ObservableCollection()

    let scanCommand =
        new ReactiveCommand<_>()
        |> withSubscribeAndDispose
            compositeDisposable
            (fun _ ->
                scanStartTime <- DateTime.Now

                let getExistingTrack =
                    if updateExistingDatabase.Value
                    then
                        tracksDatabase.Read("Default")
                        |> Option.map (fun tracks ->
                            tracks.Length |> Read |> statusSubject.OnNext

                            let tracksDictionary =
                                tracks.ToDictionary(fun track -> track.FilePath)

                            tracksDictionary.TryGetValue
                            >> function
                                | true, track -> Some track
                                | false, _ -> None)
                        |> Option.defaultValue (fun _ -> None)
                    else (fun _ -> None)

                statusSubject.OnNext Scanning

                Scanning.scan
                    getExistingTrack
                    (fun error -> Error error |> statusSubject.OnNext)
                    (fun n -> totalFiles.Value <- n)
                    (fun n -> scannedFilesSubject.OnNext n)
                    (fun tracks ->
                        statusSubject.OnNext Storing

                        tracksDatabase.Create("Default", tracks)

                        Classification.things tracks
                        |> List.iter (fun x ->
                            Application.Current.Dispatcher.Invoke(fun _ -> library.Add x))

                        statusSubject.OnNext (Done tracks.Length))
                    sourceDirectory.Value)

    let timestamp message =
        sprintf "%s %s" (DateTime.Now.ToString("HH:mm:ss.fff")) message

    do
        Infrastructure.MainThreadScheduler <- DispatcherScheduler(Application.Current.Dispatcher)

        statusSubject
        |> Observable.startWith [ Ready ]
        |> Observable.observeOn Infrastructure.MainThreadScheduler
        |> Observable.subscribe (fun newStatus ->
            match newStatus with
            | Ready -> "Ready"
            | Read tracksInDatabase ->
                sprintf "Read %i tracks from existing database" tracksInDatabase
            | Scanning -> "Scanning"
            | Error error ->
                match error with
                | Scanning.CorruptFile filePath ->
                    sprintf "Corrupt file: %s" filePath
            | Storing -> "Storing tracks to database"
            | Done filesFound -> sprintf "Done. %i files found" filesFound
            |> timestamp
            |> status.Add)
        |> addTo compositeDisposable
        |> ignore

        scannedFiles <-
            scannedFilesSubject
            |> Observable.sample (TimeSpan.FromMilliseconds 100.)
            |> toReadOnlyReactiveProperty
            |> addTo compositeDisposable

        timeRemaining <-
            scannedFiles
            |> Observable.map (fun scanned ->
                let elapsed  = (DateTime.Now - scanStartTime).TotalMilliseconds

                let perScanned = if scanned > 0 then elapsed / float scanned else 0.

                float (totalFiles.Value - scanned) * perScanned
                |> TimeSpan.FromMilliseconds)
            |> toReadOnlyReactiveProperty
            |> addTo compositeDisposable

        ([
            sourceDirectory
            totalFiles
            scanCommand
            updateExistingDatabase
            statusSubject
            scannedFilesSubject
        ] : IDisposable list)
        |> List.iter compositeDisposable.Add

    member __.SourceDirectory = sourceDirectory

    member __.TotalFiles = totalFiles
    member __.ScannedFiles = scannedFiles

    member __.Status = status

    member __.ScanCommand = scanCommand

    member __.UpdateExistingDatabase = updateExistingDatabase

    member __.TimeRemaining = timeRemaining

    member __.Library = library

    interface IDisposable with
        member this.Dispose(): unit =
            compositeDisposable.Dispose()
