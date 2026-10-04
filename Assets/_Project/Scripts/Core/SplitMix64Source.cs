namespace DarkDescent.Core
{
    /// <summary>
    /// <see cref="IRandomSource"/> riproducibile su ogni piattaforma e versione di .NET (D5 della M5):
    /// SplitMix64, poche righe scritte qui, senza dipendere da System.Random. Stesso seme, stessa
    /// sequenza, anche in build Web.
    /// </summary>
    public sealed class SplitMix64Source : IRandomSource
    {
        private const double InverseTwoTo53 = 1.0 / (1UL << 53);

        private ulong _state;

        public SplitMix64Source(ulong seed)
        {
            Seed = seed;
            _state = seed;
        }

        public ulong Seed { get; }

        /// <summary>Il prossimo numero a 64 bit della sequenza.</summary>
        public ulong NextUInt64()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>In [0, 1): i 53 bit alti, quanti ne tiene un double.</summary>
        public double NextDouble()
        {
            return (NextUInt64() >> 11) * InverseTwoTo53;
        }
    }
}
