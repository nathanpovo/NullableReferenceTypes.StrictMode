module ``Method Tests``

open NullableReferenceTypes.StrictMode.Tests
open Xunit

[<Fact>]
let ``WHEN assigning the non-null return of a method to a non-null variable SHOULD not show any diagnostics`` () =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
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

[<Fact>]
let ``WHEN assigning the nullable return of a method to a nullable variable SHOULD not show any diagnostics`` () =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        string? maybeNullString = CreateNullString();
    }

    static string? CreateNullString()
    {
        return null;
    }
}
"

[<Fact>]
let ``WHEN assigning the null-oblivious return of a method to a nullable variable SHOULD not show any diagnostics`` () =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        string? maybeNullString = CreateNullObliviousString();
    }

#nullable disable

    static string CreateNullObliviousString()
    {
        return null;
    }
}
"

[<Fact>]
let ``WHEN assigning the null-oblivious return of a method to a var variable SHOULD not show any diagnostics`` () =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        var maybeNullString = CreateNullObliviousString();
    }

#nullable disable

    public static string CreateNullObliviousString()
    {
        return null;
    }
}
"

[<Fact>]
let ``WHEN assigning the null-oblivious return of a method to a non-null variable SHOULD show diagnostics`` () =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        string nonNullButNotReally = [|CreateNullObliviousString()|];
    }

#nullable disable

    public static string CreateNullObliviousString()
    {
        return null;
    }
}
"
