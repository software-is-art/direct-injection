using Autofac;
using Autofac.Extensions.DependencyInjection;
using BenchmarkDotNet.Attributes;
using DirectInjection;
using DirectInjection.Application;
using DirectInjection.Generated;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ninject;

[assembly:Bind(
    typeof(Transient<IFoo, FooClass>),
    typeof(Transient<IBar, BarOne>),
    typeof(Transient<IBaz, BazOne>)
)]

namespace DirectInjection.Application;

[MemoryDiagnoser]
public class InjectionBenchmarks
{
    private static IServiceProvider MicrosoftDI { get; }
    private static IInstanceProvider DirectInjectionProvider { get; }
    private static AutofacServiceProvider AutofacProvider { get; }
    private static IKernel NinjectKernel { get; }
    static InjectionBenchmarks()
    {
        // Microsoft DI Setup
        IServiceCollection collection = null;
        MicrosoftDI = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                collection = services;
                services.AddTransient<IFoo, FooClass>()
                    .AddTransient<IBar, BarOne>()
                    .AddTransient<IBaz, BazOne>();
            })
            .Build()
            .Services;
        
        // Autofac setup
        var builder = new ContainerBuilder();
        builder.Populate(collection);
        AutofacProvider = new AutofacServiceProvider(builder.Build());
        
        // Ninject setup
        var kernel = new StandardKernel();
        kernel.Bind<IFoo>().To<FooClass>().InTransientScope();
        kernel.Bind<IBar>().To<BarOne>().InTransientScope();
        kernel.Bind<IBaz>().To<BazOne>().InTransientScope();
        NinjectKernel = kernel;
        
        DirectInjectionProvider = new InstanceProvider();
    }

    [Benchmark]
    public IFoo Ninject()
    {
        return NinjectKernel.Get<IFoo>();
    }

    [Benchmark]
    public IFoo Autofac()
    {
        return AutofacProvider.GetService<IFoo>();
    }

    [Benchmark]
    public IFoo MicrosoftDependencyInjection()
    {
        return MicrosoftDI.GetService<IFoo>();
    }

    [Benchmark]
    public IFoo DirectInjection()
    {
        return DirectInjectionProvider.Get<IFoo>();
    }

    [Benchmark(Baseline = true)]
    public IFoo HandWired()
    {
        return new FooClass(new BarOne(), new BazOne());
    }
}