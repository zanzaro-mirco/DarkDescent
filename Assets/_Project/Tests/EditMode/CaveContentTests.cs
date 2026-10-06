using System;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class CaveContentTests
    {
        private const int Seeds = 200;

        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private static CaveSettings Caves => AssetDatabase.LoadAssetAtPath<CaveSettings>("Assets/_Project/Data/Levels/CaveSettings.asset");

        private static DungeonSettings Crypt => AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");

        private static IEnumerable<(ulong seed, int depth, DungeonLayout layout)> Levels()
        {
            var settings = Caves;
            var generator = settings.CreateGenerator();
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                for (int depth = settings.FirstDepth; depth <= settings.LastDepth; depth++)
                {
                    yield return (seed, depth, generator.Generate(seed * 31 + (ulong)depth, depth));
                }
            }
        }

        private static List<Vector2Int> Find(DungeonLayout layout, Func<char, bool> wanted)
        {
            var cells = new List<Vector2Int>();
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (wanted(layout[x, y]))
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return cells;
        }

        private static bool Walkable(DungeonLayout layout, Vector2Int c)
        {
            return layout.IsFloor(c.x, c.y) && layout[c.x, c.y] != DungeonGenerator.StairsSymbol && !DungeonPopulator.IsObstacle(layout[c.x, c.y]);
        }

        // passi a piedi dall'ingresso, girando attorno a casse e scenografia
        private static Dictionary<Vector2Int, int> Walk(DungeonLayout layout)
        {
            var distance = new Dictionary<Vector2Int, int> { [layout.Entrance] = 0 };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(layout.Entrance);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps)
                {
                    var next = cell + step;
                    if (Walkable(layout, next) && !distance.ContainsKey(next))
                    {
                        distance[next] = distance[cell] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            return distance;
        }

        [Test, Description("Le profondità si concatenano: la cripta dall'1 al 4, la scala del 4 porta alle caverne al 5, l'8 non ha la scala")]
        public void Depths_ChainFromTheCryptToTheCaves()
        {
            var crypt = Crypt;
            var caves = Caves;
            Assert.AreEqual((1, 4, "Level_Crypt", "Level_Caves"), (crypt.FirstDepth, crypt.LastDepth, crypt.SceneName, crypt.NextScene));
            Assert.AreEqual((5, 8, "Level_Caves", ""), (caves.FirstDepth, caves.LastDepth, caves.SceneName, caves.NextScene));

            CollectionAssert.AreEqual(new[] { "Level_Crypt", "FromAbove", "4" }, DungeonLevel.CreateMap(crypt, 4711, 3, out _).GetDirective("exit"));
            CollectionAssert.AreEqual(new[] { "Level_Caves", "FromAbove", "5" }, DungeonLevel.CreateMap(crypt, 4711, 4, out _).GetDirective("exit"));
            CollectionAssert.AreEqual(new[] { "Level_Caves", "FromAbove", "6" }, DungeonLevel.CreateMap(caves, 4711, 5, out _).GetDirective("exit"));

            var last = DungeonLevel.CreateMap(caves, 4711, 8, out var layout);
            Assert.AreEqual(0, last.GetDirective("exit").Count);
            Assert.AreNotEqual(DungeonGenerator.StairsSymbol, last.GetSymbol(layout.Stairs.x, layout.Stairs.y), "all'ottavo la scala non c'è");
            CollectionAssert.AreEqual(new[] { "FromAbove" }, last.GetDirective("entrance"));
        }

        [Test, Description("Su 200 semi per profondità: quanti scheletri e casse dicono i numeri, e nessuno vicino all'ingresso")]
        public void EnemiesAndChests_AreAllThereAndAwayFromTheEntrance()
        {
            var settings = Caves;
            foreach (var (seed, depth, layout) in Levels())
            {
                var enemies = Find(layout, c => c == DungeonPopulator.EnemySymbol);
                var chests = Find(layout, c => c == DungeonPopulator.ChestSymbol);
                Assert.AreEqual(settings.EnemyCount(depth), enemies.Count, $"seme {seed}, profondità {depth}: scheletri");
                Assert.AreEqual(settings.ChestCount(depth), chests.Count, $"seme {seed}, profondità {depth}: casse");

                // senza ostacoli: i passi della caverna com'è, non i giri attorno alle casse
                var bare = LevelGrid.Distances(Cells(layout, plain: true), layout.Entrance);
                foreach (var cell in enemies.Concat(chests))
                {
                    Assert.GreaterOrEqual(bare[cell.x, cell.y], settings.QuietSteps, $"seme {seed}, profondità {depth}: {layout[cell.x, cell.y]} in {cell} troppo vicino all'ingresso");
                }
            }
        }

        [Test, Description("Su 200 semi per profondità, con casse e scenografia ogni pavimento si raggiunge a piedi, e nessun ostacolo chiude un cunicolo largo una cella")]
        public void Obstacles_NeverCutTheCave()
        {
            foreach (var (seed, depth, layout) in Levels())
            {
                var reached = Walk(layout);
                foreach (var cell in Find(layout, c => c != LevelMap.Rock))
                {
                    if (Walkable(layout, cell))
                    {
                        Assert.IsTrue(reached.ContainsKey(cell), $"seme {seed}, profondità {depth}: {cell} irraggiungibile");
                    }
                }

                Assert.IsTrue(reached.ContainsKey(layout.Stairs + Vector2Int.up) || depth == Caves.LastDepth, $"seme {seed}, profondità {depth}: davanti alla scala non si arriva");
                foreach (var cell in Find(layout, DungeonPopulator.IsObstacle))
                {
                    bool northSouth = !layout.IsFloor(cell.x, cell.y - 1) && !layout.IsFloor(cell.x, cell.y + 1);
                    bool eastWest = !layout.IsFloor(cell.x - 1, cell.y) && !layout.IsFloor(cell.x + 1, cell.y);
                    Assert.IsFalse(northSouth || eastWest, $"seme {seed}, profondità {depth}: ostacolo in un cunicolo in {cell}");
                }
            }
        }

        [Test, Description("Su 200 semi per profondità le candele ci sono, stanno contro la roccia e distanti tra loro")]
        public void Candles_AreSpreadAgainstTheRock()
        {
            var settings = Caves;
            int least = int.MaxValue;
            foreach (var (seed, depth, layout) in Levels())
            {
                var candles = Find(layout, c => c == CavePopulator.CandleSymbol);
                least = Math.Min(least, candles.Count);
                Assert.Greater(candles.Count, 3, $"seme {seed}, profondità {depth}: poche candele");
                for (int i = 0; i < candles.Count; i++)
                {
                    var c = candles[i];
                    Assert.IsTrue(Steps.Any(s => !layout.IsFloor(c.x + s.x, c.y + s.y)), $"seme {seed}: candela lontana dalla roccia in {c}");
                    for (int j = i + 1; j < candles.Count; j++)
                    {
                        int apart = Math.Max(Math.Abs(c.x - candles[j].x), Math.Abs(c.y - candles[j].y));
                        Assert.GreaterOrEqual(apart, settings.CandleSpacing, $"seme {seed}: candele troppo vicine in {c} e {candles[j]}");
                    }
                }
            }

            Debug.Log($"candele minime in una caverna: {least}");
        }

        [Test, Description("I pezzi del tileset delle caverne hanno la loro misura vera: pavimenti di 4 × 4 m, muri lunghi 4 m, muri bassi sotto 1,2 m, candela e scenografia visibili")]
        public void TilesetPieces_HaveTheirRealSize()
        {
            // alcuni FBX del pacchetto hanno la radice ruotata di −90° e scalata 100 volte: forzarla a zero e
            // a 1 fa un pavimento di 4 cm in piedi, che non si vede (com'è andata del 7.2)
            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>("Assets/_Project/Data/Levels/CaveTileset.asset");
            // i bounds valgono per un'istanza nella scena, non per l'asset
            Bounds Size(GameObject prefab)
            {
                var instance = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    var renderers = instance.GetComponentsInChildren<Renderer>();
                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers)
                    {
                        bounds.Encapsulate(r.bounds);
                    }

                    return bounds;
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }

            foreach (var floor in new[] { tileset.Floor, tileset.FloorVariant })
            {
                var size = Size(floor).size;
                Assert.AreEqual(4f, size.x, 0.05f, floor.name);
                Assert.AreEqual(4f, size.z, 0.05f, floor.name);
                Assert.Less(size.y, 0.5f, floor.name);
            }

            foreach (var wall in new[] { tileset.Wall, tileset.WallVariant })
            {
                Assert.AreEqual(4f, Size(wall).size.x, 0.05f, wall.name);
                Assert.AreEqual(4f, Size(wall).size.y, 0.05f, wall.name);
            }

            var low = Size(tileset.LowWall);
            Assert.AreEqual(4f, low.size.x, 0.1f, "il muro basso copre il lato della cella");
            Assert.Less(low.max.y, 1.2f, "il muro basso non copre il cavaliere");
            Assert.AreEqual(0f, low.center.x, 0.1f, "centrato sul lato");

            foreach (char symbol in "lbxpc")
            {
                var size = Size(tileset.GetMarkerPrefab(symbol)).size;
                Assert.Greater(size.y, 0.5f, $"'{symbol}': {tileset.GetMarkerPrefab(symbol).name} troppo piccolo");
                Assert.Less(Mathf.Max(size.x, size.z), 4f, $"'{symbol}': {tileset.GetMarkerPrefab(symbol).name} esce dalla cella");
            }
        }

        [Test, Description("Lo stesso seme dà lo stesso contenuto; e il contenuto non cambia la forma della caverna")]
        public void SameSeed_SameContent()
        {
            var generator = Caves.CreateGenerator();
            string first = MapChecks.Text(generator.Generate(4711, 6).ToMap(new Dictionary<string, string[]>()));
            Assert.AreEqual(first, MapChecks.Text(generator.Generate(4711, 6).ToMap(new Dictionary<string, string[]>())));

            // le celle di roccia sono le stesse con e senza contenuto: il popolatore lavora dopo la forma
            var layout = generator.Generate(4711, 6);
            var bare = Cells(layout, plain: true);
            var shape = ScriptableObject.CreateInstance<CaveSettings>();
            try
            {
                var plain = shape.CreateGenerator().Generate(4711, 6);
                for (int y = 0; y < layout.Height; y++)
                {
                    for (int x = 0; x < layout.Width; x++)
                    {
                        Assert.AreEqual(plain.IsFloor(x, y), bare[x, y] != LevelMap.Rock, $"({x}, {y})");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shape);
            }
        }

        // le celle della caverna; con plain i marcatori diventano pavimento, la scala resta
        private static char[,] Cells(DungeonLayout layout, bool plain)
        {
            var cells = new char[layout.Width, layout.Height];
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    char c = layout[x, y];
                    cells[x, y] = plain && c != LevelMap.Rock && c != DungeonGenerator.StairsSymbol ? LevelMap.Floor : c;
                }
            }

            return cells;
        }
    }
}
