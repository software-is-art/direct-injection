using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectInjection;

public static class Extensions
{
    public static string Namespace(this ParameterListSyntax parameter) => parameter.Ancestors()
        .Where(p => 
            p.IsKind(SyntaxKind.FileScopedNamespaceDeclaration) ||
            p.IsKind(SyntaxKind.NamespaceDeclaration)
        ).Cast<BaseNamespaceDeclarationSyntax>()
        .Select(p => p.Name.ToFullString())
        .First();

    public static string Identifier(this ParameterListSyntax parameter)
    {
        if (parameter.Parent is ConstructorDeclarationSyntax constructor)
        {
            return constructor.Identifier.ToFullString();
        }

        if (parameter.Parent is RecordDeclarationSyntax record)
        {
            return record.Identifier.ToFullString();
        }

        throw new Exception();
    }
}