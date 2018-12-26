namespace QharonSzyne.Core.Tests

open Xunit

module Tests =

    [<Theory>]
    [<InlineData(true)>]
    //[<InlineData(false)>]
    let test (value : bool) =
        Assert.True value
