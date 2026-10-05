using System.Collections.Generic;
using System.Linq;
using System.Text;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class DungeonContentTests
    {
        private const int Seeds = 200;

        private DungeonSettings _settings;
        private DungeonGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<DungeonSettings>();
            _generator = new DungeonGenerator(_settings);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
        }

        // ogni cripta di prova: profondità da 1 a 4, 200 semi ciascuna
        private IEnumerable<(ulong seed, int depth, DungeonLayout layout)> Dungeons()
        {
            for (int depth = 1; depth <= 4; depth++)
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    yield return (seed, depth, _generator.Generate(seed, depth));
                }
            }
        }

        private static int Count(DungeonLayout layout, char symbol)
        {
            int count = 0;
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (layout[x, y] == symbol)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static string Text(DungeonLayout layout)
        {
            var sb = new StringBuilder();
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    sb.Append(layout[x, y]);
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }

        [Test, Description("Scheletri 3 + 2 × profondità e casse 1 + profondità / 2 (D6), mai uno scheletro nella stanza d'ingresso")]
        public void EnemiesAndChests_GrowWithDepth()
        {
            foreach (var (seed, depth, layout) in Dungeons())
            {
                Assert.AreEqual(_settings.EnemyCount(depth), Count(layout, DungeonPopulator.EnemySymbol), $"seme {seed}, profondità {depth}");
                Assert.AreEqual(_settings.ChestCount(depth), Count(layout, DungeonPopulator.ChestSymbol), $"seme {seed}, profondità {depth}");

                RectInt start = layout.Rooms[layout.StartRoom];
                for (int y = start.yMin; y < start.yMax; y++)
                {
                    for (int x = start.xMin; x < start.xMax; x++)
                    {
                        Assert.AreNotEqual(DungeonPopulator.EnemySymbol, layout[x, y], $"seme {seed}: scheletro nella stanza d'ingresso");
                    }
                }
            }

            Assert.AreEqual(new[] { 5, 7, 9, 11 }, Enumerable.Range(1, 4).Select(_settings.EnemyCount).ToArray());
            Assert.AreEqual(new[] { 1, 2, 2, 3 }, Enumerable.Range(1, 4).Select(_settings.ChestCount).ToArray());
        }

        [Test, Description("Casse e oggetti di scena non chiudono nessun passaggio: dall'ingresso si arriva a ogni cella libera e davanti alla scala")]
        public void Obstacles_NeverBlockThePath()
        {
            foreach (var (seed, depth, layout) in Dungeons())
            {
                bool Walkable(Vector2Int c) => layout.IsFloor(c.x, c.y) && layout[c.x, c.y] != DungeonGenerator.StairsSymbol && !DungeonPopulator.IsObstacle(layout[c.x, c.y]);

                var seen = new HashSet<Vector2Int> { layout.Entrance };
                var queue = new Queue<Vector2Int>(seen);
                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    foreach (var step in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
                    {
                        if (Walkable(cell + step) && seen.Add(cell + step))
                        {
                            queue.Enqueue(cell + step);
                        }
                    }
                }

                Assert.IsTrue(seen.Contains(layout.Stairs + Vector2Int.up), $"seme {seed}, profondità {depth}: davanti alla scala non si arriva");
                for (int y = 0; y < layout.Height; y++)
                {
                    for (int x = 0; x < layout.Width; x++)
                    {
                        if (Walkable(new Vector2Int(x, y)) && !seen.Contains(new Vector2Int(x, y)))
                        {
                            Assert.Fail($"seme {seed}, profondità {depth}: ({x}, {y}) chiusa da un ostacolo\n{Text(layout)}");
                        }
                    }
                }
            }
        }

        [Test, Description("Ogni stanza ha almeno una torcia, e ogni torcia ha un muro alto a nord o a est dove il builder la appende")]
        public void EveryRoom_HasATorchOnAHighWall()
        {
            foreach (var (seed, depth, layout) in Dungeons())
            {
                foreach (var room in layout.Rooms)
                {
                    bool lit = false;
                    for (int y = room.yMin; y < room.yMax; y++)
                    {
                        for (int x = room.xMin; x < room.xMax; x++)
                        {
                            lit |= layout[x, y] == DungeonPopulator.TorchSymbol;
                        }
                    }

                    Assert.IsTrue(lit, $"seme {seed}, profondità {depth}: stanza {room} al buio\n{Text(layout)}");
                }

                for (int y = 0; y < layout.Height; y++)
                {
                    for (int x = 0; x < layout.Width; x++)
                    {
                        if (layout[x, y] == DungeonPopulator.TorchSymbol)
                        {
                            Assert.IsTrue(!layout.IsFloor(x, y - 1) || !layout.IsFloor(x + 1, y), $"seme {seed}: torcia in ({x}, {y}) senza muro alto");
                        }
                    }
                }
            }
        }

        [Test, Description("Gli ostacoli stanno contro i muri delle stanze, mai in un corridoio")]
        public void Obstacles_StandAgainstRoomWalls()
        {
            foreach (var (seed, depth, layout) in Dungeons())
            {
                for (int y = 0; y < layout.Height; y++)
                {
                    for (int x = 0; x < layout.Width; x++)
                    {
                        if (!DungeonPopulator.IsObstacle(layout[x, y]))
                        {
                            continue;
                        }

                        var cell = new Vector2Int(x, y);
                        var room = layout.Rooms.FirstOrDefault(r => r.Contains(cell));
                        Assert.IsTrue(room.Contains(cell), $"seme {seed}: ostacolo in un corridoio in ({x}, {y})");
                        Assert.IsTrue(x == room.xMin || y == room.yMin || x == room.xMax - 1 || y == room.yMax - 1, $"seme {seed}: ostacolo in mezzo alla stanza in ({x}, {y})");

                        // non dentro le balaustre della scala, non addosso al cavaliere appena entrato
                        Assert.AreNotEqual(1, Mathf.Abs(x - layout.Stairs.x) + Mathf.Abs(y - layout.Stairs.y), $"seme {seed}: ostacolo accanto alla scala");
                        Assert.AreNotEqual(1, Mathf.Abs(x - layout.Entrance.x) + Mathf.Abs(y - layout.Entrance.y), $"seme {seed}: ostacolo accanto all'ingresso");
                    }
                }
            }
        }

        [Test, Description("Stesso seme e stessa profondità, stesso contenuto")]
        public void SameSeed_SameContent()
        {
            Assert.AreEqual(Text(_generator.Generate(4711, 3)), Text(new DungeonGenerator(_settings).Generate(4711, 3)));
        }
    }
}
