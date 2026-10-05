namespace DarkDescent.Core
{
    /// <summary>
    /// Ricava dal seme della partita un seme per ogni cosa che ne ha bisogno (ADR-029): ogni nemico
    /// dalla profondità e dal punto di partenza (D5 della M5), ogni livello generato dalla profondità
    /// (D8 della M6), la pozione di ogni nemico e cassa dal seme del suo oggetto (D14 della M6).
    /// Semi ricavati e non una sequenza condivisa: il loot non cambia la mappa e la mappa non cambia
    /// il loot. Funzione nostra e non GetHashCode, che cambia tra runtime.
    /// </summary>
    public static class SeedMixer
    {
        public static ulong ForEnemy(ulong runSeed, int depth, int cellX, int cellZ)
        {
            ulong seed = Mix(runSeed ^ (ulong)(uint)depth);
            seed = Mix(seed ^ (ulong)(uint)cellX);
            return Mix(seed ^ ((ulong)(uint)cellZ << 32));
        }

        // "POTION" in ASCII: il tiro della pozione è a parte, e gli oggetti già legati al seme non cambiano
        private const ulong PotionDomain = 0x504F54494F4EUL;

        public static ulong ForPotion(ulong runSeed, int depth, int cellX, int cellZ)
        {
            return Mix(ForEnemy(runSeed, depth, cellX, cellZ) ^ PotionDomain);
        }

        // "LEVEL" in ASCII: il seme di un livello non coincide mai con quello di un nemico
        private const ulong LevelDomain = 0x4C4556454CUL;

        public static ulong ForLevel(ulong runSeed, int depth)
        {
            ulong seed = Mix(runSeed ^ LevelDomain);
            return Mix(seed ^ (ulong)(uint)depth);
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
