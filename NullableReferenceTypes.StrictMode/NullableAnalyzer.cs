using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    private static readonly string[] DiagnosticIds = ["CS8600", "CS8602", "CS8625", "CS8597"];

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
                Location location = MapDiagnosticLocation(syntaxTree, changes, x.Location);

                return new { Diagnostic = x, Location = location, };
            })
            .Select(x => Diagnostic.Create(CreateDescriptor(x.Diagnostic), x.Location));

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
}
