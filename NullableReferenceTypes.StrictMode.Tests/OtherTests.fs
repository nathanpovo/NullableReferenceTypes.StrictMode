namespace NullableReferenceTypes.StrictMode.Tests

open Xunit

type ``Other Tests``() =

    [<Fact>]
    let ``WHEN initialising a reference type from a generic method SHOULD not show any diagnostics`` () =
        NullableAnalyzerTests.VerifyAnalyzerAsync
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

    [<Theory>]
    [<InlineData("object")>]
    [<InlineData("string")>]
    let ``WHEN assigning the nullable return from a generic method to a nullable variable SHOULD not show any diagnostics``
        (objectType: string)
        =
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        {{objectType}}? maybeNullObject = CreateNullObject<{{objectType}}>();
    }

    static T? CreateNullObject<T>() where T : class
    {
        return null;
    }
}
"""

    [<Theory>]
    [<InlineData("object")>]
    [<InlineData("string")>]
    let ``WHEN assigning the null-oblivious return from a generic method to a var variable SHOULD not show any diagnostics``
        (objectType: string)
        =
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        var maybeNullObject = CreateNullObliviousObject<{{objectType}}?>();
    }

#nullable disable

    static T CreateNullObliviousObject<T>() where T : class
    {
        return null;
    }
}
"""

    [<Theory>]
    [<InlineData("object")>]
    [<InlineData("string")>]
    let ``WHEN assigning the null-oblivious return from a generic method to a nullable variable SHOULD not show any diagnostics``
        (objectType: string)
        =
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        {{objectType}}? maybeNullObject = CreateNullObliviousObject<{{objectType}}?>();
    }

#nullable disable

    public static T CreateNullObliviousObject<T>() where T : class
    {
        return null;
    }
}
"""

    [<Theory>]
    [<InlineData("object")>]
    [<InlineData("string")>]
    let ``WHEN assigning the null-oblivious return from a generic method to a non-nullable variable SHOULD show diagnostics``
        (objectType: string)
        =
        NullableAnalyzerTests.VerifyAnalyzerAsync
            $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        {{objectType}} nonNullButNotReally = {|NRTSM_CS8600:CreateNullObliviousObject<{{objectType}}>()|};
    }

#nullable disable

    static T CreateNullObliviousObject<T>() where T : class
    {
        return null;
    }
}
"""
