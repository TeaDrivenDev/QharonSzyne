namespace QharonSzyne.Core.Tests.Classificati

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
