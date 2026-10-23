using System.Collections.Generic;
using DarkDescent.Core;
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
                MapChecks.Pass($"seme {seed}", MapChecks.Reachable(_generator.Generate(seed, 5), stairs: true));
            }
        }

        [Test, Description("Su 500 semi il pavimento è tra il 35 e il 45% dell'area dentro il bordo, e il bordo resta roccia")]
        public void Floor_IsAboutFortyPercentAndInsideTheBorder()
        {
            float least = 1f, most = 0f;
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = _generator.Generate(seed, 5);
                least = Mathf.Min(least, CaveChecks.FloorFraction(layout));
                most = Mathf.Max(most, CaveChecks.FloorFraction(layout));
                MapChecks.Pass($"seme {seed}", CaveChecks.Shape(layout));
            }

            Debug.Log($"pavimento delle caverne su {Seeds} semi: da {least:P1} a {most:P1}");
        }

        [Test, Description("Su 500 semi niente pilastri di roccia di una cella in mezzo al pavimento, e niente pavimenti uniti solo in diagonale")]
        public void NoPillars_NoDiagonalOnlyContacts()
        {
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                MapChecks.Pass($"seme {seed}", CaveChecks.PillarsAndDiagonals(_generator.Generate(seed, 5)));
            }
        }

        [Test, Description("Un ingresso in uno spazio aperto e una scala ad almeno il 45% della distanza massima, con la roccia a nord e il pavimento a sud")]
        public void Stairs_AreFarAndFacingSouth()
        {
            int shortest = int.MaxValue;
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = _generator.Generate(seed, 5);
                MapChecks.Pass($"seme {seed}", MapChecks.Exits(layout, stairs: true) ?? CaveChecks.OpenEntrance(layout));
                shortest = Mathf.Min(shortest, MapChecks.Walk(layout, c => c != DungeonGenerator.StairsSymbol)[layout.Stairs + Vector2Int.up] + 1);
            }

            Debug.Log($"passi minimi dall'ingresso alla scala su {Seeds} semi: {shortest}");
        }

        [Test, Description("Con un corridoio lungo in fondo alla caverna la scala resta nella zona aperta, a 22 passi su 46: abbastanza lontana (ronda notturna 31337)")]
        public void Stairs_BeforeALongCorridor_AreFarEnough()
        {
            const ulong RunSeed = 10984456463612733930UL;
            var layout = _generator.Generate(SeedMixer.ForLevel(RunSeed, 7), 7);
            MapChecks.Pass($"seme {RunSeed}", MapChecks.Exits(layout, stairs: true));
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
