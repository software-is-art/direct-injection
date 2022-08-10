using System.Runtime.CompilerServices;

namespace DirectInjection.Application;

public interface IFoo
{
    void Do();
}

public record FooRecord(IBar Bar, IBaz Baz) : IFoo
{
    public void Do()
    {
        Bar.DoSomethingElse();
        Baz.DoSomething();
    }
}

public class FooClass : IFoo
{
    public IBar Bar { get; }
    public IBaz Baz { get; }

    public FooClass(IBar bar, IBaz baz)
    {
        Bar = bar;
        Baz = baz;
    }

    public void Do()
    {
        Baz.DoSomething();
        Bar.DoSomethingElse();
    }
}

public interface IBaz
{
    void DoSomething();
}

public interface IBar
{
    void DoSomethingElse();
}

public record BarOne : IBar
{
    public void DoSomethingElse()
    {
        Console.WriteLine("This is BarOne");
    }
}

public record BarTwo : IBar
{
    public void DoSomethingElse()
    {
        Console.WriteLine("This is BarTwo");
    }
}

public record BazOne : IBaz
{
    public void DoSomething()
    {
        Console.WriteLine("This is BazOne");
    }
}

public record BazTwo : IBaz
{
    public void DoSomething()
    {
        Console.WriteLine("This is BazTwo");
    }
}
