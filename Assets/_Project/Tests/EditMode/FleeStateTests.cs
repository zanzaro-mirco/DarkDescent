using System.Collections.Generic;
using System.Linq;
using DarkDescent.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>La fuga (24.2 della M24, D2–D6): lo stato e i dati, con un corpo finto, senza scene.</summary>
    public class FleeStateTests
    {
        private const float Frame = 0.02f;

        // un nemico senza scene che sa anche scappare: il test decide se vuole scappare e se c'è un posto dove andare
        private sealed class FleeingBody : IFleeBody
        {
            public bool TargetAlive { get; set; } = true;
            public bool InRange { get; set; }
            public bool Wants { get; set; }
            public bool HasPlace { get; set; } = true;
            public int Disengages { get; private set; }
            public int Engages { get; private set; }
            public int FleeStarts { get; private set; }
            public int FleeEnds { get; private set; }

            public bool IsTargetAlive => TargetAlive;
            public bool IsTargetInRange => InRange;
            public bool IsSwinging => false;
            public bool IsTelegraphedSwing => false;
            public bool CanSeeTarget() => true;
            public void Engage() => Engages++;
            public void Disengage() => Disengages++;
            public bool IsBeyondLeash => false;
            public bool IsHome => true;
            public void GoHome() { }
            public void ArriveHome() { }

            public bool WantsToFlee => Wants;
            public bool IsAtDestination => false;

            public bool BeginFlee()
            {
                if (!HasPlace)
                {
                    return false;
                }

                FleeStarts++;
                Wants = false; // una volta per vita: il corpo vero lo ricorda
                return true;
            }

            public void EndFlee() => FleeEnds++;
        }

        private static EnemyBrain BrainFor(FleeingBody body, float duration = 4f)
        {
            return new EnemyBrain(body, new EnemyStateBase[]
            {
                new IdleState(0.2f), new ChaseState(), new AttackState(), new FleeState(duration), new ReturnState(), new DeadState(),
            });
        }

        private static void Run(EnemyBrain brain, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                brain.Tick(Frame);
            }
        }

        private static EnemyBrain Chasing(FleeingBody body, float duration = 4f)
        {
            var brain = BrainFor(body, duration);
            Assert.IsTrue(brain.Alert());
            Assert.AreEqual(EnemyState.Chase, brain.State);
            return brain;
        }

        [Test, Description("Inseguendo, se il corpo vuole scappare passa alla fuga, lascia il bersaglio e parte")]
        public void Chase_GoesToFlee_WhenTheBodyWantsTo()
        {
            var body = new FleeingBody { Wants = true };
            var brain = Chasing(body);
            brain.Tick(Frame);
            Assert.AreEqual(EnemyState.Flee, brain.State);
            Assert.AreEqual(1, body.FleeStarts);
            Assert.GreaterOrEqual(body.Disengages, 1, "non colpisce più");
        }

        [Test, Description("Anche attaccando, dal colpo alla fuga, senza dare un altro colpo")]
        public void Attack_GoesToFlee_WhenTheBodyWantsTo()
        {
            var body = new FleeingBody { InRange = true };
            var brain = Chasing(body);
            brain.Tick(Frame);
            Assert.AreEqual(EnemyState.Attack, brain.State);
            int engages = body.Engages;
            body.Wants = true;
            brain.Tick(Frame);
            Assert.AreEqual(EnemyState.Flee, brain.State);
            Assert.AreEqual(engages, body.Engages, "il colpo non si chiede più");
        }

        [Test, Description("Un corpo che non sa scappare (lo scheletro mago, il bruto) non cambia: la fuga non c'è")]
        public void APlainBody_NeverFlees()
        {
            var brain = new EnemyBrain(new EnemyStateTests_PlainBody(), new EnemyStateBase[]
            {
                new IdleState(0.2f), new ChaseState(), new AttackState(), new ReturnState(), new DeadState(),
            });
            Assert.IsTrue(brain.Alert());
            brain.Tick(Frame);
            Assert.AreEqual(EnemyState.Chase, brain.State);
        }

        [Test, Description("La fuga dura i suoi secondi, poi chiama i compagni (EndFlee) e torna all'inseguimento")]
        public void Flee_LastsItsDuration_ThenResumesTheChase()
        {
            var body = new FleeingBody { Wants = true };
            var brain = Chasing(body, 4f);
            brain.Tick(Frame);
            Assert.AreEqual(EnemyState.Flee, brain.State);

            Run(brain, 3.9f);
            Assert.AreEqual(EnemyState.Flee, brain.State, "ancora in fuga a 3,9 s");
            Assert.AreEqual(0, body.FleeEnds);

            Run(brain, 0.3f);
            Assert.AreEqual(EnemyState.Chase, brain.State, "dopo 4 s torna a inseguire");
            Assert.AreEqual(1, body.FleeEnds);
        }

        [Test, Description("Una sola volta: finita la fuga non scatta di nuovo, anche con la vita ancora bassa")]
        public void Flee_HappensOnce()
        {
            var body = new FleeingBody { Wants = true };
            var brain = Chasing(body, 1f);
            Run(brain, 1.3f);
            Assert.AreEqual(EnemyState.Chase, brain.State);
            Run(brain, 2f);
            Assert.AreEqual(1, body.FleeStarts);
            Assert.AreNotEqual(EnemyState.Flee, brain.State);
        }

        [Test, Description("Senza un posto dove andare (un angolo) non è una fuga: torna a inseguire subito e non chiama nessuno")]
        public void NoPlaceToGo_IsNotAFlight()
        {
            var body = new FleeingBody { Wants = true, HasPlace = false };
            var brain = Chasing(body);
            brain.Tick(Frame);
            Assert.AreEqual(EnemyState.Flee, brain.State, "ci entra");
            brain.Tick(Frame);
            Assert.AreEqual(EnemyState.Chase, brain.State, "e ne esce al frame dopo");
            Assert.AreEqual(0, body.FleeEnds, "senza chiamare i compagni");
        }

        [Test, Description("Se il cavaliere muore mentre scappa, il nemico torna fermo, non a inseguire")]
        public void TargetDeadDuringFlee_GoesIdle()
        {
            var body = new FleeingBody { Wants = true };
            var brain = Chasing(body, 1f);
            brain.Tick(Frame);
            body.TargetAlive = false;
            Run(brain, 1.2f);
            Assert.AreEqual(EnemyState.Idle, brain.State);
        }

        [Test, Description("Morto mentre scappa: lo stato di morte vale da qualsiasi stato")]
        public void Death_WinsOverFlee()
        {
            var body = new FleeingBody { Wants = true };
            var brain = Chasing(body);
            brain.Tick(Frame);
            brain.Die();
            Assert.AreEqual(EnemyState.Dead, brain.State);
        }

        // un corpo che non sa scappare: il minimo dell'interfaccia di base
        private sealed class EnemyStateTests_PlainBody : IEnemyBody
        {
            public bool IsTargetAlive => true;
            public bool IsTargetInRange => false;
            public bool IsSwinging => false;
            public bool IsTelegraphedSwing => false;
            public bool CanSeeTarget() => true;
            public void Engage() { }
            public void Disengage() { }
            public bool IsBeyondLeash => false;
            public bool IsHome => true;
            public void GoHome() { }
            public void ArriveHome() { }
        }

        private static EnemyArchetype Archetype(string name)
        {
            return AssetDatabase.LoadAssetAtPath<EnemyArchetype>($"Assets/_Project/Data/Enemies/{name}.asset");
        }

        [Test, Description("Scappano lo scheletro (25%, 2 compagni) e lo sciame (30%, 2 compagni); non il bruto né il mago (D2)")]
        public void OnlySkeletonsAndTheSwarm_CanFlee()
        {
            Assert.IsTrue(Archetype("Skeleton").CanFlee);
            Assert.AreEqual(0.25f, Archetype("Skeleton").FleeBelow, 1e-6f);
            Assert.AreEqual(2, Archetype("Skeleton").FleeOnLosses);
            Assert.IsTrue(Archetype("Swarm").CanFlee);
            Assert.AreEqual(0.30f, Archetype("Swarm").FleeBelow, 1e-6f);
            Assert.IsFalse(Archetype("Brute").CanFlee, "il bruto ha il suo colpo caricato");
            Assert.IsFalse(Archetype("SkeletonMage").CanFlee, "il mago ha già la sua ritirata");
        }

        [Test, Description("I numeri di D4: quattro secondi a ×1,25, almeno 10 m dal cavaliere e al più 15 m da casa (sotto il limite dell'inseguimento)")]
        public void FleeNumbers_AreTheOnesApproved()
        {
            foreach (string name in new[] { "Skeleton", "Swarm" })
            {
                var archetype = Archetype(name);
                Assert.AreEqual(4f, archetype.FleeDuration, 1e-6f, name);
                Assert.AreEqual(1.25f, archetype.FleeSpeedFactor, 1e-6f, name);
                Assert.AreEqual(10f, archetype.FleeDistance, 1e-6f, name);
                Assert.AreEqual(15f, archetype.FleeHomeLimit, 1e-6f, name);
                Assert.Less(archetype.FleeHomeLimit, archetype.LeashRange, $"{name}: una fuga non deve portarlo oltre il limite, o tornerebbe a casa a curarsi");
            }

            Assert.AreEqual(10f, Archetype("Skeleton").FleeCallRadius, 1e-6f, "gli scheletri non hanno un branco: un raggio loro");
            Assert.AreEqual(Archetype("Swarm").PackRadius, Archetype("Swarm").FleeCallRadius, 1e-6f, "lo sciame chiama dal raggio del suo branco");
        }

        [Test, Description("Chi può scappare ha lo stato Flee tra i suoi, chi non può no")]
        public void States_IncludeFleeOnlyWhenTheArchetypeCanFlee()
        {
            var skeleton = Archetype("Skeleton").CreateStates().Select(s => s.Id).ToList();
            var brute = Archetype("Brute").CreateStates().Select(s => s.Id).ToList();
            var mage = Archetype("SkeletonMage").CreateStates().Select(s => s.Id).ToList();
            CollectionAssert.Contains(skeleton, EnemyState.Flee);
            CollectionAssert.DoesNotContain(brute, EnemyState.Flee);
            CollectionAssert.DoesNotContain(mage, EnemyState.Flee);
        }
    }
}
