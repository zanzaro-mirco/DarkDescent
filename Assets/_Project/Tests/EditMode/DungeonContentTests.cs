using System.Collections.Generic;
using System.Linq;
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

        private static string Text(DungeonLayout layout) => MapChecks.Text(layout);

        [Test, Description("Scheletri 3 + 2 × profondità e casse 1 + profondità / 2 (D6), mai uno scheletro nella stanza d'ingresso")]
        public void EnemiesAndChests_GrowWithDepth()
        {
            foreach (var (seed, depth, layout) in Dungeons())
            {
                MapChecks.Pass($"seme {seed}, profondità {depth}", CryptChecks.Population(layout, _settings, depth));
            }

            Assert.AreEqual(new[] { 5, 7, 9, 11 }, Enumerable.Range(1, 4).Select(_settings.EnemyCount).ToArray());
            Assert.AreEqual(new[] { 1, 2, 2, 3 }, Enumerable.Range(1, 4).Select(_settings.ChestCount).ToArray());
        }

        [Test, Description("Casse e oggetti di scena non chiudono nessun passaggio: dall'ingresso si arriva a ogni cella libera e davanti alla scala")]
        public void Obstacles_NeverBlockThePath()
        {
            foreach (var (seed, depth, layout) in Dungeons())
            {
                MapChecks.Pass($"seme {seed}, profondità {depth}", MapChecks.ObstaclesLeavePath(layout, stairs: true));
            }
        }

        [Test, Description("Ogni stanza ha almeno una torcia, e ogni torcia ha un muro alto a nord o a est dove il builder la appende")]
        public void EveryRoom_HasATorchOnAHighWall()
        {
            foreach (var (seed, depth, layout) in Dungeons())
            {
                MapChecks.Pass($"seme {seed}, profondità {depth}", CryptChecks.Torches(layout));
            }
        }

        [Test, Description("Gli ostacoli stanno contro i muri delle stanze, mai in un corridoio; in mezzo solo il punto d'interesse: barili, pozzo o santuario (D4 della M13)")]
        public void Obstacles_StandAgainstRoomWalls()
        {
            foreach (var (seed, depth, layout) in Dungeons())
            {
                MapChecks.Pass($"seme {seed}, profondità {depth}", CryptChecks.ObstaclesAgainstWalls(layout));
            }
        }

        [Test, Description("Stesso seme e stessa profondità, stesso contenuto")]
        public void SameSeed_SameContent()
        {
            Assert.AreEqual(Text(_generator.Generate(4711, 3)), Text(new DungeonGenerator(_settings).Generate(4711, 3)));
        }
    }
}
