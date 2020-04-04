namespace QharonSzyne.Core.Tests.Classification

open System
open System.IO

open Xunit

module LastCommonBaseDirectoryTests =

    open QharonSzyne.Core.Classification

    [<Fact>]
    let test () =
        // Arrange
        let paths =
            [
                @"first\second\third"
                @"first\second\third1"
                @"first\second\third2"
                @"first\second\third3"
                @"first\second\third4"
            ]

        let expectedResult = @"first\second"

        // Act
        let result = lastCommonBaseDirectory paths

        // Assert
        Assert.Equal(expectedResult, result)

    [<Fact>]
    let test2 () =
        // Arrange
        let paths =
            [
                @"first\second\third"
                @"first\second\third1"
                @"first\second2\third2"
                @"first\second2\third3"
                @"first\second2\third4"
            ]

        let expectedResult = @"first"

        // Act
        let result = lastCommonBaseDirectory paths

        // Assert
        Assert.Equal(expectedResult, result)

    [<Fact>]
    let test3 () =
        // Arrange
        let paths =
            [
                @"first"
                @"first\second\third1"
                @"first\second\third2"
                @"first\second\third3"
                @"first\second\third4"
            ]

        let expectedResult = @"first"

        // Act
        let result = lastCommonBaseDirectory paths

        // Assert
        Assert.Equal(expectedResult, result)

    let x selector items =
        let paths =
            items
            |> List.map (selector >> Path.GetDirectoryName)
            |> List.sort

        let excludedPaths =
            paths
            |> List.filter (fun path ->
                paths |> List.exists (fun p -> p <> path && Uri(path).IsBaseOf(Uri p)))
            |> set

        items
        |> List.filter (fun item ->
            let path = selector item |> Path.GetDirectoryName

            Set.contains path excludedPaths |> not)

    [<Fact>]
    let test4 () =
        let a =
            [
                @"D:\x\asdf.mp3"
                @"D:\x\y\asdf.mp3"
            ]
            |> x id

        Assert.Equal<string>([ @"D:\x\y\asdf.mp3" ], a)