using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace DarkDescent.Tests
{
    /// <summary>La ronda notturna in EditMode (M15): gira solo di notte, con la categoria <see cref="NightlyRun.Category"/>.</summary>
    [Category(NightlyRun.Category)]
    public class NightlyEditModeTests
    {
        // le parti della ronda: ognuna ricava i suoi semi da quello della notte (SeedMixer.ForNightly)
        private const int CryptPart = 1;
        private const int CavePart = 2;

        /// <summary>Livelli per parte (D5 della M15); se la notte si allunga si riducono qui (D10).</summary>
        private const int Levels = 2000;

        /// <summary>Gli errori scritti per intero nel messaggio: gli altri sono solo contati.</summary>
        private const int Shown = 10;

        private const int TenMinutes = 600000;

        private static T Asset<T>(string name) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>($"Assets/_Project/Data/Levels/{name}.asset");
        }

        [Test, Description("La ronda sa il seme della notte e lo scrive nel log, per rifarla")]
        public void Round_KnowsTheSeed()
        {
            ulong seed = NightlyRun.Seed();
            Assert.Greater(seed, 0UL);
            Debug.Log($"[DarkDescent] ronda notturna, EditMode: seme della notte {seed} (per rifarla: {NightlyRun.SeedOption} {seed})");
        }

        [Test, Timeout(TenMinutes), Description("Le mappe della cripta su 2000 partite nuove: i controlli dei test di ogni push e la mappa che il gioco costruisce")]
        public void CryptMaps_OnNewSeeds()
        {
            var settings = Asset<DungeonSettings>("CryptSettings");
            Round("della cripta", settings, CryptPart, (layout, depth) =>
            {
                bool stairs = HasStairs(settings, depth);
                return new[]
                {
                    MapChecks.Reachable(layout, stairs),
                    CryptChecks.Rooms(layout, settings),
                    MapChecks.Exits(layout, stairs),
                    CryptChecks.Population(layout, settings, depth),
                    MapChecks.ObstaclesLeavePath(layout, stairs),
                    CryptChecks.Torches(layout),
                    CryptChecks.ObstaclesAgainstWalls(layout),
                };
            });
        }

        [Test, Timeout(TenMinutes), Description("Le mappe delle caverne su 2000 partite nuove, sala del re compresa: i controlli dei test di ogni push e la mappa che il gioco costruisce")]
        public void CaveMaps_OnNewSeeds()
        {
            var settings = Asset<CaveSettings>("CaveSettings");
            Round("delle caverne", settings, CavePart, (layout, depth) =>
            {
                bool stairs = HasStairs(settings, depth);
                return new[]
                {
                    MapChecks.Reachable(layout, stairs),
                    CaveChecks.Shape(layout),
                    CaveChecks.PillarsAndDiagonals(layout),
                    MapChecks.Exits(layout, stairs),
                    CaveChecks.OpenEntrance(layout),
                    CaveChecks.Population(layout, settings, depth),
                    MapChecks.ObstaclesLeavePath(layout, stairs),
                    CaveChecks.Corridors(layout),
                    CaveChecks.Candles(layout, settings),
                };
            });
        }

        // l'ultima profondità senza una scena dopo non ha la scala (DungeonLevel.CreateMap)
        private static bool HasStairs(DungeonSettings settings, int depth)
        {
            return depth < settings.LastDepth || !string.IsNullOrEmpty(settings.NextScene);
        }

        // ogni livello prova tutti i controlli: gli errori si raccolgono e il test cade alla fine, con l'elenco
        private static void Round(string name, DungeonSettings settings, int part, Func<DungeonLayout, int, string[]> checks)
        {
            Assert.IsNotNull(settings, $"le impostazioni {name}");
            ulong night = NightlyRun.Seed();
            var generator = settings.CreateGenerator();
            int depths = settings.LastDepth - settings.FirstDepth + 1;
            var errors = new List<string>();
            int failed = 0;
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < Levels; i++)
            {
                ulong runSeed = SeedMixer.ForNightly(night, part, i);
                int depth = settings.FirstDepth + i % depths;
                string where = $"partita {runSeed}, profondità {depth}";
                int before = errors.Count;
                try
                {
                    // forma e contenuto dal seme del livello che il gioco usa, come nei test di ogni push
                    var layout = generator.Generate(SeedMixer.ForLevel(runSeed, depth), depth);
                    Collect(errors, where, checks(layout, depth));

                    // la mappa che il gioco costruisce: nemici speciali, forzieri, tesoro del re, scala tolta all'ultima
                    var map = DungeonLevel.CreateMap(settings, runSeed, depth, out var built);
                    bool stairs = HasStairs(settings, depth);
                    Collect(errors, where + ", mappa del gioco", MapChecks.Reachable(built, stairs), MapChecks.ObstaclesLeavePath(built, stairs));
                    if (settings is CaveSettings caves && depth == caves.BossDepth)
                    {
                        Collect(errors, where + ", sala del re", CaveChecks.BossRoom(map));
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"{where}: {e.GetType().Name}: {e.Message}");
                }

                if (errors.Count > before)
                {
                    failed++;
                }
            }

            Debug.Log($"[DarkDescent] ronda notturna, mappe {name}: {Levels} livelli, {failed} con errori, in {watch.Elapsed.TotalSeconds:F1} s");
            if (errors.Count > 0)
            {
                Assert.Fail($"mappe {name}: {failed} livelli su {Levels} con errori (per rifare la notte: {NightlyRun.SeedOption} {night}); i primi:\n\n"
                    + string.Join("\n\n", errors.Take(Shown)));
            }
        }

        private static void Collect(List<string> errors, string where, params string[] results)
        {
            foreach (string error in results)
            {
                if (error != null)
                {
                    errors.Add($"{where}: {error}");
                }
            }
        }
    }
}
