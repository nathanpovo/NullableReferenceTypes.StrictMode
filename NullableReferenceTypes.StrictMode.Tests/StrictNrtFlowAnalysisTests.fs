module ``Strict NRT Flow Analysis Tests``

open NullableReferenceTypes.StrictMode.Tests
open Xunit

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN assigning a null-oblivious property to a nullable variable, unchecked variable access SHOULD show diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        {{objectType}}? nullableObject = NullObliviousClass.NullObliviousProp;
        _ = [|nullableObject|].ToString();
    }
}

#nullable disable

static class NullObliviousClass
{
    public static {{objectType}} NullObliviousProp { get; set; } = null;
}
"""
