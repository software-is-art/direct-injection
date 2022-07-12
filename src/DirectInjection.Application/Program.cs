// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using DirectInjection.Application;

BenchmarkRunner.Run<InjectionBenchmarks>();
