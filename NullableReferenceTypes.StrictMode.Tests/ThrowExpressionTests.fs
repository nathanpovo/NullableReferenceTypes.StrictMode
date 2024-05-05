namespace NullableReferenceTypes.StrictMode.Tests

open AnalyzerTestFramework
open Xunit

type ``Throw Expression Tests``() =

    [<Theory>]
    [<InlineData("System.Exception")>]
    [<InlineData("System.InvalidOperationException")>]
    [<InlineData("System.ArgumentException")>]
    let ``WHEN initialising a new exception and using it in a throw expression SHOULD not show any diagnostics``
        (exceptionType: string)
        =
        NullableAnalyzerTests.VerifyNoDiagnosticAsync
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
        NullableAnalyzerTests.VerifyStrictFlowAnalysisDiagnosticsAsync
            [ CS8597 ]
            $$"""
#nullable enable

class ClassUnderTest
{
    public static void ExceptionThrower()
        => throw NullObliviousClass.CreateException();
}

#if !EQUIVALENT_CODE
#nullable disable
#endif

static class NullObliviousClass
{
#if EQUIVALENT_CODE
    public static {{exceptionType}}? CreateException()
#else
    public static {{exceptionType}} CreateException()
#endif
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
        NullableAnalyzerTests.VerifyStrictFlowAnalysisDiagnosticsAsync
            [ CS8597 ]
            $$"""
#nullable enable

class ClassUnderTest
{
    public static void ExceptionThrower()
        => throw NullObliviousClass.CreateException<{{exceptionType}}>();
}

#if !EQUIVALENT_CODE
#nullable disable
#endif

static class NullObliviousClass
{
#if EQUIVALENT_CODE
    public static TException? CreateException<TException>()
#else
    public static TException CreateException<TException>()
#endif
        where TException : System.Exception, new()
        => null;
}
"""
