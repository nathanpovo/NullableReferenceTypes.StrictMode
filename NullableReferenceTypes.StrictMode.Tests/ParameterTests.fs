module ``Parameter Tests``

open NullableReferenceTypes.StrictMode.Tests
open Xunit

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN passing a null-oblivious property to a nullable parameter SHOULD not show any diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        Method(new NullableObliviousClass().Test);
    }

    void Method({{objectType}}? testObject)
    {
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
let ``WHEN passing a null-oblivious field to a nullable parameter SHOULD not show any diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        Method(new NullableObliviousClass().Test);
    }

    void Method({{objectType}}? testObject)
    {
    }
}

#nullable disable

public class NullableObliviousClass
{
    public {{objectType}} Test;
}
"""

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN passing a null-oblivious property to a non-null parameter SHOULD show diagnostics`` (objectType: string) =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        Method([|new NullableObliviousClass().Test|]);
    }

    void Method({{objectType}} testObject)
    {
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
let ``WHEN passing a null-oblivious field to a non-null parameter SHOULD show diagnostics`` (objectType: string) =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        Method([|new NullableObliviousClass().Test|]);
    }

    void Method({{objectType}} testString)
    {
    }
}

#nullable disable

public class NullableObliviousClass
{
    public {{objectType}} Test;
}
"""
