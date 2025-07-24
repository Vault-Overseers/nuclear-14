using System.Runtime.CompilerServices;

namespace Content.Shared._RMC14.Random;

// https://github.com/medo64/Medo.ScrambledLinear/blob/main/src/Xoroshiro/Xoroshiro64S.cs
/// <summary>
/// 32-bit random number generator intended for floating point numbers with 64-bit state (xoroshiro64*).
/// </summary>
/// <remarks>http://prng.di.unimi.it/xoroshiro64star.c</remarks>
public record struct Xoroshiro64S
{
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public Xoroshiro64S()
        : this(DateTime.UtcNow.Ticks)
    {
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="seed">Seed value.</param>
    public Xoroshiro64S(long seed)
    {
        var sm64 = new SplitMix64(seed);
        _s0 = unchecked((uint) sm64.Next());
        _s1 = unchecked((uint) sm64.Next());
    }

    private uint _s0;
    private uint _s1;

    /// <summary>
    /// Returns the next 32-bit pseudo-random number.
    /// </summary>
    public int Next()
    {
        uint s0 = _s0;
        uint s1 = _s1;
        uint result = unchecked(s0 * (uint) 0x9E3779BB);

        s1 ^= s0;
        _s0 = RotateLeft(s0, 26) ^ s1 ^ (s1 << 9);
        _s1 = RotateLeft(s1, 13);

        return Math.Abs((int) result);
    }

    public float NextFloat()
    {
        return Next() * 4.6566128752458E-10f;
    }

    public float NextFloat(float min, float max)
    {
        return NextFloat() * (max - min) + min;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateLeft(uint x, int k)
    {
        return (x << k) | (x >> (32 - k));
    }
}
