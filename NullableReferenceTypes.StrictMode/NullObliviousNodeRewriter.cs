using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullableReferenceTypes.StrictMode;

internal static class NullObliviousNodeRewriter
{
    public static TRoot ReplaceNodeWithNullifiedNode<TRoot>(
        this TRoot root,
        SyntaxNode oldNode,
        string? typeInfo = null
    )
        where TRoot : SyntaxNode
    {
        SyntaxNode newNode;

        if (typeInfo is not null)
        {
            NullableTypeSyntax typeSyntax = SyntaxFactory.NullableType(
                SyntaxFactory.ParseTypeName(typeInfo)
            );

            newNode = SyntaxFactory.CastExpression(
                typeSyntax,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
            );
        }
        else
        {
            newNode = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        }

        return root.ReplaceNode(oldNode, newNode);
    }
}