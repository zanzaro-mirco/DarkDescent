using System.Collections.Generic;
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

        [Test, Description("Su 500 semi ogni pavimento si raggiunge a piedi dall'ingresso, e la scala ha accanto un pavimento raggiunto")]
        public void EveryFloor_IsReachableFromTheEntrance()
        {
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                MapChecks.Pass($"seme {seed}", MapChecks.Reachable(_generator.Generate(seed), stairs: true));
            }
        }

        [Test, Description("Su 500 semi almeno cinque stanze, nessuna si sovrappone a un'altra né la tocca, e i pavimenti stanno dentro il bordo di roccia")]
        public void Rooms_AreSeparatedAndInsideTheBorder()
        {
            int fewest = int.MaxValue, most = 0;
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = _generator.Generate(seed);
                fewest = Mathf.Min(fewest, layout.Rooms.Count);
                most = Mathf.Max(most, layout.Rooms.Count);
                MapChecks.Pass($"seme {seed}", CryptChecks.Rooms(layout, _settings));
            }

            Debug.Log($"stanze per livello su {Seeds} semi: da {fewest} a {most}");
        }

        [Test, Description("Un ingresso e una scala; la scala è ad almeno metà della distanza massima, con la roccia a nord (lo stendardo) e il pavimento a sud (da dove si arriva); ingresso e scala in stanze diverse")]
        public void Stairs_AreFarAndFacingSouth()
        {
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                MapChecks.Pass($"seme {seed}", MapChecks.Exits(_generator.Generate(seed), stairs: true));
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
