namespace QharonSzyne.Core

module Infrastructure =

    open System
    open System.IO

        [<RequireQualifiedAccess>]
        module Constants =
            [<Literal>]
            let ApplicationTechnicalName = "QharonSzyne"

            let ApplicationDataDirectory =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ApplicationTechnicalName)

            [<Literal>]
            let LibrariesDirectoryName = "Libraries"
