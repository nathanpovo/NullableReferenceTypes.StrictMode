module ``Field Tests``

open NullableReferenceTypes.StrictMode.Tests
open Xunit

[<Fact>]
let ``WHEN initialising a nullable enabled field with a nullable enabled property SHOULD not show any diagnostics`` () =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    private string testString;

    ClassUnderTest()
    {
        testString = new NullableEnabledClass().Test;
    }
}

class NullableEnabledClass
{
    public string Test { get; set; } = string.Empty;
}
"

[<Fact>]
let ``WHEN initialising a nullable field with a null-oblivious property SHOULD not show any diagnostics`` () =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    private string? testString;

    ClassUnderTest()
    {
        testString = new NullableObliviousClass().Test;
    }
}

#nullable disable

public class NullableObliviousClass
{
    public string Test { get; set; }
}
"

[<Fact>]
let ``WHEN initialising a non-null field with the return from a null-oblivious property SHOULD show diagnostics`` () =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    private string testString;

    ClassUnderTest()
    {
        testString = [|new NullableObliviousClass().Test|];
    }
}

#nullable disable

public class NullableObliviousClass
{
    public string Test { get; set; }
}
"

[<Fact>]
let ``WHEN initialising non-null fields with the return from a null-oblivious property SHOULD show diagnostics`` () =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    private string testString1;
    private string testString2;

    ClassUnderTest()
    {
        testString1 = testString2 = [|new NullableObliviousClass().Test|];
    }
}

#nullable disable

public class NullableObliviousClass
{
    public string Test { get; set; }
}
"

[<Fact>]
let ``WHEN initialising non-null fields (separately) with the return from a null-oblivious property SHOULD show diagnostics``
    ()
    =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    private string testString1;
    private string testString2;

    ClassUnderTest()
    {
        testString1 = [|new NullableObliviousClass().Test|];
        testString2 = [|new NullableObliviousClass().Test|];
    }
}

#nullable disable

public class NullableObliviousClass
{
    public string Test { get; set; }
}
"
