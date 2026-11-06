using System.Collections.Generic;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Pozzi e santuari nelle caverne (25.6 della M25, D10): negli slarghi, al più uno per tipo, con le probabilità della cripta, e il resto della caverna non cambia.</summary>
    public class CaveFeaturesTests
    {
        private const int Seeds = 400;
        private const int Depth = 5;

        private CaveSettings _settings;
        private CaveSettings _without;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<CaveSettings>("Assets/_Project/Data/Levels/CaveSettings.asset");

            // la stessa caverna senza pozzi né santuari: serve per vedere che il resto non si sposta
            _without = Object.Instantiate(_settings);
            var so = new SerializedObject(_without);
            so.FindProperty("_wellChance").floatValue = 0f;
            so.FindProperty("_shrineChance").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_without);
        }

        private static int Count(DungeonLayout layout, string symbols)
        {
            int count = 0;
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    count += symbols.IndexOf(layout[x, y]) >= 0 ? 1 : 0;
                }
            }

            return count;
        }

        [Test, Description("Al più un pozzo e un santuario per caverna, ognuno elencato tra le feature, e le probabilità sono quelle della cripta (a tiri di livello)")]
        public void AtMostOneWellAndOneShrine_WithTheCryptOdds()
        {
            int wells = 0, shrines = 0, both = 0;
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = _settings.CreateGenerator().Generate(seed, Depth);
                int well = Count(layout, DungeonPopulator.WellSymbols);
                int shrine = Count(layout, DungeonPopulator.ShrineSymbols);
                Assert.LessOrEqual(well, 1, $"seme {seed}");
                Assert.LessOrEqual(shrine, 1, $"seme {seed}");
                Assert.AreEqual(well + shrine, layout.Features.Count, $"seme {seed}: le feature elencate");
                wells += well;
                shrines += shrine;
                both += well + shrine == 2 ? 1 : 0;
            }

            // 1 - 0,85^4 = 48% per tipo, meno quando uno slargo non c'è
            Debug.Log($"[M25 caverne] su {Seeds} semi: {wells} pozzi, {shrines} santuari, {both} con tutti e due");
            Assert.That(wells, Is.InRange(Seeds * 0.38, Seeds * 0.56), "pozzi");
            Assert.That(shrines, Is.InRange(Seeds * 0.38, Seeds * 0.56), "santuari");
        }

        [Test, Description("Stanno in uno slargo (più di cinque vicini liberi su otto), lontano dall'ingresso e dalla scala, e il pozzo e il santuario stanno a distanza")]
        public void TheyStandInAWidening()
        {
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = _settings.CreateGenerator().Generate(seed, Depth);
                foreach (var feature in layout.Features)
                {
                    int free = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            free += (dx != 0 || dy != 0) && layout[feature.Cell.x + dx, feature.Cell.y + dy] == LevelMap.Floor ? 1 : 0;
                        }
                    }

                    Assert.Greater(free, CavePopulator.WideNeighbours, $"seme {seed}: lo slargo");
                    Assert.AreNotEqual(layout.Stairs, feature.Cell);
                    Assert.Greater(Mathf.Max(Mathf.Abs(feature.Cell.x - layout.Entrance.x), Mathf.Abs(feature.Cell.y - layout.Entrance.y)), 1, $"seme {seed}: accanto all'ingresso");
                    Assert.AreEqual(-1, feature.Room, "le caverne non hanno stanze");
                }

                if (layout.Features.Count == 2)
                {
                    var a = layout.Features[0].Cell;
                    var b = layout.Features[1].Cell;
                    Assert.GreaterOrEqual(Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y)), CavePopulator.FeatureGap, $"seme {seed}: troppo vicini");
                }
            }
        }

        [Test, Description("Dopo i pozzi e i santuari si arriva ancora a ogni cella e davanti alla scala")]
        public void TheCaveStaysReachable()
        {
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                MapChecks.Pass($"seme {seed}", MapChecks.Reachable(_settings.CreateGenerator().Generate(seed, Depth), stairs: true));
            }
        }

        [Test, Description("Il resto della caverna non cambia (D10): senza i due tiri, tolti i pozzi e i santuari, mappa, nemici, casse e arredi sono gli stessi")]
        public void EverythingElse_IsTheSame()
        {
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var with = _settings.CreateGenerator().Generate(seed, Depth);
                var without = _without.CreateGenerator().Generate(seed, Depth);
                Assert.AreEqual(0, without.Features.Count);
                var features = new HashSet<Vector2Int>();
                foreach (var feature in with.Features)
                {
                    features.Add(feature.Cell);
                }

                for (int y = 0; y < with.Height; y++)
                {
                    for (int x = 0; x < with.Width; x++)
                    {
                        char expected = features.Contains(new Vector2Int(x, y)) ? LevelMap.Floor : with[x, y];
                        Assert.AreEqual(without[x, y], expected, $"seme {seed}, cella {x},{y}");
                    }
                }
            }
        }

        [Test, Description("Quale pozzo e quale santuario lo dice l'hash della cella e della profondità, e la stessa caverna dà gli stessi due posti")]
        public void TheHashPicksTheKindAndThePlace()
        {
            for (ulong seed = 1; seed <= 60; seed++)
            {
                var first = _settings.CreateGenerator().Generate(seed, Depth);
                var again = _settings.CreateGenerator().Generate(seed, Depth);
                Assert.AreEqual(first.Features.Count, again.Features.Count);
                for (int i = 0; i < first.Features.Count; i++)
                {
                    var feature = first.Features[i];
                    Assert.AreEqual(feature.Cell, again.Features[i].Cell);
                    Assert.AreEqual(DungeonPopulator.FeatureSymbol(feature.Kind, feature.Cell, Depth), first[feature.Cell.x, feature.Cell.y]);
                }
            }
        }
    }
}
