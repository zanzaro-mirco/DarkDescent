using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Vorrak il Custode nella cripta vera (24.4 della M24, D8–D11, D16): la sala, le due fasi, la carica, il tesoro e il ricordo.</summary>
    public class GuardianPlayModeTests : SandboxFixture
    {
        private const ulong Seed = 11;

        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentGuardian_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static DamageInfo Hit(float amount) => new DamageInfo(amount, DamageType.Physical, null);

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

        private static IEnumerator WaitFor(Func<bool> condition, float timeout)
        {
            float elapsed = 0f;
            while (elapsed < timeout && !condition())
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // alla profondità 4, con il cavaliere indistruttibile e gli altri nemici spenti: il test parla del Custode
        private IEnumerator AtTheGuardian()
        {
            yield return LoadGeneratedCore();
            Manager.RunSeed = Seed;
            yield return Travel("Level_Crypt", "Start", 4);
            var level = Manager.CurrentLevel;
            Assert.IsNotNull(level.Guardian, "il Custode alla profondità 4");
            foreach (var enemy in level.Enemies)
            {
                if (enemy != null && enemy.gameObject != level.Guardian.gameObject)
                {
                    enemy.gameObject.SetActive(false);
                }
            }

            Player.GetComponent<Health>().MultiplyMax(500f);
            Player.GetComponent<Health>().Heal(float.MaxValue);
        }

        private static Vector3 Spot(Vector3 wanted)
        {
            Assert.IsTrue(NavMesh.SamplePosition(wanted, out var hit, 3f, NavMesh.AllAreas), "un punto sul NavMesh");
            return hit.position;
        }

        // il cavaliere a qualche metro dal Custode, nella direzione che ha spazio
        private void StandAt(GuardianController guardian, float distance)
        {
            Vector3 home = guardian.GetComponent<EnemyAI>().HomePosition;
            foreach (var direction in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                if (NavMesh.SamplePosition(home + direction * distance, out var hit, 0.5f, NavMesh.AllAreas)
                    && !NavMesh.Raycast(home, hit.position, out _, NavMesh.AllAreas))
                {
                    PlayerAgent.Warp(hit.position);
                    return;
                }
            }

            PlayerAgent.Warp(Spot(home + Vector3.forward * distance));
        }

        [UnityTest, Description("Alla profondità 4 il Custode aspetta fermo nella stanza della scala giù: la scala è aperta, il suo tesoro chiuso, e non c'è porta né portale")]
        public IEnumerator TheGuardian_WaitsAtTheStairs()
        {
            yield return AtTheGuardian();
            var level = Manager.CurrentLevel;
            var guardian = level.Guardian;
            var ai = guardian.GetComponent<EnemyAI>();
            Assert.AreEqual(EnemyState.Idle, ai.State);
            Assert.IsNull(level.Boss, "non è il re");
            Assert.IsNull(level.BossGate, "senza porta");
            Assert.IsFalse(guardian.IsFighting);
            Assert.AreEqual("enemy.guardian", ai.Archetype.NameKey);

            var down = level.DownExits.First();
            Assert.Less(FlatDistance(guardian.transform.position, down.transform.position), 24f, "nella sala della scala (o in quella accanto, se era una di tre celle per tre)");

            var treasure = level.Chests.Single(c => c.OpensWithGuardian);
            Assert.IsTrue(treasure.IsSealed, "chiuso finché lui vive");
            Assert.IsFalse(level.Chests.Where(c => !c.OpensWithGuardian).Any(c => c.IsSealed), "gli altri forzieri non cambiano");
            Assert.Less(FlatDistance(treasure.transform.position, guardian.transform.position), 20f, "il tesoro è nella sua sala");
        }

        [UnityTest, Description("Sceso il cavaliere nella sala comincia lo scontro: la sua barra sta in alto con il nome in oro e il segno a metà, e non si chiude niente")]
        public IEnumerator TheFight_PinsTheBar_WithGoldNameAndHalfMark()
        {
            yield return AtTheGuardian();
            var guardian = Manager.CurrentLevel.Guardian;
            var bar = UnityEngine.Object.FindFirstObjectByType<EnemyBar>();
            bool started = false;
            guardian.FightStarted += _ => started = true;
            StandAt(guardian, 5f);
            yield return WaitFor(() => started, 2f);

            Assert.IsTrue(started, "lo scontro comincia a 5 m");
            Assert.IsTrue(guardian.IsFighting);
            Assert.AreEqual(GuardianPhase.Strike, guardian.Phase);
            yield return null;
            Assert.AreSame(guardian.Health, bar.Pinned, "la sua vita sta in alto");
            Assert.AreSame(guardian.Health, bar.Shown);
            Assert.AreEqual(1f, bar.FillAmount, 1e-3f);
            Assert.IsTrue(bar.HalfMarkShown, "il segno a metà");
            Assert.AreEqual(new Color(1f, 0.82f, 0.3f, 1f), bar.NameColor, "il nome in oro");
            Assert.IsFalse(string.IsNullOrEmpty(bar.NameText));
        }

        [UnityTest, Description("Sotto la metà della vita evoca tre scheletri, una volta sola, e li lega al cavaliere senza esperienza")]
        public IEnumerator Wrath_SummonsThreeSkeletons_Once()
        {
            yield return AtTheGuardian();
            var guardian = Manager.CurrentLevel.Guardian;
            StandAt(guardian, 5f);
            yield return WaitFor(() => guardian.IsFighting, 2f);
            Assert.IsFalse(guardian.HasSummoned);
            Assert.AreEqual(0, guardian.Summons.Count);

            guardian.Health.TakeDamage(Hit(guardian.Health.Max * 0.4f));
            yield return null;
            Assert.AreEqual(GuardianPhase.Strike, guardian.Phase, "al 60% è ancora la prima fase");
            Assert.AreEqual(0, guardian.Summons.Count);

            guardian.Health.TakeDamage(Hit(guardian.Health.Max * 0.15f));
            yield return WaitFor(() => guardian.HasSummoned, 1f);
            Assert.AreEqual(GuardianPhase.Wrath, guardian.Phase);
            Assert.IsTrue(guardian.HasSummoned);
            Assert.AreEqual(3, guardian.Summons.Count, "tre scheletri");
            foreach (var summoned in guardian.Summons)
            {
                Assert.AreEqual("enemy.skeleton", summoned.Archetype.NameKey);
                Assert.IsNotNull(summoned.Archetype);
            }

            // ancora meno vita: niente seconda evocazione
            guardian.Health.TakeDamage(Hit(guardian.Health.Max * 0.2f));
            yield return null;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(3, guardian.Summons.Count(s => s != null), "una volta sola");
        }

        [UnityTest, Description("In seconda fase carica in linea retta: striscia rossa a terra, poi la corsa, colpisce chi è sulla strada, e dopo resta fermo")]
        public IEnumerator Wrath_Charges_InAStraightLine()
        {
            yield return AtTheGuardian();
            var guardian = Manager.CurrentLevel.Guardian;
            var health = Player.GetComponent<Health>();
            var line = guardian.GetComponent<TelegraphLine>();
            StandAt(guardian, 7f);
            yield return WaitFor(() => guardian.IsFighting, 2f);
            guardian.Health.TakeDamage(Hit(guardian.Health.Max * 0.55f));

            // le evocazioni non c'entrano con la carica: fuori
            yield return WaitFor(() => guardian.HasSummoned, 1f);
            foreach (var summoned in guardian.Summons)
            {
                summoned.gameObject.SetActive(false);
            }

            Assert.AreEqual(GuardianController.ChargeStage.None, guardian.Stage);
            yield return WaitFor(() => guardian.Stage == GuardianController.ChargeStage.WindUp, 8f);
            Assert.AreEqual(GuardianController.ChargeStage.WindUp, guardian.Stage, "dopo qualche secondo prepara la carica");
            Assert.IsTrue(line.IsShown, "la striscia rossa a terra");
            // la striscia finisce dove finisce lo scatto: al primo bordo del NavMesh, o dopo la distanza intera
            float freeRun = NavMesh.Raycast(guardian.transform.position, guardian.transform.position + guardian.ChargeDirection * guardian.ChargeDistance, out var runEdge, NavMesh.AllAreas)
                ? runEdge.distance
                : guardian.ChargeDistance;
            Assert.AreEqual(freeRun, line.Length, 1e-3f, "la striscia non attraversa il muro");
            Assert.LessOrEqual(line.Length, guardian.ChargeDistance + 1e-3f);
            Vector3 from = guardian.transform.position;
            Vector3 direction = guardian.ChargeDirection;
            Assert.AreEqual(1f, direction.magnitude, 1e-3f);

            // il cavaliere sulla linea, lontano: dove la corsa lo trova (a 80% della strada libera, almeno quattro metri)
            NavMesh.Raycast(from, from + direction * 9f, out var edge, NavMesh.AllAreas);
            float free = Mathf.Min(9f, edge.distance > 0f ? edge.distance : 9f);
            Assert.GreaterOrEqual(free, 6f, "strada libera davanti al Custode");
            PlayerAgent.Warp(Spot(from + direction * free * 0.8f));
            float before = health.Current;

            yield return WaitFor(() => guardian.Stage == GuardianController.ChargeStage.Run, 3f);
            Assert.AreEqual(GuardianController.ChargeStage.Run, guardian.Stage);
            Assert.IsFalse(line.IsShown, "partita, la striscia sparisce");

            // lungo la corsa il Custode resta sulla linea
            float maxOff = 0f;
            yield return WaitFor(() =>
            {
                Vector3 offset = guardian.transform.position - from;
                offset.y = 0f;
                Vector3 side = Vector3.Cross(Vector3.up, direction);
                maxOff = Mathf.Max(maxOff, Mathf.Abs(Vector3.Dot(offset, side)));
                return guardian.Stage != GuardianController.ChargeStage.Run;
            }, 3f);
            Assert.AreEqual(GuardianController.ChargeStage.Recover, guardian.Stage, "dopo la corsa, la pausa");
            Assert.Less(maxOff, 0.6f, "in linea retta");
            Assert.Greater(FlatDistance(from, guardian.transform.position), 2f, "ha corso");
            Assert.LessOrEqual(FlatDistance(from, guardian.transform.position), guardian.ChargeDistance + 0.5f);
            Assert.Less(health.Current, before, "il cavaliere sulla linea è stato colpito");

            yield return WaitFor(() => guardian.Stage == GuardianController.ChargeStage.None, 4f);
            Assert.AreEqual(GuardianController.ChargeStage.None, guardian.Stage, "finita la pausa torna a combattere");
        }

        [UnityTest, Description("Il cavaliere si leva di mezzo mentre la striscia si riempie: la corsa non lo prende")]
        public IEnumerator Wrath_Charge_MissesWhoStepsAside()
        {
            yield return AtTheGuardian();
            var guardian = Manager.CurrentLevel.Guardian;
            var health = Player.GetComponent<Health>();
            StandAt(guardian, 8f);
            yield return WaitFor(() => guardian.IsFighting, 2f);
            guardian.Health.TakeDamage(Hit(guardian.Health.Max * 0.55f));
            yield return WaitFor(() => guardian.HasSummoned, 1f);
            foreach (var summoned in guardian.Summons)
            {
                summoned.gameObject.SetActive(false);
            }

            yield return WaitFor(() => guardian.Stage == GuardianController.ChargeStage.WindUp, 8f);
            Assert.AreEqual(GuardianController.ChargeStage.WindUp, guardian.Stage);

            // di lato di tre metri rispetto alla linea
            Vector3 side = Vector3.Cross(Vector3.up, guardian.ChargeDirection);
            Vector3 aside = Player.position + side * 3.5f;
            if (!NavMesh.SamplePosition(aside, out var hit, 1f, NavMesh.AllAreas))
            {
                aside = Player.position - side * 3.5f;
                Assert.IsTrue(NavMesh.SamplePosition(aside, out hit, 1f, NavMesh.AllAreas), "un posto di lato");
            }

            PlayerAgent.Warp(hit.position);
            float before = health.Current;
            yield return WaitFor(() => guardian.Stage == GuardianController.ChargeStage.Recover, 4f);
            Assert.AreEqual(GuardianController.ChargeStage.Recover, guardian.Stage);
            Assert.AreEqual(before, health.Current, 1f, "mancato");
        }

        [UnityTest, Description("Morto il Custode: i suoi scheletri cadono con lui, la barra in alto sparisce e il tesoro si apre; il ricordo del livello lo tiene morto e il tesoro aperto")]
        public IEnumerator WhenItFalls_TheTreasureOpens_AndItStaysDead()
        {
            yield return AtTheGuardian();
            var level = Manager.CurrentLevel;
            var guardian = level.Guardian;
            var treasure = level.Chests.Single(c => c.OpensWithGuardian);
            var bar = UnityEngine.Object.FindFirstObjectByType<EnemyBar>();
            StandAt(guardian, 5f);
            yield return WaitFor(() => guardian.IsFighting, 2f);
            guardian.Health.TakeDamage(Hit(guardian.Health.Max * 0.55f));
            yield return WaitFor(() => guardian.HasSummoned, 1f);
            var summons = guardian.Summons.ToArray();
            Assert.AreEqual(3, summons.Length);
            Assert.IsTrue(treasure.IsSealed);

            guardian.Health.TakeDamage(Hit(guardian.Health.Max));
            yield return null;
            Assert.IsTrue(guardian.Health.IsDead);
            Assert.IsFalse(treasure.IsSealed, "il tesoro si apre");
            Assert.IsNull(bar.Pinned, "la barra non è più in alto");
            foreach (var summoned in summons)
            {
                Assert.IsTrue(summoned == null || summoned.GetComponent<Health>().IsDead, "gli evocati cadono con lui");
            }

            // via dalla 4 e di nuovo alla 4: non rinasce, e il suo tesoro resta aperto
            yield return Travel("Level_Crypt", "FromAbove", 3);
            yield return Travel("Level_Crypt", "FromAbove", 4);
            var again = Manager.CurrentLevel;
            Assert.IsTrue(again.Guardian == null || !again.Guardian.gameObject.activeSelf || again.Guardian.Health.IsDead, "non rinasce");
            var treasureAgain = again.Chests.Single(c => c.OpensWithGuardian);
            Assert.IsFalse(treasureAgain.IsSealed, "il tesoro resta aperto");
        }

        [UnityTest, Description("Il cavaliere cade e torna in vita all'ingresso: il Custode ricomincia dalla prima fase a vita piena, senza evocati, e la sua barra scende")]
        public IEnumerator WhenTheKnightDies_TheFightStartsOver()
        {
            yield return AtTheGuardian();
            var guardian = Manager.CurrentLevel.Guardian;
            var bar = UnityEngine.Object.FindFirstObjectByType<EnemyBar>();
            StandAt(guardian, 5f);
            yield return WaitFor(() => guardian.IsFighting, 2f);
            guardian.Health.TakeDamage(Hit(guardian.Health.Max * 0.55f));
            yield return WaitFor(() => guardian.HasSummoned, 1f);
            var summons = guardian.Summons.ToArray();
            Assert.AreEqual(GuardianPhase.Wrath, guardian.Phase);

            bool reset = false;
            guardian.FightReset += _ => reset = true;
            guardian.GetComponent<EnemyAI>().ReturnHome();
            guardian.ResetFight();
            Assert.IsTrue(reset);
            Assert.IsFalse(guardian.IsFighting);
            Assert.AreEqual(GuardianPhase.Strike, guardian.Phase);
            Assert.IsFalse(guardian.HasSummoned, "le evocazioni tornano da fare");
            Assert.AreEqual(GuardianController.ChargeStage.None, guardian.Stage);
            Assert.AreEqual(guardian.Health.Max, guardian.Health.Current, 0.01f);
            Assert.AreEqual(0, guardian.Summons.Count);
            PlayerAgent.Warp(Spot(Manager.CurrentLevel.GetEntrance("Start").position));
            yield return null;
            yield return null;
            foreach (var summoned in summons)
            {
                Assert.IsTrue(summoned == null, "gli evocati spariscono");
            }
        }
    }
}
