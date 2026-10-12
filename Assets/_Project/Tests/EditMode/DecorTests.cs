using System.Collections.Generic;
using System.Linq;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Gli arredi della cripta (D4 della M11).</summary>
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

        [Test, Description("Ogni stanza ha un tema solo; le composizioni stanno sul bordo della stanza, contro un muro, e sono parecchie: almeno una ogni due stanze")]
        public void Decor_FollowsTheRoomTheme()
        {
            var generator = new DungeonGenerator(_settings);
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = generator.Generate(seed, 2);
                var decor = Cells(layout, DungeonPopulator.IsDecor).ToList();
                Assert.GreaterOrEqual(decor.Count, layout.Rooms.Count / 2, $"seme {seed}");
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

        [Test, Description("Ogni composizione ha il suo prefab nel tileset della cripta: sta nella cella, a terra, con i collider sul layer degli ostacoli, e i modelli non hanno materiali importati")]
        public void Compositions_AreInTheTileset()
        {
            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>("Assets/_Project/Data/Levels/DungeonTileset.asset");
            foreach (char symbol in string.Concat(DungeonPopulator.DecorThemes))
            {
                var prefab = tileset.GetMarkerPrefab(symbol);
                Assert.IsNotNull(prefab, $"'{symbol}' senza prefab");
                var colliders = prefab.GetComponentsInChildren<Collider>();
                Assert.Greater(colliders.Length, 0, prefab.name);
                Assert.IsTrue(colliders.All(c => c.gameObject.layer == ObstacleLayer), prefab.name + ": collider fuori dal layer degli ostacoli");
                foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
                {
                    Assert.IsTrue(renderer.sharedMaterials.All(m => m != null && m.name == "M_Dungeon"), $"{prefab.name}: {renderer.name}");
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
