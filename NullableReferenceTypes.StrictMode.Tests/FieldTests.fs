module ``Field Tests``

open NullableReferenceTypes.StrictMode.Tests
open Xunit

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN initialising a nullable enabled field with a nullable enabled property SHOULD not show any diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests.VerifyAnalyzerAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    private {{objectType}} testString;

    ClassUnderTest()
    {
        testString = new NullableEnabledClass().Test;
    }
}

class NullableEnabledClass
{
    public {{objectType}} Test { get; set; } = string.Empty;
}
"""

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN initialising a nullable field with a null-oblivious property SHOULD not show any diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests.VerifyAnalyzerAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    private {{objectType}}? testString;

    ClassUnderTest()
    {
        testString = new NullableObliviousClass().Test;
    }
}

#nullable disable

public class NullableObliviousClass
{
    public {{objectType}} Test { get; set; }
}
"""

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN initialising a non-null field with the return from a null-oblivious property SHOULD show diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests.VerifyAnalyzerAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    private {{objectType}} testString;

    ClassUnderTest()
    {
        testString = [|new NullableObliviousClass().Test|];
    }
}

#nullable disable

public class NullableObliviousClass
{
    public {{objectType}} Test { get; set; }
}
"""

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN initialising non-null fields with the return from a null-oblivious property SHOULD show diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests.VerifyAnalyzerAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    private {{objectType}} testString1;
    private {{objectType}} testString2;

    ClassUnderTest()
    {
        testString1 = testString2 = [|new NullableObliviousClass().Test|];
    }
}

#nullable disable

public class NullableObliviousClass
{
    public {{objectType}} Test { get; set; }
}
"""

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN initialising non-null fields (separately) with the return from a null-oblivious property SHOULD show diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests.VerifyAnalyzerAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    private {{objectType}} testString1;
    private {{objectType}} testString2;

    ClassUnderTest()
    {
        testString1 = [|new NullableObliviousClass().Test|];
        testString2 = [|new NullableObliviousClass().Test|];
    }
}

#nullable disable

public class NullableObliviousClass
{
    public {{objectType}} Test { get; set; }
}
"""
