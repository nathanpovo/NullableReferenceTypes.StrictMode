module ``Throw Expression Tests``

open NullableReferenceTypes.StrictMode.Tests
open Xunit

[<Theory>]
[<InlineData("System.Exception")>]
[<InlineData("System.InvalidOperationException")>]
[<InlineData("System.ArgumentException")>]
let ``WHEN initialising a new exception and using it in a throw expression SHOULD not show any diagnostics``
    (exceptionType: string)
    =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    public static void ExceptionThrower()
        => throw new {{exceptionType}}();
}
"""

[<Theory>]
[<InlineData("System.Exception")>]
[<InlineData("System.InvalidOperationException")>]
[<InlineData("System.ArgumentException")>]
let ``WHEN getting an exception from a null-oblivious method and using it in a throw expression SHOULD show diagnostics``
    (exceptionType: string)
    =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    public static void ExceptionThrower()
        => throw [|NullObliviousClass.CreateException()|];
}

#nullable disable

static class NullObliviousClass
{
    public static {{exceptionType}} CreateException()
        => null;
}
"""

[<Theory>]
[<InlineData("System.Exception")>]
[<InlineData("System.InvalidOperationException")>]
[<InlineData("System.ArgumentException")>]
let ``WHEN getting an exception from a null-oblivious generic method and using it in a throw expression SHOULD show diagnostics``
    (exceptionType: string)
    =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    public static void ExceptionThrower()
        => throw [|NullObliviousClass.CreateException<{{exceptionType}}>()|];
}

#nullable disable

static class NullObliviousClass
{
    public static TException CreateException<TException>()
        where TException : System.Exception, new()
        => null;
}
"""
