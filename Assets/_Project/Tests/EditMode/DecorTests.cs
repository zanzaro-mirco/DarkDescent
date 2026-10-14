using System.Collections.Generic;
using System.Linq;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Gli arredi della cripta (D4 della M11; colonnati e mucchietti dal passo 11.7).</summary>
    public class DecorTests
    {
        private const int Seeds = 100;
        private const int ObstacleLayer = 7;

        private DungeonSettings _settings;
        private DungeonSettings _bare;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<DungeonSettings>();
            _bare = ScriptableObject.CreateInstance<DungeonSettings>();
            var so = new SerializedObject(_bare);
            so.FindProperty("_maxDecorPerRoom").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_bare);
        }

        private static IEnumerable<Vector2Int> Cells(DungeonLayout layout, System.Func<char, bool> wanted)
        {
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (wanted(layout[x, y]))
                    {
                        yield return new Vector2Int(x, y);
                    }
                }
            }
        }

        [Test, Description("Gli arredi hanno un flusso loro del seme: tolti, la cripta è uguale cella per cella a quella senza arredi, con gli stessi nemici, casse e oggetti di scena")]
        public void Decor_DoesNotMoveAnythingElse()
        {
            var decorated = new DungeonGenerator(_settings);
            var bare = new DungeonGenerator(_bare);
            for (int depth = 1; depth <= 4; depth++)
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var with = decorated.Generate(seed, depth);
                    var without = bare.Generate(seed, depth);
                    Assert.AreEqual(0, Cells(without, DungeonPopulator.IsDecor).Count());
                    for (int y = 0; y < with.Height; y++)
                    {
                        for (int x = 0; x < with.Width; x++)
                        {
                            char c = with[x, y];
                            Assert.AreEqual(DungeonPopulator.IsDecor(c) ? LevelMap.Floor : c, without[x, y], $"seme {seed}, profondità {depth}, cella ({x}, {y})");
                        }
                    }
                }
            }
        }

        private static bool IsComposition(char c)
        {
            return DungeonPopulator.IsDecor(c) && c != DungeonPopulator.ColumnSymbol && !DungeonPopulator.IsScatter(c);
        }

        [Test, Description("Ogni stanza ha un tema solo; le composizioni stanno sul bordo della stanza, contro un muro, e sono tante: almeno una a stanza")]
        public void Decor_FollowsTheRoomTheme()
        {
            var generator = new DungeonGenerator(_settings);
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = generator.Generate(seed, 2);
                var decor = Cells(layout, IsComposition).ToList();
                Assert.GreaterOrEqual(decor.Count, layout.Rooms.Count, $"seme {seed}");
                foreach (var room in layout.Rooms)
                {
                    var inside = decor.Where(room.Contains).ToList();
                    var themes = inside.Select(c => DungeonPopulator.DecorThemes.Single(t => t.IndexOf(layout[c.x, c.y]) >= 0)).Distinct().ToList();
                    Assert.LessOrEqual(themes.Count, 1, $"seme {seed}: una stanza con più temi");
                    foreach (var cell in inside)
                    {
                        bool border = cell.x == room.xMin || cell.y == room.yMin || cell.x == room.xMax - 1 || cell.y == room.yMax - 1;
                        Assert.IsTrue(border, $"seme {seed}: composizione in mezzo alla stanza");
                    }
                }

                Assert.IsTrue(decor.All(c => DungeonPopulator.IsObstacle(layout[c.x, c.y])), "gli arredi sono ostacoli");
            }
        }

        [Test, Description("Le stanze grandi hanno spesso un colonnato in mezzo: due file a una cella dai muri, mai accanto a un passaggio, a una cassa o alla scala, e simmetrico anche quando manca qualche colonna")]
        public void Columns_StandInTheMiddle()
        {
            var generator = new DungeonGenerator(_settings);
            int bigRooms = 0;
            int withColumns = 0;
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = generator.Generate(seed, 3);
                var columns = Cells(layout, c => c == DungeonPopulator.ColumnSymbol).ToList();
                foreach (var room in layout.Rooms)
                {
                    var inside = columns.Where(room.Contains).ToList();
                    bool big = room.width >= DungeonPopulator.MinColumnRoom && room.height >= DungeonPopulator.MinColumnRoom;
                    if (!big)
                    {
                        Assert.AreEqual(0, inside.Count, $"seme {seed}: colonne in una stanza piccola");
                        continue;
                    }

                    bigRooms++;
                    if (inside.Count == 0)
                    {
                        continue;
                    }

                    withColumns++;
                    var planned = DungeonPopulator.ColumnCells(room);
                    Assert.GreaterOrEqual(inside.Count, 2, $"seme {seed}: una colonna sola");
                    CollectionAssert.IsSubsetOf(inside, planned, $"seme {seed}: colonna fuori dalle file");
                    foreach (var cell in inside)
                    {
                        CollectionAssert.IsSubsetOf(DungeonPopulator.Mirrors(room, cell).Distinct().ToList(), inside, $"seme {seed}: colonnato non simmetrico");
                    }

                    foreach (var cell in inside)
                    {
                        Assert.IsFalse(cell.x == room.xMin || cell.y == room.yMin || cell.x == room.xMax - 1 || cell.y == room.yMax - 1, "una colonna sul bordo");
                        foreach (var step in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                        {
                            char next = layout[cell.x + step.x, cell.y + step.y];
                            Assert.AreNotEqual(DungeonPopulator.ChestSymbol, next, $"seme {seed}: colonna davanti a una cassa");
                            Assert.AreNotEqual(DungeonGenerator.StairsSymbol, next, $"seme {seed}: colonna accanto alla scala");
                        }
                    }
                }
            }

            Assert.Greater(bigRooms, 20, "stanze grandi da provare");
            Assert.Greater(withColumns, bigRooms / 4, "i colonnati sono frequenti");
        }

        [Test, Description("Qualche mucchietto a terra, di preferenza in mezzo alla stanza: si attraversa, quindi non è un ostacolo")]
        public void Scatter_LiesInTheMiddle()
        {
            var generator = new DungeonGenerator(_settings);
            int total = 0;
            int inner = 0;
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = generator.Generate(seed, 2);
                foreach (var cell in Cells(layout, DungeonPopulator.IsScatter))
                {
                    total++;
                    Assert.IsFalse(DungeonPopulator.IsObstacle(layout[cell.x, cell.y]));
                    var room = layout.Rooms.First(r => r.Contains(cell));
                    if (cell.x > room.xMin && cell.y > room.yMin && cell.x < room.xMax - 1 && cell.y < room.yMax - 1)
                    {
                        inner++;
                    }
                }
            }

            Assert.Greater(total, Seeds, "almeno uno a livello");
            Assert.Greater(inner, total * 3 / 4, "quasi tutti lontani dai muri");
        }

        [Test, Description("Ogni arredo ha il suo prefab nel tileset della cripta: sta nella cella, a terra, con il materiale del dungeon (le ossa il loro); composizioni e colonne hanno i collider sul layer degli ostacoli, i mucchietti nessun collider")]
        public void Compositions_AreInTheTileset()
        {
            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>("Assets/_Project/Data/Levels/DungeonTileset.asset");
            foreach (char symbol in string.Concat(DungeonPopulator.DecorThemes) + DungeonPopulator.ColumnSymbol + DungeonPopulator.ScatterSymbols)
            {
                var prefab = tileset.GetMarkerPrefab(symbol);
                Assert.IsNotNull(prefab, $"'{symbol}' senza prefab");
                var colliders = prefab.GetComponentsInChildren<Collider>();
                if (DungeonPopulator.IsScatter(symbol))
                {
                    Assert.AreEqual(0, colliders.Length, prefab.name + ": un mucchietto si attraversa");
                }
                else
                {
                    Assert.Greater(colliders.Length, 0, prefab.name);
                }

                Assert.IsTrue(colliders.All(c => c.gameObject.layer == ObstacleLayer), prefab.name + ": collider fuori dal layer degli ostacoli");
                foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
                {
                    Assert.IsTrue(renderer.sharedMaterials.All(m => m != null && (m.name == "M_Dungeon" || m.name == "M_Bone")), $"{prefab.name}: {renderer.name}");
                    var mesh = renderer.GetComponent<MeshFilter>().sharedMesh.bounds;
                    Vector3 min = prefab.transform.InverseTransformPoint(renderer.transform.TransformPoint(mesh.min));
                    Vector3 max = prefab.transform.InverseTransformPoint(renderer.transform.TransformPoint(mesh.max));
                    foreach (var corner in new[] { min, max })
                    {
                        Assert.That(corner.x, Is.InRange(-LevelMap.CellSize / 2f, LevelMap.CellSize / 2f), $"{prefab.name}: {renderer.name} esce dalla cella");
                        Assert.That(corner.z, Is.InRange(-LevelMap.CellSize / 2f, LevelMap.CellSize / 2f), $"{prefab.name}: {renderer.name} esce dalla cella");
                        Assert.GreaterOrEqual(corner.y, -0.05f, $"{prefab.name}: {renderer.name} sotto il pavimento");
                    }
                }
            }
        }
    }
}
