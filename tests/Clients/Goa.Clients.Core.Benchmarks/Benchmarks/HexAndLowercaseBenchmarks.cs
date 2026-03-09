using System.Runtime.CompilerServices;
using BenchmarkDotNet.Order;

namespace Goa.Clients.Core.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class HexAndLowercaseBenchmarks
{
    private byte[] _hash = null!;
    private string _shortHeader = null!;
    private string _longHeader = null!;

    [GlobalSetup]
    public void Setup()
    {
        _hash = SHA256.HashData("benchmark-test-data"u8);
        _shortHeader = "Content-Type";
        _longHeader = "X-Amz-Content-Sha256";
    }

    // ===== BytesToHex =====

    [Benchmark(Baseline = true), BenchmarkCategory("BytesToHex")]
    public char Hex_BitManipulation()
    {
        Span<char> hex = stackalloc char[64];
        BytesToHex_Scalar(_hash, hex);
        return hex[0];
    }

    [Benchmark, BenchmarkCategory("BytesToHex")]
    public char Hex_ConvertTryToHexStringLower()
    {
        Span<char> hex = stackalloc char[64];
        Convert.TryToHexStringLower(_hash, hex, out _);
        return hex[0];
    }

    [Benchmark, BenchmarkCategory("BytesToHex")]
    public string Hex_ConvertToHexStringLower()
    {
        return Convert.ToHexStringLower(_hash);
    }

    // ===== WriteLowercase (short header: "Content-Type", 12 chars) =====

    [Benchmark(Baseline = true), BenchmarkCategory("Lowercase_Short")]
    public int Lower_CharByChar_Short()
    {
        Span<char> dest = stackalloc char[64];
        return WriteLowercase_Scalar(dest, _shortHeader);
    }

    [Benchmark, BenchmarkCategory("Lowercase_Short")]
    public int Lower_SpanToLowerInvariant_Short()
    {
        Span<char> dest = stackalloc char[64];
        _shortHeader.AsSpan().ToLowerInvariant(dest);
        return _shortHeader.Length;
    }

    [Benchmark, BenchmarkCategory("Lowercase_Short")]
    public int Lower_AsciiToLower_Short()
    {
        Span<char> dest = stackalloc char[64];
        System.Text.Ascii.ToLower(_shortHeader, dest, out var written);
        return written;
    }

    // ===== WriteLowercase (long header: "X-Amz-Content-Sha256", 20 chars) =====

    [Benchmark(Baseline = true), BenchmarkCategory("Lowercase_Long")]
    public int Lower_CharByChar_Long()
    {
        Span<char> dest = stackalloc char[64];
        return WriteLowercase_Scalar(dest, _longHeader);
    }

    [Benchmark, BenchmarkCategory("Lowercase_Long")]
    public int Lower_SpanToLowerInvariant_Long()
    {
        Span<char> dest = stackalloc char[64];
        _longHeader.AsSpan().ToLowerInvariant(dest);
        return _longHeader.Length;
    }

    [Benchmark, BenchmarkCategory("Lowercase_Long")]
    public int Lower_AsciiToLower_Long()
    {
        Span<char> dest = stackalloc char[64];
        System.Text.Ascii.ToLower(_longHeader, dest, out var written);
        return written;
    }

    // ===== Current implementations (reproduced from RequestSigner) =====

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void BytesToHex_Scalar(ReadOnlySpan<byte> input, Span<char> hexOut)
    {
        for (var i = 0; i < input.Length; i++)
        {
            var b = input[i];
            hexOut[i << 1] = (char)(87 + (b >> 4) + ((((b >> 4) - 10) >> 31) & -39));
            hexOut[(i << 1) + 1] = (char)(87 + (b & 0xF) + ((((b & 0xF) - 10) >> 31) & -39));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteLowercase_Scalar(Span<char> dest, string s)
    {
        var i = 0;
        for (; i < s.Length; i++) dest[i] = char.ToLowerInvariant(s[i]);
        return i;
    }
}
