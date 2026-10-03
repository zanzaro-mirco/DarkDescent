using System;

namespace DarkDescent.Core
{
    /// <summary>
    /// <see cref="IRandomSource"/> su <see cref="System.Random"/>: con lo stesso seme, la stessa
    /// sequenza. Alla M5 è quello che rende riproducibile un drop.
    /// </summary>
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }

        public int Seed { get; }

        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}
