namespace NullableReferenceTypes.StrictMode.Tests

open Xunit

type ``Parameter Tests``() =

    [<Theory>]
    [<InlineData("object")>]
    [<InlineData("string")>]
    let ``WHEN passing a null-oblivious property to a nullable parameter SHOULD not show any diagnostics``
        (objectType: string)
        =
        NullableAnalyzerTests.VerifyNoDiagnosticAsync
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
        NullableAnalyzerTests.VerifyNoDiagnosticAsync
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
    let ``WHEN passing a null-oblivious property to a non-null parameter SHOULD show diagnostics``
        (objectType: string)
        =
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        Method({|NRTSM_CS8625:new NullableObliviousClass().Test|});
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
    let ``WHEN passing a null-oblivious property to non-null parameters SHOULD show diagnostics`` (objectType: string) =
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        Method({|NRTSM_CS8625:new NullableObliviousClass().Test|}, {|NRTSM_CS8625:new NullableObliviousClass().Test|});
    }

    void Method({{objectType}} testObject1, {{objectType}} testObject2)
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
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        Method({|NRTSM_CS8625:new NullableObliviousClass().Test|});
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

    [<Theory>]
    [<InlineData("object", "float?")>]
    [<InlineData("object", "int?")>]
    [<InlineData("object", "string?")>]
    [<InlineData("string", "float?")>]
    [<InlineData("string", "int?")>]
    [<InlineData("string", "object?")>]
    let ``WHEN passing a null-oblivious field to an overloaded method with a non-null parameter SHOULD show diagnostics``
        (objectType: string, parameterType: string)
        =
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        Method({|NRTSM_CS8625:new NullableObliviousClass().Test|});
    }

    void Method({{objectType}} testString)
    {
    }

    void Method({{parameterType}} testObject1)
    {
    }
}

#nullable disable

public class NullableObliviousClass
{
    public {{objectType}} Test;
}
"""
