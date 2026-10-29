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

        // "DECALS": le macchie a terra di un livello (D5 della M17), dopo tutto il resto e con il loro flusso
        private const ulong DecalDomain = 0x444543414C53UL;

        /// <summary>Il seme delle macchie a terra, dal seme del livello: mappa, nemici, casse e arredi non cambiano.</summary>
        public static ulong ForDecals(ulong levelSeed)
        {
            return Mix(levelSeed ^ DecalDomain);
        }

        // "SHOP": l'assortimento del mercante, a una profondità e a un ritorno in città (D5 della M10)
        private const ulong ShopDomain = 0x53484F50UL;

        public static ulong ForShop(ulong runSeed, int depth, int visit)
        {
            return Mix(ForLevel(runSeed, depth) ^ ShopDomain ^ Mix((ulong)(uint)visit));
        }

        /// <summary>
        /// Il seme di un banco del borgo (D18 della M18): ogni banco ha il suo flusso. Il banco 0 è
        /// quello di <see cref="ForShop(ulong, int, int)"/>.
        /// </summary>
        public static ulong ForShop(ulong runSeed, int depth, int visit, int stall)
        {
            ulong seed = ForShop(runSeed, depth, visit);
            return stall == 0 ? seed : Mix(seed ^ ((ulong)(uint)stall << 32));
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
        private const ulong NamedDomain = 0x4E414D4544UL;

        /// <summary>
        /// Il tiro che fa di un oggetto caduto un unico o un pezzo di set (D7 della M16): dal seme
        /// dell'oggetto stesso, così non tocca nessun altro tiro di nemici, casse e barili.
        /// </summary>
        public static ulong ForNamed(ulong itemSeed)
        {
            return Mix(itemSeed ^ NamedDomain);
        }

        /// <summary>Il tiro del tesoro del re che garantisce a volte un unico o un pezzo di set (D7 della M16).</summary>
        public static ulong ForNamedGuarantee(ulong runSeed, int depth, int cellX, int cellZ)
        {
            return Mix(ForEnemy(runSeed, depth, cellX, cellZ) ^ NamedDomain);
        }

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

        // "COMFORT": cosa dà la cassa senza oggetto, pozione o oro (D13 della M13)
        private const ulong ComfortDomain = 0x434F4D464F5254UL;

        public static ulong ForComfort(ulong runSeed, int depth, int cellX, int cellZ)
        {
            return Mix(ForEnemy(runSeed, depth, cellX, cellZ) ^ ComfortDomain);
        }

        // "WORLD": il seme di un mondo nuovo (D4 della M14), da quello del mondo prima e dal suo numero
        private const ulong WorldDomain = 0x574F524C44UL;

        /// <summary>
        /// Il seme del mondo numero <paramref name="world"/>, ricavato da quello del mondo prima: niente
        /// orologio, così la stessa partita rifà gli stessi mondi.
        /// </summary>
        public static ulong ForWorld(ulong previousSeed, int world)
        {
            return Mix(Mix(previousSeed ^ WorldDomain) ^ (ulong)(uint)world);
        }

        // "BOSS": i modificatori del re alle difficoltà alte (D5 della M14), uno per mondo
        private const ulong BossDomain = 0x424F5353UL;

        public static ulong ForBoss(ulong runSeed)
        {
            return Mix(runSeed ^ BossDomain);
        }

        // "HERO": l'equipaggiamento del cavaliere di -herolevel (D13 della M14), un seme per slot
        private const ulong HeroDomain = 0x4845524FUL;

        public static ulong ForHero(ulong runSeed, int slot)
        {
            return Mix(Mix(runSeed ^ HeroDomain) ^ (ulong)(uint)slot);
        }

        // "NIGHT": i semi della ronda notturna (D4 della M15), uno per parte e per caso
        private const ulong NightDomain = 0x4E49474854UL;

        /// <summary>
        /// Il seme del caso numero <paramref name="index"/> della parte <paramref name="part"/> nella
        /// notte <paramref name="nightSeed"/>: ogni parte ha i suoi, e una notte nuova li cambia tutti.
        /// </summary>
        public static ulong ForNightly(ulong nightSeed, int part, int index)
        {
            return Mix(Mix(Mix(nightSeed ^ NightDomain) ^ (ulong)(uint)part) ^ (ulong)(uint)index);
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
