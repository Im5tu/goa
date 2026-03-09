using System.Buffers;

namespace Goa.Clients.Core.Http;

/// <summary>
/// An <see cref="IBufferWriter{T}"/> backed by <see cref="ArrayPool{T}"/> to eliminate per-request byte[] allocations.
/// Rents from the shared pool on construction and returns on disposal.
/// Callers must ensure the writer outlives any <see cref="ReadOnlyMemory{T}"/> obtained from <see cref="WrittenMemory"/>.
/// </summary>
public sealed class PooledBufferWriter : IBufferWriter<byte>, IDisposable
{
    private byte[]? _buffer;
    private int _written;

    /// <summary>
    /// Initializes a new <see cref="PooledBufferWriter"/> with an initial capacity rented from the shared pool.
    /// </summary>
    /// <param name="initialCapacity">The minimum initial buffer size to rent.</param>
    public PooledBufferWriter(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
    }

    /// <summary>Gets the number of bytes written so far.</summary>
    public int WrittenCount => _written;

    /// <summary>Gets a read-only span over the written portion of the buffer.</summary>
    public ReadOnlySpan<byte> WrittenSpan
    {
        get
        {
            ObjectDisposedException.ThrowIf(_buffer is null, this);
            return _buffer.AsSpan(0, _written);
        }
    }

    /// <summary>Gets a read-only memory over the written portion of the buffer.</summary>
    public ReadOnlyMemory<byte> WrittenMemory
    {
        get
        {
            ObjectDisposedException.ThrowIf(_buffer is null, this);
            return _buffer.AsMemory(0, _written);
        }
    }

    /// <inheritdoc />
    public void Advance(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ObjectDisposedException.ThrowIf(_buffer is null, this);
        if (_written + count > _buffer.Length)
            throw new InvalidOperationException("Cannot advance past the end of the buffer.");
        _written += count;
    }

    /// <inheritdoc />
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        ObjectDisposedException.ThrowIf(_buffer is null, this);
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_written);
    }

    /// <inheritdoc />
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        ObjectDisposedException.ThrowIf(_buffer is null, this);
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_written);
    }

    private void EnsureCapacity(int sizeHint)
    {
        if (sizeHint <= 0) sizeHint = 256;
        if (_written + sizeHint <= _buffer!.Length) return;
        var newSize = Math.Max(_buffer.Length * 2, _written + sizeHint);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        _buffer.AsSpan(0, _written).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
        _buffer = newBuffer;
    }

    /// <summary>
    /// Returns the rented buffer to the shared <see cref="ArrayPool{T}"/>, clearing it to prevent data leakage.
    /// </summary>
    public void Dispose()
    {
        var buf = _buffer;
        if (buf != null)
        {
            _buffer = null;
            ArrayPool<byte>.Shared.Return(buf, clearArray: true);
        }
    }
}
