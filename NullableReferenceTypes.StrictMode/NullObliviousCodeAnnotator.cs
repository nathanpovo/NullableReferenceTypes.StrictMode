using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullableReferenceTypes.StrictMode;

internal class NullObliviousCodeAnnotator : CSharpSyntaxRewriter
{
    private readonly SemanticModel semanticModel;

    internal NullObliviousCodeAnnotator(SemanticModel semanticModel)
    {
        this.semanticModel = semanticModel;
    }

    private static bool IsSymbolNullOblivious(ISymbol? symbol) =>
        symbol switch
        {
            IPropertySymbol { NullableAnnotation: NullableAnnotation.None } => true,
            IFieldSymbol { NullableAnnotation: NullableAnnotation.None } => true,
            IMethodSymbol
            {
                ReturnNullableAnnotation: NullableAnnotation.None,
                IsGenericMethod: false
            }
                => true,
            // Generic methods are a special case since they can be declared as "public T GetValue<T>()"
            // This confuses the analyzer check above since it thinks that T is a properly annotated type
            // This is a special check that checks if the definition of the method itself was annotated rather than checking the return type
            IMethodSymbol
            {
                OriginalDefinition.ReturnNullableAnnotation: NullableAnnotation.None,
                IsGenericMethod: true
            }
                => true,
            _ => false
        };

    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        EqualsValueClauseSyntax? initializer = node.Initializer;

        if (initializer is null)
        {
            return base.VisitVariableDeclarator(node);
        }

        ExpressionSyntax initializerValue = initializer.Value;
        ISymbol? initializerValueSymbol = semanticModel.GetSymbolInfo(initializerValue).Symbol;

        if (initializerValueSymbol is null || !IsSymbolNullOblivious(initializerValueSymbol))
        {
            return base.VisitVariableDeclarator(node);
        }

        // Check if the variable is being declared into a "var"
        // This check is being done to minimise the performance impact of getting the type of an initialiser
        if (node.Parent is VariableDeclarationSyntax { Type.IsVar: true })
        {
            string? typeDisplayString = semanticModel
                .GetTypeInfo(initializerValue)
                .Type?.ToMinimalDisplayString(
                    semanticModel,
                    initializerValue.GetLocation().SourceSpan.Start
                );

            return AnnotateNode(node, initializer, typeDisplayString);
        }

        return AnnotateNode(node, initializer, null);
    }

    private static VariableDeclaratorSyntax AnnotateNode(
        VariableDeclaratorSyntax node,
        EqualsValueClauseSyntax variableInitializer,
        string? typeSymbol
    ) =>
        node.ReplaceNode(
            variableInitializer,
            variableInitializer.WithValue(
                variableInitializer.Value.WithAdditionalAnnotations(
                    new SyntaxAnnotation(AnnotationKind.NullObliviousCodeAnnotationKind, typeSymbol)
                )
            )
        );

    public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        ISymbol? assignmentSymbol = semanticModel.GetSymbolInfo(node.Right).Symbol;

        if (assignmentSymbol is null || !IsSymbolNullOblivious(assignmentSymbol))
        {
            return base.VisitAssignmentExpression(node);
        }

        return AnnotateNode(node);
    }

    private static SyntaxNode AnnotateNode(AssignmentExpressionSyntax node) =>
        node.ReplaceNode(
            node,
            node.WithRight(
                node.Right.WithAdditionalAnnotations(
                    new SyntaxAnnotation(AnnotationKind.NullObliviousCodeAnnotationKind)
                )
            )
        );

    public override SyntaxNode? VisitArgument(ArgumentSyntax node)
    {
        ISymbol? argumentSymbol = semanticModel.GetSymbolInfo(node.Expression).Symbol;

        if (!IsSymbolNullOblivious(argumentSymbol))
        {
            return base.VisitArgument(node);
        }

        return AnnotateNode(node);
    }

    private static SyntaxNode AnnotateNode(ArgumentSyntax node) =>
        node.ReplaceNode(
            node,
            node.WithExpression(
                node.Expression.WithAdditionalAnnotations(
                    new SyntaxAnnotation(AnnotationKind.NullObliviousCodeAnnotationKind)
                )
            )
        );
}
