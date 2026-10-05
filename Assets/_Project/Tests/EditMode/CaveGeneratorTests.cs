using System.Collections.Generic;
using System.Linq;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class CaveGeneratorTests
    {
        private const int Seeds = 500;

        private CaveSettings _settings;
        private ILevelGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            // i numeri di D1 della M7, quelli scritti nella classe
            _settings = ScriptableObject.CreateInstance<CaveSettings>();
            _generator = _settings.CreateGenerator();
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

        [Test, Description("Le impostazioni scelgono l'algoritmo: le caverne il random walk, la cripta il BSP")]
        public void Settings_ChooseTheGenerator()
        {
            Assert.IsInstanceOf<CaveGenerator>(_generator);
            var crypt = ScriptableObject.CreateInstance<DungeonSettings>();
            Assert.IsInstanceOf<DungeonGenerator>(crypt.CreateGenerator());
            Object.DestroyImmediate(crypt);
        }

        [Test, Description("Su 500 semi ogni pavimento si raggiunge a piedi dall'ingresso, e davanti alla scala si arriva")]
        public void EveryFloor_IsReachableFromTheEntrance()
        {
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = _generator.Generate(seed, 5);
                var map = Map(layout);
                var distances = MapChecks.Walk(map, layout.Entrance);
                for (int y = 0; y < map.Height; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        if (map.IsFloor(x, y) && map.GetSymbol(x, y) != DungeonGenerator.StairsSymbol && !distances.ContainsKey(new Vector2Int(x, y)))
                        {
                            Assert.Fail($"seme {seed}: ({x}, {y}) irraggiungibile\n{MapChecks.Text(map)}");
                        }
                    }
                }

                Assert.IsTrue(distances.ContainsKey(layout.Stairs + Vector2Int.up), $"seme {seed}: davanti alla scala non si arriva");
            }
        }

        [Test, Description("Su 500 semi il pavimento è tra il 35 e il 45% dell'area dentro il bordo, e il bordo resta roccia")]
        public void Floor_IsAboutFortyPercentAndInsideTheBorder()
        {
            float least = 1f, most = 0f;
            int interior = (_settings.Width - 2) * (_settings.Height - 2);
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = _generator.Generate(seed, 5);
                int floor = 0;
                for (int y = 0; y < layout.Height; y++)
                {
                    for (int x = 0; x < layout.Width; x++)
                    {
                        if (!layout.IsFloor(x, y))
                        {
                            continue;
                        }

                        floor++;
                        if (x == 0 || y == 0 || x == layout.Width - 1 || y == layout.Height - 1)
                        {
                            Assert.Fail($"seme {seed}: pavimento sul bordo in ({x}, {y})");
                        }
                    }
                }

                float fraction = floor / (float)interior;
                least = Mathf.Min(least, fraction);
                most = Mathf.Max(most, fraction);
                if (fraction < 0.35f || fraction > 0.45f)
                {
                    Assert.Fail($"seme {seed}: pavimento al {fraction:P0}\n{MapChecks.Text(Map(layout))}");
                }
            }

            Debug.Log($"pavimento delle caverne su {Seeds} semi: da {least:P1} a {most:P1}");
        }

        [Test, Description("Su 500 semi niente pilastri di roccia di una cella in mezzo al pavimento, e niente pavimenti uniti solo in diagonale")]
        public void NoPillars_NoDiagonalOnlyContacts()
        {
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = _generator.Generate(seed, 5);
                for (int y = 1; y < layout.Height - 1; y++)
                {
                    for (int x = 1; x < layout.Width - 1; x++)
                    {
                        if (!layout.IsFloor(x, y) && layout.IsFloor(x - 1, y) && layout.IsFloor(x + 1, y) && layout.IsFloor(x, y - 1) && layout.IsFloor(x, y + 1))
                        {
                            Assert.Fail($"seme {seed}: pilastro in ({x}, {y})\n{MapChecks.Text(Map(layout))}");
                        }

                        bool a = layout.IsFloor(x, y), b = layout.IsFloor(x + 1, y), c = layout.IsFloor(x, y + 1), d = layout.IsFloor(x + 1, y + 1);
                        if ((a && d && !b && !c) || (b && c && !a && !d))
                        {
                            Assert.Fail($"seme {seed}: pavimenti uniti solo in diagonale in ({x}, {y})\n{MapChecks.Text(Map(layout))}");
                        }
                    }
                }
            }
        }

        [Test, Description("Un ingresso in uno spazio aperto e una scala ad almeno metà della distanza massima, con la roccia a nord e il pavimento a sud")]
        public void Stairs_AreFarAndFacingSouth()
        {
            int shortest = int.MaxValue;
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = _generator.Generate(seed, 5);
                var map = Map(layout);
                Assert.AreEqual(1, map.Markers.Count(m => m.Symbol == DungeonGenerator.EntranceSymbol), $"seme {seed}");
                Assert.AreEqual(1, map.Markers.Count(m => m.Symbol == DungeonGenerator.StairsSymbol), $"seme {seed}");

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        Assert.IsTrue(map.IsFloor(layout.Entrance.x + dx, layout.Entrance.y + dy), $"seme {seed}: l'ingresso non è in uno spazio aperto");
                    }
                }

                Assert.IsFalse(map.IsFloor(layout.Stairs.x, layout.Stairs.y - 1), $"seme {seed}: a nord della scala va la roccia");
                var distances = MapChecks.Walk(map, layout.Entrance);
                int stairs = distances[layout.Stairs + Vector2Int.up] + 1;
                int farthest = distances.Values.Max();
                shortest = Mathf.Min(shortest, stairs);
                if (stairs * 2 < farthest)
                {
                    Assert.Fail($"seme {seed}: scala a {stairs} passi, il punto più lontano a {farthest}\n{MapChecks.Text(map)}");
                }
            }

            Debug.Log($"passi minimi dall'ingresso alla scala su {Seeds} semi: {shortest}");
        }

        [Test, Description("Lo stesso seme dà la stessa caverna, anche da un generatore nuovo; semi diversi caverne diverse")]
        public void SameSeed_SameCave()
        {
            string first = MapChecks.Text(Map(_generator.Generate(4711, 5)));
            Assert.AreEqual(first, MapChecks.Text(Map(_generator.Generate(4711, 5))));
            Assert.AreEqual(first, MapChecks.Text(Map(_settings.CreateGenerator().Generate(4711, 5))), "anche da un generatore nuovo");
            Assert.AreNotEqual(first, MapChecks.Text(Map(_generator.Generate(4712, 5))));
            Debug.Log("caverna del seme 4711:\n" + first);
        }
    }
}
