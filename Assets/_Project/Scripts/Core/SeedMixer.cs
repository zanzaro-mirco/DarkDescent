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

        // "SCROLL" in ASCII: la pergamena di un livello non sposta né la mappa né il loot dei nemici
        private const ulong ScrollDomain = 0x5343524F4C4CUL;

        public static ulong ForScroll(ulong runSeed, int depth)
        {
            return Mix(ForLevel(runSeed, depth) ^ ScrollDomain);
        }

        // "GOLD" in ASCII: l'oro di un nemico o di una cassa non sposta né l'oggetto né la pozione
        private const ulong GoldDomain = 0x474F4C44UL;

        public static ulong ForGold(ulong runSeed, int depth, int cellX, int cellZ)
        {
            return Mix(ForEnemy(runSeed, depth, cellX, cellZ) ^ GoldDomain);
        }

        // "DECOR": gli arredi di un livello (D4 della M11), dopo tutto il resto e con il loro flusso
        private const ulong DecorDomain = 0x4445434F52UL;

        /// <summary>Il seme degli arredi, dal seme del livello: mappa, nemici e casse non cambiano.</summary>
        public static ulong ForDecor(ulong levelSeed)
        {
            return Mix(levelSeed ^ DecorDomain);
        }

        // "SHOP": l'assortimento del mercante, a una profondità e a un ritorno in città (D5 della M10)
        private const ulong ShopDomain = 0x53484F50UL;

        public static ulong ForShop(ulong runSeed, int depth, int visit)
        {
            return Mix(ForLevel(runSeed, depth) ^ ShopDomain ^ Mix((ulong)(uint)visit));
        }

        // "SPECIAL": i nemici speciali di un livello (M12), dopo il popolatore e con il loro flusso
        private const ulong SpecialDomain = 0x5350454349414CUL;

        /// <summary>Il seme dei campioni e dell'unico, dal seme del livello: mappa, nemici, casse e arredi non cambiano.</summary>
        public static ulong ForSpecials(ulong levelSeed)
        {
            return Mix(levelSeed ^ SpecialDomain);
        }

        // "UNIQUE": se l'unico del mondo sta a una profondità (D6 della M12), un tiro per profondità
        private const ulong UniqueDomain = 0x554E49515545UL;

        public static ulong ForUnique(ulong runSeed, int depth)
        {
            return Mix(ForLevel(runSeed, depth) ^ UniqueDomain);
        }

        // "EXTRA": i tiri in più di campioni, unici e forzieri (D7 della M12), uno per indice
        private const ulong ExtraDomain = 0x4558545241UL;

        /// <summary>Il seme del tiro in più numero <paramref name="index"/>: il primo oggetto, la pozione e l'oro non cambiano.</summary>
        public static ulong ForExtraLoot(ulong runSeed, int depth, int cellX, int cellZ, int index)
        {
            return Mix(ForEnemy(runSeed, depth, cellX, cellZ) ^ ExtraDomain ^ Mix((ulong)(uint)index));
        }

        // "BARREL": il loot di un barile da rompere (D5 della M13), lontano da quello di nemici e casse
        private const ulong BarrelDomain = 0x42415252454CUL;

        /// <summary>Il seme dell'oggetto di un barile, dalla sua cella: il loot di nemici e casse non cambia.</summary>
        public static ulong ForBarrel(ulong runSeed, int depth, int cellX, int cellZ)
        {
            return Mix(ForEnemy(runSeed, depth, cellX, cellZ) ^ BarrelDomain);
        }

        /// <summary>Il seme della pozione di un barile: come per i nemici, un tiro a parte dall'oggetto.</summary>
        public static ulong ForBarrelPotion(ulong runSeed, int depth, int cellX, int cellZ)
        {
            return Mix(ForBarrel(runSeed, depth, cellX, cellZ) ^ PotionDomain);
        }

        /// <summary>Il seme dell'oro di un barile, a parte da oggetto e pozione.</summary>
        public static ulong ForBarrelGold(ulong runSeed, int depth, int cellX, int cellZ)
        {
            return Mix(ForBarrel(runSeed, depth, cellX, cellZ) ^ GoldDomain);
        }

        // "BOOM": se un barile da rompere è esplosivo (D8 della M13), dalla sua cella: il loot non cambia
        private const ulong ExplosiveDomain = 0x424F4F4DUL;

        /// <summary>Il seme che decide se il barile di questa cella è esplosivo.</summary>
        public static ulong ForExplosive(ulong runSeed, int depth, int cellX, int cellZ)
        {
            return Mix(ForBarrel(runSeed, depth, cellX, cellZ) ^ ExplosiveDomain);
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
