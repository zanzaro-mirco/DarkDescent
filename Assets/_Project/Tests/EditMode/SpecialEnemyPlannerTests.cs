using System;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class SpecialEnemyPlannerTests
    {
        private const int Seeds = 150;

        private static readonly string[] Modifiers = { "fast", "frozen", "regenerating", "unstoppable", "enraged" };

        private static readonly SpecialEnemyRules Rules = new SpecialEnemyRules(2, 1, 0.4f, new Vector2Int(2, 4), new Vector2Int(4, 6), 5, 1f / 3f);

        private static DungeonSettings Crypt => AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");

        private static CaveSettings Caves => AssetDatabase.LoadAssetAtPath<CaveSettings>("Assets/_Project/Data/Levels/CaveSettings.asset");

        private static DungeonLayout Generate(DungeonSettings settings, ulong seed, int depth)
        {
            return settings.CreateGenerator().Generate(seed * 31 + (ulong)depth, depth);
        }

        private static char[,] Snapshot(DungeonLayout layout)
        {
            var cells = new char[layout.Width, layout.Height];
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    cells[x, y] = layout[x, y];
                }
            }

            return cells;
        }

        private static List<ChampionSpawn> Place(DungeonLayout layout, int depth, ulong seed, bool withUnique = false)
        {
            return SpecialEnemyPlanner.Place(layout, depth, Rules, Modifiers, withUnique, new SplitMix64Source(SeedMixer.ForSpecials(seed)));
        }

        private static int Chebyshev(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private static IEnumerable<(DungeonSettings settings, int depth)> Cases()
        {
            yield return (Crypt, 3);
            yield return (Crypt, 4);
            yield return (Caves, 6);
            yield return (Caves, 8);
        }

        [Test, Description("Prima della profondità minima non ci sono nemici speciali e il livello non cambia")]
        public void Place_BeforeMinDepth_ChangesNothing()
        {
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var layout = Generate(Crypt, seed, 1);
                var before = Snapshot(layout);
                var champions = Place(layout, 1, seed, withUnique: true);
                Assert.IsEmpty(champions);
                CollectionAssert.AreEqual(before, Snapshot(layout), $"seme {seed}");
            }
        }

        [Test, Description("Campioni e scorte vanno solo su celle di pavimento libere: nemici, casse, arredi e scala dei semi già provati restano dove sono")]
        public void Place_KeepsWhatWasThere()
        {
            foreach (var (settings, depth) in Cases())
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var layout = Generate(settings, seed, depth);
                    var before = Snapshot(layout);
                    Place(layout, depth, seed, withUnique: true);
                    for (int y = 0; y < layout.Height; y++)
                    {
                        for (int x = 0; x < layout.Width; x++)
                        {
                            if (before[x, y] != LevelMap.Floor)
                            {
                                Assert.AreEqual(before[x, y], layout[x, y], $"{settings.name} seme {seed} cella ({x}, {y})");
                            }
                            else if (layout[x, y] != LevelMap.Floor)
                            {
                                char placed = layout[x, y];
                                Assert.IsTrue(SpecialEnemyPlanner.CanBeChampion(placed) || placed == SpecialEnemyPlanner.UniqueSymbol
                                    || placed == SpecialEnemyPlanner.GoldChestSymbol, $"solo nemici e forzieri nelle celle nuove, non '{placed}'");
                            }
                        }
                    }
                }
            }
        }

        [Test, Description("Un campione è un nemico che c'era già, mai il re, con un modificatore fino alla profondità 4 e due dalla 5, tutti diversi")]
        public void Champions_AreExistingEnemiesWithModifiers()
        {
            foreach (var (settings, depth) in Cases())
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var layout = Generate(settings, seed, depth);
                    var before = Snapshot(layout);
                    foreach (var champion in Place(layout, depth, seed))
                    {
                        Assert.IsTrue(SpecialEnemyPlanner.CanBeChampion(before[champion.X, champion.Y]), "un nemico messo dal popolatore");
                        Assert.AreEqual(Rules.ModifiersAt(depth), champion.Modifiers.Count);
                        CollectionAssert.AllItemsAreUnique(champion.Modifiers);
                        CollectionAssert.IsSubsetOf(champion.Modifiers, Modifiers);
                    }
                }
            }
        }

        [Test, Description("Un gruppo di campioni sicuro e a volte due, lontani tra loro: in quasi tutti i livelli almeno uno, mai più di due")]
        public void Champions_OneOrTwoGroups()
        {
            foreach (var (settings, depth) in Cases())
            {
                int withChampion = 0;
                int withTwo = 0;
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var champions = Place(Generate(settings, seed, depth), depth, seed);
                    Assert.LessOrEqual(champions.Count, Rules.Groups + 1);
                    withChampion += champions.Count > 0 ? 1 : 0;
                    withTwo += champions.Count > 1 ? 1 : 0;
                    if (champions.Count == 2)
                    {
                        var a = new Vector2Int(champions[0].X, champions[0].Y);
                        var b = new Vector2Int(champions[1].X, champions[1].Y);
                        Assert.GreaterOrEqual(Chebyshev(a, b), Rules.Spacing);
                    }
                }

                Assert.GreaterOrEqual(withChampion, Seeds * 95 / 100, $"{settings.name} {depth}: livelli con un campione");
                Assert.That(withTwo, Is.InRange(Seeds * 20 / 100, Seeds * 60 / 100), $"{settings.name} {depth}: livelli con due gruppi");
            }
        }

        [Test, Description("Ogni campione ha la sua scorta: nemici nuovi dello stesso tipo, vicini a lui, da 2 a 4 (lo sciame da 4 a 6)")]
        public void Champions_HaveAnEscort()
        {
            int full = 0;
            int total = 0;
            foreach (var (settings, depth) in Cases())
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var layout = Generate(settings, seed, depth);
                    var before = Snapshot(layout);
                    foreach (var champion in Place(layout, depth, seed))
                    {
                        char symbol = layout[champion.X, champion.Y];
                        int escort = 0;
                        for (int y = champion.Y - Rules.EscortRadius; y <= champion.Y + Rules.EscortRadius; y++)
                        {
                            for (int x = champion.X - Rules.EscortRadius; x <= champion.X + Rules.EscortRadius; x++)
                            {
                                if (x >= 0 && y >= 0 && x < layout.Width && y < layout.Height && before[x, y] == LevelMap.Floor && layout[x, y] == symbol)
                                {
                                    escort++;
                                }
                            }
                        }

                        var range = symbol == CavePopulator.SwarmSymbol ? Rules.SwarmEscort : Rules.Escort;
                        Assert.LessOrEqual(escort, range.y, $"{settings.name} seme {seed}");
                        full += escort >= range.x ? 1 : 0;
                        total++;
                    }
                }
            }

            // in uno spazio stretto la scorta può essere più piccola, ma è raro
            Assert.GreaterOrEqual(full, total * 9 / 10, "scorte complete");
        }

        [Test, Description("Nessun nemico speciale vicino all'ingresso, nella stanza di partenza della cripta o nella sala del re con il giro attorno")]
        public void Specials_StayAwayFromEntranceAndKing()
        {
            foreach (var (settings, depth) in Cases())
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var layout = Generate(settings, seed, depth);
                    var before = Snapshot(layout);
                    var champions = Place(layout, depth, seed, withUnique: true);
                    var added = new List<Vector2Int>();
                    for (int y = 0; y < layout.Height; y++)
                    {
                        for (int x = 0; x < layout.Width; x++)
                        {
                            if (before[x, y] != layout[x, y])
                            {
                                added.Add(new Vector2Int(x, y));
                            }
                        }
                    }

                    foreach (var cell in added.Concat(champions.Select(c => new Vector2Int(c.X, c.Y))))
                    {
                        Assert.GreaterOrEqual(Chebyshev(cell, layout.Entrance), Rules.EntranceDistance, $"{settings.name} seme {seed}");
                        if (layout.StartRoom >= 0)
                        {
                            Assert.IsFalse(layout.Rooms[layout.StartRoom].Contains(cell), "stanza di partenza");
                        }

                        if (layout.HasBossRoom)
                        {
                            Assert.Greater(Chebyshev(cell, layout.BossCell), CaveGenerator.BossRoomHalf + 2, "sala del re");
                        }
                    }
                }
            }
        }

        [Test, Description("Lo stesso seme mette gli stessi campioni con gli stessi modificatori e la stessa scorta")]
        public void Place_IsDeterministic()
        {
            for (ulong seed = 1; seed <= 40; seed++)
            {
                var first = Generate(Caves, seed, 6);
                var second = Generate(Caves, seed, 6);
                var a = Place(first, 6, seed, withUnique: true).Select(c => c.ToToken()).ToList();
                var b = Place(second, 6, seed, withUnique: true).Select(c => c.ToToken()).ToList();
                CollectionAssert.AreEqual(a, b);
                CollectionAssert.AreEqual(Snapshot(first), Snapshot(second));
            }
        }

        [Test, Description("Con l'unico nel livello c'è un solo simbolo dell'unico, lontano dall'ingresso, con scheletri di scorta attorno")]
        public void Unique_IsPlacedWithEscort()
        {
            foreach (var (settings, depth) in Cases())
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var layout = Generate(settings, seed, depth);
                    var before = Snapshot(layout);
                    Place(layout, depth, seed, withUnique: true);
                    var uniques = new List<Vector2Int>();
                    for (int y = 0; y < layout.Height; y++)
                    {
                        for (int x = 0; x < layout.Width; x++)
                        {
                            if (layout[x, y] == SpecialEnemyPlanner.UniqueSymbol)
                            {
                                uniques.Add(new Vector2Int(x, y));
                            }
                        }
                    }

                    Assert.AreEqual(1, uniques.Count, $"{settings.name} seme {seed}");
                    var unique = uniques[0];
                    Assert.GreaterOrEqual(Chebyshev(unique, layout.Entrance), Rules.EntranceDistance + 2);
                    int escort = 0;
                    for (int y = unique.y - Rules.EscortRadius; y <= unique.y + Rules.EscortRadius; y++)
                    {
                        for (int x = unique.x - Rules.EscortRadius; x <= unique.x + Rules.EscortRadius; x++)
                        {
                            if (x >= 0 && y >= 0 && x < layout.Width && y < layout.Height && before[x, y] == LevelMap.Floor && layout[x, y] == Rules.UniqueEscort)
                            {
                                escort++;
                            }
                        }
                    }

                    Assert.Greater(escort, 0, $"{settings.name} seme {seed}: la scorta dell'unico");
                }
            }
        }

        [Test, Description("L'unico sta al più a una profondità per mondo, sempre la stessa per lo stesso seme; con una possibilità su tre a profondità, su tre profondità manca in circa tre mondi su dieci")]
        public void UniqueDepth_AtMostOncePerWorld()
        {
            int missing = 0;
            var counts = new int[5];
            for (ulong seed = 1; seed <= 600; seed++)
            {
                int depth = SpecialEnemyPlanner.UniqueDepth(seed, 2, 4, 1f / 3f);
                Assert.AreEqual(depth, SpecialEnemyPlanner.UniqueDepth(seed, 2, 4, 1f / 3f));
                Assert.IsTrue(depth == -1 || (depth >= 2 && depth <= 4));
                if (depth < 0)
                {
                    missing++;
                }
                else
                {
                    counts[depth]++;
                }
            }

            // (2/3)^3 = 0,30; le profondità prima sono più probabili delle dopo
            Assert.That(missing, Is.InRange(130, 230));
            Assert.Greater(counts[2], counts[3]);
            Assert.Greater(counts[3], counts[4]);
        }

        [Test, Description("Nelle caverne l'unico non sta mai alla profondità del re, e nella cripta mai alla prima")]
        public void UniqueDepth_FollowsTheSettings()
        {
            var caves = Caves;
            var crypt = Crypt;
            for (ulong seed = 1; seed <= 300; seed++)
            {
                int cave = caves.UniqueDepth(seed);
                Assert.AreNotEqual(caves.BossDepth, cave);
                Assert.IsTrue(cave == -1 || (cave >= caves.FirstDepth && cave <= caves.LastDepth));
                int cryptDepth = crypt.UniqueDepth(seed);
                Assert.IsTrue(cryptDepth == -1 || (cryptDepth >= crypt.Specials.MinDepth && cryptDepth <= crypt.LastDepth));
            }
        }

        [Test, Description("Un campione passa dalla direttiva della mappa e torna uguale; le parole sbagliate si saltano")]
        public void ChampionSpawn_RoundTripsThroughTheMap()
        {
            var spawn = new ChampionSpawn(12, 7, new[] { "fast", "frozen" });
            Assert.AreEqual("12,7,fast,frozen", spawn.ToToken());
            var map = LevelMap.Parse("@champions 12,7,fast,frozen 3,4 nonsense\n...\n");
            var spawns = ChampionSpawn.FromMap(map);
            Assert.AreEqual(2, spawns.Count);
            Assert.AreEqual(12, spawns[0].X);
            Assert.AreEqual(7, spawns[0].Y);
            CollectionAssert.AreEqual(new[] { "fast", "frozen" }, spawns[0].Modifiers);
            Assert.AreEqual(3, spawns[1].X);
            Assert.IsEmpty(spawns[1].Modifiers);
        }

        private static List<Vector2Int> Find(DungeonLayout layout, char symbol)
        {
            var cells = new List<Vector2Int>();
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (layout[x, y] == symbol)
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return cells;
        }

        private static bool Walkable(DungeonLayout layout, Vector2Int cell)
        {
            char c = layout[cell.x, cell.y];
            return c != LevelMap.Rock && c != DungeonGenerator.StairsSymbol && !DungeonPopulator.IsObstacle(c);
        }

        private static int Reachable(DungeonLayout layout)
        {
            var seen = new HashSet<Vector2Int> { layout.Entrance };
            var queue = new Queue<Vector2Int>(seen);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in LevelGrid.Steps)
                {
                    var next = cell + step;
                    if (Walkable(layout, next) && seen.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return seen.Count;
        }

        [Test, Description("Accanto all'unico c'è sempre un forziere, accanto ai campioni circa una volta su quattro (D13 della M13), qualcuna in più per il forziere dell'unico vicino; mai più forzieri che capi")]
        public void GoldChests_BesideUniqueAndHalfTheChampions()
        {
            int champions = 0, withChest = 0, uniques = 0, uniqueWithChest = 0;
            int radius = Rules.EscortRadius + 1 + SpecialEnemyPlanner.GoldChestReach;
            foreach (var (settings, depth) in Cases())
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var layout = Generate(settings, seed, depth);
                    var spawns = Place(layout, depth, seed, withUnique: true);
                    var chests = Find(layout, SpecialEnemyPlanner.GoldChestSymbol);
                    var unique = Find(layout, SpecialEnemyPlanner.UniqueSymbol);
                    Assert.LessOrEqual(chests.Count, spawns.Count + unique.Count, $"{settings.name} seme {seed}");
                    foreach (var cell in unique)
                    {
                        uniques++;
                        uniqueWithChest += chests.Any(c => Chebyshev(c, cell) <= radius) ? 1 : 0;
                    }

                    foreach (var spawn in spawns)
                    {
                        champions++;
                        withChest += chests.Any(c => Chebyshev(c, new Vector2Int(spawn.X, spawn.Y)) <= radius) ? 1 : 0;
                    }
                }
            }

            // 12 unici su 600 senza posto, in stanze strette, da quando una cassa tiene libero il davanti
            // (ronda notturna del 25 ott 2026): prima erano 11
            Assert.Greater(uniqueWithChest / (double)uniques, 0.975, "l'unico ha il suo forziere");
            Assert.That(withChest / (double)champions, Is.InRange(0.18, 0.42), "un campione su quattro");
        }

        [Test, Description("Un forziere ha il davanti libero, come le casse, quasi sempre contro un muro, e non chiude nessun passaggio")]
        public void GoldChests_AgainstAWallAndReachable()
        {
            int total = 0, againstWall = 0;
            foreach (var (settings, depth) in Cases())
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var layout = Generate(settings, seed, depth);
                    int before = Reachable(layout);
                    Place(layout, depth, seed, withUnique: true);
                    var chests = Find(layout, SpecialEnemyPlanner.GoldChestSymbol);
                    Assert.AreEqual(before - chests.Count, Reachable(layout), $"{settings.name} seme {seed}: si arriva ovunque come prima");
                    foreach (var cell in chests)
                    {
                        total++;
                        var front = SpecialEnemyPlanner.FrontOf(layout, cell, out bool wall);
                        againstWall += wall ? 1 : 0;
                        Assert.IsTrue(Walkable(layout, cell + front), $"{settings.name} seme {seed}: davanti al forziere ({cell.x}, {cell.y}) non si sta");
                    }
                }
            }

            Assert.Greater(againstWall / (double)total, 0.8, "di solito contro un muro");
        }

        [Test, Description("Il tesoro del re sta nella sala del boss, contro la parete di fronte alla porta; senza sala non c'è")]
        public void KingsTreasure_FacesTheGate()
        {
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = Generate(Caves, seed, Caves.BossDepth);
                if (!layout.HasBossRoom)
                {
                    Assert.IsFalse(SpecialEnemyPlanner.PlaceKingsTreasure(layout), $"seme {seed}: senza sala niente tesoro");
                    continue;
                }

                Assert.IsTrue(SpecialEnemyPlanner.PlaceKingsTreasure(layout), $"seme {seed}");
                var treasure = Find(layout, SpecialEnemyPlanner.TreasureSymbol);
                Assert.AreEqual(1, treasure.Count);
                var cell = treasure[0];
                Assert.AreEqual(CaveGenerator.BossRoomHalf, Chebyshev(cell, layout.BossCell), "sul bordo della sala");
                if (layout.GateCell.x >= 0)
                {
                    Assert.AreEqual(CaveGenerator.BossRoomHalf * 2 + 1, Chebyshev(cell, layout.GateCell), "dall'altra parte della porta");
                    var toGate = SpecialEnemyPlanner.TowardGate(layout);
                    Assert.AreEqual(layout.BossCell - toGate * CaveGenerator.BossRoomHalf, cell);
                }

                // dietro, fuori dalla sala, la roccia: il builder lo gira verso la sala e non chiude passaggi
                var offset = cell - layout.BossCell;
                var back = Math.Abs(offset.x) == CaveGenerator.BossRoomHalf ? new Vector2Int(Math.Sign(offset.x), 0) : new Vector2Int(0, Math.Sign(offset.y));
                Assert.IsFalse(layout.IsFloor(cell.x + back.x, cell.y + back.y), $"seme {seed}: dietro c'è la roccia");
            }

            for (ulong seed = 1; seed <= 20; seed++)
            {
                var crypt = Generate(Crypt, seed, 4);
                Assert.IsFalse(SpecialEnemyPlanner.PlaceKingsTreasure(crypt));
                Assert.IsEmpty(Find(crypt, SpecialEnemyPlanner.TreasureSymbol));
            }
        }
    }
}
