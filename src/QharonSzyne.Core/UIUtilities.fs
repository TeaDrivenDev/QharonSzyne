namespace QharonSzyne.Core.UIUtilities

open System.Windows.Data
open System.Windows.Shell

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
