using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NullableReferenceTypes.StrictMode;

internal static class NullObliviousNodeRewriter
{
    public static TRoot ReplaceNodeWithNullifiedNode<TRoot>(
        this TRoot root,
        ExpressionSyntax oldNode,
        string? typeInfo = null
    )
        where TRoot : SyntaxNode
    {
        SyntaxNode newNode;

        if (typeInfo is not null)
        {
            NullableTypeSyntax typeSyntax = NullableType(ParseTypeName(typeInfo));

            newNode = CastExpression(typeSyntax, oldNode);
        }
        else
        {
            newNode = LiteralExpression(SyntaxKind.NullLiteralExpression);
        }

        return root.ReplaceNode(oldNode, newNode);
    }
}
