using System;
using System.Collections.Generic;
using DarkDescent.Enemies;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DarkDescent.Tests
{
    public class EnemyStateTests
    {
        private const float Frame = 0.02f;

        // Un nemico senza scene: il test decide cosa vede e cosa tocca, e conta le richieste.
        private sealed class FakeBody : IEnemyBody
        {
            public bool TargetAlive { get; set; } = true;
            public bool Sees { get; set; }
            public bool InRange { get; set; }
            public bool Swinging { get; set; }
            public bool Quick { get; set; }
            public int Looks { get; private set; }
            public int Engages { get; private set; }
            public int Disengages { get; private set; }

            public bool IsTargetAlive => TargetAlive;

            public bool IsTargetInRange => InRange;

            public bool IsSwinging => Swinging;

            public bool IsTelegraphedSwing => Swinging && !Quick;

            public bool CanSeeTarget()
            {
                Looks++;
                return Sees;
            }

            public void Engage()
            {
                Engages++;
            }

            public void Disengage()
            {
                Disengages++;
            }

            public bool BeyondLeash { get; set; }
            public bool Home { get; set; }
            public int HomeTrips { get; private set; }
            public int Arrivals { get; private set; }

            public bool IsBeyondLeash => BeyondLeash;

            public bool IsHome => Home;

            public void GoHome()
            {
                HomeTrips++;
            }

            public void ArriveHome()
            {
                Arrivals++;
            }
        }

        private EnemyArchetype _archetype;
        private FakeBody _body;
        private EnemyBrain _brain;
        private List<(EnemyState from, EnemyState to)> _changes;

        [SetUp]
        public void SetUp()
        {
            _archetype = ScriptableObject.CreateInstance<EnemyArchetype>();
            _body = new FakeBody();
            _brain = new EnemyBrain(_body, _archetype.CreateStates());
            _changes = new List<(EnemyState, EnemyState)>();
            _brain.StateChanged += (from, to) => _changes.Add((from, to));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_archetype);
        }

        private void Run(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                _brain.Tick(Frame);
            }
        }

        [Test, Description("Fermo guarda subito, poi solo ogni intervallo di percezione: un raggio ogni 0,2 s, non a ogni frame")]
        public void Idle_LooksOncePerInterval()
        {
            Run(1);
            Assert.AreEqual(1, _body.Looks, "il primo sguardo è subito");
            Run(9);
            Assert.AreEqual(1, _body.Looks, "nei 0,2 s dopo non guarda");
            Run(2);
            Assert.AreEqual(2, _body.Looks, "allo scadere guarda di nuovo");
            Assert.AreEqual(EnemyState.Idle, _brain.State);
            Assert.AreEqual(0, _body.Engages);
        }

        [Test, Description("Vedendo il bersaglio insegue; a portata attacca; fuori portata e senza colpo in volo torna a inseguire")]
        public void SeeChaseAttackChase()
        {
            _body.Sees = true;
            Run(1);
            Assert.AreEqual(EnemyState.Chase, _brain.State);
            Assert.AreEqual(0, _body.Engages, "il cambio vale dal frame dopo, come nello switch");

            Run(1);
            Assert.AreEqual(EnemyState.Chase, _brain.State);
            Assert.AreEqual(1, _body.Engages, "inseguendo chiede il colpo, e il corpo si avvicina");

            _body.InRange = true;
            Run(1);
            Assert.AreEqual(EnemyState.Attack, _brain.State);
            Run(3);
            Assert.AreEqual(5, _body.Engages, "attaccando lo chiede a ogni frame");

            _body.InRange = false;
            _body.Swinging = true;
            Run(1);
            Assert.AreEqual(EnemyState.Attack, _brain.State, "un colpo in volo si finisce");
            _body.Swinging = false;
            Run(1);
            Assert.AreEqual(EnemyState.Chase, _brain.State);
            CollectionAssert.AreEqual(new[] { (EnemyState.Idle, EnemyState.Chase), (EnemyState.Chase, EnemyState.Attack), (EnemyState.Attack, EnemyState.Chase) }, _changes);
        }

        [Test, Description("Inseguendo troppo lontano da casa lascia il bersaglio e ci torna senza guardarsi attorno; arrivato guarisce e torna fermo, e può rivedere il cavaliere")]
        public void Chase_BeyondLeash_GoesHomeAndHeals()
        {
            _body.Sees = true;
            Run(2);
            Assert.AreEqual(EnemyState.Chase, _brain.State);
            int engages = _body.Engages;

            _body.BeyondLeash = true;
            Run(1);
            Assert.AreEqual(EnemyState.Return, _brain.State);
            Assert.AreEqual(1, _body.HomeTrips, "si avvia verso casa");
            Assert.AreEqual(1, _body.Disengages, "lascia il bersaglio");

            _body.BeyondLeash = false;
            int looks = _body.Looks;
            Run(20);
            Assert.AreEqual(EnemyState.Return, _brain.State, "finché non è a casa non torna a inseguire, anche vedendo il cavaliere");
            Assert.AreEqual(engages, _body.Engages, "nessun colpo chiesto tornando");
            Assert.AreEqual(looks, _body.Looks, "non si guarda attorno");
            Assert.AreEqual(0, _body.Arrivals);

            _body.Home = true;
            Run(1);
            Assert.AreEqual(EnemyState.Idle, _brain.State);
            Assert.AreEqual(1, _body.Arrivals, "arrivato, guarisce");

            Run(11);
            Assert.AreEqual(EnemyState.Chase, _brain.State, "da fermo, al prossimo sguardo, può vederlo di nuovo");
        }

        [Test, Description("Chi sta già colpendo non guarda la distanza da casa: il duello si finisce")]
        public void Attack_IgnoresLeash()
        {
            _body.Sees = true;
            _body.InRange = true;
            Run(2);
            Assert.AreEqual(EnemyState.Attack, _brain.State);

            _body.BeyondLeash = true;
            Run(5);
            Assert.AreEqual(EnemyState.Attack, _brain.State);
            Assert.AreEqual(0, _body.HomeTrips);
        }

        [Test, Description("Tornando a casa non risponde all'avviso di un compagno")]
        public void Returning_IgnoresAlert()
        {
            _body.Sees = true;
            Run(2);
            _body.BeyondLeash = true;
            Run(1);
            Assert.IsFalse(_brain.Alert());
            Assert.AreEqual(EnemyState.Return, _brain.State);
        }

        [Test, Description("Un colpo già partito porta dall'inseguimento all'attacco anche fuori portata")]
        public void Chase_SwingingMeansAttack()
        {
            _body.Sees = true;
            Run(1);
            _body.Swinging = true;
            Run(1);
            Assert.AreEqual(EnemyState.Attack, _brain.State);
        }

        [Test, Description("Con il bersaglio morto, sia inseguendo sia attaccando, lascia il bersaglio e torna fermo")]
        public void DeadTarget_BackToIdle([Values(false, true)] bool attacking)
        {
            _body.Sees = true;
            _body.InRange = attacking;
            Run(attacking ? 2 : 1);
            Assert.AreEqual(attacking ? EnemyState.Attack : EnemyState.Chase, _brain.State);

            _body.TargetAlive = false;
            Run(1);
            Assert.AreEqual(EnemyState.Idle, _brain.State);
            Assert.AreEqual(1, _body.Disengages);

            // morto il bersaglio non si guarda: lo sguardo costa, e non c'è niente da vedere
            int looks = _body.Looks;
            Run(50);
            Assert.AreEqual(looks, _body.Looks);
        }

        [Test, Description("La morte vale da qualsiasi stato e subito; da morto non decide più niente")]
        public void Die_FromAnyState()
        {
            _body.Sees = true;
            _body.InRange = true;
            Run(2);
            Assert.AreEqual(EnemyState.Attack, _brain.State);

            _brain.Die();
            Assert.AreEqual(EnemyState.Dead, _brain.State);
            Assert.AreEqual(1, _body.Disengages, "da morto lascia il bersaglio");
            int engages = _body.Engages;
            Run(20);
            _brain.Die();
            Assert.AreEqual(EnemyState.Dead, _brain.State);
            Assert.AreEqual(engages, _body.Engages);
            Assert.AreEqual(1, _changes.FindAll(c => c.to == EnemyState.Dead).Count, "morto una volta sola");
        }

        [Test, Description("Avvisato da un compagno, chi è fermo insegue senza aver visto; chi insegue, è morto o ha il bersaglio morto non cambia")]
        public void Alert_WakesOnlyTheIdle()
        {
            Assert.IsTrue(_brain.Alert());
            Assert.AreEqual(EnemyState.Chase, _brain.State);
            Assert.AreEqual(0, _body.Looks, "non ha guardato");
            Assert.IsFalse(_brain.Alert(), "inseguendo non cambia niente");

            var other = new FakeBody { TargetAlive = false };
            var otherBrain = new EnemyBrain(other, _archetype.CreateStates());
            Assert.IsFalse(otherBrain.Alert(), "bersaglio già morto");
            Assert.AreEqual(EnemyState.Idle, otherBrain.State);

            other.TargetAlive = true;
            otherBrain.Die();
            Assert.IsFalse(otherBrain.Alert(), "da morto");
            Assert.AreEqual(EnemyState.Dead, otherBrain.State);
        }

        [Test, Description("Il bruto: partito il colpo carica finché il colpo non finisce, poi resta fermo senza chiedere colpi per il tempo di recupero, poi torna a inseguire")]
        public void Brute_WindsUpThenRecovers()
        {
            var brute = ScriptableObject.CreateInstance<EnemyArchetype>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"_recoverTime\":0.6}", brute);
                Assert.IsTrue(brute.IsTelegraphed);
                var body = new FakeBody { Sees = true, InRange = true };
                var brain = new EnemyBrain(body, brute.CreateStates());
                Run(brain, 2);
                Assert.AreEqual(EnemyState.Attack, brain.State);

                body.Swinging = true;
                Run(brain, 1);
                Assert.AreEqual(EnemyState.WindUp, brain.State, "partito il colpo, carica");
                int engages = body.Engages;
                Run(brain, 40);
                Assert.AreEqual(EnemyState.WindUp, brain.State, "carica finché il colpo è in volo");
                Assert.AreEqual(engages, body.Engages, "caricando non chiede altri colpi");

                body.Swinging = false;
                Run(brain, 1);
                Assert.AreEqual(EnemyState.Recover, brain.State);
                Assert.AreEqual(1, body.Disengages, "fermo: lascia il bersaglio");
                Run(brain, 29);
                Assert.AreEqual(EnemyState.Recover, brain.State, "0,58 s dopo è ancora fermo");
                Assert.AreEqual(engages, body.Engages);
                Run(brain, 2);
                Assert.AreEqual(EnemyState.Chase, brain.State, "passati 0,6 s riparte");
            }
            finally
            {
                Object.DestroyImmediate(brute);
            }
        }

        [Test, Description("Un colpo del bruto annullato (il cavaliere lo interrompe) porta comunque al recupero; con il bersaglio morto dopo il recupero si ferma")]
        public void Brute_InterruptedStillRecovers()
        {
            var brute = ScriptableObject.CreateInstance<EnemyArchetype>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"_recoverTime\":0.2}", brute);
                var body = new FakeBody { Sees = true, InRange = true };
                var brain = new EnemyBrain(body, brute.CreateStates());
                Run(brain, 2);
                body.Swinging = true;
                Run(brain, 2);
                Assert.AreEqual(EnemyState.WindUp, brain.State);
                body.Swinging = false;
                body.TargetAlive = false;
                Run(brain, 1);
                Assert.AreEqual(EnemyState.Recover, brain.State);
                Run(brain, 20);
                Assert.AreEqual(EnemyState.Idle, brain.State);
            }
            finally
            {
                Object.DestroyImmediate(brute);
            }
        }

        [Test, Description("I colpi leggeri del bruto non si caricano: restano un attacco, senza recupero; quello forte dopo carica")]
        public void Brute_QuickSwingsStayInAttack()
        {
            var brute = ScriptableObject.CreateInstance<EnemyArchetype>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"_recoverTime\":0.6}", brute);
                var body = new FakeBody { Sees = true, InRange = true, Swinging = true, Quick = true };
                var brain = new EnemyBrain(body, brute.CreateStates());
                Run(brain, 20);
                Assert.AreEqual(EnemyState.Attack, brain.State, "il colpo leggero non carica");

                body.Swinging = false;
                Run(brain, 1);
                Assert.AreEqual(EnemyState.Attack, brain.State, "finito, niente recupero");

                body.Swinging = true;
                body.Quick = false;
                Run(brain, 1);
                Assert.AreEqual(EnemyState.WindUp, brain.State, "il colpo forte sì");
            }
            finally
            {
                Object.DestroyImmediate(brute);
            }
        }

        [Test, Description("Lo scheletro non carica: un colpo in volo resta un attacco")]
        public void Skeleton_NeverWindsUp()
        {
            Assert.IsFalse(_archetype.IsTelegraphed);
            _body.Sees = true;
            _body.InRange = true;
            _body.Swinging = true;
            Run(10);
            Assert.AreEqual(EnemyState.Attack, _brain.State);
        }

        private static void Run(EnemyBrain brain, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                brain.Tick(Frame);
            }
        }

        [Test, Description("Ogni nemico ha i suoi stati: il conto della percezione di uno non sposta quello dell'altro; senza Idle o Dead il cervello non parte")]
        public void States_ArePerEnemy()
        {
            var other = new FakeBody();
            var otherBrain = new EnemyBrain(other, _archetype.CreateStates());
            Run(1);
            otherBrain.Tick(Frame);
            Assert.AreEqual(1, other.Looks, "il secondo guarda subito anche lui");

            Assert.Throws<ArgumentException>(() => new EnemyBrain(new FakeBody(), new EnemyStateBase[] { new ChaseState() }));
        }
    }
}
