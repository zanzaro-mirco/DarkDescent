using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Magic;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Save;
using DarkDescent.Town;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Le partite da sole della ronda notturna (D6 e D13 della M15): dalla città al re e oltre, con
    /// tutti i sistemi insieme, e a ogni livello il salvataggio su file e la ripresa dal menu (parte
    /// e); la partita lunga, mondo dopo mondo, cerca quello che resta in memoria.
    /// </summary>
    [Category(NightlyRun.Category)]
    public class NightlyGameTests : SandboxFixture
    {
        // le parti della ronda: i semi delle partite vengono da quello della notte; 1 e 2 sono le
        // mappe, 3 i livelli costruiti, 5–7 le misure
        private const int GamePart = 4;
        private const int LongPart = 8;

        private const int LastDepth = 8;

        /// <summary>Da questa profondità si torna una volta in città con il portale, e si riparte.</summary>
        private const int PortalDepth = 3;

        /// <summary>I mondi della partita lunga, ognuno dalla città al re (D13).</summary>
        private const int LongWorlds = 6;

        /// <summary>Ogni passo ha questo tempo per arrivare, poi il test cade (D6).</summary>
        private const float StepTime = 20f;

        private const int HalfHour = 1800000;

        private const long Megabyte = 1024 * 1024;

        // le misure della partita lunga che il rapporto scrive sempre; dei tipi di Unity solo quelli che cambiano
        private static readonly string[] Headline = { "oggetti in scena", "gestori degli eventi", "memoria gestita (MB)", "memoria di Unity (MB)" };

        private string _folder;
        private readonly List<string> _errors = new List<string>();
        private Health _health;
        private int _deaths;
        private int _kills;
        private int _chests;
        private int _pickedUp;
        private int _resumes;
        private int _levels;

        // il passo in corso, per il rapporto di una partita che si ferma a metà
        private string _step = "l'avvio del gioco";

        private static CompositionRoot Root => Object.FindFirstObjectByType<CompositionRoot>();

        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static MainMenu Menu => Object.FindFirstObjectByType<MainMenu>();

        private static SaveGame Save => Object.FindFirstObjectByType<SaveGame>();

        private static TownPortals Portals => Object.FindFirstObjectByType<TownPortals>();

        private static DifficultyPanel Panel => Object.FindFirstObjectByType<DifficultyPanel>();

        private static ConfirmDialog Confirm => Panel.transform.parent.Find("Confirm").GetComponent<ConfirmDialog>();

        private static VictoryScreen Victory => Object.FindFirstObjectByType<VictoryScreen>();

        private static GameObject Hero => GameObject.Find("Player");

        private string SavePath => Path.Combine(_folder, "save_1.json");

        // le difficoltà della tabella: una partita da sola per ognuna (D13)
        private static IEnumerable<int> Difficulties()
        {
            return Enumerable.Range(0, Table().Count);
        }

        private static DifficultyTable Table()
        {
            return AssetDatabase.LoadAssetAtPath<DifficultyTable>("Assets/_Project/Data/Progression/Difficulties.asset");
        }

        [SetUp]
        public void UseTemporarySaves()
        {
            // la stessa istanza serve tutte le partite della classe: i conti ripartono da zero
            _errors.Clear();
            _deaths = _kills = _chests = _pickedUp = _resumes = _levels = 0;
            _step = "l'avvio del gioco";
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentNightly_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Unguard();
            Time.timeScale = 1f;
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        [UnityTest, Timeout(HalfHour), Description("Una partita da sola per ogni difficoltà: dalla città giù fino all'8 per le scale, a ogni livello nemici uccisi, barili rotti, casse aperte, pozzi e santuari usati e tutto raccolto; una volta in città e ritorno con il portale; a ogni livello salvata, ripresa dal menu e uguale; il re, il portale della vittoria, l'altare e il mondo nuovo intero, fino al suo re")]
        public IEnumerator Game_FromTownToTheKingAndBeyond([ValueSource(nameof(Difficulties))] int difficulty)
        {
            ulong night = NightlyRun.Seed();
            ulong seed = SeedMixer.ForNightly(night, GamePart, difficulty);
            int difficulties = Table().Count;
            string title = $"Partita da sola, difficoltà {difficulty}";
            var watch = Stopwatch.StartNew();
            bool reported = false;

            // il rapporto si scrive anche se un passo si ferma a metà (trappola della M15)
            try
            {
                yield return NewGame(seed, difficulty);
                Debug.Log($"[DarkDescent] ronda notturna, partita da sola: seme {seed}, difficoltà {difficulty}");

                for (int depth = 1; depth <= LastDepth; depth++)
                {
                    yield return TakeStairs(depth);
                    yield return ClearLevel();
                    if (depth == PortalDepth)
                    {
                        yield return PortalToTownAndBack(depth);
                    }

                    yield return SaveAndResume(level => level.Depth == depth, $"profondità {depth}");
                }

                yield return KillTheKing();
                yield return ChangeTheWorld(seed, 1, difficulty + 1 < difficulties ? difficulty + 1 : -1);
                for (int depth = 1; depth <= LastDepth; depth++)
                {
                    yield return TakeStairs(depth);
                    yield return ClearLevel();
                    yield return SaveAndResume(level => level.Depth == depth, $"profondità {depth} del mondo nuovo");
                }

                yield return KillTheKing();
                if (_deaths > 0)
                {
                    _errors.Add($"il cavaliere è morto {_deaths} volte, invulnerabile");
                }

                string summary = $"seme della partita {seed}: {_kills} nemici, {_chests} casse, {_pickedUp} cose raccolte, "
                    + $"{_resumes} riprese dal menu, livello {Hero.GetComponent<PlayerProgress>().Progress.Level}, in {watch.Elapsed.TotalSeconds:F0} s";
                Debug.Log($"[DarkDescent] ronda notturna, partita da sola, difficoltà {difficulty}: {summary}");
                NightlyReport.Append(NightlyReport.Part(title, summary, _errors));
                reported = true;
                if (_errors.Count > 0)
                {
                    Assert.Fail($"partita da sola (seme {seed}, difficoltà {difficulty}; per rifare la notte: {NightlyRun.SeedOption} {night}):\n\n"
                        + string.Join("\n\n", _errors));
                }
            }
            finally
            {
                if (!reported)
                {
                    _errors.Add($"{_step}: la partita si è fermata qui (il perché è nei risultati dei test)");
                    NightlyReport.Append(NightlyReport.Part(title, $"seme della partita {seed}: interrotta dopo {_resumes} riprese", _errors));
                }
            }
        }

        [UnityTest, Timeout(HalfHour), Description("La partita lunga: sei mondi di fila alla stessa difficoltà, dalla città al re, senza mai tornare al menu; dopo ogni re, in città, niente cresce a ogni mondo: oggetti in scena, oggetti di Unity per tipo, gestori degli eventi, memoria")]
        public IEnumerator LongGame_LeavesNothingBehind()
        {
            ulong night = NightlyRun.Seed();
            ulong seed = SeedMixer.ForNightly(night, LongPart, 0);
            int difficulty = (int)(SeedMixer.ForNightly(night, LongPart, 1) % (ulong)Table().Count);
            var snapshots = new List<Dictionary<string, long>>();
            var watch = Stopwatch.StartNew();
            bool reported = false;

            try
            {
                yield return NewGame(seed, difficulty);
                Debug.Log($"[DarkDescent] ronda notturna, partita lunga: seme {seed}, difficoltà {difficulty}");

                // la prima vittoria a una difficoltà passa dal menu, che rifà la scena Core: da lì in
                // poi la partita non lascia più il gioco, e ogni mondo deve lasciare la città com'era
                ulong worldSeed = seed;
                for (int world = 0; world < LongWorlds; world++)
                {
                    if (world > 0)
                    {
                        yield return ChangeTheWorld(worldSeed, world, -1);
                        worldSeed = Manager.RunSeed;
                    }

                    for (int depth = 1; depth <= LastDepth; depth++)
                    {
                        yield return TakeStairs(depth);
                        yield return ClearLevel();
                        if (depth == PortalDepth)
                        {
                            yield return PortalToTownAndBack(depth);
                        }
                    }

                    yield return KillTheKing();
                    yield return Measure(snapshots);
                }

                if (_deaths > 0)
                {
                    _errors.Add($"il cavaliere è morto {_deaths} volte, invulnerabile");
                }

                _errors.AddRange(Growing(snapshots));
                string summary = $"seme della partita {seed}, difficoltà {difficulty}: {LongWorlds} mondi, {_levels} livelli, {_kills} nemici, "
                    + $"livello {Hero.GetComponent<PlayerProgress>().Progress.Level}, in {watch.Elapsed.TotalSeconds:F0} s. In città dopo ogni re "
                    + "(errore se una misura cresce a ogni mondo):";
                Debug.Log($"[DarkDescent] ronda notturna, partita lunga: {summary}\n{Describe(snapshots)}");
                NightlyReport.Append(NightlyReport.Part("Partita lunga", summary, _errors, Describe(snapshots)));
                reported = true;
                if (_errors.Count > 0)
                {
                    Assert.Fail($"partita lunga (seme {seed}, difficoltà {difficulty}; per rifare la notte: {NightlyRun.SeedOption} {night}):\n\n"
                        + string.Join("\n\n", _errors));
                }
            }
            finally
            {
                if (!reported)
                {
                    _errors.Add($"{_step}: la partita si è fermata qui (il perché è nei risultati dei test)");
                    NightlyReport.Append(NightlyReport.Part("Partita lunga", $"seme della partita {seed}, difficoltà {difficulty}: interrotta dopo {_levels} livelli", _errors));
                }
            }
        }

        // una partita nuova dal menu, nel posto 1, alla difficoltà data; poi il seme della partita
        private IEnumerator NewGame(ulong seed, int difficulty)
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, $"-difficulty {difficulty}");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            Menu.NewGameButton.onClick.Invoke();
            Menu.SlotView(1).Button.onClick.Invoke();
            yield return Arrive(level => level.IsTown, "la partita nuova in città");
            Root.UseLootSeed(seed);
            Manager.RunSeed = seed;
            Assert.AreEqual(difficulty, Root.Difficulty.Current, "la difficoltà del seme");
            Guard();
        }

        // il passo arriva entro il suo tempo, o la partita si ferma lì (D6)
        private IEnumerator Arrive(Func<LevelContext, bool> arrived, string what)
        {
            _step = what;
            for (float t = 0f; Manager == null || Manager.IsTransitioning || Manager.CurrentLevel == null || !arrived(Manager.CurrentLevel); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, StepTime, $"{what}: non arriva");
                yield return null;
            }

            yield return null;
        }

        // invulnerabile: ogni colpo torna indietro, e una morte si conta
        private void Guard()
        {
            Unguard();
            _health = Hero.GetComponent<Health>();
            _health.Damaged += HandleDamaged;
            _health.Died += HandleDied;
        }

        private void Unguard()
        {
            if (_health != null)
            {
                _health.Damaged -= HandleDamaged;
                _health.Died -= HandleDied;
            }

            _health = null;
        }

        private void HandleDamaged(DamageInfo info, float amount)
        {
            _health.SetCurrent(_health.Max);
        }

        private void HandleDied()
        {
            _deaths++;
            _health.Revive();
        }

        private IEnumerator TakeStairs(int depth)
        {
            var exit = Manager.CurrentLevel.Exits.FirstOrDefault();
            Assert.IsNotNull(exit, $"verso la profondità {depth}: nessuna scala");
            var interactable = exit.GetComponent<Interactable>();
            Hero.GetComponent<NavMeshAgent>().Warp(interactable.ApproachPoint);
            Hero.GetComponent<PlayerController>().Use(interactable);
            yield return Arrive(level => level.Depth == depth, $"la scala verso la profondità {depth}");
            _levels++;
        }

        // quello che fa chi gioca in un livello: nemici, barili, casse, pozzi e santuari, e raccoglie
        private IEnumerator ClearLevel()
        {
            var level = Manager.CurrentLevel;
            foreach (var enemy in level.Enemies.Where(e => e != null && e.isActiveAndEnabled && e.GetComponent<BossController>() == null))
            {
                var health = enemy.GetComponent<Health>();
                if (!health.IsDead)
                {
                    health.TakeDamage(new DamageInfo(health.Max, DamageType.Physical, null));
                    _kills++;
                }
            }

            yield return null;
            foreach (var barrel in level.Breakables.Where(b => b != null && !b.IsBroken))
            {
                barrel.TakeDamage(new DamageInfo(1000f, DamageType.Physical, null));
            }

            var agent = Hero.GetComponent<NavMeshAgent>();
            foreach (var chest in level.Chests.Where(c => !c.IsOpen))
            {
                var interactable = chest.GetComponent<Interactable>();
                agent.Warp(interactable.ApproachPoint);
                interactable.Use(Hero);
                _chests++;
            }

            foreach (var feature in level.Features.Where(f => !f.IsSpent))
            {
                var interactable = feature.GetComponent<Interactable>();
                agent.Warp(interactable.ApproachPoint);
                interactable.Use(Hero);
            }

            // il loot cade e si posa; gli esplosivi scoppiano dopo la miccia
            yield return new WaitForSeconds(2f);
            foreach (var item in Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None))
            {
                item.GetComponent<Interactable>().Use(Hero);
                _pickedUp++;
            }

            yield return null;
        }

        private IEnumerator PortalToTownAndBack(int depth)
        {
            Portals.OpenTownPortal();
            Hero.GetComponent<PlayerController>().Use(Portals.OpenPortal.Interactable);
            yield return Arrive(level => level.IsTown, $"il portale dalla profondità {depth} alla città");
            Assert.IsNotNull(Portals.OpenPortal, "in città il portale del ritorno");
            Hero.GetComponent<PlayerController>().Use(Portals.OpenPortal.Interactable);
            yield return Arrive(level => level.Depth == depth, $"il portale della città verso la profondità {depth}");
        }

        // dalla pausa al menu, che salva; poi Continua: lo stato ripreso è quello del file, campo per
        // campo, tranne il tempo di gioco e il mana, che intanto vanno avanti (come in SaveLoadTests).
        // I numeri entro un millimetro: un oggetto a terra torna a 99,54547119 da 99,54547882, il
        // rumore dei float nel passare la posizione (semi della notte 20261023 e 98765)
        private IEnumerator SaveAndResume(Func<LevelContext, bool> where, string what)
        {
            Unguard();
            var pause = Object.FindFirstObjectByType<PauseMenu>();
            pause.Open();
            pause.QuitButton.onClick.Invoke();
            yield return null;
            yield return null;
            Assert.IsNotNull(Menu, $"{what}: il menu dopo la pausa");
            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(SavePath, out SaveData saved), $"{what}: il salvataggio");

            Menu.ContinueButton.onClick.Invoke();
            yield return Arrive(where, $"{what}: Continua");
            Guard();
            _resumes++;

            var magic = Hero.GetComponent<PlayerMagic>();
            string resumed = Save.Capture().WithMagic(magic.Book.KnownIds(), magic.Book.Selected, saved.Mana)
                .WithStats(saved.PlayTime, saved.Kills, saved.Deaths, saved.GoldCollected, saved.BossDefeated).ToJson();
            string expected = saved.ToJson();
            int differs = FirstDifference(expected, resumed);
            if (differs >= 0)
            {
                _errors.Add($"{what}: ripresa diversa dal salvataggio, {Around(expected, resumed, differs)}");
            }
        }

        private IEnumerator KillTheKing()
        {
            var boss = Manager.CurrentLevel.Boss;
            Assert.IsNotNull(boss, "all'8 il re");
            boss.Health.TakeDamage(new DamageInfo(boss.Health.Max, DamageType.Physical, null));
            yield return null;

            var portal = Portals.VictoryPortal;
            Assert.IsNotNull(portal, "morto il re, il portale della vittoria");
            Assert.IsTrue(NavMesh.SamplePosition(portal.Interactable.ApproachPoint, out var hit, 2f, NavMesh.AllAreas), "al portale della vittoria si arriva");
            Hero.GetComponent<NavMeshAgent>().Warp(hit.position);
            Hero.GetComponent<PlayerController>().Use(portal.Interactable);
            yield return Arrive(level => level.IsTown, "il portale della vittoria");

            // la prima volta a questa difficoltà, la schermata; chiusa, si torna al menu e si continua
            if (Victory.IsOpen)
            {
                Unguard();
                Victory.Finish();
                yield return null;
                yield return null;
                Menu.ContinueButton.onClick.Invoke();
                yield return Arrive(level => level.IsTown, "Continua dopo la vittoria");
                Guard();
            }
        }

        // all'altare la difficoltà data, o lo stesso mondo rifatto con -1: in tutti e due i casi il
        // mondo numero world, con il seme che viene da quello del mondo prima
        private IEnumerator ChangeTheWorld(ulong previousSeed, int world, int difficulty)
        {
            var altar = Manager.CurrentLevel.Altar;
            Assert.IsNotNull(altar, "l'altare in città");
            altar.GetComponent<Interactable>().Use(Hero);
            Assert.IsTrue(Panel.IsOpen, "il pannello dell'altare");
            if (difficulty >= 0 && Panel.Row(difficulty).interactable)
            {
                Panel.Row(difficulty).onClick.Invoke();
            }
            else
            {
                Panel.NewWorldButton.onClick.Invoke();
            }

            Assert.IsTrue(Confirm.IsOpen, "la conferma dell'altare");
            Confirm.Yes.onClick.Invoke();
            yield return null;
            Assert.AreEqual(world, Root.Difficulty.World, "il mondo nuovo");
            Assert.AreEqual(SeedMixer.ForWorld(previousSeed, world), Manager.RunSeed, "il seme del mondo nuovo");
        }

        // quello che c'è in città dopo un mondo: gli oggetti distrutti spariscono a fine frame, e la
        // memoria si conta dopo la raccolta
        private IEnumerator Measure(List<Dictionary<string, long>> snapshots)
        {
            _step = $"la misura dopo il mondo {snapshots.Count}";
            yield return null;
            yield return null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var snapshot = new Dictionary<string, long>
            {
                [Headline[0]] = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                [Headline[1]] = CountHandlers(),
                [Headline[2]] = GC.GetTotalMemory(true) / Megabyte,
                [Headline[3]] = Profiler.GetTotalAllocatedMemoryLong() / Megabyte,
            };
            foreach (var type in Resources.FindObjectsOfTypeAll<Object>().GroupBy(o => o.GetType().Name))
            {
                snapshot[type.Key] = type.Count();
            }

            snapshots.Add(snapshot);
        }

        // i gestori attaccati agli eventi dei componenti del gioco: un += senza il suo -= li fa crescere
        private static long CountHandlers()
        {
            long count = 0;
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                for (var type = behaviour.GetType(); type != null && type.Namespace != null && type.Namespace.StartsWith("DarkDescent"); type = type.BaseType)
                {
                    foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        if (typeof(Delegate).IsAssignableFrom(field.FieldType) && field.GetValue(behaviour) is Delegate handler)
                        {
                            count += handler.GetInvocationList().Length;
                        }
                    }
                }
            }

            return count;
        }

        // le misure che crescono a ogni mondo, dal primo all'ultimo: quello che il gioco lascia indietro
        private static IEnumerable<string> Growing(List<Dictionary<string, long>> snapshots)
        {
            foreach (string key in snapshots[0].Keys)
            {
                var values = snapshots.Select(s => s.TryGetValue(key, out long v) ? v : 0).ToList();
                if (values.Zip(values.Skip(1), (a, b) => b > a).All(up => up))
                {
                    yield return $"{key} cresce a ogni mondo: {string.Join(", ", values)}";
                }
            }
        }

        // la tabella del rapporto: le quattro misure di sempre, e i tipi di Unity che cambiano
        private static string Describe(List<Dictionary<string, long>> snapshots)
        {
            var keys = Headline.Concat(snapshots.SelectMany(s => s.Keys).Distinct().Except(Headline)
                .Where(k => snapshots.Select(s => s.TryGetValue(k, out long v) ? v : 0).Distinct().Count() > 1).OrderBy(k => k));
            var table = new StringBuilder("| Misura | " + string.Join(" | ", Enumerable.Range(1, snapshots.Count).Select(w => $"Mondo {w}")) + " |\n");
            table.Append("|---|" + string.Concat(Enumerable.Repeat("---|", snapshots.Count)) + "\n");
            foreach (string key in keys)
            {
                table.Append($"| {key} | " + string.Join(" | ", snapshots.Select(s => s.TryGetValue(key, out long v) ? v : 0)) + " |\n");
            }

            return table.ToString();
        }

        /// <summary>Quanto possono differire due numeri dello stesso campo.</summary>
        private const double SameNumber = 0.001;

        // dove due JSON cominciano a differire, o -1: il testo uguale carattere per carattere, i numeri
        // entro SameNumber
        private static int FirstDifference(string expected, string actual)
        {
            int i = 0, j = 0;
            while (i < expected.Length && j < actual.Length)
            {
                if (IsNumberStart(expected[i]) && IsNumberStart(actual[j]))
                {
                    int start = i;
                    string a = ReadNumber(expected, ref i), b = ReadNumber(actual, ref j);
                    if (a != b && !(TryNumber(a, out double x) && TryNumber(b, out double y) && Math.Abs(x - y) <= SameNumber))
                    {
                        return start;
                    }

                    continue;
                }

                if (expected[i] != actual[j])
                {
                    return i;
                }

                i++;
                j++;
            }

            return i == expected.Length && j == actual.Length ? -1 : i;
        }

        private static bool IsNumberStart(char c)
        {
            return char.IsDigit(c) || c == '-';
        }

        private static string ReadNumber(string text, ref int i)
        {
            int start = i;
            while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '-' || text[i] == '+' || text[i] == '.' || text[i] == 'e' || text[i] == 'E'))
            {
                i++;
            }

            return text.Substring(start, i - start);
        }

        private static bool TryNumber(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        // i due testi attorno al punto in cui differiscono
        private static string Around(string expected, string actual, int at)
        {
            int from = Math.Max(0, at - 60);
            string Cut(string text) => from < text.Length ? text.Substring(from, Math.Min(text.Length - from, 140)) : "";
            return $"dal carattere {at}:\natteso:  {Cut(expected)}\nripreso: {Cut(actual)}";
        }
    }
}
