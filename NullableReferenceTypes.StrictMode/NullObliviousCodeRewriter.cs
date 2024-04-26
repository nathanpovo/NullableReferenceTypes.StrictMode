using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullableReferenceTypes.StrictMode;

internal class NullObliviousCodeRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel semanticModel;

    internal NullObliviousCodeRewriter(SemanticModel semanticModel)
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
        string? typeDisplayString = null;
        if (node.Parent is VariableDeclarationSyntax { Type.IsVar: true })
        {
            typeDisplayString = semanticModel
                .GetTypeInfo(initializerValue)
                .Type?.ToMinimalDisplayString(
                    semanticModel,
                    initializerValue.GetLocation().SourceSpan.Start
                );
        }

        return node.ReplaceNodeWithNullifiedNode(initializerValue, typeDisplayString);
    }

    public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        ExpressionSyntax expressionSyntax = node.Right;
        ISymbol? assignmentSymbol = semanticModel.GetSymbolInfo(expressionSyntax).Symbol;

        if (assignmentSymbol is null || !IsSymbolNullOblivious(assignmentSymbol))
        {
            return base.VisitAssignmentExpression(node);
        }

        return node.ReplaceNodeWithNullifiedNode(expressionSyntax);
    }

    public override SyntaxNode? VisitArgument(ArgumentSyntax node)
    {
        ExpressionSyntax argumentExpressionSyntax = node.Expression;
        ISymbol? argumentSymbol = semanticModel.GetSymbolInfo(argumentExpressionSyntax).Symbol;

        if (!IsSymbolNullOblivious(argumentSymbol))
        {
            return base.VisitArgument(node);
        }

        string? typeDisplayString = semanticModel
            .GetTypeInfo(argumentExpressionSyntax)
            .Type?.ToMinimalDisplayString(
                semanticModel,
                argumentExpressionSyntax.GetLocation().SourceSpan.Start
            );

        return node.ReplaceNodeWithNullifiedNode(argumentExpressionSyntax, typeDisplayString);
    }

    public override SyntaxNode? VisitThrowExpression(ThrowExpressionSyntax node)
    {
        ExpressionSyntax expressionSyntax = node.Expression;
        ISymbol? symbol = semanticModel.GetSymbolInfo(expressionSyntax).Symbol;

        if (!IsSymbolNullOblivious(symbol))
        {
            return base.VisitThrowExpression(node);
        }

        return node.ReplaceNodeWithNullifiedNode(expressionSyntax);
    }

    public override SyntaxNode? VisitThrowStatement(ThrowStatementSyntax node)
    {
        ExpressionSyntax? expressionSyntax = node.Expression;

        if (expressionSyntax is null)
        {
            return base.VisitThrowStatement(node);
        }

        ISymbol? symbol = semanticModel.GetSymbolInfo(expressionSyntax).Symbol;

        if (!IsSymbolNullOblivious(symbol))
        {
            return base.VisitThrowStatement(node);
        }

        return node.ReplaceNodeWithNullifiedNode(expressionSyntax);
    }
}
