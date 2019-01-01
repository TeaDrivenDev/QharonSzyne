namespace QharonSzyne.Core.UIUtilities

open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Shell
open QharonSzyne.Core

type CurrentAndTotalToPercentageConverter () =
    static member Instance = CurrentAndTotalToPercentageConverter() :> IMultiValueConverter

    interface IMultiValueConverter with
        member this.Convert(values: obj [], targetType: System.Type, parameter: obj, culture: System.Globalization.CultureInfo): obj =
            match values with
            | [| :? int as current; :? int as total |] -> float current / float total
            | _ -> 0.
            :> obj

        member this.ConvertBack(value: obj, targetTypes: System.Type [], parameter: obj, culture: System.Globalization.CultureInfo): obj [] =
            raise (System.NotImplementedException())

type CurrentAndTotalToProgressStateConverter () =
    static member Instance = CurrentAndTotalToProgressStateConverter() :> IMultiValueConverter

    interface IMultiValueConverter with
        member this.Convert(values: obj [], targetType: System.Type, parameter: obj, culture: System.Globalization.CultureInfo): obj =
            match values with
            | [| :? int as current; :? int as total |] ->
                if total > 0 && current < total
                then TaskbarItemProgressState.Normal
                else TaskbarItemProgressState.None
            | _ -> TaskbarItemProgressState.None
            :> obj

        member this.ConvertBack(value: obj, targetTypes: System.Type [], parameter: obj, culture: System.Globalization.CultureInfo): obj [] =
            raise (System.NotImplementedException())

type StringConcatenationConverter() =
    static member Instance = StringConcatenationConverter() :> IValueConverter

    interface IValueConverter with
        member this.Convert(value: obj, targetType: System.Type, parameter: obj, culture: System.Globalization.CultureInfo): obj =
            match value, string parameter with
            | (:? (_ seq) as values), separator -> String.concat separator (values |> Seq.map string)
            | _ -> ""
            :> obj

        member this.ConvertBack(value: obj, targetType: System.Type, parameter: obj, culture: System.Globalization.CultureInfo): obj =
            raise (System.NotImplementedException())

type ScanResultTreeViewTemplateSelector() =
    inherit DataTemplateSelector()

    override __.SelectTemplate(item : obj, container : DependencyObject) =
        match item with
        | :? Classification.XArtist -> __.ArtistTemplate
        | _ -> null

    member val ArtistTemplate = Unchecked.defaultof<DataTemplate> with get, set

    member val ReleaseTemplate = Unchecked.defaultof<DataTemplate> with get, set
