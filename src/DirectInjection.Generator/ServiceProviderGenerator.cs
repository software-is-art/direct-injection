using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DirectInjection;

[Generator]
public class ServiceProviderGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var explicitBindings = GetExplicitBindings(context);
        var constructorsWithNonZeroArity = context.Compilation.SyntaxTrees
            .SelectMany(s => s.GetRoot().DescendantNodes().Select(n => (context.Compilation.GetSemanticModel(s), n)))
            .Where(sn =>
            {
                var (_, syntaxNode) = sn;
                return syntaxNode.IsKind(SyntaxKind.ParameterList) &&
                       (
                           syntaxNode.Parent.IsKind(SyntaxKind.RecordDeclaration) ||
                           syntaxNode.Parent.IsKind(SyntaxKind.RecordStructDeclaration) ||
                           syntaxNode.Parent.IsKind(SyntaxKind.ConstructorDeclaration)
                       );
            }).ToImmutableArray();
        //Debug.Break();
        var constructors = constructorsWithNonZeroArity
            .Select(sn =>
            {
                var (semanticModel, syntaxNode) = sn;
                var parameterListSyntax = (ParameterListSyntax) syntaxNode;
                return new KeyValuePair<string, ImmutableArray<string?>>($"{parameterListSyntax.Namespace()}.{parameterListSyntax.Identifier()}",
                    parameterListSyntax.Parameters.Select(p =>
                        $"{semanticModel.GetTypeInfo(p.Type).ConvertedType.ToDisplayString()}"
                    ).ToImmutableArray());
            })
            .ToImmutableDictionary();
        var source = $@"
namespace DirectInjection.Generated
{{
    public class InstanceProvider : DirectInjection.IInstanceProvider
    {{
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
        public TType Get<TType>()
        {{
            return typeof(TType) switch
            {{
                {string.Join(Environment.NewLine, explicitBindings.Select(b => {
                    var contract = b.Value;
                    return $"var x when x == typeof({contract}) => (TType)Activate(default({contract})),";
                }))}
                _ => throw new Exception()
            }};
        }}
        {string.Join(Environment.NewLine, explicitBindings.Select(kvp => {
            var provided = kvp.Key;
            var contract = kvp.Value;
            if (!constructors.TryGetValue(provided, out var parameters)) {
                parameters = ImmutableArray<string?>.Empty;
            }
            return @$"
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
public static {contract} Activate(in {contract} result) {{
                return new {provided}({string.Join(",", parameters.Select(p => $"Activate(default({p}))"))});
            }}";
        }))}
    }}
}}";
        context.AddSource("InstanceProvider.cs", SourceText.From(source, Encoding.UTF8));
    }

    private ImmutableDictionary<string, string> GetExplicitBindings(GeneratorExecutionContext context)
    {
        var attributes = context.Compilation.Assembly
            .GetAttributes().FirstOrDefault(t => t.AttributeClass?.ToDisplayString() == "DirectInjection.BindAttribute");
        if (attributes == default)
        {
            return ImmutableDictionary<string, string>.Empty;
        }
        var bindings = ImmutableDictionary<string, string>.Empty.ToBuilder();
        foreach (var value in attributes.ConstructorArguments[0].Values)
        {
            if (value.Value is INamedTypeSymbol named)
            {
                var args = named.TypeArguments;
                bindings[args[1].ToDisplayString()] = args[0].ToDisplayString();
            }
        }
        return bindings.ToImmutable();
    }
}