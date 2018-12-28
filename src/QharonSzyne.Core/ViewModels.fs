namespace QharonSzyne.Core.ViewModels

open System
open System.Collections.ObjectModel
open System.ComponentModel
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

open Utilities

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
    | Scanning
    | Error of QharonSzyne.Core.Scanning.ScanningError
    | Done of filesFound:int

type ScannerViewModel() =
    inherit PropertyChangedBase()

    let compositeDisposable = new CompositeDisposable()

    let sourceDirectory = new ReactiveProperty<_>("")
    let totalFiles = new ReactiveProperty<_>(0)
    let scannedFiles = new ReactiveProperty<_>(0)
    let status = new ObservableCollection<_>()

    let statusSubject = new System.Reactive.Subjects.Subject<_>()

    let scanCommand =
        new ReactiveCommand<_>()
        |> withSubscribeAndDispose
            compositeDisposable
            (fun _ ->
                statusSubject.OnNext Scanning

                QharonSzyne.Core.Scanning.scan
                    (fun _ -> None)
                    (fun error -> Error error |> statusSubject.OnNext)
                    (fun n -> totalFiles.Value <- n)
                    (fun n -> scannedFiles.Value <- n)
                    (fun tracks -> statusSubject.OnNext (Done tracks.Length))
                    sourceDirectory.Value)

    do
        QharonSzyne.Core.Infrastructure.MainThreadScheduler <- DispatcherScheduler(Application.Current.Dispatcher)

        statusSubject
        |> Observable.startWith [ Ready ]
        |> Observable.observeOn QharonSzyne.Core.Infrastructure.MainThreadScheduler
        |> Observable.subscribe (fun newStatus ->
            match newStatus with
            | Ready -> "Ready"
            | Scanning -> "Scanning"
            | Error error ->
                match error with
                | QharonSzyne.Core.Scanning.CorruptFile filePath ->
                    sprintf "Corrupt file: %s" filePath
            | Done filesFound -> sprintf "Done. %i files found" filesFound
            |> status.Add)
        |> addTo compositeDisposable
        |> ignore

        ([
            sourceDirectory
            totalFiles
            scannedFiles
            scanCommand
        ] : IDisposable list)
        |> List.iter compositeDisposable.Add

    member __.SourceDirectory = sourceDirectory

    member __.TotalFiles = totalFiles
    member __.ScannedFiles = scannedFiles

    member __.Status = status

    member __.ScanCommand = scanCommand

    interface IDisposable with
        member this.Dispose(): unit =
            compositeDisposable.Dispose()
