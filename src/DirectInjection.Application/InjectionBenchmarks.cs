using BenchmarkDotNet.Attributes;
using DirectInjection.Generated;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DirectInjection.Application;

[MemoryDiagnoser]
public class InjectionBenchmarks
{
    private static IServiceProvider MicrosoftDI { get; }
    private static IInstanceProvider DirectInjection { get; }
    static InjectionBenchmarks()
    {
        MicrosoftDI = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
                services.AddTransient<IFoo, FooClass>()
                    .AddTransient<IBar, BarOne>()
                    .AddTransient<IBaz, BazOne>()
            )
            .Build()
            .Services;
        DirectInjection = new InstanceProvider();
    }

    [Benchmark]
    public IFoo MicrosoftServiceProvider()
    {
        return MicrosoftDI.GetService<IFoo>();
    }

    [Benchmark]
    public IFoo CompileTimeInstanceProvider()
    {
        return DirectInjection.Get<IFoo>();
    }

    [Benchmark(Baseline = true)]
    public IFoo HandWired()
    {
        return new FooClass(new BarOne(), new BazOne());
    }
}