// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using DirectInjection;
using DirectInjection.Application;
BenchmarkRunner.Run<InjectionBenchmarks>();