module ``Other Tests``

open NullableReferenceTypes.StrictMode.Tests
open Xunit

[<Fact>]
let ``WHEN initialising a reference type from a generic method SHOULD not show any diagnostics`` () =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        TestClass nonNullObject = CreateObject<TestClass>();
    }

    static T CreateObject<T>() where T : class, new()
        => new T();
}

class TestClass
{
    public TestClass() { }
}
"

[<Fact>]
let ``WHEN assigning the nullable return from a generic method to a nullable variable SHOULD not show any diagnostics``
    ()
    =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        string? maybeNullString = CreateNullString<string>();
    }

    static string? CreateNullString<T>() where T : class
    {
        return null;
    }
}
"

[<Fact>]
let ``WHEN assigning the null-oblivious return from a generic method to a var variable SHOULD not show any diagnostics``
    ()
    =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        var maybeNullString = CreateNullObliviousString<string?>();
    }

#nullable disable

    static T CreateNullObliviousString<T>() where T : class
    {
        return null;
    }
}
"

[<Fact>]
let ``WHEN assigning the null-oblivious return from a generic method to a nullable variable SHOULD not show any diagnostics``
    ()
    =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        string? maybeNullString = CreateNullObliviousString<string?>();
    }

#nullable disable

    public static T CreateNullObliviousString<T>() where T : class
    {
        return null;
    }
}
"

[<Fact>]
let ``WHEN assigning the null-oblivious return from a generic method to a non-nullable variable SHOULD show diagnostics``
    ()
    =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        string nonNullButNotReally = [|CreateNullObliviousString<string>()|];
    }

#nullable disable

    static T CreateNullObliviousString<T>() where T : class
    {
        return null;
    }
}
"
