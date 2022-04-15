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
        //Debug.Break();
        var bindings = GetExplicitBindings(context);
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

    internal class Scope : DirectInjection.IScope {{
            private int disposed;

            ~Scope() {{
                Dispose(true);
            }}

            private void Dispose(bool isFinalizer) {{
                if (disposed != 0 || Interlocked.Increment(ref disposed) != 1) {{
                    return;
                }}

                
                
                if (!isFinalizer) {{
                    GC.SuppressFinalize(true);
                }}
            }}

            public IScope GetScope() => new Scope();

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
            public TType Get<TType>()
            {{
                return typeof(TType) switch
                {{
                    {string.Join(Environment.NewLine, bindings.All.Select(b => {
                        var (_, contract) = b;
                        return $"var x when x == typeof({contract}) => (TType)Activate(this, default({contract})),";
                    }))}
                    _ => throw new Exception()
                }};
            }}
            {string.Join(Environment.NewLine, bindings.Transient.Select(kvp => {
                var provided = kvp.Key;
                var contract = kvp.Value;
                if (!constructors.TryGetValue(provided, out var parameters)) {
                    parameters = ImmutableArray<string?>.Empty;
                }
                return @$"
                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    private static {contract} Activate(Scope scope, in {contract} result) {{
                        return new {provided}({string.Join(",", parameters.Select(p => $"Activate(scope, default({p}))"))});
                    }}";
            }))}
            {string.Join(Environment.NewLine, bindings.Scoped.Select(kvp => {
                var provided = kvp.Key;
                var contract = kvp.Value;
                if (!constructors.TryGetValue(provided, out var parameters)) {
                    parameters = ImmutableArray<string?>.Empty;
                }
                var propertyIdentifier = $"scoped_{contract.Replace(".", "_")}";
                var setIdentifier = $"{propertyIdentifier}Set";
                var lockIdentifier = $"{propertyIdentifier}Lock";
                return @$"
                    private readonly object {lockIdentifier} = new object();
                    private bool {setIdentifier};
                    private {contract} {propertyIdentifier};
                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    private static {contract} Activate(Scope scope, in {contract} result) {{
                        if (!scope.{setIdentifier}) {{
                            lock (scope.{lockIdentifier}) {{
                                if (!scope.{setIdentifier}) {{
                                   scope.{propertyIdentifier} = new {provided}({string.Join(",", parameters.Select(p => $"Activate(scope, default({p}))"))});
                                }}
                            }}
                        }}
                        return scope.{propertyIdentifier};
                    }}";
            }))}

            public void Dispose() => Dispose(false);
        }}

    public class InstanceProvider : DirectInjection.IInstanceProvider
    {{

        public IScope GetScope() => new Scope();

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
        public TType Get<TType>()
        {{
            return typeof(TType) switch
            {{
                {string.Join(Environment.NewLine, bindings.Transient.Select(b => {
                    var contract = b.Value;
                    return $"var x when x == typeof({contract}) => (TType)Activate(default({contract})),";
                }))}
                _ => throw new Exception()
            }};
        }}
        {string.Join(Environment.NewLine, bindings.Transient.Select(kvp => {
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
        //Debug.Break();
        File.WriteAllText("/Users/callum/Debug.cs", source);
        context.AddSource("InstanceProvider.cs", SourceText.From(source, Encoding.UTF8));
    }
    
    private string ScopedActivator(string? contract, string? provided, ImmutableArray<string?> parameters)
    {
        return @$"
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                public static {contract} Activate(in {contract} result) {{
                    return new {provided}({string.Join(",", parameters.Select(p => $"Activate(default({p}))"))});
                }}";
    }

    private string NonScopedActivator(string? contract, string? provided, ImmutableArray<string?> parameters)
    {
        return @$"
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                public static {contract} Activate(in {contract} result) {{
                    return new {provided}({string.Join(",", parameters.Select(p => $"Activate(default({p}))"))});
                }}";
    }

    private Bindings GetExplicitBindings(GeneratorExecutionContext context)
    {
        var attributes = context.Compilation.Assembly
            .GetAttributes().FirstOrDefault(t => t.AttributeClass?.ToDisplayString() == "DirectInjection.BindAttribute");
        if (attributes == default)
        {
            return Bindings.Empty;
        }
        var transient = ImmutableDictionary<string, string>.Empty.ToBuilder();
        var scoped = ImmutableDictionary<string, string>.Empty.ToBuilder();
        foreach (var value in attributes.ConstructorArguments[0].Values)
        {
            if (value.Value is INamedTypeSymbol named)
            {
                var target = named.ConstructUnboundGenericType().ToDisplayString() switch
                {
                    "DirectInjection.Transient<,>" => transient,
                    "DirectInjection.Scoped<,>" => scoped,
                    _ => throw new Exception()
                };
                var args = named.TypeArguments;
                target[args[1].ToDisplayString()] = args[0].ToDisplayString();
            }
        }

        return new Bindings(transient.ToImmutable(), scoped.ToImmutable());
    }

    private class Bindings
    {
        public static Bindings Empty { get; } = new (
            ImmutableDictionary<string, string>.Empty,
            ImmutableDictionary<string, string>.Empty
        );
        public Bindings(
            ImmutableDictionary<string, string> transient,
            ImmutableDictionary<string, string> scoped
        )
        {
            Transient = transient;
            Scoped = scoped;
        }
        public ImmutableDictionary<string, string> Transient { get; }
        public ImmutableDictionary<string, string> Scoped { get; }

        public IEnumerable<(string provided, string contract)> All =>
            Transient.Concat(Scoped).Select(kvp => (kvp.Key, kvp.Value));
    }
}