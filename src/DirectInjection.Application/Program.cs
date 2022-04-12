// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using DirectInjection;
using DirectInjection.Application;
[assembly:Bind(
    typeof(Transient<IFoo, FooClass>),
    typeof(Transient<IBar, BarOne>),
    typeof(Transient<IBaz, BazOne>)
)]
BenchmarkRunner.Run<InjectionBenchmarks>();