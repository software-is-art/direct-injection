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
        var namespaces = constructorsWithNonZeroArity
            .Select(c => new KeyValuePair<string, ImmutableArray<string?>>($"{c.Namespace()}.{c.Identifier()}",
                c.Parameters.Select(p => p.Type?.ToFullString()).ToImmutableArray()))
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
                    return $"var x when x == typeof({contract}) => (TType)Activate_{contract.Replace(".", "_")}(),";
                }))}
                _ => throw new Exception()
            }};
        }}
        {string.Join(Environment.NewLine, explicitBindings.Select(kvp => {
            var provided = kvp.Key;
            var contract = kvp.Value;
            if (!namespaces.TryGetValue(provided, out var parameters)) {
                parameters = ImmutableArray<string?>.Empty;
            }
            // Cheating here until we can load in fully qualified names for the contracts
            return @$"
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
private static {contract} Activate_{contract.Replace(".", "_")}() {{
                return new {provided}({string.Join(",", parameters.Select(p => $"Activate_DirectInjection_Application_{p.Replace(".", "_")}()"))});
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