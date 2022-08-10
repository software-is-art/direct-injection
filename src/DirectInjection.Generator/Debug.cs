using System.Diagnostics;

namespace DirectInjection;

public static class Debug
{
    public static void Break()
    {
        if (!Debugger.IsAttached)
        {
            SpinWait.SpinUntil(() => Debugger.IsAttached);
        }
        Debugger.Break();
    }
}
