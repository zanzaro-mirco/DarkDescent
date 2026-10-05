using System.Collections.Generic;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class DungeonGeneratorTests
    {
        private const int Seeds = 500;

        private DungeonSettings _settings;
        private DungeonGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            // i numeri di D2, quelli scritti nella classe
            _settings = ScriptableObject.CreateInstance<DungeonSettings>();
            _generator = new DungeonGenerator(_settings);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
        }

        private static LevelMap Map(DungeonLayout layout)
        {
            return layout.ToMap(new Dictionary<string, string[]>());
        }

        private static string Text(LevelMap map) => MapChecks.Text(map);

        private static Dictionary<Vector2Int, int> Walk(LevelMap map, Vector2Int from) => MapChecks.Walk(map, from);

        [Test, Description("Su 500 semi ogni pavimento si raggiunge a piedi dall'ingresso, e la scala ha accanto un pavimento raggiunto")]
        public void EveryFloor_IsReachableFromTheEntrance()
        {
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = _generator.Generate(seed);
                var map = Map(layout);
                var distances = Walk(map, layout.Entrance);

                for (int y = 0; y < map.Height; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        // il testo della mappa solo se serve: comporlo a ogni cella rallenta il test di secondi
                        if (map.IsFloor(x, y) && map.GetSymbol(x, y) != DungeonGenerator.StairsSymbol && !distances.ContainsKey(new Vector2Int(x, y)))
                        {
                            Assert.Fail($"seme {seed}: ({x}, {y}) irraggiungibile\n{Text(map)}");
                        }
                    }
                }

                Assert.IsTrue(distances.ContainsKey(layout.Stairs + Vector2Int.up), $"seme {seed}: davanti alla scala non si arriva");
            }
        }

        [Test, Description("Su 500 semi nessuna stanza si sovrappone a un'altra né la tocca, e i pavimenti stanno dentro il bordo di roccia")]
        public void Rooms_AreSeparatedAndInsideTheBorder()
        {
            int fewest = int.MaxValue, most = 0;
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = _generator.Generate(seed);
                var rooms = layout.Rooms;
                fewest = Mathf.Min(fewest, rooms.Count);
                most = Mathf.Max(most, rooms.Count);
                for (int i = 0; i < rooms.Count; i++)
                {
                    var grown = new RectInt(rooms[i].x - 1, rooms[i].y - 1, rooms[i].width + 2, rooms[i].height + 2);
                    for (int j = i + 1; j < rooms.Count; j++)
                    {
                        Assert.IsFalse(grown.Overlaps(rooms[j]), $"seme {seed}: le stanze {rooms[i]} e {rooms[j]} si toccano");
                    }

                    Assert.That(rooms[i].width, Is.InRange(_settings.MinRoom, _settings.MaxRoom));
                    Assert.That(rooms[i].height, Is.InRange(_settings.MinRoom, _settings.MaxRoom));
                }

                for (int x = 0; x < layout.Width; x++)
                {
                    Assert.IsFalse(layout.IsFloor(x, 0) || layout.IsFloor(x, layout.Height - 1), $"seme {seed}: pavimento sul bordo");
                }

                for (int y = 0; y < layout.Height; y++)
                {
                    Assert.IsFalse(layout.IsFloor(0, y) || layout.IsFloor(layout.Width - 1, y), $"seme {seed}: pavimento sul bordo");
                }
            }

            Assert.GreaterOrEqual(fewest, 5, "una cripta di 28 × 28 celle ha almeno cinque stanze");
            Debug.Log($"stanze per livello su {Seeds} semi: da {fewest} a {most}");
        }

        [Test, Description("Un ingresso e una scala; la scala è ad almeno metà della distanza massima, con la roccia a nord (lo stendardo) e il pavimento a sud (da dove si arriva)")]
        public void Stairs_AreFarAndFacingSouth()
        {
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = _generator.Generate(seed);
                var map = Map(layout);
                Assert.AreEqual(1, map.Markers.Count(m => m.Symbol == DungeonGenerator.EntranceSymbol), $"seme {seed}");
                Assert.AreEqual(1, map.Markers.Count(m => m.Symbol == DungeonGenerator.StairsSymbol), $"seme {seed}");
                Assert.AreNotEqual(layout.StartRoom, layout.ExitRoom);

                var distances = Walk(map, layout.Entrance);
                int stairs = distances[layout.Stairs + Vector2Int.up] + 1;
                if (stairs * 2 < distances.Values.Max())
                {
                    Assert.Fail($"seme {seed}: scala troppo vicina all'ingresso\n{Text(map)}");
                }

                Assert.IsFalse(map.IsFloor(layout.Stairs.x, layout.Stairs.y - 1), $"seme {seed}: a nord della scala non c'è il muro");
                Assert.IsTrue(map.IsFloor(layout.Stairs.x, layout.Stairs.y + 1), $"seme {seed}: a sud della scala non c'è pavimento");
            }
        }

        [Test, Description("Stesso seme, stessa cripta; semi diversi, cripte diverse")]
        public void SameSeed_SameDungeon()
        {
            Assert.AreEqual(Text(Map(_generator.Generate(4711))), Text(Map(_generator.Generate(4711))));
            Assert.AreEqual(Text(Map(_generator.Generate(4711))), Text(Map(new DungeonGenerator(_settings).Generate(4711))), "anche da un generatore nuovo");

            var seen = new HashSet<string>();
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                seen.Add(Text(Map(_generator.Generate(seed))));
            }

            Assert.AreEqual(Seeds, seen.Count, "ogni seme dà una cripta sua");
        }

        [Test, Description("Il seme di un livello dipende da partita e profondità, ed è diverso da quello dei nemici")]
        public void LevelSeed_IsStableAndSeparate()
        {
            ulong seed = SeedMixer.ForLevel(4711UL, 1);
            Assert.AreEqual(seed, SeedMixer.ForLevel(4711UL, 1));
            CollectionAssert.DoesNotContain(new[]
            {
                SeedMixer.ForLevel(4712UL, 1),
                SeedMixer.ForLevel(4711UL, 2),
                SeedMixer.ForEnemy(4711UL, 1, 0, 0),
            }, seed);
        }
    }
}
