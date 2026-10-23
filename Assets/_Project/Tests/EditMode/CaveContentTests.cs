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

        [Test, Description("Su 200 semi per profondità: scheletri, sciame, bruti e maghi negli intervalli della tabella, le casse dei numeri, e nessuno vicino all'ingresso")]
        public void EnemiesAndChests_AreAllThereAndAwayFromTheEntrance()
        {
            var settings = Caves;
            foreach (var (seed, depth, layout) in Levels())
            {
                MapChecks.Pass($"seme {seed}, profondità {depth}", CaveChecks.Population(layout, settings, depth));
            }
        }

        [Test, Description("Su 200 semi per profondità, con casse e scenografia ogni pavimento si raggiunge a piedi, e nessun ostacolo chiude un cunicolo largo una cella")]
        public void Obstacles_NeverCutTheCave()
        {
            int last = Caves.LastDepth;
            foreach (var (seed, depth, layout) in Levels())
            {
                string where = $"seme {seed}, profondità {depth}";
                MapChecks.Pass(where, MapChecks.ObstaclesLeavePath(layout, stairs: depth < last));
                MapChecks.Pass(where, CaveChecks.Corridors(layout));
            }
        }

        [Test, Description("In media su 200 semi i numeri di D10: al 5 circa cinque scheletri, uno sciame e un bruto; all'8, con il boss, circa quattro scheletri, due sciami e due bruti")]
        public void Averages_MatchD10()
        {
            var settings = Caves;
            var sums = new Dictionary<int, (float skeletons, float swarm, float brutes)>();
            foreach (var (_, depth, layout) in Levels())
            {
                var enemies = MapChecks.Find(layout, CavePopulator.IsEnemy).Select(c => layout[c.x, c.y]).ToList();
                sums.TryGetValue(depth, out var sum);
                sums[depth] = (sum.skeletons + enemies.Count(c => c == DungeonPopulator.EnemySymbol),
                    sum.swarm + enemies.Count(c => c == CavePopulator.SwarmSymbol),
                    sum.brutes + enemies.Count(c => c == CavePopulator.BruteSymbol));
            }

            var at5 = (sums[5].skeletons / Seeds, sums[5].swarm / Seeds, sums[5].brutes / Seeds);
            var at8 = (sums[8].skeletons / Seeds, sums[8].swarm / Seeds, sums[8].brutes / Seeds);
            Debug.Log($"in media al 5: {at5}; all'8: {at8}");
            Assert.AreEqual(5f, at5.Item1, 0.5f, "scheletri al 5");
            Assert.AreEqual(4f, at5.Item2, 0.5f, "un gruppo di sciame al 5, da 3 a 5 (prova della M7)");
            Assert.AreEqual(1f, at5.Item3, 0.001f, "un bruto al 5");
            Assert.AreEqual(4f, at8.Item1, 0.5f, "scheletri all'8");
            Assert.AreEqual(8f, at8.Item2, 1f, "due gruppi di sciame all'8, dove c'è anche il boss");
            Assert.AreEqual(2f, at8.Item3, 0.001f, "due bruti all'8");
        }

        [Test, Description("Una riga della tabella vale dalla sua profondità in giù; prima della prima vale la prima")]
        public void SpawnTable_RowForDepth()
        {
            var table = SpawnTable.Caves();
            Assert.AreEqual(5, table.For(3).Depth);
            Assert.AreEqual(5, table.For(5).Depth);
            Assert.AreEqual(6, table.For(6).Depth);
            Assert.AreEqual(8, table.For(8).Depth);
            Assert.AreEqual(8, table.For(12).Depth);
            Assert.Throws<InvalidOperationException>(() => new SpawnTable().For(5));
        }

        [Test, Description("Su 200 semi per profondità le candele ci sono, stanno contro la roccia e distanti tra loro")]
        public void Candles_AreSpreadAgainstTheRock()
        {
            var settings = Caves;
            int least = int.MaxValue;
            foreach (var (seed, depth, layout) in Levels())
            {
                least = Math.Min(least, MapChecks.Count(layout, CavePopulator.CandleSymbol));
                MapChecks.Pass($"seme {seed}, profondità {depth}", CaveChecks.Candles(layout, settings));
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
