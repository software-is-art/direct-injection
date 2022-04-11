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
            .SelectMany(s => s.GetRoot().DescendantNodes()).Where(n =>
                n.IsKind(SyntaxKind.ParameterList) && (
                    n.Parent.IsKind(SyntaxKind.RecordDeclaration) ||
                    n.Parent.IsKind(SyntaxKind.RecordStructDeclaration) ||
                    n.Parent.IsKind(SyntaxKind.ConstructorDeclaration)
                )).Cast<ParameterListSyntax>()
            .ToImmutableArray();
        // Namespace and Identifier are done. Need to add parameter list to tuple.
        var namespaces = constructorsWithNonZeroArity
            .Select(c => new KeyValuePair<string, ImmutableArray<string?>>($"{c.Namespace()}.{c.Identifier()}",
                c.Parameters.Select(p => p.Type?.ToFullString()).ToImmutableArray()))
            .ToImmutableDictionary();
        //Debug.Break();
        var source = $@"
namespace DirectInjection.Generated
{{
    public class InstanceProvider : DirectInjection.IInstanceProvider
    {{
        public TType Get<TType>()
        {{
            return Activate(default(TType));
        }}
        private static TType Activate<TType>(in TType contract) {{ return contract; }}
        {string.Join(Environment.NewLine, explicitBindings.Select(kvp => {
            var provided = kvp.Key;
            var contract = kvp.Value;
            if (!namespaces.TryGetValue(provided, out var parameters)) {
                parameters = ImmutableArray<string?>.Empty; // If there is no non-zero arity constructor it must be parameterless
            }
            return @$"private static {contract} Activate(in {contract} contract) {{
                return new {provided}({string.Join(",", parameters.Select(p => $"Activate(default(DirectInjection.Application.{p}))"))});
            }}";
        }))}
    }}
}}";
        //Debug.Break();
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