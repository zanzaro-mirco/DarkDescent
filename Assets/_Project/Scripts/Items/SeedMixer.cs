namespace DarkDescent.Items
{
    /// <summary>
    /// Mescola il seme della partita con la profondità e il punto di partenza di un nemico (D5 della
    /// M5): ogni nemico ha un seme suo, sempre lo stesso, in qualunque ordine si uccidano. Funzione
    /// nostra e non GetHashCode, che cambia tra runtime.
    /// </summary>
    public static class SeedMixer
    {
        public static ulong ForEnemy(ulong runSeed, int depth, int cellX, int cellZ)
        {
            ulong seed = Mix(runSeed ^ (ulong)(uint)depth);
            seed = Mix(seed ^ (ulong)(uint)cellX);
            return Mix(seed ^ ((ulong)(uint)cellZ << 32));
        }

        // il finale di SplitMix64: ogni bit dell'ingresso cambia metà dei bit dell'uscita
        private static ulong Mix(ulong value)
        {
            unchecked
            {
                value += 0x9E3779B97F4A7C15UL;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }
    }
}
