using System;
using DarkDescent.Core;

namespace DarkDescent.Items
{
    /// <summary>
    /// Il loot di una partita: seme della partita, generatore e la regola che lega i due. Il seme di
    /// un nemico viene da profondità e punto di partenza; da lì la loot table decide se cade qualcosa
    /// e cosa, e il generatore fa l'oggetto con un seme suo. Logica pura: lo stesso ingresso dà
    /// sempre lo stesso oggetto, e chiederlo prima della morte non cambia niente.
    /// </summary>
    public sealed class LootRoller
    {
        private readonly ItemGenerator _generator;

        public LootRoller(ItemGenerator generator, ulong runSeed)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            RunSeed = runSeed;
        }

        /// <summary>Il seme della partita: da -seed, oppure casuale e scritto nel log.</summary>
        public ulong RunSeed { get; set; }

        /// <summary>L'oggetto che lascia il nemico partito da questa cella, o null se non lascia niente.</summary>
        public ItemInstance Roll(LootTable table, int depth, int cellX, int cellZ)
        {
            if (table == null)
            {
                return null;
            }

            var random = new SplitMix64Source(SeedMixer.ForEnemy(RunSeed, depth, cellX, cellZ));
            if (!table.TryRoll(random, out var item))
            {
                return null;
            }

            // il livello dell'oggetto è la profondità del livello (D8)
            return _generator.Generate(item, depth, random.NextUInt64());
        }
    }
}
