using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Progression;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Quanti oggetti lascia un livello, in media (D13 della M13): genera i livelli come il gioco
    /// (<see cref="DungeonLevel.CreateMap"/>, nemici speciali e scorte compresi) e somma per ogni fonte
    /// quanti oggetti ci si aspettano dai numeri delle tabelle, divisi per rarità. Valori attesi, non
    /// tiri: due conteggi uguali danno lo stesso risultato, e 200 livelli per profondità bastano.
    /// Contano gli oggetti con una rarità: pozioni, oro e pergamene no. Con le probabilità di una
    /// difficoltà (<see cref="NamedOdds"/>) conta anche unici e pezzi di set (D11 della M16).
    /// </summary>
    public static class LootCensus
    {
        public const string CryptSettings = "Assets/_Project/Data/Levels/CryptSettings.asset";
        public const string CaveSettings = "Assets/_Project/Data/Levels/CaveSettings.asset";
        public const string CryptTileset = "Assets/_Project/Data/Levels/DungeonTileset.asset";
        public const string CaveTileset = "Assets/_Project/Data/Levels/CaveTileset.asset";
        public const string NamedItemsPath = "Assets/_Project/Data/NamedItemDatabase.asset";
        public const string ProgressionPath = "Assets/_Project/Data/Progression/Progression.asset";

        /// <summary>
        /// Le probabilità di unici e set di una difficoltà e il livello degli oggetti di ogni
        /// profondità, come le dà il gioco al <c>LootRoller</c> (D7 della M16).
        /// </summary>
        public sealed class NamedOdds
        {
            private readonly Func<int, int> _itemLevel;

            public NamedOdds(NamedItemDatabase items, float uniqueChance, float setChance, Func<int, int> itemLevel)
            {
                Items = items ?? throw new ArgumentNullException(nameof(items));
                UniqueChance = uniqueChance;
                SetChance = setChance;
                _itemLevel = itemLevel ?? throw new ArgumentNullException(nameof(itemLevel));
            }

            public NamedItemDatabase Items { get; }

            public float UniqueChance { get; }

            public float SetChance { get; }

            public int ItemLevel(int depth) => Math.Max(1, _itemLevel(depth));

            /// <summary>Le probabilità della difficoltà numero <paramref name="tier"/>, con il livello degli oggetti che le dà.</summary>
            public static NamedOdds For(DifficultyTier tier, ProgressionSettings settings, NamedItemDatabase items)
            {
                return new NamedOdds(items, tier.UniqueChance, tier.SetChance, depth => settings.ZoneLevel(depth, tier.ZoneLevelBonus));
            }
        }

        /// <summary>Un tiro di loot di un livello: da dove viene, con quale tabella e quale bonus.</summary>
        public readonly struct Source
        {
            public Source(string name, LootTable table, LootBonus bonus, float chanceFactor)
            {
                Name = name;
                Table = table;
                Bonus = bonus;
                ChanceFactor = chanceFactor;
            }

            public string Name { get; }

            public LootTable Table { get; }

            public LootBonus Bonus { get; }

            public float ChanceFactor { get; }
        }

        /// <summary>Gli oggetti attesi di uno o più livelli, per fonte e per rarità.</summary>
        public sealed class Tally
        {
            private readonly Dictionary<string, double> _bySource = new Dictionary<string, double>();
            private readonly Dictionary<string, double> _namedBySource = new Dictionary<string, double>();

            public double Items { get; private set; }

            public double Magic { get; private set; }

            public double Rare { get; private set; }

            /// <summary>Gli unici attesi: zero se il conteggio non ha avuto <see cref="NamedOdds"/>.</summary>
            public double Unique { get; private set; }

            public double Set { get; private set; }

            public int Levels { get; private set; }

            public IReadOnlyDictionary<string, double> BySource => _bySource;

            /// <summary>Unici e pezzi di set attesi per fonte.</summary>
            public IReadOnlyDictionary<string, double> NamedBySource => _namedBySource;

            public double PerLevel => Levels > 0 ? Items / Levels : 0.0;

            public void Add(string source, double items, double magic, double rare)
            {
                _bySource[source] = (_bySource.TryGetValue(source, out double n) ? n : 0.0) + items;
                Items += items;
                Magic += magic;
                Rare += rare;
            }

            public void AddNamed(string source, double unique, double set)
            {
                if (unique + set <= 0.0)
                {
                    return;
                }

                _namedBySource[source] = (_namedBySource.TryGetValue(source, out double n) ? n : 0.0) + unique + set;
                Unique += unique;
                Set += set;
            }

            public void CountLevel()
            {
                Levels++;
            }

            public void Merge(Tally other)
            {
                foreach (var pair in other._bySource)
                {
                    _bySource[pair.Key] = (_bySource.TryGetValue(pair.Key, out double n) ? n : 0.0) + pair.Value;
                }

                foreach (var pair in other._namedBySource)
                {
                    _namedBySource[pair.Key] = (_namedBySource.TryGetValue(pair.Key, out double n) ? n : 0.0) + pair.Value;
                }

                Items += other.Items;
                Magic += other.Magic;
                Rare += other.Rare;
                Unique += other.Unique;
                Set += other.Set;
                Levels += other.Levels;
            }
        }

        /// <summary>
        /// Gli oggetti attesi del livello di una partita a una profondità; con <paramref name="odds"/>
        /// anche gli unici e i pezzi di set.
        /// </summary>
        public static Tally Level(DungeonSettings settings, LevelTileset tileset, ChampionSettings champions, ulong runSeed, int depth, NamedOdds odds = null)
        {
            return Count(Sources(settings, tileset, champions, runSeed, depth), depth, odds);
        }

        /// <summary>Gli oggetti attesi di un livello dai suoi tiri: lo stesso livello si conta con più difficoltà senza rigenerarlo.</summary>
        public static Tally Count(IReadOnlyList<Source> sources, int depth, NamedOdds odds = null)
        {
            var tally = new Tally();
            tally.CountLevel();
            foreach (var source in sources)
            {
                AddRolls(tally, source, depth, odds);
            }

            return tally;
        }

        /// <summary>I tiri di loot del livello di una partita a una profondità, generato come nel gioco.</summary>
        public static List<Source> Sources(DungeonSettings settings, LevelTileset tileset, ChampionSettings champions, ulong runSeed, int depth)
        {
            var sources = new List<Source>();
            var map = DungeonLevel.CreateMap(settings, runSeed, depth, out _);
            var leaders = new HashSet<Vector2Int>(ChampionSpawn.FromMap(map).Select(c => new Vector2Int(c.X, c.Y)));
            var escorts = SpecialEnemyPlanner.EscortsFromMap(map);
            foreach (var marker in map.Markers)
            {
                var prefab = tileset.GetMarkerPrefab(marker.Symbol);
                if (prefab == null)
                {
                    continue;
                }

                var cell = new Vector2Int(marker.X, marker.Y);
                if (prefab.TryGetComponent(out LootDrop drop) && drop.LootTable != null)
                {
                    EnemyRank? rank = prefab.TryGetComponent(out SpecialEnemy special) ? special.Rank : (EnemyRank?)null;
                    if (leaders.Contains(cell))
                    {
                        rank = EnemyRank.Champion;
                    }

                    var bonus = rank.HasValue ? champions.For(rank.Value)?.Loot : null;
                    string source = rank == EnemyRank.Unique ? "unico" : rank == EnemyRank.Champion ? "campioni"
                        : escorts.Contains(cell) ? "scorte" : prefab.name;
                    sources.Add(new Source(source, drop.LootTable, bonus, escorts.Contains(cell) ? 0.5f : 1f));
                }
                else if (prefab.TryGetComponent(out Chest chest) && chest.LootTable != null)
                {
                    string source = chest.Bonus != null && chest.Bonus.NamedGuarantee > 0f ? "tesoro del re"
                        : chest.Bonus != null && chest.Bonus.ExtraRolls > 0 ? "forzieri" : "casse";
                    sources.Add(new Source(source, chest.LootTable, chest.Bonus, 1f));
                }
                else
                {
                    foreach (var barrel in prefab.GetComponentsInChildren<Breakable>(true))
                    {
                        sources.Add(new Source("barili", barrel.LootTable, null, 1f));
                    }
                }
            }

            return sources;
        }

        /// <summary>Le profondità da <paramref name="first"/> a <paramref name="last"/>, <paramref name="seeds"/> partite ciascuna: sempre le stesse.</summary>
        public static Tally Range(DungeonSettings settings, LevelTileset tileset, ChampionSettings champions, int first, int last, int seeds, NamedOdds odds = null)
        {
            return Range(settings, tileset, champions, first, last, FixedSeeds(seeds), odds);
        }

        /// <summary>Le profondità da <paramref name="first"/> a <paramref name="last"/>, una partita per seme: la ronda notturna dà i suoi (M15).</summary>
        public static Tally Range(DungeonSettings settings, LevelTileset tileset, ChampionSettings champions, int first, int last, IReadOnlyList<ulong> runSeeds, NamedOdds odds = null)
        {
            var total = new Tally();
            for (int depth = first; depth <= last; depth++)
            {
                foreach (ulong seed in runSeeds)
                {
                    total.Merge(Level(settings, tileset, champions, seed, depth, odds));
                }
            }

            return total;
        }

        /// <summary>
        /// Unici e pezzi di set attesi in una discesa intera, dalla profondità 1 all'8, per ogni
        /// difficoltà della tabella (D11 della M16): un conteggio per difficoltà, sugli stessi livelli.
        /// </summary>
        public static List<Tally> NamedPerDifficulty(DifficultyTable difficulties, IReadOnlyList<ulong> runSeeds)
        {
            var champions = AssetDatabase.LoadAssetAtPath<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset");
            var settings = AssetDatabase.LoadAssetAtPath<ProgressionSettings>(ProgressionPath);
            var items = AssetDatabase.LoadAssetAtPath<NamedItemDatabase>(NamedItemsPath);

            // i tiri di ogni livello, una volta sola: la mappa non dipende dalla difficoltà
            var levels = new List<(int depth, List<Source> sources)>();
            foreach (var (settingsPath, tilesetPath) in new[] { (CryptSettings, CryptTileset), (CaveSettings, CaveTileset) })
            {
                var dungeon = AssetDatabase.LoadAssetAtPath<DungeonSettings>(settingsPath);
                var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>(tilesetPath);
                for (int depth = dungeon.FirstDepth; depth <= dungeon.LastDepth; depth++)
                {
                    foreach (ulong seed in runSeeds)
                    {
                        levels.Add((depth, Sources(dungeon, tileset, champions, seed, depth)));
                    }
                }
            }

            var tallies = new List<Tally>(difficulties.Count);
            for (int t = 0; t < difficulties.Count; t++)
            {
                var odds = NamedOdds.For(difficulties.Tier(t), settings, items);
                var total = new Tally();
                foreach (var (depth, sources) in levels)
                {
                    total.Merge(Count(sources, depth, odds));
                }

                tallies.Add(total);
            }

            return tallies;
        }

        /// <summary>Quante discese contiene un conteggio fatto su tutte le profondità.</summary>
        public static double Descents(Tally tally) => tally.Levels / 8.0;

        /// <summary>I semi delle partite di sempre, da 1 a <paramref name="count"/>, sparsi.</summary>
        public static List<ulong> FixedSeeds(int count)
        {
            var seeds = new List<ulong>(count);
            for (ulong seed = 1; seed <= (ulong)count; seed++)
            {
                seeds.Add(seed * 7919UL);
            }

            return seeds;
        }

        // Il tiro di sempre con la probabilità della tabella, se il bonus non lo toglie, più i tiri in più,
        // che cadono sempre con le rarità spostate; la rarità minima della tabella alza le altre
        private static void AddRolls(Tally tally, Source roll, int depth, NamedOdds odds)
        {
            var (source, table, bonus, chanceFactor) = (roll.Name, roll.Table, roll.Bonus, roll.ChanceFactor);
            if (table == null)
            {
                return;
            }

            AddNamed(tally, roll, depth, odds);
            if (bonus == null || !bonus.ReplacesRoll)
            {
                double chance = table.DropChance * chanceFactor;
                var (magic, rare) = Shares(RarityTable.Default, table.MinRarity);
                tally.Add(source, chance, chance * magic, chance * rare);
            }

            if (bonus == null || bonus.ExtraRolls <= 0)
            {
                return;
            }

            var (shiftedMagic, shiftedRare) = Shares(RarityTable.Default.Shifted(bonus.MagicFactor, bonus.RareFactor), table.MinRarity);
            double rares = bonus.ExtraRolls * shiftedRare;
            double magics = bonus.ExtraRolls * shiftedMagic;
            if (bonus.Guaranteed == Rarity.Rare && rares < 1.0)
            {
                // l'ultimo tiro diventa raro se nessuno lo è: almeno uno, preso dagli altri
                double missing = 1.0 - rares;
                magics = System.Math.Max(0.0, magics - missing * shiftedMagic / System.Math.Max(1e-9, 1.0 - shiftedRare));
                rares = 1.0;
            }

            double extraChance = bonus.ExtraChance;
            tally.Add(source, bonus.ExtraRolls * extraChance, magics * extraChance, rares * extraChance);
        }

        // Unici e set attesi di un tiro (D7 della M16): ogni oggetto caduto, se il tiro a parte riesce,
        // diventa un unico o un pezzo della sua base, se ce n'è uno al suo livello; il tesoro del re ha
        // il tiro suo quando nessun oggetto lo è già
        private static void AddNamed(Tally tally, Source roll, int depth, NamedOdds odds)
        {
            if (odds == null)
            {
                return;
            }

            var (table, bonus) = (roll.Table, roll.Bonus);
            var (coverUnique, coverSet, coverAny) = Coverage(table, odds.Items, odds.ItemLevel(depth));
            double unique = 0.0, set = 0.0, none = 1.0;
            if (bonus == null || !bonus.ReplacesRoll)
            {
                double chance = table.DropChance * roll.ChanceFactor;
                var (u, s) = NamedShares(odds, 1f, coverUnique, coverSet);
                unique += chance * u;
                set += chance * s;
                none *= 1.0 - chance * (u + s);
            }

            int extra = bonus != null ? bonus.ExtraRolls : 0;
            if (extra > 0)
            {
                double falls = bonus.ExtraChance;
                var (u, s) = NamedShares(odds, bonus.NamedFactor, coverUnique, coverSet);
                unique += extra * falls * u;
                set += extra * falls * s;
                none *= Math.Pow(1.0 - falls * (u + s), extra);

                // il tiro del re: se nessuno lo è già, il primo oggetto con un unico o un pezzo della sua
                // base lo diventa; unico o set a caso, e se manca l'uno l'altro
                if (bonus.NamedGuarantee > 0f && coverAny > 0.0)
                {
                    double guaranteed = none * bonus.NamedGuarantee * (1.0 - Math.Pow(1.0 - falls * coverAny, extra));
                    double uniqueShare = (0.5 * coverUnique + 0.5 * (coverAny - coverSet)) / coverAny;
                    unique += guaranteed * uniqueShare;
                    set += guaranteed * (1.0 - uniqueShare);
                }
            }

            tally.AddNamed(roll.Name, unique, set);
        }

        // la probabilità che un oggetto caduto diventi un unico o un pezzo di set: il tiro a parte e la base
        private static (double unique, double set) NamedShares(NamedOdds odds, float factor, double coverUnique, double coverSet)
        {
            double unique = Math.Min(1.0, odds.UniqueChance * (double)factor);
            double set = Math.Min(1.0, (odds.UniqueChance + odds.SetChance) * (double)factor) - unique;
            return (unique * coverUnique, set * coverSet);
        }

        /// <summary>
        /// La parte delle basi della tabella, a peso, che ha un unico, un pezzo di set o almeno uno dei
        /// due al livello dato.
        /// </summary>
        public static (double unique, double set, double any) Coverage(LootTable table, NamedItemDatabase items, int itemLevel)
        {
            var candidates = new List<UniqueDefinition>();
            double total = 0.0, unique = 0.0, set = 0.0, any = 0.0;
            foreach (var entry in table.Entries)
            {
                if (entry.Item == null || entry.Weight <= 0)
                {
                    continue;
                }

                total += entry.Weight;
                items.Candidates(entry.Item, itemLevel, Rarity.Unique, candidates);
                bool hasUnique = candidates.Count > 0;
                items.Candidates(entry.Item, itemLevel, Rarity.Set, candidates);
                bool hasSet = candidates.Count > 0;
                unique += hasUnique ? entry.Weight : 0;
                set += hasSet ? entry.Weight : 0;
                any += hasUnique || hasSet ? entry.Weight : 0;
            }

            return total > 0.0 ? (unique / total, set / total, any / total) : (0.0, 0.0, 0.0);
        }

        private static (double magic, double rare) Shares(RarityTable rarities, Rarity minimum)
        {
            double magic = rarities.Probability(Rarity.Magic);
            double rare = rarities.Probability(Rarity.Rare);
            if (minimum >= Rarity.Rare)
            {
                return (0.0, 1.0);
            }

            return minimum == Rarity.Magic ? (1.0 - rare, rare) : (magic, rare);
        }

        [MenuItem("DarkDescent/Conta il loot dei livelli")]
        public static void Report()
        {
            Debug.Log(Describe(200));
        }

        /// <summary>Il resoconto per profondità: oggetti a livello, per fonte e quanti magici e rari.</summary>
        public static string Describe(int seeds)
        {
            var champions = AssetDatabase.LoadAssetAtPath<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset");
            var text = new StringBuilder("[LootCensus] oggetti attesi a livello\n");
            var all = new Tally();
            foreach (var (settingsPath, tilesetPath) in new[] { (CryptSettings, CryptTileset), (CaveSettings, CaveTileset) })
            {
                var settings = AssetDatabase.LoadAssetAtPath<DungeonSettings>(settingsPath);
                var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>(tilesetPath);
                for (int depth = settings.FirstDepth; depth <= settings.LastDepth; depth++)
                {
                    var tally = Range(settings, tileset, champions, depth, depth, seeds);
                    all.Merge(tally);
                    text.Append($"profondità {depth}: {tally.PerLevel:0.00} (magici {tally.Magic / tally.Items:P0}, rari {tally.Rare / tally.Items:P0}) — ");
                    text.AppendLine(string.Join(", ", tally.BySource.OrderByDescending(p => p.Value).Select(p => $"{p.Key} {p.Value / tally.Levels:0.00}")));
                }
            }

            text.Append($"media: {all.PerLevel:0.00} oggetti a livello (magici {all.Magic / all.Items:P0}, rari {all.Rare / all.Items:P0})");
            return text.ToString();
        }
    }
}
