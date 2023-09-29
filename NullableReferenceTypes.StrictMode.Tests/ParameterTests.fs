module ``Parameter Tests``

open NullableReferenceTypes.StrictMode.Tests
open Xunit

[<Fact>]
let ``WHEN passing a null-oblivious property to a nullable parameter SHOULD not show any diagnostics`` () =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        Method(new NullableObliviousClass().Test);
    }

    void Method(string? testString)
    {
    }
}

#nullable disable

public class NullableObliviousClass
{
    public string Test { get; set; }
}
"

[<Fact>]
let ``WHEN passing a null-oblivious field to a nullable parameter SHOULD not show any diagnostics`` () =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        Method(new NullableObliviousClass().Test);
    }

    void Method(string? testString)
    {
    }
}

#nullable disable

public class NullableObliviousClass
{
    public string Test;
}
"

[<Fact>]
let ``WHEN passing a null-oblivious property to a non-null parameter SHOULD show diagnostics`` () =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        Method([|new NullableObliviousClass().Test|]);
    }

    void Method(string testString)
    {
    }
}

#nullable disable

public class NullableObliviousClass
{
    public string Test { get; set; }
}
"

[<Fact>]
let ``WHEN passing a null-oblivious field to a non-null parameter SHOULD show diagnostics`` () =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        Method([|new NullableObliviousClass().Test|]);
    }

    void Method(string testString)
    {
    }
}

#nullable disable

public class NullableObliviousClass
{
    public string Test;
}
"
