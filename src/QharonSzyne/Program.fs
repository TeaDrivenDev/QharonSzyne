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

            let tracksDatabase =
                System.IO.Path.Combine(
                    Core.Infrastructure.Constants.ApplicationDataDirectory,
                    Core.Infrastructure.Constants.LibrariesDirectoryName)
                |> Core.Database.LiteDB.LiteDbTracksDatabase
                //|> Core.Database.Sqlite.SqliteTracksDatabase

            view.DataContext <- new QharonSzyne.Core.ViewModels.ScannerViewModel(tracksDatabase)
            view.Show())

        let exitCode = app.Run()

        printfn "Enter the gates\nQharon awaits"

        exitCode
