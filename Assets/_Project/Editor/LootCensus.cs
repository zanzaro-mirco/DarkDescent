using System.Collections.Generic;
using System.Linq;
using System.Text;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Quanti oggetti lascia un livello, in media (D13 della M13): genera i livelli come il gioco
    /// (<see cref="DungeonLevel.CreateMap"/>, nemici speciali e scorte compresi) e somma per ogni fonte
    /// quanti oggetti ci si aspettano dai numeri delle tabelle, divisi per rarità. Valori attesi, non
    /// tiri: due conteggi uguali danno lo stesso risultato, e 200 livelli per profondità bastano.
    /// Contano gli oggetti con una rarità: pozioni, oro e pergamene no.
    /// </summary>
    public static class LootCensus
    {
        public const string CryptSettings = "Assets/_Project/Data/Levels/CryptSettings.asset";
        public const string CaveSettings = "Assets/_Project/Data/Levels/CaveSettings.asset";
        public const string CryptTileset = "Assets/_Project/Data/Levels/DungeonTileset.asset";
        public const string CaveTileset = "Assets/_Project/Data/Levels/CaveTileset.asset";

        /// <summary>Gli oggetti attesi di uno o più livelli, per fonte e per rarità.</summary>
        public sealed class Tally
        {
            private readonly Dictionary<string, double> _bySource = new Dictionary<string, double>();

            public double Items { get; private set; }

            public double Magic { get; private set; }

            public double Rare { get; private set; }

            public int Levels { get; private set; }

            public IReadOnlyDictionary<string, double> BySource => _bySource;

            public double PerLevel => Levels > 0 ? Items / Levels : 0.0;

            public void Add(string source, double items, double magic, double rare)
            {
                _bySource[source] = (_bySource.TryGetValue(source, out double n) ? n : 0.0) + items;
                Items += items;
                Magic += magic;
                Rare += rare;
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

                Items += other.Items;
                Magic += other.Magic;
                Rare += other.Rare;
                Levels += other.Levels;
            }
        }

        /// <summary>Gli oggetti attesi del livello di una partita a una profondità.</summary>
        public static Tally Level(DungeonSettings settings, LevelTileset tileset, ChampionSettings champions, ulong runSeed, int depth)
        {
            var tally = new Tally();
            tally.CountLevel();
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
                    AddRolls(tally, source, drop.LootTable, bonus, escorts.Contains(cell) ? 0.5f : 1f);
                }
                else if (prefab.TryGetComponent(out Chest chest) && chest.LootTable != null)
                {
                    AddRolls(tally, chest.Bonus != null && chest.Bonus.ExtraRolls > 0 ? "forzieri" : "casse", chest.LootTable, chest.Bonus, 1f);
                }
                else
                {
                    foreach (var barrel in prefab.GetComponentsInChildren<Breakable>(true))
                    {
                        AddRolls(tally, "barili", barrel.LootTable, null, 1f);
                    }
                }
            }

            return tally;
        }

        /// <summary>Le profondità da <paramref name="first"/> a <paramref name="last"/>, <paramref name="seeds"/> partite ciascuna: sempre le stesse.</summary>
        public static Tally Range(DungeonSettings settings, LevelTileset tileset, ChampionSettings champions, int first, int last, int seeds)
        {
            return Range(settings, tileset, champions, first, last, FixedSeeds(seeds));
        }

        /// <summary>Le profondità da <paramref name="first"/> a <paramref name="last"/>, una partita per seme: la ronda notturna dà i suoi (M15).</summary>
        public static Tally Range(DungeonSettings settings, LevelTileset tileset, ChampionSettings champions, int first, int last, IReadOnlyList<ulong> runSeeds)
        {
            var total = new Tally();
            for (int depth = first; depth <= last; depth++)
            {
                foreach (ulong seed in runSeeds)
                {
                    total.Merge(Level(settings, tileset, champions, seed, depth));
                }
            }

            return total;
        }

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
        private static void AddRolls(Tally tally, string source, LootTable table, LootBonus bonus, float chanceFactor)
        {
            if (table == null)
            {
                return;
            }

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
