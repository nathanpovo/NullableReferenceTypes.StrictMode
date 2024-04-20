namespace NullableReferenceTypes.StrictMode.Tests

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
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    public static void ExceptionThrower()
        => throw {|NRTSM_CS8597:NullObliviousClass.CreateException()|};
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
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    public static void ExceptionThrower()
        => throw {|NRTSM_CS8597:NullObliviousClass.CreateException<{{exceptionType}}>()|};
}

#nullable disable

static class NullObliviousClass
{
    public static TException CreateException<TException>()
        where TException : System.Exception, new()
        => null;
}
"""
