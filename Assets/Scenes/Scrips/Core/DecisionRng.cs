using System;


namespace AutoChess.Core
{
    public enum DecisionStream : ulong
    {
        TargetJitter = 0xA1B2C3D4E5F60718UL,
        StrafeSide   = 0x1234567890ABCDEFUL,
        TieBreak     = 0x0F1E2D3C4B5A6978UL,
    }

    /// <summary>
    /// Deterministic, platform-stable RNG. No UnityEngine.Random.
    /// </summary>
    public static class DecisionRng
    {
        // SplitMix64 step
        private static ulong NextU64(ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        // FNV-1a hash for string -> ulong (stable)
        private static ulong HashString(string s)
        {
            unchecked
            {
                ulong h = 1469598103934665603UL;
                for (int i = 0; i < s.Length; i++)
                {
                    h ^= (byte)s[i];
                    h *= 1099511628211UL;
                }
                return h;
            }
        }

        private static ulong Mix(ulong a, ulong b) => NextU64(a ^ NextU64(b));

        /// <summary>
        /// Returns [0, 1) deterministic float from seed+stream+keys.
        /// </summary>
        public static float Value01(int battleSeed, DecisionStream stream, string keyA, string keyB, int tick)
        {
            ulong s = (ulong)(uint)battleSeed;
            ulong x = Mix(s, (ulong)stream);
            x = Mix(x, HashString(keyA));
            x = Mix(x, HashString(keyB));
            x = Mix(x, (ulong)(uint)tick);

            // take top 24 bits to float mantissa-ish
            uint u = (uint)(x >> 40); // 24 bits
            return u / 16777216f;     // 2^24
        }

        /// <summary>
        /// Returns [-1, 1] deterministic.
        /// </summary>
        public static float Signed(int battleSeed, DecisionStream stream, string keyA, string keyB, int tick)
        {
            return Value01(battleSeed, stream, keyA, keyB, tick) * 2f - 1f;
        }

        /// <summary>
        /// Deterministic bool.
        /// </summary>
        public static bool Coin(int battleSeed, DecisionStream stream, string keyA, string keyB, int tick, float p = 0.5f)
        {
            return Value01(battleSeed, stream, keyA, keyB, tick) < p;
        }
    }
}