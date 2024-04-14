namespace NullableReferenceTypes.StrictMode.Tests

open Xunit

type ``Method Tests``() =

    [<Fact>]
    let ``WHEN assigning the non-null return of a method to a non-null variable SHOULD not show any diagnostics`` () =
        NullableAnalyzerTests.VerifyAnalyzerAsync
            @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        string nonNullString = CreateNonNullString();
    }

    static string CreateNonNullString()
    {
        return string.Empty;
    }
}
"

    [<Theory>]
    [<InlineData("object")>]
    [<InlineData("string")>]
    let ``WHEN assigning the nullable return of a method to a nullable variable SHOULD not show any diagnostics``
        (objectType: string)
        =
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        {{objectType}}? maybeNullObject = CreateNullObject();
    }

    static {{objectType}}? CreateNullObject()
    {
        return null;
    }
}
"""

    [<Theory>]
    [<InlineData("object")>]
    [<InlineData("string")>]
    let ``WHEN assigning the null-oblivious return of a method to a nullable variable SHOULD not show any diagnostics``
        (objectType: string)
        =
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        {{objectType}}? maybeNullObject = CreateNullObliviousObject();
    }

#nullable disable

    static {{objectType}} CreateNullObliviousObject()
    {
        return null;
    }
}
"""

    [<Theory>]
    [<InlineData("object")>]
    [<InlineData("string")>]
    let ``WHEN assigning the null-oblivious return of a method to a var variable SHOULD not show any diagnostics``
        (objectType: string)
        =
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        var maybeNullObject = CreateNullObliviousObject();
    }

#nullable disable

    public static {{objectType}} CreateNullObliviousObject()
    {
        return null;
    }
}
"""

    [<Theory>]
    [<InlineData("object")>]
    [<InlineData("string")>]
    let ``WHEN assigning the null-oblivious return of a method to a non-null variable SHOULD show diagnostics``
        (objectType: string)
        =
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        {{objectType}} nonNullButNotReally = {|NRTSM_CS8600:CreateNullObliviousObject()|};
    }

#nullable disable

    public static {{objectType}} CreateNullObliviousObject()
    {
        return null;
    }
}
"""
