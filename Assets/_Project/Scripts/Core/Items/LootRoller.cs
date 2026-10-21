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
        private readonly Func<int, int> _itemLevel;

        /// <param name="itemLevel">
        /// Il livello degli oggetti di una profondità: nel gioco il livello della zona (D7 della M14).
        /// Senza, la profondità stessa, come prima della M14.
        /// </param>
        public LootRoller(ItemGenerator generator, ulong runSeed, Func<int, int> itemLevel = null)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            RunSeed = runSeed;
            _itemLevel = itemLevel;
        }

        /// <summary>Il livello degli oggetti che cadono a questa profondità, almeno 1.</summary>
        public int ItemLevel(int depth)
        {
            return Math.Max(1, _itemLevel != null ? _itemLevel(depth) : depth);
        }

        /// <summary>Il seme della partita: da -seed, oppure casuale e scritto nel log.</summary>
        public ulong RunSeed { get; set; }

        /// <summary>
        /// L'oggetto che lascia il nemico partito da questa cella, o null se non lascia niente. Con
        /// <paramref name="chanceFactor"/> la probabilità della tabella si moltiplica (le scorte, D13 della
        /// M13); lo stesso tiro, quindi l'oggetto, se cade, è quello di sempre.
        /// </summary>
        public ItemInstance Roll(LootTable table, int depth, int cellX, int cellZ, float chanceFactor = 1f)
        {
            return table != null ? RollFrom(table, depth, SeedMixer.ForEnemy(RunSeed, depth, cellX, cellZ), chanceFactor) : null;
        }

        /// <summary>
        /// La cassa che non lascia un oggetto dà al suo posto una pozione o il doppio dell'oro (D13 della
        /// M13): true per la pozione, da un tiro suo della cella.
        /// </summary>
        public bool RollChestComfort(int depth, int cellX, int cellZ)
        {
            return new SplitMix64Source(SeedMixer.ForComfort(RunSeed, depth, cellX, cellZ)).NextDouble() < 0.5;
        }

        /// <summary>
        /// L'oggetto del barile da rompere di questa cella, o null (D5 della M13): la stessa regola dei
        /// nemici con i semi del barile, che non toccano quelli di nemici e casse.
        /// </summary>
        public ItemInstance RollBarrel(LootTable table, int depth, int cellX, int cellZ)
        {
            return table != null ? RollFrom(table, depth, SeedMixer.ForBarrel(RunSeed, depth, cellX, cellZ)) : null;
        }

        /// <summary>La pozione del barile di questa cella, o null.</summary>
        public ItemInstance RollBarrelPotion(LootTable table, int depth, int cellX, int cellZ)
        {
            return table != null ? PotionFrom(table, SeedMixer.ForBarrelPotion(RunSeed, depth, cellX, cellZ)) : null;
        }

        /// <summary>L'oro del barile di questa cella, 0 se non ne lascia.</summary>
        public int RollBarrelGold(LootTable table, int depth, int cellX, int cellZ)
        {
            return table != null ? GoldFrom(table, depth, SeedMixer.ForBarrelGold(RunSeed, depth, cellX, cellZ)) : 0;
        }

        private ItemInstance RollFrom(LootTable table, int depth, ulong rollSeed, float chanceFactor = 1f)
        {
            var random = new SplitMix64Source(rollSeed);
            if (!table.TryRoll(random, out var item, chanceFactor))
            {
                return null;
            }

            // il livello dell'oggetto viene dalla profondità (D7 della M14); sotto la rarità minima della
            // tabella, lo stesso oggetto con lo stesso seme rifatto a quella rarità
            ulong seed = random.NextUInt64();
            int level = ItemLevel(depth);
            var generated = _generator.Generate(item, level, seed);
            return generated.Rarity < table.MinRarity ? _generator.Generate(item, level, seed, table.MinRarity) : generated;
        }

        /// <summary>
        /// La pozione che lascia in più il nemico, o la cassa, partito da questa cella; null se non ne
        /// lascia. Un tiro a parte con un seme suo: non cambia l'oggetto di <see cref="Roll"/>.
        /// </summary>
        public ItemInstance RollPotion(LootTable table, int depth, int cellX, int cellZ)
        {
            return table != null ? PotionFrom(table, SeedMixer.ForPotion(RunSeed, depth, cellX, cellZ)) : null;
        }

        private static ItemInstance PotionFrom(LootTable table, ulong seed)
        {
            if (table.Potion == null && table.ManaPotion == null)
            {
                return null;
            }

            var potion = table.PotionFor(new SplitMix64Source(seed).NextDouble());
            return potion != null ? new ItemInstance(potion) : null;
        }

        /// <summary>
        /// L'oro che lascia il nemico, o la cassa, partito da questa cella; 0 se non ne lascia (D4 della
        /// M10). Un tiro a parte con un seme suo, come la pozione: oggetto e pozione non cambiano.
        /// </summary>
        public int RollGold(LootTable table, int depth, int cellX, int cellZ)
        {
            return table != null ? GoldFrom(table, depth, SeedMixer.ForGold(RunSeed, depth, cellX, cellZ)) : 0;
        }

        private static int GoldFrom(LootTable table, int depth, ulong seed)
        {
            return table.Gold != null ? table.GoldFor(new SplitMix64Source(seed), depth) : 0;
        }

        /// <summary>
        /// L'oro con il bonus di campioni, unici e forzieri (D7 della M12): lo stesso tiro di
        /// <see cref="RollGold(LootTable, int, int, int)"/>, moltiplicato e arrotondato. Senza bonus è quello.
        /// </summary>
        public int RollGold(LootTable table, int depth, int cellX, int cellZ, LootBonus bonus)
        {
            int gold = RollGold(table, depth, cellX, cellZ);
            if (bonus == null || gold <= 0)
            {
                return gold;
            }

            return Math.Max(gold, (int)Math.Round(gold * (double)bonus.GoldFactor, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Gli oggetti in più di campioni, unici e forzieri (D7 e D8 della M12), in <paramref name="into"/>,
        /// svuotata prima. Ogni tiro ha un seme suo e cade sempre, con le rarità spostate del bonus; se tra
        /// <paramref name="first"/> e questi non c'è la rarità garantita, la prende l'ultimo. Senza bonus, niente.
        /// </summary>
        public void RollExtra(LootTable table, LootBonus bonus, ItemInstance first, int depth, int cellX, int cellZ, List<ItemInstance> into)
        {
            into.Clear();
            if (table == null || bonus == null || bonus.ExtraRolls <= 0)
            {
                return;
            }

            var rarities = _generator.Rarities.Shifted(bonus.MagicFactor, bonus.RareFactor);
            bool guaranteed = first != null && first.Rarity >= bonus.Guaranteed;
            for (int i = 0; i < bonus.ExtraRolls; i++)
            {
                var random = new SplitMix64Source(SeedMixer.ForExtraLoot(RunSeed, depth, cellX, cellZ, i));

                // il tiro in più che non sempre cade (il campione, D13 della M13): un numero in più solo per lui
                if (bonus.ExtraChance < 1f && random.NextDouble() >= bonus.ExtraChance)
                {
                    continue;
                }

                var item = table.PickBase(random);
                if (item == null)
                {
                    return;
                }

                ulong seed = random.NextUInt64();
                var generated = _generator.Generate(item, ItemLevel(depth), seed, rarities);
                var floor = generated.Rarity < table.MinRarity ? table.MinRarity : generated.Rarity;
                if (!guaranteed && i == bonus.ExtraRolls - 1 && floor < bonus.Guaranteed)
                {
                    floor = bonus.Guaranteed;
                }

                // come in Roll: lo stesso oggetto con lo stesso seme, rifatto alla rarità che gli manca
                if (floor > generated.Rarity)
                {
                    generated = _generator.Generate(item, ItemLevel(depth), seed, floor);
                }

                guaranteed |= generated.Rarity >= bonus.Guaranteed;
                into.Add(generated);
            }
        }

        /// <summary>
        /// Gli oggetti del mercante (D5 della M10): <see cref="MerchantTable.StockSize"/> basi pesate,
        /// del livello della profondità <paramref name="depth"/>, da un seme di profondità e ritorno in città. Stessi
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

                into.Add(_generator.Generate(item, ItemLevel(depth), random.NextUInt64()));
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
