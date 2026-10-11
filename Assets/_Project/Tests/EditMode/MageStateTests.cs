using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class MageStateTests
    {
        private sealed class FakeRangedBody : IRangedBody
        {
            public bool TargetAlive { get; set; } = true;
            public bool Sees { get; set; } = true;
            public float Distance { get; set; } = 8f;
            public bool ClearShot { get; set; } = true;
            public bool Casting { get; set; }
            public bool Ready { get; set; } = true;
            public bool CanRetreat { get; set; } = true;
            public bool Arrived { get; set; }
            public int Casts { get; private set; }
            public int Approaches { get; private set; }
            public int Holds { get; private set; }
            public int Retreats { get; private set; }

            public bool IsTargetAlive => TargetAlive;
            public bool IsTargetInRange => false;
            public bool IsSwinging => false;
            public bool IsTelegraphedSwing => false;
            public bool IsBeyondLeash => false;
            public bool IsHome => true;
            public float TargetDistance => Distance;
            public bool IsCasting => Casting;
            public bool IsCastReady => Ready;
            public bool IsAtDestination => Arrived;

            public bool CanSeeTarget() => Sees;
            public void Engage() { }
            public void Disengage() { }
            public void GoHome() { }
            public void ArriveHome() { }
            public bool HasClearShot() => ClearShot;

            public void Cast()
            {
                Casts++;
                Ready = false;
            }

            public void Approach() => Approaches++;
            public void HoldPosition() => Holds++;

            public bool TryRetreat()
            {
                Retreats++;
                return CanRetreat;
            }
        }

        private static EnemyArchetype Mage => AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/_Project/Data/Enemies/SkeletonMage.asset");

        private FakeRangedBody _body;
        private EnemyBrain _brain;

        [SetUp]
        public void SetUp()
        {
            _body = new FakeRangedBody();
            _brain = new EnemyBrain(_body, Mage.CreateStates());
        }

        private void Tick(int frames = 1, float deltaTime = 0.1f)
        {
            for (int i = 0; i < frames; i++)
            {
                _brain.Tick(deltaTime);
            }
        }

        [Test, Description("L'archetipo della scheda (D6): lancia entro 10 m, scappa sotto i 4, cerca un punto a 7 m; esperienza 60")]
        public void Archetype_MatchesTheScheda()
        {
            Assert.IsTrue(Mage.IsRanged);
            Assert.AreEqual((10f, 4f, 7f, 60), (Mage.CastRange, Mage.RetreatBelow, Mage.RetreatDistance, Mage.Experience));
        }

        [Test, Description("Visto il cavaliere a 8 m con il tiro libero, il mago passa al combattimento a distanza, si ferma e lancia; durante il gesto resta fermo e non lancia di nuovo")]
        public void SeesTarget_HoldsAndCasts()
        {
            Tick();
            Assert.AreEqual(EnemyState.Ranged, _brain.State);
            Tick();
            Assert.AreEqual(1, _body.Casts);
            Assert.AreEqual(0, _body.Approaches);

            _body.Casting = true;
            _body.Ready = true;
            Tick(5);
            Assert.AreEqual(1, _body.Casts, "il gesto in corso si finisce");
            Assert.AreEqual(EnemyState.Ranged, _brain.State);
        }

        [Test, Description("Oltre i 10 m, o con un muro in mezzo, si avvicina invece di lanciare")]
        public void TooFarOrBlocked_Approaches()
        {
            _body.Distance = 12f;
            Tick(3);
            Assert.AreEqual(0, _body.Casts);
            Assert.Greater(_body.Approaches, 0);

            _body.Distance = 6f;
            _body.ClearShot = false;
            Tick(5);
            Assert.AreEqual(0, _body.Casts, "con il muro in mezzo non lancia");

            _body.ClearShot = true;
            Tick(5);
            Assert.AreEqual(1, _body.Casts, "il tiro libero si ricontrolla dopo un poco");
        }

        [Test, Description("Con il cavaliere sotto i 4 m, passata la pausa, si allontana; arrivato torna a lanciare")]
        public void TooClose_RetreatsAfterThePause()
        {
            _body.Distance = 3f;
            _body.Ready = false;
            Tick();
            Assert.AreEqual(EnemyState.Ranged, _brain.State);
            Tick(10);
            Assert.AreEqual(EnemyState.Ranged, _brain.State, "nel primo secondo e mezzo resta");
            Tick(6);
            Assert.AreEqual(EnemyState.Retreat, _brain.State);
            Assert.AreEqual(1, _body.Retreats);

            _body.Arrived = true;
            Tick();
            Assert.AreEqual(EnemyState.Ranged, _brain.State);
        }

        [Test, Description("In un angolo, senza un punto dove scappare, torna subito a lanciare da dov'è e non riprova a ogni frame")]
        public void Cornered_StaysAndCasts()
        {
            _body.Distance = 3f;
            _body.CanRetreat = false;
            _body.Ready = false;
            Tick(17);
            Assert.AreEqual(1, _body.Retreats);
            Tick();
            Assert.AreEqual(EnemyState.Ranged, _brain.State);
            _body.Ready = true;
            Tick(3);
            Assert.AreEqual(1, _body.Casts, "lancia da dov'è");
            Assert.AreEqual(1, _body.Retreats, "la pausa evita un tentativo a ogni frame");
        }

        [Test, Description("Un compagno lo avvisa: da fermo passa al combattimento a distanza, non all'inseguimento")]
        public void Alert_GoesRanged()
        {
            _body.Sees = false;
            Tick();
            Assert.AreEqual(EnemyState.Idle, _brain.State);
            Assert.IsTrue(_brain.Alert());
            Assert.AreEqual(EnemyState.Ranged, _brain.State);
        }

        private static int Mages(DungeonLayout layout)
        {
            int count = 0;
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    count += layout[x, y] == DungeonPopulator.MageSymbol ? 1 : 0;
                }
            }

            return count;
        }

        [Test, Description("Nella cripta niente maghi alle profondità 1 e 2, da 1 a 2 dalla 3; nelle caverne secondo la tabella")]
        public void Spawn_FollowsTheDepth()
        {
            var crypt = AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");
            var caves = AssetDatabase.LoadAssetAtPath<CaveSettings>("Assets/_Project/Data/Levels/CaveSettings.asset");
            for (ulong seed = 1; seed <= 20; seed++)
            {
                for (int depth = 1; depth <= 4; depth++)
                {
                    int mages = Mages(crypt.CreateGenerator().Generate(SeedMixer.ForLevel(seed, depth), depth));
                    if (depth < 3)
                    {
                        Assert.AreEqual(0, mages, $"cripta {depth}, seme {seed}");
                    }
                    else
                    {
                        Assert.That(mages, Is.InRange(1, 2), $"cripta {depth}, seme {seed}");
                    }
                }

                for (int depth = 5; depth <= 8; depth++)
                {
                    var row = caves.SpawnTable.For(depth);
                    int mages = Mages(caves.CreateGenerator().Generate(SeedMixer.ForLevel(seed, depth), depth));
                    Assert.That(mages, Is.InRange(row.Mages.x, row.Mages.y), $"caverne {depth}, seme {seed}");
                }
            }
        }
    }
}
