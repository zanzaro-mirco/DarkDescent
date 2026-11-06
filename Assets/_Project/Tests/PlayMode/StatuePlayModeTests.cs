using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>La statua che si anima nella cripta vera (25.5 della M25, D7): si scrolla quando ci si avvicina alla cassa, diventa un nemico, lascia macerie, e il ricordo la ritrova.</summary>
    public class StatuePlayModeTests : SandboxFixture
    {
        private const int SeedsToTry = 60;

        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentStatue_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            StatueGuard.Enabled = false;
            ChestSecret.Enabled = false;
            SealedRoom.Enabled = false;
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static void UseSeed(ulong seed)
        {
            Manager.RunSeed = seed;
            UnityEngine.Object.FindFirstObjectByType<CompositionRoot>().UseLootSeed(seed);
        }

        private static IEnumerator Travel(string scene, string entrance, int depth)
        {
            var before = Manager.CurrentLevel;
            Manager.LoadLevel(scene, entrance, depth);
            yield return null;
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel == before; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, scene + " non arriva");
                yield return null;
            }

            yield return null;
            yield return null;
        }

        // il primo livello della cripta, dal seme 1 in su, con una statua; con gli altri segreti spenti salvo quelli chiesti
        private IEnumerator LevelWithAStatue(bool secrets, Action<int, ulong> found)
        {
            yield return LoadGeneratedCore();
            StatueGuard.Enabled = true;
            ChestSecret.Enabled = secrets;
            SealedRoom.Enabled = secrets;
            for (int seed = 1; seed <= SeedsToTry; seed++)
            {
                for (int depth = StatueRules.FirstDepth; depth <= StatueRules.LastDepth; depth++)
                {
                    if (!StatueRules.LevelWantsOne((ulong)seed, depth))
                    {
                        continue;
                    }

                    UseSeed((ulong)seed);
                    yield return Travel("Level_Crypt", "Start", depth);
                    if (Manager.CurrentLevel.Secrets.OfType<StatueGuard>().Any())
                    {
                        var health = Player.GetComponent<Health>();
                        health.MultiplyMax(500f);
                        health.Heal(float.MaxValue);
                        found(depth, (ulong)seed);
                        yield break;
                    }
                }
            }

            Assert.Fail($"nessun livello di {SeedsToTry} semi ha una statua");
        }

        private static StatueGuard Guard => Manager.CurrentLevel.Secrets.OfType<StatueGuard>().First();

        private void StandNear(Vector3 chest)
        {
            var guard = Guard;
            Vector3 wanted = chest + (guard.transform.position - chest).normalized * 1.8f;
            Player.GetComponent<NavMeshAgent>().Warp(NavMesh.SamplePosition(wanted, out var hit, 3f, NavMesh.AllAreas) ? hit.position : wanted);
        }

        [UnityTest, Timeout(300000), Description("La statua (D7): sta in piedi accanto a una cassa, ferma, finché il cavaliere è lontano")]
        public IEnumerator TheStatue_StandsStillNextToAChest()
        {
            yield return LevelWithAStatue(false, (d, s) => { });
            var guard = Guard;
            Assert.AreEqual(StatueGuard.Phase.Still, guard.State);
            Assert.IsFalse(guard.IsDone);
            Assert.IsTrue(Manager.CurrentLevel.Chests.Any(c => Vector3.Distance(c.transform.position, guard.ChestPosition) < 0.1f), "guarda una cassa del livello");
            Assert.Less(Vector3.Distance(guard.transform.position, guard.ChestPosition), 6f, "le sta accanto");
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(StatueGuard.Phase.Still, guard.State, "il cavaliere è lontano: resta ferma");
        }

        [UnityTest, Timeout(300000), Description("Avvicinandosi alla cassa la statua si scrolla e, dopo l'avviso, prende vita: un nemico legato come gli altri, fuori dal conto dei nemici del livello")]
        public IEnumerator GettingCloseToTheChest_WakesTheStatue()
        {
            yield return LevelWithAStatue(false, (d, s) => { });
            var level = Manager.CurrentLevel;
            int enemies = level.Enemies.Count;
            var guard = Guard;
            StandNear(guard.ChestPosition);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(StatueGuard.Phase.Shaking, guard.State, "si scrolla: c'è un avviso");
            Assert.IsNull(guard.Enemy, "ancora pietra");

            yield return new WaitForSeconds(1.6f);
            Assert.AreEqual(StatueGuard.Phase.Awake, guard.State);
            Assert.IsNotNull(guard.Enemy, "il nemico è nato");
            Assert.IsFalse(guard.transform.GetChild(0).gameObject.activeSelf, "la statua di pietra non c'è più");
            Assert.AreEqual(enemies, level.Enemies.Count, "il nemico della statua non entra nel conto dei nemici del livello");

            var health = guard.Enemy.GetComponent<Health>();
            Assert.Greater(health.Max, 150f, "più duro di un bruto");
            Assert.AreEqual(Localizer.Get("enemy.statue"), Localizer.Get(guard.Enemy.Archetype.NameKey));
        }

        [UnityTest, Timeout(300000), Description("Morta, la statua lascia macerie e non si anima più, e il ricordo del livello (ADR-079) la ritrova in macerie")]
        public IEnumerator ADeadStatue_LeavesRubble_AndIsRemembered()
        {
            int depth = 0;
            ulong seed = 0;
            yield return LevelWithAStatue(false, (d, s) => { depth = d; seed = s; });
            var guard = Guard;
            StandNear(guard.ChestPosition);
            yield return new WaitForSeconds(2.4f);
            Assert.AreEqual(StatueGuard.Phase.Awake, guard.State);

            var health = guard.Enemy.GetComponent<Health>();
            health.TakeDamage(new DamageInfo(health.Max * 5f, DamageType.Physical, Player.gameObject));
            yield return null;
            Assert.AreEqual(StatueGuard.Phase.Done, guard.State);
            Assert.IsTrue(guard.IsDone);
            Assert.IsTrue(guard.transform.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Rubble" && t.gameObject.activeSelf), "macerie");

            yield return Travel("Level_Crypt", "Start", depth == 2 ? 3 : 2);
            UseSeed(seed);
            yield return Travel("Level_Crypt", "Start", depth);
            var back = Guard;
            Assert.IsTrue(back.IsDone, "ricordata caduta");
            StandNear(back.ChestPosition);
            yield return new WaitForSeconds(1.8f);
            Assert.AreEqual(StatueGuard.Phase.Done, back.State, "non si anima di nuovo");
            Assert.IsNull(back.Enemy);
        }

        [UnityTest, Timeout(300000), Description("Lasciando il livello a metà la statua torna in piedi, come il mimic: la fuga non conta come vittoria")]
        public IEnumerator LeavingMidFight_PutsTheStatueBackOnItsFeet()
        {
            int depth = 0;
            ulong seed = 0;
            yield return LevelWithAStatue(false, (d, s) => { depth = d; seed = s; });
            var guard = Guard;
            StandNear(guard.ChestPosition);
            yield return new WaitForSeconds(2.4f);
            Assert.AreEqual(StatueGuard.Phase.Awake, guard.State);

            yield return Travel("Level_Crypt", "Start", depth == 2 ? 3 : 2);
            UseSeed(seed);
            yield return Travel("Level_Crypt", "Start", depth);
            var back = Guard;
            Assert.AreEqual(StatueGuard.Phase.Still, back.State, "di nuovo statua");
            Assert.IsFalse(back.IsDone);
        }

        [UnityTest, Timeout(300000), Description("La statua non tocca il livello (D14): con l'interruttore acceso e spento le casse e i nemici sono gli stessi, nello stesso posto")]
        public IEnumerator TheStatue_DoesNotChangeTheLevel()
        {
            int depth = 0;
            ulong seed = 0;
            yield return LevelWithAStatue(false, (d, s) => { depth = d; seed = s; });
            var on = Manager.CurrentLevel;
            var chestsOn = on.Chests.Select(c => c.transform.position).ToList();
            var enemiesOn = on.Enemies.Select(e => e.transform.position).ToList();

            StatueGuard.Enabled = false;
            UseSeed(seed);
            yield return Travel("Level_Crypt", "Start", depth);
            var off = Manager.CurrentLevel;
            Assert.IsFalse(off.Secrets.OfType<StatueGuard>().Any(), "spento: niente statua");
            CollectionAssert.AreEqual(chestsOn, off.Chests.Select(c => c.transform.position).ToList());
            CollectionAssert.AreEqual(enemiesOn, off.Enemies.Select(e => e.transform.position).ToList());
        }

        [UnityTest, Timeout(600000), Description("Mai nella stanza del baule maledetto, né sulla cassa di un mimic, né nel fondo sigillato (D7), e mai più di tre segreti che contano in un livello (D16)")]
        public IEnumerator TheStatue_KeepsAwayFromTheCursedChest_AndRespectsTheCap()
        {
            yield return LoadGeneratedCore();
            StatueGuard.Enabled = true;
            ChestSecret.Enabled = true;
            SealedRoom.Enabled = true;
            int statues = 0;
            int withCursed = 0;
            for (int seed = 1; seed <= SeedsToTry; seed++)
            {
                for (int depth = StatueRules.FirstDepth; depth <= StatueRules.LastDepth; depth++)
                {
                    UseSeed((ulong)seed);
                    yield return Travel("Level_Crypt", "Start", depth);
                    var level = Manager.CurrentLevel;
                    int counted = level.Secrets.OfType<SealedRoom>().Count() + level.Chests.Count(c => c.Secret == ChestSecretKind.Cursed) + level.Secrets.OfType<StatueGuard>().Count();
                    Assert.LessOrEqual(counted, 3, $"seme {seed}, profondità {depth}: più di tre segreti");

                    var guard = level.Secrets.OfType<StatueGuard>().FirstOrDefault();
                    if (guard == null)
                    {
                        continue;
                    }

                    statues++;
                    var guarded = level.Chests.First(c => Vector3.Distance(c.transform.position, guard.ChestPosition) < 0.1f);
                    Assert.AreEqual(ChestSecretKind.None, guarded.Secret, $"seme {seed}, profondità {depth}: la cassa della statua è un mimic o un baule");
                    var room = level.Secrets.OfType<SealedRoom>().FirstOrDefault();
                    Assert.IsFalse(room != null && room.Touches(guarded.transform.position), $"seme {seed}, profondità {depth}: la cassa della statua è nel fondo sigillato");
                    foreach (var cursed in level.Chests.Where(c => c.Secret == ChestSecretKind.Cursed))
                    {
                        withCursed++;
                        Assert.GreaterOrEqual(Vector3.Distance(cursed.transform.position, guard.ChestPosition), StatueRules.CursedSeparation, $"seme {seed}, profondità {depth}: statua e baule maledetto nella stessa stanza");
                    }
                }
            }

            Assert.Greater(statues, 15, "troppo poche statue per provare");
            Debug.Log($"[M25 statua] {statues} statue in {SeedsToTry * 3} livelli, {withCursed} nello stesso livello di un baule maledetto");
        }
    }
}
