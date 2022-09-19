using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Factory
{
    public interface IFoo { }

    public record Foo() : IFoo { }

    public interface IBar { }

    public record Bar(IFoo Foo) : IBar { }

    public class Factories<TDependencies> : IFactory<Foo>, IFactory<Bar>
        where TDependencies : IDependency<IFoo, Static>
    {
        public static Bar New(Bar? contract = default)
        {
            CombinedFactory<TDependencies>.New(default(Bar));
            CombinedFactory<TDependencies>.New(default(Foo));
            return new Bar(TDependencies.Get());
        }

        public static Foo New(Foo? contract = default)
        {
            return new Foo();
        }
    }

    public class CombinedFactory<TDependecies>
        : Combine<FooFactory, Foo, BarFactory<TDependecies>, Bar>
        where TDependecies : IDependency<IFoo, Static> { }

    public class FooFactory : IFactory<Foo>
    {
        public static Foo New(Foo? contract = default)
        {
            return new Foo();
        }
    }

    public class BarFactory<TDependencies> : IFactory<Bar>
        where TDependencies : IDependency<IFoo, Static>
    {
        public static Bar New(Bar? contract = default)
        {
            return new Bar(TDependencies.Get());
        }
    }

    public class Combine<TFactoryA, TTypeA, TFactoryB, TTypeB>
        : CombineB<TFactoryB, TTypeB>,
            IFactory<TTypeA>
        where TFactoryA : IFactory<TTypeA>
        where TFactoryB : IFactory<TTypeB>
    {
        public static TTypeA New(TTypeA? activation = default) => TFactoryA.New();
    }

    public class CombineB<TFactoryB, TTypeB> : IFactory<TTypeB> where TFactoryB : IFactory<TTypeB>
    {
        public static TTypeB New(TTypeB? activation = default)
        {
            throw new NotImplementedException();
        }
    }

    public class Dependencies<TFactories> : IDependency<IFoo, Static>, IDependency<IBar, Transient>
        where TFactories : IFactory<Foo>, IFactory<Bar>
    {
        public static IFoo Get(IFoo? activation = default, Static? scope = default) =>
            Static.Scope<TFactories, Foo>();

        public static IBar Get(IBar? activation = default, Transient? scope = default) =>
            Transient.Scope<TFactories, Bar>();
    }

    public class Dependencies : Dependencies<Factories<Dependencies>> { }

    public class Consumer<TDependecies>
        where TDependecies : IDependency<IBar, Transient>, IDependency<IFoo, Transient>
    {
        public IFoo Foo { get; } = TDependecies.Get(default(IFoo), default(Transient));
        public IBar Bar { get; } = TDependecies.Get(default(IBar), default(Transient));
    }

    public class Transient : IScope
    {
        public static TContract Scope<TFactory, TContract>() where TFactory : IFactory<TContract> =>
            TFactory.New();
    }

    public class Static : IScope
    {
        private class Singleton<TFactory, TContract> where TFactory : IFactory<TContract>
        {
            public static TContract Get { get; } = TFactory.New();
        }

        public static TContract Scope<TFactory, TContract>() where TFactory : IFactory<TContract> =>
            Singleton<TFactory, TContract>.Get;
    }

    public interface IScope
    {
        static abstract TContract Scope<TFactory, TContract>() where TFactory : IFactory<TContract>;
    }

    public interface IDependency<TContract, TScope>
    {
        static abstract TContract Get(TContract? activation = default, TScope? scope = default);
    }

    public interface IFactory<TContract>
    {
        static abstract TContract New(TContract? activation = default);
    }
}
