namespace QharonSzyne.Core.ViewModels

open System
open System.ComponentModel
open System.Reactive.Disposables
open System.Windows

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
        reactiveCommand.WithSubscribe(onNext, compositeDisposable.Add)

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

type ScannerViewModel() =
    inherit PropertyChangedBase()

    let compositeDisposable = new CompositeDisposable()

    let sourceDirectory = new ReactiveProperty<_>("")
    let totalFiles = new ReactiveProperty<_>(0)
    let scannedFiles = new ReactiveProperty<_>(0)
    let status = new ReactiveProperty<_>("Ready")

    let messageBoxCommand =
        new ReactiveCommand<_>()
        |> withSubscribeAndDispose
            compositeDisposable
            (Action<_>(fun _ -> MessageBox.Show("From ViewModel") |> ignore))

    do

        ([
            sourceDirectory
            totalFiles
            scannedFiles
            status
            messageBoxCommand
        ] : IDisposable list)
        |> List.iter compositeDisposable.Add

    member __.SourceDirectory = sourceDirectory

    member __.TotalFiles = totalFiles
    member __.ScannedFiles = scannedFiles

    member __.Status = status

    member __.MessageBoxCommand = messageBoxCommand

    interface IDisposable with
        member this.Dispose(): unit =
            compositeDisposable.Dispose()
