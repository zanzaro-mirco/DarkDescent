using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using DarkDescent.Player;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Diagnostics
{
    /// <summary>
    /// La misura delle prestazioni in build (D6 della M11). Con <c>-benchmark</c> sulla riga di comando
    /// il cavaliere gira da solo per città, cripta, caverne e scontro con il re, combattendo chi trova e
    /// senza morire; per ogni tratto una riga con frame, main thread, allocazioni e rendering va nel log
    /// e, se dopo l'opzione c'è un percorso, in quel file. Poi il gioco si chiude. Si avvia con
    /// <c>-seed</c>, che salta il menu e rende i livelli sempre uguali. Con <c>-benchmark-levels</c>
    /// misura invece quanto costa costruire i livelli (D19 della M18).
    /// </summary>
    public sealed class Benchmark : MonoBehaviour
    {
        public const string Option = "-benchmark";

        /// <summary>I secondi di ogni tratto, se non sono i 20 di partenza: <c>-benchmark-seconds 5</c>.</summary>
        public const string SecondsOption = "-benchmark-seconds";

        /// <summary>Un tetto agli fps, per una registrazione del Profiler che stia nella sua storia: <c>-benchmark-fps 30</c>.</summary>
        public const string FpsOption = "-benchmark-fps";

        /// <summary>
        /// Al posto del giro, i tempi dei livelli (D19 della M18): il borgo, la cripta alle profondità 1 e
        /// 4 e le caverne alla 8, ognuno costruito con tanti semi quanti dice il numero (20 senza):
        /// generazione, costruzione e NavMesh, in media e il peggiore. <c>-benchmark-levels 20</c>.
        /// </summary>
        public const string LevelsOption = "-benchmark-levels";

        private const int DefaultLevelSeeds = 20;

        private const float DefaultStageSeconds = 20f;
        private const float SettleSeconds = 1.5f;
        private const float LoadTimeout = 30f;
        private const float TargetRange = 30f;
        private const float WalkInterval = 4f;
        private const float WalkDistance = 15f;

        // 20 s fino a 1500 fps
        private readonly FrameStats _stats = new FrameStats(30000);
        private readonly StringBuilder _report = new StringBuilder();
        private readonly System.Random _random = new System.Random(7);

        private LevelManager _levels;
        private PlayerMotor _motor;
        private MeleeAttack _attack;
        private Health _health;
        private string _path;
        private float _stageSeconds = DefaultStageSeconds;
        private int _levelSeeds;

        private ProfilerRecorder _mainThread;
        private ProfilerRecorder _gcAlloc;
        private ProfilerRecorder _batches;
        private ProfilerRecorder _setPass;
        private ProfilerRecorder _triangles;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfAsked()
        {
            if (CommandLine.HasFlag(Environment.GetCommandLineArgs(), Option))
            {
                new GameObject("Benchmark").AddComponent<Benchmark>();
            }
        }

        private void Awake()
        {
            _levels = FindFirstObjectByType<LevelManager>();
            _motor = FindFirstObjectByType<PlayerMotor>();
            if (_motor != null)
            {
                _attack = _motor.GetComponent<MeleeAttack>();
                _health = _motor.GetComponent<Health>();
            }

            // il percorso è facoltativo: un'altra opzione subito dopo non è un file
            string[] args = Environment.GetCommandLineArgs();
            if (CommandLine.TryGetValue(args, Option, out string path) && !path.StartsWith("-"))
            {
                _path = path;
            }

            if (CommandLine.TryGetValue(args, SecondsOption, out string seconds)
                && float.TryParse(seconds, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed)
                && parsed > 0f)
            {
                _stageSeconds = parsed;
            }

            if (CommandLine.TryGetValue(args, FpsOption, out string fps) && int.TryParse(fps, out int cap) && cap > 0)
            {
                Application.targetFrameRate = cap;
            }

            if (CommandLine.HasFlag(args, LevelsOption))
            {
                _levelSeeds = CommandLine.TryGetValue(args, LevelsOption, out string seeds) && int.TryParse(seeds, out int count) && count > 0
                    ? count
                    : DefaultLevelSeeds;
            }
        }

        private void OnEnable()
        {
            _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            _gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        }

        private void OnDisable()
        {
            _mainThread.Dispose();
            _gcAlloc.Dispose();
            _batches.Dispose();
            _setPass.Dispose();
            _triangles.Dispose();
        }

        private IEnumerator Start()
        {
            // senza tetto si misura il margine: il vSync del gioco va nel resoconto e poi si spegne.
            // Una finestra senza focus fermerebbe il player e un frame durerebbe secondi
            int gameVSync = QualitySettings.vSyncCount;
            QualitySettings.vSyncCount = 0;
            Application.runInBackground = true;

            _report.AppendLine($"DarkDescent {Application.version}{(Debug.isDebugBuild ? ", build di sviluppo" : "")}");
            _report.AppendLine($"{SystemInfo.processorType}, {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}), "
                + $"{Screen.width}x{Screen.height}, qualità {QualitySettings.names[QualitySettings.GetQualityLevel()]}, "
                + $"vSync del gioco {gameVSync} (spento per la misura), fps massimi {Application.targetFrameRate}, "
                + $"filtro retrò a {Shader.GetGlobalFloat("_RetroStrength").ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}");

            if (_levels == null || _health == null || _attack == null)
            {
                Finish("manca la scena Core");
                yield break;
            }

            // il menu iniziale aspetta un click: senza -seed la misura non parte
            for (float t = 0f; _levels.CurrentLevel == null || _levels.IsTransitioning; t += Time.unscaledDeltaTime)
            {
                if (t > LoadTimeout)
                {
                    Finish("nessun livello: va avviato con -seed");
                    yield break;
                }

                yield return null;
            }

            if (_levelSeeds > 0)
            {
                yield return MeasureBuild("borgo", "Town", "Start", 0);
                yield return MeasureBuild("cripta, profondità 1", "Level_Crypt", "Start", 1);
                yield return MeasureBuild("cripta, profondità 4", "Level_Crypt", "FromAbove", 4);
                yield return MeasureBuild("caverne, profondità 8", "Level_Caves", "FromAbove", 8);
                Finish(null);
                yield break;
            }

            yield return Measure("città", null, 0, false);
            yield return Measure("cripta, profondità 2", "Level_Crypt", 2, false);
            yield return Measure("cripta, profondità 4", "Level_Crypt", 4, false);
            yield return Measure("caverne, profondità 6", "Level_Caves", 6, false);
            yield return Measure("re delle ossa, profondità 8", "Level_Caves", 8, true);
            Finish(null);
        }

        private IEnumerator Measure(string label, string scene, int depth, bool boss)
        {
            if (scene != null)
            {
                _levels.LoadLevel(scene, "FromAbove", depth);
                for (float t = 0f; _levels.IsTransitioning || _levels.CurrentLevel == null || _levels.CurrentLevel.Depth != depth; t += Time.unscaledDeltaTime)
                {
                    if (t > LoadTimeout)
                    {
                        _report.AppendLine($"{label}: il livello non arriva");
                        yield break;
                    }

                    yield return null;
                }
            }

            // le uscite non scattano: il giro a caso porterebbe il cavaliere al livello dopo
            LevelContext level = _levels.CurrentLevel;
            for (int i = 0; i < level.Exits.Count; i++)
            {
                foreach (var trigger in level.Exits[i].GetComponentsInChildren<Collider>())
                {
                    trigger.enabled = false;
                }
            }

            for (float t = 0f; t < SettleSeconds; t += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            BossController king = boss ? level.Boss : null;
            if (boss)
            {
                if (king == null)
                {
                    _report.AppendLine($"{label}: il re non c'è");
                    yield break;
                }

                // davanti alla sala, come nei test: entrando la porta si chiude e lo scontro comincia
                Vector3 home = king.GetComponent<EnemyAI>().HomePosition;
                if (!NavMesh.SamplePosition(home + Vector3.back * 5f, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    _report.AppendLine($"{label}: nessun punto davanti alla sala");
                    yield break;
                }

                _motor.GetComponent<NavMeshAgent>().Warp(hit.position);
                for (float t = 0f; !king.IsFighting; t += Time.unscaledDeltaTime)
                {
                    if (t > 5f)
                    {
                        _report.AppendLine($"{label}: lo scontro non comincia");
                        yield break;
                    }

                    yield return null;
                }
            }

            _stats.Reset();
            float nextTarget = 0f;
            float nextWalk = 0f;
            for (float t = 0f; t < _stageSeconds; t += Time.unscaledDeltaTime)
            {
                if (_levels.CurrentLevel != level)
                {
                    _report.AppendLine($"{label}: il livello è cambiato durante la misura");
                    yield break;
                }

                // il cavaliere non muore: la vita torna piena dopo ogni colpo
                if (!_health.IsDead && _health.Current < _health.Max)
                {
                    _health.SetCurrent(_health.Max);
                }

                if (king != null)
                {
                    // le fasi del re con la vita: lo sciame a metà, la furia sotto un terzo
                    LowerTo(king.Health, t > _stageSeconds * 0.65f ? 0.3f : t > _stageSeconds * 0.3f ? 0.55f : 1f);
                }

                if (t >= nextTarget)
                {
                    nextTarget = t + 0.5f;
                    if (!_attack.HasTarget && !TryAttackNearest(king) && t >= nextWalk)
                    {
                        nextWalk = t + WalkInterval;
                        Walk();
                    }
                }

                _stats.Add(Time.unscaledDeltaTime * 1000f,
                    _mainThread.Valid ? _mainThread.LastValue / 1e6f : -1f,
                    _gcAlloc.Valid ? _gcAlloc.LastValue : -1,
                    _batches.Valid ? _batches.LastValue : -1,
                    _setPass.Valid ? _setPass.LastValue : -1,
                    _triangles.Valid ? _triangles.LastValue : -1);
                yield return null;
            }

            string line = _stats.Describe(label);
            _report.AppendLine(line);
            Debug.Log("[DarkDescent] benchmark " + line);
        }

        // Lo stesso livello costruito con semi diversi: si entra, si leggono i tempi del BuiltLevel della
        // scena e si passa al seme dopo. Il cavaliere resta a vita piena: tra un ingresso e l'altro
        // non deve morire
        private IEnumerator MeasureBuild(string label, string scene, string entrance, int depth)
        {
            var totals = new List<long>();
            long generate = 0, build = 0, bake = 0, worst = -1;
            ulong worstSeed = 0;
            for (int i = 0; i < _levelSeeds; i++)
            {
                ulong seed = 1000UL + (ulong)i;
                var previous = _levels.CurrentLevel;
                _levels.RunSeed = seed;
                _levels.LoadLevel(scene, entrance, depth);
                for (float t = 0f; _levels.IsTransitioning || _levels.CurrentLevel == null || _levels.CurrentLevel == previous; t += Time.unscaledDeltaTime)
                {
                    if (t > LoadTimeout)
                    {
                        _report.AppendLine($"{label}: il livello del seme {seed} non arriva");
                        yield break;
                    }

                    yield return null;
                }

                if (!_health.IsDead)
                {
                    _health.SetCurrent(_health.Max);
                }

                BuiltLevel built = null;
                foreach (var root in _levels.CurrentLevel.gameObject.scene.GetRootGameObjects())
                {
                    if (root.TryGetComponent(out built))
                    {
                        break;
                    }
                }

                if (built == null)
                {
                    _report.AppendLine($"{label}: la scena {scene} non si costruisce");
                    yield break;
                }

                var (g, b, n) = built.Timings;
                generate += g;
                build += b;
                bake += n;
                long total = g + b + n;
                totals.Add(total);
                if (total > worst)
                {
                    worst = total;
                    worstSeed = seed;
                }
            }

            float count = totals.Count;
            string line = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}: {1} semi, in media generazione {2:0.0} + costruzione {3:0.0} + NavMesh {4:0.0} = {5:0.0} ms, il peggiore {6} ms (seme {7})",
                label, totals.Count, generate / count, build / count, bake / count, (generate + build + bake) / count, worst, worstSeed);
            _report.AppendLine(line);
            Debug.Log("[DarkDescent] benchmark " + line);
        }

        private static void LowerTo(Health health, float fraction)
        {
            float wanted = health.Max * fraction;
            if (!health.IsDead && health.Current > wanted)
            {
                health.SetCurrent(wanted);
            }
        }

        private bool TryAttackNearest(BossController king)
        {
            if (king != null && !king.Health.IsDead)
            {
                _attack.SetTarget(king.Health);
                return true;
            }

            var enemies = _levels.CurrentLevel.Enemies;
            Vector3 position = _motor.transform.position;
            Health nearest = null;
            float best = TargetRange * TargetRange;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null)
                {
                    continue;
                }

                var health = enemies[i].GetComponent<Health>();
                float distance = (enemies[i].transform.position - position).sqrMagnitude;
                if (health != null && !health.IsDead && distance < best)
                {
                    best = distance;
                    nearest = health;
                }
            }

            if (nearest == null)
            {
                return false;
            }

            _attack.SetTarget(nearest);
            return true;
        }

        private void Walk()
        {
            var offset = new Vector3((float)(_random.NextDouble() * 2d - 1d), 0f, (float)(_random.NextDouble() * 2d - 1d));
            _motor.MoveTo(_motor.transform.position + offset.normalized * WalkDistance);
        }

        private void Finish(string error)
        {
            if (error != null)
            {
                _report.AppendLine("misura interrotta: " + error);
            }

            string report = _report.ToString();
            Debug.Log("[DarkDescent] benchmark finito\n" + report);
            if (_path != null)
            {
                File.WriteAllText(_path, report);
            }

            Application.Quit();
        }
    }
}
