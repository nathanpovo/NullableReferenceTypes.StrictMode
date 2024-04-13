using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace NullableReferenceTypes.StrictMode;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullableAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic IDs to check for when checking the cloned compilation
    /// </summary>
    private static readonly string[] DiagnosticIds = ["CS8600", "CS8625", "CS8597"];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        DiagnosticIds
            .Concat(["CS8602"])
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

        SyntaxNode annotatedSyntaxNode = new NullObliviousCodeAnnotator(semanticModel).Visit(
            syntaxNode
        );

        SyntaxNode nullifiedSyntaxNode = new SyntaxRewriter().Visit(annotatedSyntaxNode);

        ImmutableArray<Diagnostic> compilationCloneDiagnostics = semanticModel
            .Compilation.Clone()
            .ReplaceSyntaxTree(syntaxTree, nullifiedSyntaxNode.SyntaxTree)
            .GetDiagnostics();

        IEnumerable<Diagnostic> diagnostics = compilationCloneDiagnostics
            .Where(x => DiagnosticIds.Contains(x.Id))
            .Select(x =>
            {
                Location? location = GetOriginalNodeLocation(
                    syntaxTree,
                    annotatedSyntaxNode,
                    nullifiedSyntaxNode,
                    x
                );

                return new { Diagnostic = x, Location = location };
            })
            .Where(x => x.Location is not null)
            .Select(x => new { x.Diagnostic, Location = x.Location ?? Location.None })
            .Select(x => Diagnostic.Create(CreateDescriptor(x.Diagnostic), x.Location));

        context.ReportDiagnostics(diagnostics);

        // Get all the nullable diagnostics that are already present in the compilation
        // This will be used to ensure that the newly-found diagnostics are not already present in the compilation
        // If this check is not done then the user would see duplicate diagnostics
        List<Location> existingCs8602Diagnostics = semanticModel
            .GetDiagnostics(cancellationToken: cancellationToken)
            .Where(x => x.Id == "CS8602")
            .Select(x => x.Location)
            .ToList();

        IEnumerable<Diagnostic> cs8602Diagnostics = compilationCloneDiagnostics
            .Where(x => x.Id == "CS8602")
            .Select(x =>
            {
                Location? location = GetOriginalNodeLocation(syntaxNode, nullifiedSyntaxNode, x);

                return new { Diagnostic = x, Location = location };
            })
            .Where(x => x.Location is not null)
            .Select(x => new { x.Diagnostic, Location = x.Location ?? Location.None })
            .Where(x => existingCs8602Diagnostics.Contains(x.Location) == false)
            .Select(x => Diagnostic.Create(CreateDescriptor(x.Diagnostic), x.Location));

        context.ReportDiagnostics(cs8602Diagnostics);
    }

    /// <summary>
    /// Tries to get the original node location that is covered by the diagnostic in the modified compilation
    /// </summary>
    private static Location? GetOriginalNodeLocation(
        SyntaxNode originalSyntaxNode,
        SyntaxNode modifiedSyntaxNode,
        Diagnostic diagnostic
    )
    {
        SyntaxNode modifiedNode = modifiedSyntaxNode.FindNode(diagnostic.Location.SourceSpan);

        SyntaxKind nodeKind = modifiedNode.Kind();

        return originalSyntaxNode
            .DescendantNodes(_ => true)
            .Where(x => x.IsKind(nodeKind))
            .FirstOrDefault(x => x.IsEquivalentTo(modifiedNode))
            ?.GetLocation();
    }

    /// <summary>
    /// Tries to get the original location of the node that is covered by the diagnostic in the modified compilation
    /// </summary>
    private static Location? GetOriginalNodeLocation(
        SyntaxTree originalSyntaxTree,
        SyntaxNode annotatedSyntaxNode,
        SyntaxNode modifiedSyntaxNode,
        Diagnostic diagnostic
    )
    {
        SyntaxNode modifiedNode = modifiedSyntaxNode.FindNode(
            diagnostic.Location.SourceSpan,
            getInnermostNodeForTie: true
        );

        SyntaxAnnotation? annotation = modifiedNode
            .GetAnnotations(AnnotationKind.NullObliviousCode)
            .SingleOrDefault();

        if (annotation is null)
        {
            return null;
        }

        TextSpan? span = annotatedSyntaxNode.GetAnnotatedNodes(annotation).SingleOrDefault()?.Span;

        if (span is null)
        {
            return null;
        }

        return Location.Create(originalSyntaxTree, span.Value);
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
