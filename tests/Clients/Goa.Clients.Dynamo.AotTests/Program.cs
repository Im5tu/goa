using System;
using System.Runtime.CompilerServices;
using Goa.Clients.Dynamo.Models;

// Layout assertions that only mean anything when executed as a Native AOT binary.
//
// AttributeValue is a struct holding a GC reference, stored in List<AttributeValue> and
// Dictionary<string, AttributeValue> throughout the client, so its size sets the array stride and
// the stride decides whether elements past [0] hold their reference at a pointer-aligned address.
//
// It once carried [StructLayout(LayoutKind.Explicit, Pack = 1)], leaving it 10 bytes wide under
// AOT. CoreCLR rounds a GC-ref-bearing struct up to pointer size, so a JIT test run cannot see the
// problem at all; ILCompiler does not, so every element past [0] was misaligned and a compacting GC
// clobbered the reference while leaving the type tag intact. Reads of valid stored data then threw
// NullReferenceException or InvalidCastException.
//
// Runs as a plain executable rather than a test host so that nothing reflection-based is dragged in.

var failures = 0;

var size = Unsafe.SizeOf<AttributeValue>();
Check(
    "sizeof(AttributeValue) is pointer-aligned",
    size % IntPtr.Size == 0,
    $"sizeof is {size}, not a multiple of {IntPtr.Size}. Check for a Pack or Size on its StructLayout.");

var probe = new AttributeValue[2];
var stride = Unsafe.ByteOffset(ref probe[0], ref probe[1]).ToInt64();
Check(
    "AttributeValue[] stride is pointer-aligned",
    stride % IntPtr.Size == 0,
    $"stride is {stride}, so every element past [0] holds a misaligned object reference.");

// The failure mode itself: fill an array large enough to be relocated, force a compacting gen2
// collection, then read every reference back.
const int count = 4096;
var values = new AttributeValue[count];
for (var i = 0; i < count; i++)
    values[i] = AttributeValue.String($"value-{i}");

GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
GC.WaitForPendingFinalizers();
GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

var corrupted = 0;
for (var i = 0; i < count; i++)
{
    try
    {
        if (values[i].S != $"value-{i}")
            corrupted++;
    }
    catch (Exception)
    {
        // A corrupted reference surfaces as NullReferenceException or InvalidCastException.
        corrupted++;
    }
}

Check(
    "references survive a compacting collection",
    corrupted == 0,
    $"{corrupted} of {count} references were corrupted after a compacting gen2 collection.");

Console.WriteLine(failures == 0
    ? $"PASS  sizeof={size} stride={stride}"
    : $"FAIL  {failures} check(s) failed (sizeof={size} stride={stride})");

return failures == 0 ? 0 : 1;

void Check(string name, bool ok, string detail)
{
    Console.WriteLine(ok ? $"  ok    {name}" : $"  FAIL  {name}: {detail}");
    if (!ok)
        failures++;
}
