using System.Collections;

namespace Goa.Clients.Core.Logging;

/// <summary>
/// Zero-allocation log scope with 8 entries. Implements IReadOnlyList so
/// Microsoft.Extensions.Logging can enumerate properties without a Dictionary allocation.
/// </summary>
internal readonly struct LogScope8 : IReadOnlyList<KeyValuePair<string, object>>
{
    private readonly KeyValuePair<string, object> _0, _1, _2, _3, _4, _5, _6, _7;

    public LogScope8(
        KeyValuePair<string, object> p0, KeyValuePair<string, object> p1,
        KeyValuePair<string, object> p2, KeyValuePair<string, object> p3,
        KeyValuePair<string, object> p4, KeyValuePair<string, object> p5,
        KeyValuePair<string, object> p6, KeyValuePair<string, object> p7)
    {
        _0 = p0; _1 = p1; _2 = p2; _3 = p3;
        _4 = p4; _5 = p5; _6 = p6; _7 = p7;
    }

    public int Count => 8;

    public KeyValuePair<string, object> this[int index] => index switch
    {
        0 => _0, 1 => _1, 2 => _2, 3 => _3,
        4 => _4, 5 => _5, 6 => _6, 7 => _7,
        _ => throw new IndexOutOfRangeException()
    };

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<KeyValuePair<string, object>>
    {
        private readonly LogScope8 _scope;
        private int _index;

        internal Enumerator(LogScope8 scope) { _scope = scope; _index = -1; }

        public KeyValuePair<string, object> Current => _scope[_index];
        object IEnumerator.Current => Current;
        public bool MoveNext() => ++_index < 8;
        public void Reset() => _index = -1;
        public void Dispose() { }
    }
}

/// <summary>
/// Zero-allocation log scope with up to 4 entries. Implements IReadOnlyList so
/// Microsoft.Extensions.Logging can enumerate properties without a Dictionary allocation.
/// </summary>
internal readonly struct LogScope4 : IReadOnlyList<KeyValuePair<string, object>>
{
    private readonly KeyValuePair<string, object> _0, _1, _2, _3;
    private readonly int _count;

    public LogScope4(
        KeyValuePair<string, object> p0, KeyValuePair<string, object> p1)
    {
        _0 = p0; _1 = p1;
        _count = 2;
    }

    public LogScope4(
        KeyValuePair<string, object> p0, KeyValuePair<string, object> p1,
        KeyValuePair<string, object> p2)
    {
        _0 = p0; _1 = p1; _2 = p2;
        _count = 3;
    }

    public LogScope4(
        KeyValuePair<string, object> p0, KeyValuePair<string, object> p1,
        KeyValuePair<string, object> p2, KeyValuePair<string, object> p3)
    {
        _0 = p0; _1 = p1; _2 = p2; _3 = p3;
        _count = 4;
    }

    public int Count => _count;

    public KeyValuePair<string, object> this[int index] =>
        index < _count ? index switch
        {
            0 => _0, 1 => _1, 2 => _2, 3 => _3,
            _ => throw new IndexOutOfRangeException()
        } : throw new IndexOutOfRangeException();

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<KeyValuePair<string, object>>
    {
        private readonly LogScope4 _scope;
        private int _index;

        internal Enumerator(LogScope4 scope) { _scope = scope; _index = -1; }

        public KeyValuePair<string, object> Current => _scope[_index];
        object IEnumerator.Current => Current;
        public bool MoveNext() => ++_index < _scope._count;
        public void Reset() => _index = -1;
        public void Dispose() { }
    }
}
