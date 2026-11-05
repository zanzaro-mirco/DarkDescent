using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>La fuga dei nemici (24.2 della M24, D2–D6): nella stanza della sandbox, con i nemici che nei test di solito non scappano.</summary>
    public class FleeTests : SandboxFixture
    {
        private static readonly Vector3 Near = new Vector3(6f, 0f, 1f);

        [TearDown]
        public void Restore()
        {
            EnemyBehaviours.FleeEnabled = false;
            Time.timeScale = 1f;
        }

        private static DamageInfo Damage(float amount) => new DamageInfo(amount, DamageType.Physical, null);

        private static IEnumerator WaitFor(System.Func<bool> condition, float timeout)
        {
            float elapsed = 0f;
            while (elapsed < timeout && !condition())
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator Chasing(bool all = false)
        {
            yield return LoadSandbox(allSkeletons: all);
            EnemyBehaviours.FleeEnabled = true;
            PlayerAgent.Warp(Near);
            yield return null;
        }

        [UnityTest, Description("Uno scheletro sotto il 25% di vita scappa, a ×1,25, e lascia il cavaliere; in fuga si allontana fino a 10 m")]
        public IEnumerator Skeleton_FleesBelowAQuarter()
        {
            yield return Chasing();
            var ai = GameObject.Find("Skeleton").GetComponent<EnemyAI>();
            var agent = ai.GetComponent<NavMeshAgent>();
            var health = ai.GetComponent<Health>();
            float speed = agent.speed;
            ai.Alert();
            yield return null;
            Assert.AreEqual(EnemyState.Chase, ai.State);

            health.TakeDamage(Damage(health.Max * 0.8f));
            yield return WaitFor(() => ai.State == EnemyState.Flee, 1f);
            Assert.AreEqual(EnemyState.Flee, ai.State);
            Assert.IsTrue(ai.HasFled);
            Assert.AreEqual(speed * 1.25f, agent.speed, 0.01f, "×1,25 mentre scappa");
            Assert.IsFalse(ai.GetComponent<MeleeAttack>().HasTarget, "non insegue più");

            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(EnemyState.Flee, ai.State, "ancora in fuga dopo 2,5 s");
            Assert.GreaterOrEqual(FlatDistance(ai.transform.position, Player.position), 9f, "si è allontanato");
            Assert.LessOrEqual(FlatDistance(ai.transform.position, ai.HomePosition), 15.5f, "senza uscire dal raggio da casa");
            Assert.Less(health.Current, health.Max * 0.25f + 0.01f, "non si cura");
        }

        [UnityTest, Description("Dopo quattro secondi torna a inseguire alla velocità di prima, e non scappa una seconda volta con la vita ancora bassa")]
        public IEnumerator Skeleton_ComesBack_AndFleesOnlyOnce()
        {
            yield return Chasing();
            var ai = GameObject.Find("Skeleton").GetComponent<EnemyAI>();
            var agent = ai.GetComponent<NavMeshAgent>();
            var health = ai.GetComponent<Health>();
            float speed = agent.speed;
            ai.Alert();
            yield return null;
            health.TakeDamage(Damage(health.Max * 0.8f));
            yield return WaitFor(() => ai.State == EnemyState.Flee, 1f);
            Assert.AreEqual(EnemyState.Flee, ai.State);

            yield return WaitFor(() => ai.State != EnemyState.Flee, 6f);
            Assert.AreEqual(EnemyState.Chase, ai.State, "torna all'inseguimento");
            Assert.AreEqual(speed, agent.speed, 0.01f, "e alla velocità di prima");
            yield return null;
            Assert.IsTrue(ai.GetComponent<MeleeAttack>().HasTarget, "riprende il cavaliere");

            for (float t = 0f; t < 2f; t += Time.deltaTime)
            {
                Assert.AreNotEqual(EnemyState.Flee, ai.State, "una sola volta per vita");
                yield return null;
            }
        }

        [UnityTest, Description("Con l'interruttore spento (i test) un nemico a vita bassa continua a combattere")]
        public IEnumerator WithTheSwitchOff_NobodyFlees()
        {
            yield return Chasing();
            EnemyBehaviours.FleeEnabled = false;
            var ai = GameObject.Find("Skeleton").GetComponent<EnemyAI>();
            var health = ai.GetComponent<Health>();
            ai.Alert();
            health.TakeDamage(Damage(health.Max * 0.8f));
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                Assert.AreNotEqual(EnemyState.Flee, ai.State);
                yield return null;
            }

            Assert.IsFalse(ai.HasFled);
        }

        [UnityTest, Description("Due compagni morti vicino, entro tre secondi, fanno scappare anche chi sta bene")]
        public IEnumerator TwoCompanionsDying_MakeTheThirdFlee()
        {
            yield return Chasing(all: true);
            var skeletons = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Where(e => e.Archetype.CanFlee).ToArray();
            Assert.GreaterOrEqual(skeletons.Length, 3);
            var third = skeletons[0];
            var companions = skeletons.Skip(1).Take(2).ToArray();

            // i compagni accanto al terzo (entro 8 m), tutti svegli
            Vector3 center = third.transform.position;
            for (int i = 0; i < companions.Length; i++)
            {
                if (NavMesh.SamplePosition(center + new Vector3(2f + i, 0f, 1f), out var hit, 2f, NavMesh.AllAreas))
                {
                    companions[i].GetComponent<NavMeshAgent>().Warp(hit.position);
                }
            }

            foreach (var other in skeletons)
            {
                other.Alert();
            }

            yield return null;
            Assert.AreEqual(EnemyState.Chase, third.State);
            Assert.IsFalse(third.HasFled);

            foreach (var companion in companions)
            {
                var life = companion.GetComponent<Health>();
                life.TakeDamage(Damage(life.Max * 10f));
            }

            yield return WaitFor(() => third.State == EnemyState.Flee, 1f);
            Assert.AreEqual(EnemyState.Flee, third.State, "il lutto lo spaventa, con la vita piena");
            Assert.AreEqual(third.GetComponent<Health>().Max, third.GetComponent<Health>().Current, 0.01f);
        }

        [UnityTest, Description("Un solo compagno morto non basta: ne servono due")]
        public IEnumerator OneCompanionDying_IsNotEnough()
        {
            yield return Chasing(all: true);
            var skeletons = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Where(e => e.Archetype.CanFlee).ToArray();
            var third = skeletons[0];
            var other = skeletons[1];
            if (NavMesh.SamplePosition(third.transform.position + new Vector3(2f, 0f, 1f), out var hit, 2f, NavMesh.AllAreas))
            {
                other.GetComponent<NavMeshAgent>().Warp(hit.position);
            }

            third.Alert();
            yield return null;
            var life = other.GetComponent<Health>();
            life.TakeDamage(Damage(life.Max * 10f));
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                Assert.AreNotEqual(EnemyState.Flee, third.State);
                yield return null;
            }
        }

        [UnityTest, Description("Finita la fuga chiama i compagni dello stesso tipo entro 10 m: quello fermo comincia a inseguire")]
        public IEnumerator EndOfTheFlight_CallsTheCompanions()
        {
            yield return Chasing(all: true);
            var skeletons = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Where(e => e.Archetype.CanFlee).ToArray();
            var runner = GameObject.Find("Skeleton") != null ? GameObject.Find("Skeleton").GetComponent<EnemyAI>() : skeletons[0];
            var listener = skeletons.First(e => e != runner);

            // il compagno fermo a pochi metri dal fuggitivo, lontano dal cavaliere (fuori dal raggio di vista di 8 m)
            if (NavMesh.SamplePosition(runner.transform.position + new Vector3(0f, 0f, 5f), out var hit, 3f, NavMesh.AllAreas))
            {
                listener.GetComponent<NavMeshAgent>().Warp(hit.position);
            }

            Assert.AreEqual(EnemyState.Idle, listener.State);
            runner.Alert();
            yield return null;
            var health = runner.GetComponent<Health>();
            health.TakeDamage(Damage(health.Max * 0.8f));
            yield return WaitFor(() => runner.State == EnemyState.Flee, 1f);
            Assert.AreEqual(EnemyState.Flee, runner.State);
            Assert.AreEqual(EnemyState.Idle, listener.State, "durante la fuga nessuno è chiamato");

            yield return WaitFor(() => runner.State != EnemyState.Flee, 6f);
            yield return null;
            Assert.AreNotEqual(EnemyState.Idle, listener.State, "alla fine accorre");
        }
    }
}
