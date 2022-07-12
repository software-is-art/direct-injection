using System.Collections.Immutable;

namespace DirectInjection;

[AttributeUsage(AttributeTargets.Assembly)]
public class BindAttribute : Attribute
{
    public ImmutableArray<Type> Bindings { get; }

    public BindAttribute(params Type[] bindings)
    {
        Bindings = bindings.ToImmutableArray();
    }
}
