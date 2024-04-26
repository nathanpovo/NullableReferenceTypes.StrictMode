using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace NullableReferenceTypes.StrictMode;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullableAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic IDs to check for when checking the cloned compilation
    /// </summary>
    private static readonly string[] DiagnosticIds =
    [
        "CS8600",
        "CS8602",
        "CS8604",
        "CS8618",
        "CS8625",
        "CS8597"
    ];

    /// <summary>
    /// Cached compiled expressed to get the value of <see cref="DiagnosticWithInfo.Arguments"/> from a
    /// <see cref="DiagnosticWithInfo"/>.
    /// </summary>
    private static readonly Func<
        Diagnostic,
        IReadOnlyList<object>?
    > DiagnosticMessageArgumentsCompiledExpression = CreateMessageArgumentsCompiledExpression();

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        DiagnosticIds
            .Select(x => new DiagnosticDescriptor(
                $"NRTSM_{x}",
                string.Empty,
                string.Empty,
                string.Empty,
                DiagnosticSeverity.Warning,
                true
            ))
            .ToImmutableArray();

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSemanticModelAction(Action);
    }

    private static void Action(SemanticModelAnalysisContext context)
    {
        CancellationToken cancellationToken = context.CancellationToken;

        SemanticModel semanticModel = context.SemanticModel;
        SyntaxTree syntaxTree = semanticModel.SyntaxTree;
        SyntaxNode syntaxNode = syntaxTree.GetRoot(cancellationToken);

        SyntaxNode nullifiedSyntaxNode = new NullObliviousCodeRewriter(semanticModel).Visit(
            syntaxNode
        );

        IList<TextChange> changes = syntaxTree.GetChanges(nullifiedSyntaxNode.SyntaxTree);

        ImmutableArray<Diagnostic> compilationCloneDiagnostics = semanticModel
            .Compilation.Clone()
            .ReplaceSyntaxTree(syntaxTree, nullifiedSyntaxNode.SyntaxTree)
            .GetDiagnostics();

        IEnumerable<Diagnostic> diagnostics = compilationCloneDiagnostics
            .Where(x => DiagnosticIds.Contains(x.Id))
            .Select(x =>
            {
                object[]? messageArguments = GetDiagnosticMessageArguments(x);

                Location location = MapDiagnosticLocation(syntaxTree, changes, x.Location);

                return new
                {
                    Diagnostic = x,
                    Location = location,
                    MessageArguments = messageArguments
                };
            })
            .Select(x =>
                Diagnostic.Create(
                    CreateDescriptor(x.Diagnostic),
                    x.Location,
                    messageArgs: x.MessageArguments
                )
            );

        context.ReportDiagnostics(diagnostics);
    }

    private static Location MapDiagnosticLocation(
        SyntaxTree originalSyntaxNode,
        IList<TextChange> textChanges,
        Location location
    )
    {
        int start = MapPointToOriginalSyntaxTree(textChanges, location.SourceSpan.Start);
        int end = MapPointToOriginalSyntaxTree(textChanges, location.SourceSpan.End);

        TextSpan textSpan = TextSpan.FromBounds(start, end);

        return Location.Create(originalSyntaxNode, textSpan);
    }

    private static int MapPointToOriginalSyntaxTree(
        IEnumerable<TextChange> textChanges,
        int location
    )
    {
        // Maps a point to the original syntax tree by reversing all the changes that had been done to the syntax tree.
        // Only the changes that were applied before (in terms of span position) the location to be mapped need to be
        // applied since anything else does not affect the point.
        IEnumerable<int> changesToApply = textChanges
            .Where(x => x.Span.End <= location)
            .Select(x => x.Span.Length - (x.NewText?.Length ?? 0));

        return changesToApply.Aggregate(
            location,
            (currentLocation, differenceToApply) => currentLocation - differenceToApply
        );
    }

    private static DiagnosticDescriptor CreateDescriptor(Diagnostic diagnostic)
    {
        DiagnosticDescriptor originalDescriptor = diagnostic.Descriptor;

        return new DiagnosticDescriptor(
            $"NRTSM_{originalDescriptor.Id}",
            originalDescriptor.Title,
            originalDescriptor.MessageFormat,
            originalDescriptor.Category,
            originalDescriptor.DefaultSeverity,
            originalDescriptor.IsEnabledByDefault,
            originalDescriptor.Description
        );
    }

    private static object[]? GetDiagnosticMessageArguments(Diagnostic diagnostic) =>
        DiagnosticMessageArgumentsCompiledExpression(diagnostic) as object[];

    /// <summary>
    /// Creates a compiled expression to access the internal property <see cref="DiagnosticWithInfo.Arguments"/> inside
    /// the internal class <see cref="DiagnosticWithInfo"/>.
    /// <remarks>
    /// <para>
    /// Reflection had to be used because there is no public way to access the required data.
    /// </para>
    /// <para>
    /// The compiled expressed is a faster alternative to reflection that still allows us to access the internal
    /// <see cref="DiagnosticWithInfo.Arguments"/> property.
    /// </para>
    /// <para>
    /// This compiled expression was based on
    /// <a href="https://www.codeproject.com/Articles/1118828/Faster-than-Reflection-Delegates-Part">this post</a>.
    /// </para>
    /// <para>
    /// Additional resources:
    /// <br/>
    /// https://codeblog.jonskeet.uk/2008/08/09/making-reflection-fly-and-exploring-delegates/
    /// <br/>
    /// https://justinmchase.com/2009/09/01/dynamically-compiled-lambdas-vs-pure-reflection/
    /// <br/>
    /// https://stackoverflow.com/questions/7189642/speeding-up-reflection-invoke-c-net
    /// <br/>
    /// https://www.reddit.com/r/dotnet/comments/ddvgif/reflection_vs_delegate_setters_in_net_core_30/
    /// <br/>
    /// https://www.palmmedia.de/Blog/2012/2/4/reflection-vs-compiled-expressions-vs-delegates-performance-comparison
    /// </para>
    /// </remarks>
    /// </summary>
    ///
    private static Func<
        Diagnostic,
        IReadOnlyList<object>?
    > CreateMessageArgumentsCompiledExpression()
    {
        // An internal type in the "Microsoft.CodeAnalysis" assembly that contains some additional information that is
        // not included in the public type "Diagnostic" (which is the base type of "DiagnosticWithInfo").
        // https://stackoverflow.com/questions/1259222/how-to-access-internal-class-using-reflection
        Type? type = Type.GetType(
            "Microsoft.CodeAnalysis.DiagnosticWithInfo, Microsoft.CodeAnalysis"
        );

        if (type is null)
        {
            return _ => null;
        }

        // An internal property that contains a collection of objects that would be used to format the diagnostic
        // composite string.
        // https://learn.microsoft.com/en-us/dotnet/standard/base-types/composite-formatting
        PropertyInfo? propertyInfo = type.GetProperty(
            "Arguments",
            BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.Instance
        );

        if (propertyInfo is null)
        {
            return _ => null;
        }

        ParameterExpression sourceObjectParam = Expression.Parameter(typeof(Diagnostic));

        return Expression
            .Lambda<Func<Diagnostic, IReadOnlyList<object>>>(
                Expression.Call(
                    Expression.Convert(sourceObjectParam, type),
                    propertyInfo.GetMethod
                ),
                sourceObjectParam
            )
            .Compile();
    }
}
