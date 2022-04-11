// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using DirectInjection;
using DirectInjection.Application;
[assembly:Bind(
    typeof(Binding<IFoo, Foo>),
    typeof(Binding<IBar, BarOne>),
    typeof(Binding<IBaz, BazOne>)
)]
var provider = new DirectInjection.Generated.InstanceProvider();
provider.Get<IFoo>().Do();