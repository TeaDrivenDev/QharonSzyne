namespace QharonSzyne

module Program =

    open System

    open QharonSzyne.UI.Views

    [<STAThread>]
    [<EntryPoint>]
    let main argv =
        let app = App()
        app.Startup.Add (fun _ ->
            let view = ScannerView()
            view.DataContext <- new QharonSzyne.Core.ViewModels.ScannerViewModel()
            view.Show())

        let exitCode = app.Run()

        printfn "Enter the gates\nQharon awaits"

        exitCode
