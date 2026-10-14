namespace SupportDesk.Lifetimes;

public sealed class TransientProbe { }
public sealed class ScopedProbe : IDisposable
{
    public bool IsDisposed { get; private set; }
    public void Dispose() => IsDisposed = true;
}
public sealed class SingletonProbe { }
public sealed class BadSingleton
{
    public ScopedProbe Scoped { get; }
    public BadSingleton(ScopedProbe scoped) => Scoped = scoped;
}
