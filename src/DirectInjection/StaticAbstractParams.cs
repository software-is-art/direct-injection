namespace Params
{
    public interface ILabel<TLabel> where TLabel : ILabel<TLabel>
    {
        static TValue Get<TParam, TValue>(in TParam parameters)
            where TParam : IParam<TLabel, TValue>
        {
            return TParam.Get(default(TLabel));
        }
    }

    public interface IParam<TLabel, TValue> where TLabel : ILabel<TLabel>
    {
        static abstract TValue Get(TLabel? label);
    }

    public class Cube
    {
        public static double Volume<TParams>(in TParams parameters)
            where TParams : IParam<Height, double>, IParam<Width, double>, IParam<Depth, double>
        {
            TParams.Get(default(Height));
            return ILabel<Height>.Get<TParams, double>(in parameters);
        }
    }

    public class Depth : ILabel<Depth> { }

    public class Height : ILabel<Height> { }

    public class Width : ILabel<Width> { }
}
