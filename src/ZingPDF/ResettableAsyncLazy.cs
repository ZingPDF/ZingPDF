using Nito.AsyncEx;
using System.Runtime.CompilerServices;

namespace ZingPDF;

public class ResettableAsyncLazy<T>
{
    private readonly Func<Task<T>> _factory;
    private AsyncLazy<T> _lazy;

    public ResettableAsyncLazy(Func<Task<T>> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _lazy = new AsyncLazy<T>(_factory);
    }

    public Task<T> Task => _lazy.Task;

    public void Reset()
    {
        _lazy = new AsyncLazy<T>(_factory);
    }

    #region awaitable

    /// <summary>
    /// Asynchronous infrastructure support. This method permits instances of <see cref="ResettableAsyncLazy&lt;T&gt;"/> to be await'ed.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public TaskAwaiter<T> GetAwaiter()
    {
        return Task.GetAwaiter();
    }

    /// <summary>
    /// Asynchronous infrastructure support. This method permits instances of <see cref="ResettableAsyncLazy&lt;T&gt;"/> to be await'ed.
    /// </summary>
    public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
    {
        return Task.ConfigureAwait(continueOnCapturedContext);
    }

    #endregion
}
