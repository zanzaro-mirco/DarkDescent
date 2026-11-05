using System.Linq;
using DarkDescent.Enemies;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    /// <summary>La ronda (24.3 della M24, D7): lo stato e il piano dei punti, con un corpo finto, senza scene.</summary>
    public class PatrolStateTests
    {
        private const float Frame = 0.02f;

        // un nemico di ronda senza scene: il test decide quando "arriva" a un punto e se vede il cavaliere
        private sealed class PatrollingBody : IPatrolBody
        {
            public bool Patroller { get; set; } = true;
            public bool Sees { get; set; }
            public bool AtPoint { get; set; }
            public float Pause { get; set; } = 2f;
            public int Walks { get; private set; }
            public int Holds { get; private set; }
            public int Ends { get; private set; }
            public bool Beyond { get; set; }
            public bool Home { get; set; } = true;
            public int GoHomes { get; private set; }

            public bool IsPatroller => Patroller;
            public float PatrolPause => Pause;
            public bool IsAtPatrolPoint => AtPoint;
            public void WalkToNextPatrolPoint() { Walks++; AtPoint = false; }
            public void HoldAtPatrolPoint() => Holds++;
            public void EndPatrol() => Ends++;

            public bool IsTargetAlive => true;
            public bool IsTargetInRange => false;
            public bool IsSwinging => false;
            public bool IsTelegraphedSwing => false;
            public bool CanSeeTarget() => Sees;
            public void Engage() { }
            public void Disengage() { }
            public bool IsBeyondLeash => Beyond;
            public bool IsHome => Home;
            public void GoHome() { GoHomes++; Home = false; }
            public void ArriveHome() { Beyond = false; }
        }

        private static EnemyBrain BrainFor(PatrollingBody body)
        {
            return new EnemyBrain(body, new EnemyStateBase[]
            {
                new IdleState(0.2f), new ChaseState(), new AttackState(), new ReturnState(), new PatrolState(0.2f), new DeadState(),
            });
        }

        private static void Run(EnemyBrain brain, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                brain.Tick(Frame);
            }
        }

        private static EnemyArchetype Archetype(string name)
        {
            return AssetDatabase.LoadAssetAtPath<EnemyArchetype>($"Assets/_Project/Data/Enemies/{name}.asset");
        }

        [Test, Description("Fermo, chi fa la ronda passa alla ronda; chi non la fa resta fermo")]
        public void Idle_GoesToPatrol_OnlyForAPatroller()
        {
            var brain = BrainFor(new PatrollingBody());
            brain.Tick(Frame);
            Assert.AreEqual(EnemyState.Patrol, brain.State);

            var still = BrainFor(new PatrollingBody { Patroller = false });
            Run(still, 1f);
            Assert.AreEqual(EnemyState.Idle, still.State);
        }

        [Test, Description("Sta fermo la pausa del primo punto, poi parte; arrivato si ferma la pausa del punto e riparte")]
        public void Patrol_PausesThenWalksThenPausesAgain()
        {
            var body = new PatrollingBody { Pause = 2f };
            var brain = BrainFor(body);
            brain.Tick(Frame);
            Assert.AreEqual(EnemyState.Patrol, brain.State);

            Run(brain, 1.8f);
            Assert.AreEqual(0, body.Walks, "ancora fermo a 1,8 s");
            Run(brain, 0.4f);
            Assert.AreEqual(1, body.Walks, "dopo 2 s parte");

            Run(brain, 1f);
            Assert.AreEqual(0, body.Holds, "cammina finché non arriva");
            body.AtPoint = true;
            body.Pause = 3f;
            brain.Tick(Frame);
            Assert.AreEqual(1, body.Holds, "arrivato si ferma");
            Run(brain, 2.8f);
            Assert.AreEqual(1, body.Walks, "la pausa è quella del punto: 3 s");
            Run(brain, 0.4f);
            Assert.AreEqual(2, body.Walks);
            Assert.AreEqual(EnemyState.Patrol, brain.State);
        }

        [Test, Description("Un punto che non si raggiunge (un compagno sulla strada) vale come raggiunto dopo 12 s")]
        public void AnUnreachablePoint_CountsAsReachedAfterTheLimit()
        {
            var body = new PatrollingBody { Pause = 0.1f };
            var brain = BrainFor(body);
            Run(brain, 0.3f);
            Assert.AreEqual(1, body.Walks);
            Run(brain, PatrolState.MaxWalk - 1f);
            Assert.AreEqual(0, body.Holds);
            Run(brain, 1.5f);
            Assert.AreEqual(1, body.Holds);
        }

        [Test, Description("Se vede il cavaliere insegue come gli altri, e la ronda finisce (la velocità torna)")]
        public void SeeingTheTarget_StartsTheChase_AndEndsThePatrol()
        {
            var body = new PatrollingBody();
            var brain = BrainFor(body);
            brain.Tick(Frame);
            body.Sees = true;
            Run(brain, 0.5f);
            Assert.AreEqual(EnemyState.Chase, brain.State);
            Assert.AreEqual(1, body.Ends, "EndPatrol una volta, all'uscita dallo stato");
        }

        [Test, Description("Un compagno che lo avvisa (Alert) lo fa inseguire anche di ronda")]
        public void AnAlert_WakesAPatroller()
        {
            var body = new PatrollingBody();
            var brain = BrainFor(body);
            brain.Tick(Frame);
            Assert.IsTrue(brain.Alert());
            Assert.AreEqual(EnemyState.Chase, brain.State);
            Assert.AreEqual(1, body.Ends);
        }

        [Test, Description("Oltre il limite torna a casa e, arrivato, torna alla ronda, non fermo")]
        public void AfterReturningHome_ItPatrolsAgain()
        {
            var body = new PatrollingBody();
            var brain = BrainFor(body);
            brain.Tick(Frame);
            body.Sees = true;
            Run(brain, 0.5f);
            Assert.AreEqual(EnemyState.Chase, brain.State);

            body.Sees = false;
            body.Beyond = true;
            brain.Tick(Frame);
            brain.Tick(Frame);
            Assert.AreEqual(EnemyState.Return, brain.State);
            body.Home = true;
            Run(brain, 0.1f);
            Assert.AreEqual(EnemyState.Patrol, brain.State, "da fermo (Idle) riprende la ronda");
        }

        [Test, Description("Morto di ronda: la morte vale da qualsiasi stato e la ronda finisce")]
        public void Death_EndsThePatrol()
        {
            var body = new PatrollingBody();
            var brain = BrainFor(body);
            brain.Tick(Frame);
            brain.Die();
            Assert.AreEqual(EnemyState.Dead, brain.State);
            Assert.AreEqual(1, body.Ends);
        }

        [Test, Description("Riposato (il cavaliere torna in vita): da fermo, e da fermo riprende la ronda")]
        public void Rest_ReturnsToIdle_ThenPatrol()
        {
            var body = new PatrollingBody();
            var brain = BrainFor(body);
            brain.Tick(Frame);
            Assert.IsTrue(brain.Rest());
            Assert.AreEqual(EnemyState.Idle, brain.State);
            Assert.AreEqual(1, body.Ends);
            brain.Tick(Frame);
            Assert.AreEqual(EnemyState.Patrol, brain.State);
        }

        // --- il piano dei punti ---

        [Test, Description("Stessa cella e stessa profondità, stessa ronda: nessun tiro del seme")]
        public void SameCellAndDepth_SamePlan()
        {
            for (int depth = 1; depth <= 8; depth++)
            {
                for (int x = -3; x <= 20; x += 5)
                {
                    var a = PatrolPlan.For(depth, x, -x * 2, 0.5f, 8f);
                    var b = PatrolPlan.For(depth, x, -x * 2, 0.5f, 8f);
                    Assert.AreEqual(a.Patrols, b.Patrols);
                    Assert.AreEqual(a.Count, b.Count);
                    for (int i = 0; i < a.Count; i++)
                    {
                        Assert.AreEqual(a.Offset(i), b.Offset(i));
                        Assert.AreEqual(a.Pause(i), b.Pause(i));
                    }
                }
            }
        }

        [Test, Description("Uno su quattro con la quota 0,25; due o tre punti; raggio tra 3 e 8 m; pausa tra 2 e 4 s")]
        public void ThePlanKeepsItsNumbers()
        {
            int patrols = 0;
            int total = 0;
            int twos = 0;
            int threes = 0;
            for (int depth = 1; depth <= 8; depth++)
            {
                for (int x = 0; x < 60; x++)
                {
                    for (int z = 0; z < 20; z++)
                    {
                        var plan = PatrolPlan.For(depth, x, z, 0.25f, 8f);
                        total++;
                        if (!plan.Patrols)
                        {
                            Assert.AreEqual(0, plan.Count);
                            continue;
                        }

                        patrols++;
                        Assert.That(plan.Count, Is.InRange(PatrolPlan.MinPoints, PatrolPlan.MaxPoints));
                        twos += plan.Count == 2 ? 1 : 0;
                        threes += plan.Count == 3 ? 1 : 0;
                        for (int i = 0; i < plan.Count; i++)
                        {
                            Assert.That(plan.Radius(i), Is.InRange(PatrolPlan.MinRadius, 8f));
                            Assert.That(plan.Offset(i).magnitude, Is.EqualTo(plan.Radius(i)).Within(1e-3f));
                            Assert.That(plan.Pause(i), Is.InRange(PatrolPlan.MinPause, PatrolPlan.MaxPause));
                        }
                    }
                }
            }

            Assert.That(patrols / (double)total, Is.EqualTo(0.25).Within(0.02), "uno su quattro");
            Assert.Greater(twos, patrols / 4, "i due punti sono comuni");
            Assert.Greater(threes, patrols / 4, "e anche i tre");
        }

        [Test, Description("Cambiare la quota non sposta i punti di chi già girava: i tiri sono presi sempre tutti, nello stesso ordine")]
        public void ChangingTheShare_DoesNotMoveThePoints()
        {
            for (int x = 0; x < 40; x++)
            {
                var small = PatrolPlan.For(3, x, 5, 0.25f, 8f);
                var wide = PatrolPlan.For(3, x, 5, 0.9f, 8f);
                if (small.Patrols)
                {
                    Assert.IsTrue(wide.Patrols);
                    Assert.AreEqual(small.Count, wide.Count);
                    for (int i = 0; i < small.Count; i++)
                    {
                        Assert.AreEqual(small.Offset(i), wide.Offset(i));
                    }
                }
            }
        }

        [Test, Description("Cella e profondità cambiano la ronda: la stessa cella a un'altra profondità è un altro nemico")]
        public void DifferentDepth_DifferentPlan()
        {
            int same = 0;
            for (int x = 0; x < 40; x++)
            {
                var a = PatrolPlan.For(2, x, 7, 1f, 8f);
                var b = PatrolPlan.For(5, x, 7, 1f, 8f);
                same += a.Offset(0) == b.Offset(0) ? 1 : 0;
            }

            Assert.Less(same, 3);
        }

        // --- i dati ---

        [Test, Description("Girano scheletro e bruto, uno su quattro, a 60% del passo e entro 8 m; non lo sciame né il mago (D7)")]
        public void OnlySkeletonAndBrute_CanPatrol()
        {
            foreach (string name in new[] { "Skeleton", "Brute" })
            {
                var archetype = Archetype(name);
                Assert.IsTrue(archetype.CanPatrol, name);
                Assert.AreEqual(0.25f, archetype.PatrolShare, 1e-6f, name);
                Assert.AreEqual(0.6f, archetype.PatrolSpeedFactor, 1e-6f, name);
                Assert.AreEqual(8f, archetype.PatrolRadius, 1e-6f, name);
                Assert.Less(archetype.PatrolRadius, archetype.LeashRange, $"{name}: la ronda sta dentro il limite dell'inseguimento");
            }

            Assert.IsFalse(Archetype("Swarm").CanPatrol, "lo sciame ha già il suo branco");
            Assert.IsFalse(Archetype("SkeletonMage").CanPatrol, "il mago ha il suo modo di stare fermo");
        }

        [Test, Description("Chi può girare ha lo stato Patrol tra i suoi, chi non può no; il bruto lo ha accanto al suo colpo caricato")]
        public void States_IncludePatrolOnlyWhenTheArchetypeCanPatrol()
        {
            foreach (string name in new[] { "Skeleton", "Brute" })
            {
                CollectionAssert.Contains(Archetype(name).CreateStates().Select(s => s.Id).ToList(), EnemyState.Patrol, name);
            }

            CollectionAssert.DoesNotContain(Archetype("Swarm").CreateStates().Select(s => s.Id).ToList(), EnemyState.Patrol);
            CollectionAssert.DoesNotContain(Archetype("SkeletonMage").CreateStates().Select(s => s.Id).ToList(), EnemyState.Patrol);

            var brute = Archetype("Brute").CreateStates().Select(s => s.Id).ToList();
            CollectionAssert.IsSupersetOf(brute, new[] { EnemyState.WindUp, EnemyState.Recover, EnemyState.Patrol });
        }
    }
}
