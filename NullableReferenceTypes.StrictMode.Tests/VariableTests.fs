module ``Variable Tests``

open NullableReferenceTypes.StrictMode.Tests
open Xunit

[<Theory>]
[<InlineData("string", " = string.Empty;")>]
[<InlineData("string?", "")>]
let ``WHEN initialising a nullable enabled variable with a nullable enabled property SHOULD not show any diagnostics``
    (variableType: string, typeInitialiser: string)
    =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        $@"
#nullable enable

namespace TestApp
{{
    internal static class Program
    {{
        private static void Main()
        {{
            {variableType} testString = new NullableEnabledClass().Test;
        }}
    }}

    public class NullableEnabledClass
    {{
        public {variableType} Test {{ get; set; }}{typeInitialiser}
    }}
}}
"

[<Fact>]
let ``WHEN initialising a nullable variable with a null-oblivious property SHOULD not show any diagnostics`` () =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        @"
#nullable enable

class ClassUnderTest
{
    ClassUnderTest()
    {
        string? testString = new NullableObliviousClass().Test;
    }
}

#nullable disable

public class NullableObliviousClass
{
    public string Test { get; set; }
}
"

[<Theory>]
[<InlineData("string", " = string.Empty;")>]
[<InlineData("string?", "")>]
let ``WHEN initialising a var variable with a nullable enabled property SHOULD not show any diagnostics``
    (variableType: string, typeInitialiser: string)
    =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        $@"
#nullable enable

namespace TestApp
{{
    internal static class Program
    {{
        private static void Main()
        {{
            var testString = new NullableEnabledClass().Test;
        }}
    }}

    public class NullableEnabledClass
    {{
        public {variableType} Test {{ get; set; }}{typeInitialiser}
    }}
}}
"

[<Theory>]
[<InlineData("string", " = string.Empty;")>]
[<InlineData("string?", "")>]
let ``WHEN initialising a var variable with a nullable enabled property SHOULD not show any diagnostics2``
    (variableType: string, typeInitialiser: string)
    =
    NullableAnalyzerTests().VerifyNoDiagnosticAsync
        $@"
#nullable enable
public class ClassUnderTest
{{
    public void MethodUnderTest()
    {{
        var testString = new NullableEnabledClass().Test;
    }}
}}

public class NullableEnabledClass
{{
    public {variableType} Test {{ get; set; }}{typeInitialiser}
}}
"

[<Fact>]
let ``WHEN initialising a variable with the return from a null-oblivious property SHOULD show diagnostics`` () =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        @"
#nullable enable

namespace TestApp
{
    internal static class Program
    {
        private static void Main()
        {
            string nonNullButNotReally = [|new NullableObliviousClass().Test|];
        }
    }

#nullable disable

    public class NullableObliviousClass
    {
        public string Test { get; set; } = null;
    }
}
"

[<Fact>]
let ``WHEN initialising variables with the return from a null-oblivious property SHOULD show diagnostics`` () =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        @"
#nullable enable

namespace TestApp
{
    internal static class Program
    {
        private static void Main()
        {
            string nonNullButNotReally = [|new NullableObliviousClass().Test|],
                nonNullButNotReally2 = [|new NullableObliviousClass().Test|];
        }
    }

#nullable disable

    public class NullableObliviousClass
    {
        public string Test { get; set; }
    }
}
"


[<Fact>]
let ``WHEN initialising variables (separately) with the return from a null-oblivious property SHOULD show diagnostics``
    ()
    =
    NullableAnalyzerTests().VerifyDiagnosticAsync
        @"
#nullable enable

namespace TestApp
{
    internal static class Program
    {
        private static void Main()
        {
            string nonNullButNotReally = [|new NullableObliviousClass().Test|];
            string nonNullButNotReally2 = [|new NullableObliviousClass().Test|];
        }
    }

#nullable disable

    public class NullableObliviousClass
    {
        public string Test { get; set; }
    }
}
"
