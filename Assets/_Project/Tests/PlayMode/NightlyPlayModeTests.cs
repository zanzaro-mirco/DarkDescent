using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Town;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace DarkDescent.Tests
{
    /// <summary>La ronda notturna in PlayMode (M15): gira solo di notte, con la categoria <see cref="NightlyRun.Category"/>.</summary>
    [Category(NightlyRun.Category)]
    public class NightlyPlayModeTests : SandboxFixture
    {
        // la parte della ronda: i semi delle partite vengono da quello della notte (SeedMixer.ForNightly);
        // 1 e 2 sono le mappe in EditMode
        private const int BuiltPart = 3;

        /// <summary>Partite della parte (b), ognuna dalla profondità 1 all'8 (D5 e D13 della M15); se la notte si allunga si riducono qui (D10).</summary>
        private const int Games = 200;

        private const int Depths = 8;

        private const int CryptDepths = 4;

        /// <summary>Generazione, costruzione e NavMesh di un livello: sul runner oltre questo è un errore (D7).</summary>
        private const long SlowestLevel = 150;

        /// <summary>Quanto lontano dal pezzo si cerca il NavMesh.</summary>
        private const float NearPiece = 1f;

        /// <summary>La portata dell'arma più corta: un barile si rompe da questa distanza dal suo bordo.</summary>
        private const float ShortestReach = 0.5f;

        /// <summary>Due pezzi più vicini di così stanno nello stesso punto.</summary>
        private const float SamePlace = 0.3f;

        /// <summary>
        /// Quanto lontano dal sondaggio attorno a un barile si cerca il NavMesh. Dove un ostacolo l'ha
        /// ritagliato, con 25 cm SamplePosition non trova il NavMesh nemmeno sotto il sondaggio, con
        /// 60 cm sì, nel punto esatto (ronda notturna del 25 ott 2026, partita 7520343961558074204).
        /// Il punto trovato conta solo entro la portata.
        /// </summary>
        private const float ProbeSearch = 0.6f;

        private const int OneHour = 3600000;

        // quanti pezzi la ronda ha provato davvero, per il riassunto: un controllo che non trova niente
        // da provare passa sempre
        private int _pieces;
        private int _barrels;
        private int _portals;
        private int _decals;

        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static TownPortals Portals => Object.FindFirstObjectByType<TownPortals>();

        private static LevelRecorder Recorder => Object.FindFirstObjectByType<LevelRecorder>();

        [Test, Description("La ronda sa il seme della notte anche in PlayMode, dove arriva dalla stessa riga di comando")]
        public void Round_KnowsTheSeed()
        {
            ulong seed = NightlyRun.Seed();
            Assert.Greater(seed, 0UL);
            Debug.Log($"[DarkDescent] ronda notturna, PlayMode: seme della notte {seed} (per rifarla: {NightlyRun.SeedOption} {seed})");
        }

        [UnityTest, Timeout(OneHour), Description("200 partite nuove dalla profondità 1 all'8, costruite come nel gioco: dall'ingresso si arriva sul NavMesh alla scala, a casse e forzieri, pozzi e santuari e al re, e si rompe ogni barile; i nemici stanno sul NavMesh, il portale trova un posto raggiungibile, nessun pezzo sta sopra un altro, e nessun livello supera i 150 ms")]
        public IEnumerator BuiltLevels_AreWhole()
        {
            ulong night = NightlyRun.Seed();
            var errors = new List<string>();
            var timings = Enumerable.Range(0, Depths + 1).Select(_ => new List<long>()).ToArray();
            int failed = 0;
            int built = 0;
            string where = "l'avvio del gioco";
            bool reported = false;
            var watch = Stopwatch.StartNew();

            // il rapporto si scrive anche se un passo si ferma a metà (trappola della M15)
            try
            {
                yield return LoadGeneratedCore();
                for (int game = 0; game < Games; game++)
                {
                    ulong runSeed = SeedMixer.ForNightly(night, BuiltPart, game);
                    Manager.RunSeed = runSeed;
                    for (int depth = 1; depth <= Depths; depth++)
                    {
                        where = $"partita {runSeed}, profondità {depth}";
                        var previous = Manager.CurrentLevel;
                        Manager.LoadLevel(depth <= CryptDepths ? "Level_Crypt" : "Level_Caves", DungeonLevel.EntranceAt(depth), depth);
                        yield return WaitForNewLevel(previous, depth, where);

                        // i ricordi sono per scena e profondità, non per seme: quelli della partita di prima non valgono
                        Recorder.Memory.Import(null);

                        // un frame per gli agent dei nemici, uno per i barili che ritagliano il NavMesh
                        yield return null;
                        yield return null;

                        int before = errors.Count;
                        Vector3 entrance = Player.position;
                        timings[depth].Add(CheckLevel(where, errors));
                        yield return CheckBarrels(entrance, where, errors);
                        built++;
                        if (errors.Count > before)
                        {
                            failed++;
                        }
                    }

                    // le scene si scaricano, gli asset no (trappola della M15)
                    yield return Resources.UnloadUnusedAssets();
                }

                var table = new StringBuilder("| Profondità | Media | Massimo |\n|---|---|---|\n");
                for (int depth = 1; depth <= Depths; depth++)
                {
                    Debug.Log($"[DarkDescent] ronda notturna, tempi della profondità {depth}: media {timings[depth].Average():F0} ms, massimo {timings[depth].Max()} ms");
                    table.Append($"| {depth} | {timings[depth].Average():F0} ms | {timings[depth].Max()} ms |\n");
                }

                string summary = $"{built} livelli, {failed} con errori, in {watch.Elapsed.TotalSeconds:F0} s; provati {_pieces} pezzi raggiunti, "
                    + $"{_barrels} barili rotti, {_portals} portali, {_decals} macchie a terra. Tempi di generazione, costruzione e NavMesh (errore oltre {SlowestLevel} ms):";
                Debug.Log($"[DarkDescent] ronda notturna, livelli costruiti: {summary}");
                NightlyReport.Append(NightlyReport.Part("Livelli costruiti", summary, errors, table.ToString()));
                reported = true;
                if (errors.Count > 0)
                {
                    Assert.Fail($"livelli costruiti: {failed} livelli su {built} con errori (per rifare la notte: {NightlyRun.SeedOption} {night}); i primi:\n\n"
                        + string.Join("\n", errors.Take(NightlyReport.ShownErrors)));
                }
            }
            finally
            {
                if (!reported)
                {
                    errors.Add($"{where}: la parte si è fermata qui (il perché è nei risultati dei test)");
                    NightlyReport.Append(NightlyReport.Part("Livelli costruiti", $"interrotta dopo {built} livelli, a {where}", errors));
                }
            }
        }

        // un livello nuovo, non quello di prima: la prima partita ricarica la profondità 1 da cui parte il gioco
        private static IEnumerator WaitForNewLevel(LevelContext previous, int depth, string where)
        {
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel == previous
                || Manager.CurrentLevel.Depth != depth; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, $"{where}: il livello non si carica");
                yield return null;
            }
        }

        // i controlli di un livello appena costruito; restituisce quanto ci ha messo
        private long CheckLevel(string where, List<string> errors)
        {
            void Error(string text) => errors.Add($"{where}: {text}");

            var level = Manager.CurrentLevel;
            var (generate, build, bake) = Object.FindFirstObjectByType<DungeonLevel>().Timings;
            long total = generate + build + bake;
            if (total > SlowestLevel)
            {
                Error($"costruito in {total} ms (generazione {generate}, costruzione {build}, NavMesh {bake}), oltre {SlowestLevel}");
            }

            if (!PlayerAgent.isOnNavMesh)
            {
                Error("il cavaliere arriva fuori dal NavMesh");
                return total;
            }

            // dall'ingresso, a piedi sul NavMesh
            Vector3 entrance = Player.position;
            foreach (var exit in level.Exits)
            {
                Reach(entrance, exit.GetComponent<Interactable>().ApproachPoint, NearPiece, "la scala", Error);
            }

            foreach (var chest in level.Chests)
            {
                Reach(entrance, chest.GetComponent<Interactable>().ApproachPoint, NearPiece, chest.name, Error);
            }

            foreach (var feature in level.Features)
            {
                Reach(entrance, feature.GetComponent<Interactable>().ApproachPoint, NearPiece, feature.name, Error);
            }

            if (level.Boss != null)
            {
                Reach(entrance, level.Boss.transform.position, NearPiece, "il re", Error);
            }

            foreach (var enemy in level.Enemies)
            {
                if (enemy != null && enemy.isActiveAndEnabled && !enemy.GetComponent<NavMeshAgent>().isOnNavMesh)
                {
                    Error($"{enemy.name} fuori dal NavMesh in {enemy.transform.position}");
                }
            }

            SamePlaces(level, Error);

            // le macchie a terra (M17): tutte quelle della mappa, in una mesh sola
            int decals = DecalSpot.FromMap(level.Map).Count;
            var layer = level.GetComponentInChildren<DecalLayer>();
            if (decals > 0 && (layer == null || layer.Count != decals))
            {
                Error($"le macchie a terra sono {(layer != null ? layer.Count : 0)}, la mappa ne ha {decals}");
            }

            _decals += decals;

            // il portale, dall'ingresso e davanti a ogni cassa e ogni pozzo o santuario, girati verso il
            // pezzo: il posto peggiore, tra muri e barili interi (v1.2.1); i nemici e il re fermi, così non
            // si combatte, e niente portale nella sala del re, dove non si apre
            foreach (var enemy in level.Enemies.Where(e => e != null))
            {
                enemy.gameObject.SetActive(false);
            }

            var spots = new List<(Vector3 from, Vector3 toward, string where)> { (entrance, entrance + Player.forward, "all'ingresso") };
            spots.AddRange(level.Chests.Select(c => (c.GetComponent<Interactable>().ApproachPoint, c.transform.position, $"davanti a {c.name}")));
            spots.AddRange(level.Features.Select(f => (f.GetComponent<Interactable>().ApproachPoint, f.transform.position, $"davanti a {f.name}")));
            foreach (var (from, toward, place) in spots)
            {
                if (level.Boss != null && level.Boss.HoldsPlayerAt(from))
                {
                    continue;
                }

                if (PlayerAgent.Warp(from))
                {
                    TryPortal(toward, place, Error);
                    _portals++;
                }
            }

            if (level.Boss != null)
            {
                level.Boss.gameObject.SetActive(false);
            }

            return total;
        }

        // I barili si rompono da fuori, come fa chi gioca: se ne raggiunge uno, lo si rompe (il suo
        // ostacolo smette di ritagliare il NavMesh) e si riprova con gli altri. Quello in fondo a un
        // gruppo contro il muro si apre così, dopo quelli davanti; resta un errore solo quello che non
        // si apre mai.
        private IEnumerator CheckBarrels(Vector3 entrance, string where, List<string> errors)
        {
            var left = Manager.CurrentLevel.Breakables.Where(b => b != null).ToList();
            var path = new NavMeshPath();
            while (left.Count > 0)
            {
                var hit = left.Where(b => CanHit(entrance, b, path)).ToList();
                if (hit.Count == 0)
                {
                    break;
                }

                _barrels += hit.Count;
                foreach (var barrel in hit)
                {
                    foreach (var obstacle in barrel.GetComponentsInChildren<NavMeshObstacle>())
                    {
                        obstacle.enabled = false;
                    }

                    left.Remove(barrel);
                }

                // il ritaglio si toglie al frame dopo
                yield return null;
                yield return null;
            }

            foreach (var barrel in left)
            {
                errors.Add($"{where}: {barrel.name} in {barrel.transform.position} non si raggiunge per romperlo");
            }
        }

        // un punto del NavMesh, raggiungibile dall'ingresso, da cui l'arma più corta arriva al barile
        private bool CanHit(Vector3 entrance, Breakable barrel, NavMeshPath path)
        {
            Vector3 center = barrel.transform.position;
            float reach = PlayerAgent.radius + barrel.Radius + ShortestReach;
            for (int ring = 0; ring < 3; ring++)
            {
                float radius = reach * (0.6f + 0.2f * ring);
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * 22.5f * Mathf.Deg2Rad;
                    Vector3 probe = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    if (!NavMesh.SamplePosition(probe, out var hit, ProbeSearch, NavMesh.AllAreas))
                    {
                        continue;
                    }

                    Vector3 apart = hit.position - center;
                    apart.y = 0f;
                    if (apart.magnitude <= reach && NavMesh.CalculatePath(entrance, hit.position, NavMesh.AllAreas, path)
                        && path.status == NavMeshPathStatus.PathComplete)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void Reach(Vector3 from, Vector3 target, float near, string what, Action<string> error)
        {
            _pieces++;
            if (!NavMesh.SamplePosition(target, out var hit, near, NavMesh.AllAreas))
            {
                error($"{what} in {target} fuori dal NavMesh");
                return;
            }

            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                error($"a {what} in {target} non si arriva dall'ingresso");
            }
        }

        private void TryPortal(Vector3 toward, string place, Action<string> error)
        {
            Vector3 facing = toward - Player.position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.0001f)
            {
                Player.rotation = Quaternion.LookRotation(facing);
            }

            Portals.OpenTownPortal();
            Vector3 spot = Portals.OpenPortal.transform.position;

            // con un raggio stretto SamplePosition a volte non trova il NavMesh anche sotto il punto
            // (partita 3019584370815887483, profondità 4): si cerca largo e si vuole il punto vicino
            var path = new NavMeshPath();
            bool onMesh = NavMesh.SamplePosition(spot, out var hit, NearPiece, NavMesh.AllAreas) && Vector3.Distance(hit.position, spot) <= SamePlace;
            if (!onMesh || !NavMesh.CalculatePath(Player.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                error($"il portale aperto {place}, in {spot}, non si raggiunge");
            }
        }

        // casse, pozzi, santuari, barili, spuntoni e scale: mai due nello stesso punto
        private static void SamePlaces(LevelContext level, Action<string> error)
        {
            var pieces = level.Chests.Select(c => (c.name, c.transform.position))
                .Concat(level.Features.Select(f => (f.name, f.transform.position)))
                .Concat(level.Breakables.Select(b => (b.name, b.transform.position)))
                .Concat(level.Traps.Select(t => (t.name, t.transform.position)))
                .Concat(level.Exits.Select(e => (e.name, e.transform.position)))
                .ToList();
            for (int i = 0; i < pieces.Count; i++)
            {
                for (int j = i + 1; j < pieces.Count; j++)
                {
                    Vector3 apart = pieces[i].position - pieces[j].position;
                    apart.y = 0f;
                    if (apart.magnitude < SamePlace)
                    {
                        error($"{pieces[i].name} e {pieces[j].name} nello stesso punto, {pieces[i].position}");
                    }
                }
            }
        }
    }
}
