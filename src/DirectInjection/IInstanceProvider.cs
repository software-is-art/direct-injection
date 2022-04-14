namespace DirectInjection;

public interface IInstanceProvider
{
    TType Get<TType>();
}