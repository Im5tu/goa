using Microsoft.Extensions.Logging;

namespace Goa.Functions.Core.Logging;

internal sealed class LogScopeProvider : IExternalScopeProvider
{
    private readonly AsyncLocal<Scope?> _currentScope = new();

    public IDisposable Push(object? state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new Scope(this, state);
    }

    public void ForEachScope<TState>(Action<object?, TState> callback, TState state)
    {
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        var scope = _currentScope.Value;

        while (scope != null)
        {
            callback(scope.State, state);
            scope = scope.Parent;
        }
    }

    private sealed class Scope : IDisposable
    {
        private readonly LogScopeProvider _provider;
        private bool _disposed = false;

        public object State { get; }
        public Scope? Parent { get; }

        public Scope(LogScopeProvider provider, object state)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(state);

            _provider = provider;
            State = state;
            Parent = provider._currentScope.Value;
            provider._currentScope.Value = this;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            // Ensure the current scope is this instance, to avoid tampering from other threads or async flows
            if (_provider._currentScope.Value == this)
            {
                _provider._currentScope.Value = Parent;
            }
            _disposed = true;
        }
    }
}
