namespace NullableReferenceTypes.StrictMode.Tests

open Xunit

type ``Field Tests``() =

    [<Theory>]
    [<InlineData("object")>]
    [<InlineData("string")>]
    let ``WHEN initialising a nullable enabled field with a nullable enabled property SHOULD not show any diagnostics``
        (objectType: string)
        =
        NullableAnalyzerTests.VerifyNoDiagnosticAsync
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
        NullableAnalyzerTests.VerifyNoDiagnosticAsync
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
        NullableAnalyzerTests.VerifyStrictFlowAnalysisDiagnosticsAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    private {{objectType}} testString;

    ClassUnderTest()
    {
        testString = new NullableObliviousClass().Test;
    }
}

#if !EQUIVALENT_CODE
#nullable disable
#endif

public class NullableObliviousClass
{
#if EQUIVALENT_CODE
    public {{objectType}}? Test { get; set; }
#else
    public {{objectType}} Test { get; set; }
#endif
}
"""

    [<Theory>]
    [<InlineData("object")>]
    [<InlineData("string")>]
    let ``WHEN initialising non-null fields with the return from a null-oblivious property SHOULD show diagnostics``
        (objectType: string)
        =
        NullableAnalyzerTests.VerifyStrictFlowAnalysisDiagnosticsAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    private {{objectType}} testString1;
    private {{objectType}} testString2;

    ClassUnderTest()
    {
        testString1 = testString2 = new NullableObliviousClass().Test;
    }
}

#if !EQUIVALENT_CODE
#nullable disable
#endif

public class NullableObliviousClass
{
#if EQUIVALENT_CODE
    public {{objectType}}? Test { get; set; }
#else
    public {{objectType}} Test { get; set; }
#endif
}
"""

    [<Theory>]
    [<InlineData("object")>]
    [<InlineData("string")>]
    let ``WHEN initialising non-null fields (separately) with the return from a null-oblivious property SHOULD show diagnostics``
        (objectType: string)
        =
        NullableAnalyzerTests.VerifyStrictFlowAnalysisDiagnosticsAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    private {{objectType}} testString1;
    private {{objectType}} testString2;

    ClassUnderTest()
    {
        testString1 = new NullableObliviousClass().Test;
        testString2 = new NullableObliviousClass().Test;
    }
}

#if !EQUIVALENT_CODE
#nullable disable
#endif

public class NullableObliviousClass
{
#if EQUIVALENT_CODE
    public {{objectType}}? Test { get; set; }
#else
    public {{objectType}} Test { get; set; }
#endif
}
"""
