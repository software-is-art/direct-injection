// See https://aka.ms/new-console-template for more information

using DirectInjection;
using DirectInjection.Application;
[assembly:Bind(
    typeof(Binding<IFoo, FooClass>),
    typeof(Binding<IBar, BarOne>),
    typeof(Binding<IBaz, BazOne>)
)]
var provider = new DirectInjection.Generated.InstanceProvider();
provider.Get<IFoo>().Do();