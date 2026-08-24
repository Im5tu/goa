using System.Runtime.CompilerServices;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Tests;

/// <summary>
/// <see cref="AttributeValue"/> is a struct holding a GC reference, and it is stored in
/// <c>List&lt;AttributeValue&gt;</c> and <c>Dictionary&lt;string, AttributeValue&gt;</c> throughout
/// the client. Its size therefore sets the array stride, and the stride decides whether elements
/// past [0] hold their object reference at a pointer-aligned address.
///
/// It once carried <c>[StructLayout(LayoutKind.Explicit, Pack = 1)]</c>, which left it 10 bytes
/// wide under Native AOT. CoreCLR rounds a GC-ref-bearing struct up to pointer size, so this was
/// invisible in a JIT test run; ILCompiler does not, so every element past [0] was misaligned and a
/// compacting GC clobbered the reference while leaving the type tag intact — surfacing as
/// NullReferenceException and InvalidCastException when reading valid stored data.
///
/// These assertions hold on CoreCLR too (16 either way), so they cannot catch a reintroduced
/// <c>Pack</c> on their own. Their job is to fail loudly if a future layout change makes the size a
/// non-multiple of the pointer size on ANY runtime, and to carry the reasoning.
/// </summary>
public class AttributeValueLayoutTests
{
    [Test]
    public async Task Size_IsPointerAligned_SoArrayElementsKeepAlignedReferences()
    {
        var size = Unsafe.SizeOf<AttributeValue>();

        await Assert.That(size % IntPtr.Size)
            .IsEqualTo(0)
            .Because(
                $"AttributeValue is {size} bytes, which is not a multiple of {IntPtr.Size}. It holds a "
                + "GC reference and is stored in arrays, so a non-aligned size misaligns every element "
                + "past [0] and lets a compacting GC corrupt the reference under Native AOT. Check for "
                + "a Pack or Size on its StructLayout.");
    }

    [Test]
    public async Task ArrayStride_IsPointerAligned()
    {
        // The stride is what actually determines element alignment; assert it directly rather than
        // inferring it from the size.
        var values = new AttributeValue[4];
        var stride = Unsafe.ByteOffset(ref values[0], ref values[1]).ToInt64();

        await Assert.That(stride % IntPtr.Size)
            .IsEqualTo(0L)
            .Because($"AttributeValue[] stride is {stride} bytes, so elements past [0] are misaligned.");
    }

    /// <summary>
    /// Round-trips references through a list large enough to be relocated, then forces a compacting
    /// collection. With a misaligned stride this is where the references got clobbered.
    /// </summary>
    [Test]
    public async Task ReferencesSurviveACompactingCollection()
    {
        const int count = 512;
        var values = new List<AttributeValue>(count);
        for (var i = 0; i < count; i++)
            values.Add(AttributeValue.String($"value-{i}"));

        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        for (var i = 0; i < count; i++)
            await Assert.That(values[i].S).IsEqualTo($"value-{i}");
    }
}
