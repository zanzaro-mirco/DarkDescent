using System;
using System.Collections.Generic;
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

        /// <summary>
        /// La pozione che lascia in più il nemico, o la cassa, partito da questa cella; null se non ne
        /// lascia. Un tiro a parte con un seme suo: non cambia l'oggetto di <see cref="Roll"/>.
        /// </summary>
        public ItemInstance RollPotion(LootTable table, int depth, int cellX, int cellZ)
        {
            if (table == null || (table.Potion == null && table.ManaPotion == null))
            {
                return null;
            }

            var random = new SplitMix64Source(SeedMixer.ForPotion(RunSeed, depth, cellX, cellZ));
            var potion = table.PotionFor(random.NextDouble());
            return potion != null ? new ItemInstance(potion) : null;
        }

        /// <summary>
        /// L'oro che lascia il nemico, o la cassa, partito da questa cella; 0 se non ne lascia (D4 della
        /// M10). Un tiro a parte con un seme suo, come la pozione: oggetto e pozione non cambiano.
        /// </summary>
        public int RollGold(LootTable table, int depth, int cellX, int cellZ)
        {
            if (table == null || table.Gold == null)
            {
                return 0;
            }

            return table.GoldFor(new SplitMix64Source(SeedMixer.ForGold(RunSeed, depth, cellX, cellZ)), depth);
        }

        /// <summary>
        /// La pergamena del ritorno che lascia il nemico, o la cassa, partito da questa cella; null se
        /// non ne lascia (D6 della M10). Un tiro a parte con un seme suo: il resto del loot non cambia.
        /// </summary>
        public ItemInstance RollReturnScroll(LootTable table, int depth, int cellX, int cellZ)
        {
            if (table == null || table.ReturnScroll == null)
            {
                return null;
            }

            var random = new SplitMix64Source(SeedMixer.ForReturnScroll(RunSeed, depth, cellX, cellZ));
            return random.NextDouble() < table.ReturnScrollChance ? new ItemInstance(table.ReturnScroll) : null;
        }

        /// <summary>
        /// Gli oggetti del mercante (D5 della M10): <see cref="MerchantTable.StockSize"/> basi pesate,
        /// del livello <paramref name="depth"/>, da un seme di profondità e ritorno in città. Stessi
        /// ingressi, stesso assortimento: non si salva. <paramref name="into"/> viene svuotata prima.
        /// </summary>
        public void RollStock(MerchantTable table, int depth, int visit, List<ItemInstance> into)
        {
            into.Clear();
            if (table == null)
            {
                return;
            }

            var random = new SplitMix64Source(SeedMixer.ForShop(RunSeed, depth, visit));
            for (int i = 0; i < table.StockSize; i++)
            {
                var item = table.PickStock(random);
                if (item == null)
                {
                    return;
                }

                into.Add(_generator.Generate(item, depth, random.NextUInt64()));
            }
        }

        /// <summary>
        /// La pergamena di un livello (D5 della M9), o null: un tiro per profondità con un seme suo.
        /// <paramref name="wanted"/> dice quali incantesimi può ancora insegnare; <paramref name="carrier"/>,
        /// tra 0 e 1, sceglie tra nemici e casse chi la porta.
        /// </summary>
        public ItemInstance RollScroll(ScrollTable table, int depth, Func<Magic.SpellDefinition, bool> wanted, out double carrier)
        {
            carrier = 0.0;
            if (table == null)
            {
                return null;
            }

            var random = new SplitMix64Source(SeedMixer.ForScroll(RunSeed, depth));
            return table.TryRoll(random, depth, wanted, out var scroll, out carrier) ? new ItemInstance(scroll) : null;
        }
    }
}
