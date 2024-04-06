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

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN assigning a null-oblivious property to a nullable variable, checked variable access SHOULD not show diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        {{objectType}}? nullableObject = NullObliviousClass.NullObliviousProp;

        if (nullableObject is not null)
        {
            _ = nullableObject.ToString();
        }
    }
}

#nullable disable

static class NullObliviousClass
{
    public static {{objectType}} NullObliviousProp { get; set; } = null;
}
"""

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN assigning a null-oblivious property to a var variable, unchecked variable access SHOULD show diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        var nullableObject = NullObliviousClass.NullObliviousProp;
        _ = [|nullableObject|].ToString();
    }
}

#nullable disable

static class NullObliviousClass
{
    public static {{objectType}} NullObliviousProp { get; set; } = null;
}
"""

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN assigning a null-oblivious property to a var variable, checked variable access SHOULD not show diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        var nullableObject = NullObliviousClass.NullObliviousProp;

        if (nullableObject is not null)
        {
            _ = nullableObject.ToString();
        }
    }
}

#nullable disable

static class NullObliviousClass
{
    public static {{objectType}} NullObliviousProp { get; set; } = null;
}
"""

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN assigning the null-oblivious return of a method to a var variable, unchecked variable access SHOULD show diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        var maybeNullObject = CreateNullObliviousObject();
        _ = [|maybeNullObject|].ToString();
    }

#nullable disable

    public static {{objectType}} CreateNullObliviousObject()
    {
        return null;
    }
}
"""

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN assigning the null-oblivious return of a method to a var variable, checked variable access SHOULD not show diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        var maybeNullObject = CreateNullObliviousObject();

        if (maybeNullObject is not null)
        {
            _ = maybeNullObject.ToString();
        }
    }

#nullable disable

    public static {{objectType}} CreateNullObliviousObject()
    {
        return null;
    }
}
"""

[<Theory>]
[<InlineData("object")>]
[<InlineData("string")>]
let ``WHEN assigning the null-oblivious return from a generic method to a var variable, unchecked variable access SHOULD show diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        var maybeNullObject = CreateNullObliviousObject<{{objectType}}>();
        _ = [|maybeNullObject|].ToString();
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
[<InlineData("object?")>]
[<InlineData("string?")>]
let ``WHEN assigning the null-oblivious return from a generic method to a var variable, checked variable access SHOULD not show diagnostics``
    (objectType: string)
    =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        $$"""
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        var maybeNullObject = CreateNullObliviousObject<{{objectType}}>();

        if (maybeNullObject is not null)
        {
            _ = maybeNullObject.ToString();
        }
    }

#nullable disable

    static T CreateNullObliviousObject<T>() where T : class
    {
        return null;
    }
}
"""
