using System.Collections.Generic;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>La stanza sigillata (25.4 della M25, D5 e D16): il piano che sceglie la porta e la leva su una mappa generata, provato su molti semi.</summary>
    public class SealedRoomPlannerTests
    {
        private const int Seeds = 150;

        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private DungeonSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<DungeonSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
        }

        private LevelMap MapOf(ulong seed, int depth)
        {
            return new DungeonGenerator(_settings).Generate(seed, depth).ToMap(null);
        }

        private static Vector2Int Find(LevelMap map, char symbol)
        {
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    if (map.GetSymbol(x, y) == symbol)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            Assert.Fail("manca il simbolo " + symbol);
            return default;
        }

        // le celle che l'ingresso raggiunge, con la porta chiusa o aperta
        private static bool[,] Reach(bool[,] walkable, Vector2Int from, Vector2Int? blocked)
        {
            int width = walkable.GetLength(0);
            int height = walkable.GetLength(1);
            var seen = new bool[width, height];
            var queue = new Queue<Vector2Int>();
            seen[from.x, from.y] = true;
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps)
                {
                    var next = cell + step;
                    if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height || seen[next.x, next.y] || !walkable[next.x, next.y] || next == blocked)
                    {
                        continue;
                    }

                    seen[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            return seen;
        }

        [Test, Description("Su molti semi, a ogni profondità della cripta: con la porta chiusa l'ingresso raggiunge la scala e la leva e non il fondo, con la porta aperta raggiunge tutto il fondo (D5)")]
        public void TheDoorSeparatesOnlyTheDeadEnd()
        {
            int plans = 0;
            for (int depth = SealedRoomPlanner.FirstDepth; depth <= SealedRoomPlanner.LastDepth; depth++)
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var map = MapOf(seed, depth);
                    var plan = SealedRoomPlanner.Find(map, seed, depth, requireChest: true);
                    if (plan == null)
                    {
                        continue;
                    }

                    plans++;
                    string where = $"profondità {depth}, seme {seed}";
                    var walkable = SealedRoomPlanner.Walkability(map);
                    var entrance = Find(map, DungeonGenerator.EntranceSymbol);
                    var stairs = Find(map, DungeonGenerator.StairsSymbol);
                    var closed = Reach(walkable, entrance, plan.Door);
                    var open = Reach(walkable, entrance, null);

                    Assert.IsTrue(walkable[plan.Door.x, plan.Door.y], where + ": la porta sta su una cella di cammino");
                    Assert.AreEqual(LevelMap.Floor, map.GetSymbol(plan.Door.x, plan.Door.y), where + ": su pavimento semplice");

                    // la scala giù si raggiunge dal suo davanti, con la porta chiusa
                    bool stairsReached = false;
                    foreach (var step in Steps)
                    {
                        var front = stairs + step;
                        stairsReached |= walkable[front.x, front.y] && closed[front.x, front.y];
                    }

                    Assert.IsTrue(stairsReached, where + ": la scala si raggiunge a porta chiusa");
                    Assert.IsTrue(closed[plan.Lever.x, plan.Lever.y], where + ": la leva si raggiunge a porta chiusa");
                    var leverFront = plan.Lever + plan.LeverFacing;
                    Assert.IsTrue(closed[leverFront.x, leverFront.y], where + ": si arriva davanti alla leva a porta chiusa");
                    Assert.IsTrue(map.IsRock(plan.Lever.x - plan.LeverFacing.x, plan.Lever.y - plan.LeverFacing.y), where + ": la leva ha il muro alle spalle");
                    Assert.GreaterOrEqual(plan.LeverDistance, SealedRoomPlanner.MinLeverDistance / 2, where + ": la leva è lontana dalla porta");

                    Assert.GreaterOrEqual(plan.Region.Count, SealedRoomPlanner.MinRegionCells, where + ": il fondo è una stanza");
                    foreach (var cell in plan.Region)
                    {
                        Assert.IsFalse(closed[cell.x, cell.y], where + $": il fondo {cell} è chiuso a porta chiusa");
                        Assert.IsTrue(open[cell.x, cell.y], where + $": il fondo {cell} si raggiunge a porta aperta");
                        Assert.AreNotEqual(DungeonGenerator.EntranceSymbol, map.GetSymbol(cell.x, cell.y), where + ": niente ingresso nel fondo");
                    }

                    // tutto il livello camminabile fuori dal fondo si raggiunge ancora: la porta non toglie altro
                    var inRegion = new HashSet<Vector2Int>(plan.Region);
                    for (int x = 0; x < map.Width; x++)
                    {
                        for (int y = 0; y < map.Height; y++)
                        {
                            var cell = new Vector2Int(x, y);
                            if (walkable[x, y] && cell != plan.Door && !inRegion.Contains(cell))
                            {
                                Assert.IsTrue(closed[x, y], where + $": {cell} resta raggiungibile a porta chiusa");
                            }
                        }
                    }

                    Assert.GreaterOrEqual(plan.Chests, 1, where + ": c'è il tesoro");
                }
            }

            Assert.Greater(plans, 150, "ci sono molti livelli con un fondo adatto");
        }

        [Test, Description("La stanza sigillata c'è solo nella cripta, dalla profondità 2 alla 4: mai alla 1 né alle caverne (D16)")]
        public void OnlyTheCryptFromDepthTwoToFour()
        {
            for (ulong seed = 1; seed <= 60; seed++)
            {
                Assert.IsFalse(SealedRoomPlanner.LevelWantsOne(seed, 1));
                for (int depth = 5; depth <= 8; depth++)
                {
                    Assert.IsFalse(SealedRoomPlanner.LevelWantsOne(seed, depth), $"profondità {depth}");
                }
            }
        }

        [Test, Description("Circa un livello su tre della cripta ha la stanza sigillata (D5), con il suo piano (porta, leva e un fondo con una cassa)")]
        public void AboutOneLevelInThreeHasASealedRoom()
        {
            int have = 0;
            int total = 0;
            for (int depth = SealedRoomPlanner.FirstDepth; depth <= SealedRoomPlanner.LastDepth; depth++)
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    total++;
                    if (SealedRoomPlanner.For(MapOf(seed, depth), seed, depth) != null)
                    {
                        have++;
                    }
                }
            }

            Assert.That(have / (double)total, Is.InRange(0.25, 0.42), $"{have} livelli su {total}");
        }

        [Test, Description("Lo stesso livello dà sempre lo stesso piano: la porta e la leva stanno nelle stesse celle (D3)")]
        public void ThePlanIsDeterministic()
        {
            for (ulong seed = 1; seed <= 40; seed++)
            {
                var map = MapOf(seed, 3);
                var a = SealedRoomPlanner.Find(map, seed, 3, true);
                var b = SealedRoomPlanner.Find(map, seed, 3, true);
                Assert.AreEqual(a == null, b == null);
                if (a != null)
                {
                    Assert.AreEqual(a.Door, b.Door);
                    Assert.AreEqual(a.Lever, b.Lever);
                    Assert.AreEqual(a.Region.Count, b.Region.Count);
                }
            }
        }
    }
}
