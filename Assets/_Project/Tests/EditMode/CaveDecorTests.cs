using System.Collections.Generic;
using System.Linq;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Gli arredi delle caverne (D4 della M11).</summary>
    public class CaveDecorTests
    {
        private const int Seeds = 100;
        private const int ObstacleLayer = 7;
        private const string SettingsPath = "Assets/_Project/Data/Levels/CaveSettings.asset";

        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private CaveSettings _settings;
        private CaveSettings _bare;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<CaveSettings>(SettingsPath);
            _bare = Object.Instantiate(_settings);
            var so = new SerializedObject(_bare);
            so.FindProperty("_floorPerDecor").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_bare);
        }

        private static List<Vector2Int> Decor(DungeonLayout layout)
        {
            var cells = new List<Vector2Int>();
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (CavePopulator.IsDecor(layout[x, y]))
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return cells;
        }

        [Test, Description("Gli arredi hanno un flusso loro del seme: tolti, la caverna è uguale cella per cella a quella senza arredi, con gli stessi nemici, casse, candele e boss")]
        public void Decor_DoesNotMoveAnythingElse()
        {
            var decorated = _settings.CreateGenerator();
            var bare = _bare.CreateGenerator();
            for (int depth = 5; depth <= 8; depth++)
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var with = decorated.Generate(seed, depth);
                    var without = bare.Generate(seed, depth);
                    Assert.AreEqual(0, Decor(without).Count);
                    for (int y = 0; y < with.Height; y++)
                    {
                        for (int x = 0; x < with.Width; x++)
                        {
                            char c = with[x, y];
                            // i gruppi di barili contro la roccia (D5 della M13) e gli spuntoni (D8) arrivano con gli arredi
                            bool decor = CavePopulator.IsDecor(c) || DungeonPopulator.BarrelWallSymbols.IndexOf(c) >= 0 || c == DungeonPopulator.SpikeSymbol;
                            Assert.AreEqual(decor ? LevelMap.Floor : c, without[x, y], $"seme {seed}, profondità {depth}, cella ({x}, {y})");
                        }
                    }
                }
            }
        }

        [Test, Description("Le composizioni sono parecchie e sparse, contro la roccia, mai in un cunicolo né accanto a casse, scala o porta del boss; ogni zona ha un tema solo, e dall'ingresso si arriva ancora dappertutto")]
        public void Decor_FollowsTheRules()
        {
            var generator = _settings.CreateGenerator();
            for (int depth = 5; depth <= 8; depth++)
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var layout = generator.Generate(seed, depth);
                    var decor = Decor(layout);
                    Assert.GreaterOrEqual(decor.Count, 6, $"seme {seed}, profondità {depth}");
                    var zones = new Dictionary<Vector2Int, string>();
                    foreach (var cell in decor)
                    {
                        string where = $"seme {seed}, profondità {depth}, cella {cell}";
                        Assert.IsTrue(Steps.Any(s => !layout.IsFloor(cell.x + s.x, cell.y + s.y)), where + ": lontano dalla roccia");
                        bool northSouth = !layout.IsFloor(cell.x, cell.y - 1) && !layout.IsFloor(cell.x, cell.y + 1);
                        bool eastWest = !layout.IsFloor(cell.x - 1, cell.y) && !layout.IsFloor(cell.x + 1, cell.y);
                        Assert.IsFalse(northSouth || eastWest, where + ": in un cunicolo");
                        foreach (var step in Steps)
                        {
                            char next = layout[cell.x + step.x, cell.y + step.y];
                            Assert.AreNotEqual(DungeonPopulator.ChestSymbol, next, where + ": accanto a una cassa");
                            Assert.AreNotEqual(DungeonGenerator.StairsSymbol, next, where + ": accanto alla scala");
                            Assert.AreNotEqual(CavePopulator.GateSymbol, next, where + ": accanto alla porta del boss");
                        }

                        Assert.IsTrue(decor.Where(o => o != cell).All(o => Mathf.Max(Mathf.Abs(o.x - cell.x), Mathf.Abs(o.y - cell.y)) >= _settings.DecorSpacing), where + ": troppo vicine");
                        if (layout.HasBossRoom)
                        {
                            int fromBoss = Mathf.Max(Mathf.Abs(layout.BossCell.x - cell.x), Mathf.Abs(layout.BossCell.y - cell.y));
                            Assert.Greater(fromBoss, CaveGenerator.BossRoomHalf + 2, where + ": nella sala del boss");
                        }

                        var zone = new Vector2Int(cell.x / CavePopulator.DecorZone, cell.y / CavePopulator.DecorZone);
                        string theme = CavePopulator.DecorThemes.Single(t => t.IndexOf(layout[cell.x, cell.y]) >= 0);
                        if (zones.TryGetValue(zone, out var other))
                        {
                            Assert.AreEqual(other, theme, where + ": una zona con più temi");
                        }

                        zones[zone] = theme;
                        Assert.IsTrue(DungeonPopulator.IsObstacle(layout[cell.x, cell.y]), "gli arredi sono ostacoli");
                    }

                    AssertAllReachable(layout, $"seme {seed}, profondità {depth}");
                }
            }
        }

        // ogni cella dove si cammina si raggiunge dall'ingresso, e ogni cassa ha accanto una di quelle
        private static void AssertAllReachable(DungeonLayout layout, string where)
        {
            bool Walkable(int x, int y)
            {
                char c = layout[x, y];
                return c != LevelMap.Rock && c != DungeonGenerator.StairsSymbol && !DungeonPopulator.IsObstacle(c);
            }

            var seen = new bool[layout.Width, layout.Height];
            var queue = new Queue<Vector2Int>();
            seen[layout.Entrance.x, layout.Entrance.y] = true;
            queue.Enqueue(layout.Entrance);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps)
                {
                    var next = cell + step;
                    if (Walkable(next.x, next.y) && !seen[next.x, next.y])
                    {
                        seen[next.x, next.y] = true;
                        queue.Enqueue(next);
                    }
                }
            }

            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (Walkable(x, y))
                    {
                        Assert.IsTrue(seen[x, y], $"{where}: ({x}, {y}) irraggiungibile");
                    }

                    if (layout[x, y] == DungeonPopulator.ChestSymbol)
                    {
                        Assert.IsTrue(Steps.Any(s => seen[x + s.x, y + s.y]), $"{where}: cassa in ({x}, {y}) chiusa");
                    }
                }
            }
        }

        [Test, Description("Ogni composizione ha il suo prefab nel tileset delle caverne: sta nella cella, a terra, con almeno un collider sul layer degli ostacoli, e con i materiali delle caverne")]
        public void Compositions_AreInTheTileset()
        {
            var allowed = new[] { "M_Dungeon", "M_CaveCrystal", "M_Bone" };
            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>("Assets/_Project/Data/Levels/CaveTileset.asset");
            foreach (char symbol in string.Concat(CavePopulator.DecorThemes))
            {
                var prefab = tileset.GetMarkerPrefab(symbol);
                Assert.IsNotNull(prefab, $"'{symbol}' senza prefab");
                var colliders = prefab.GetComponentsInChildren<Collider>();
                Assert.Greater(colliders.Length, 0, prefab.name);
                Assert.IsTrue(colliders.All(c => c.gameObject.layer == ObstacleLayer), prefab.name + ": collider fuori dal layer degli ostacoli");
                var instance = Object.Instantiate(prefab);
                try
                {
                    foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                    {
                        Assert.IsTrue(renderer.sharedMaterials.All(m => m != null && allowed.Contains(m.name)), $"{prefab.name}: {renderer.name}");
                        var bounds = renderer.bounds;
                        foreach (var corner in new[] { bounds.min, bounds.max })
                        {
                            Assert.That(corner.x, Is.InRange(-LevelMap.CellSize / 2f, LevelMap.CellSize / 2f), $"{prefab.name}: {renderer.name} esce dalla cella");
                            Assert.That(corner.z, Is.InRange(-LevelMap.CellSize / 2f, LevelMap.CellSize / 2f), $"{prefab.name}: {renderer.name} esce dalla cella");
                        }

                        Assert.GreaterOrEqual(bounds.min.y, -0.05f, $"{prefab.name}: {renderer.name} sotto il pavimento");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        [Test, Description("I simboli degli arredi delle caverne non sono già usati da altro")]
        public void Symbols_AreFree()
        {
            string taken = "#.<>TScmbxplwBKGi" + "DEFLNORUVWYZ";
            foreach (char symbol in string.Concat(CavePopulator.DecorThemes))
            {
                Assert.AreEqual(-1, taken.IndexOf(symbol), $"'{symbol}'");
                Assert.IsFalse(DungeonPopulator.IsDecor(symbol), $"'{symbol}' è anche della cripta");
            }
        }
    }
}
