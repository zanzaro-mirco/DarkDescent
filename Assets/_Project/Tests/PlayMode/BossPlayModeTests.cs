using System;
using System.Collections;
using System.Linq;
using DarkDescent.Audio;
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
using UnityEngine.UI;

namespace DarkDescent.Tests
{
    /// <summary>Lo scontro con il Re delle ossa alla profondità 8 (D8 della M10).</summary>
    public class BossPlayModeTests : SandboxFixture
    {
        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static CompositionRoot Root => UnityEngine.Object.FindFirstObjectByType<CompositionRoot>();

        private static EnemyBar Bar => UnityEngine.Object.FindFirstObjectByType<EnemyBar>();

        private Health PlayerHealth => Player.GetComponent<Health>();

        private static DamageInfo Damage(float amount) => new DamageInfo(amount, DamageType.Physical, null);

        private IEnumerator GoToDepth8()
        {
            yield return LoadGeneratedCore();
            Manager.LoadLevel("Level_Caves", "FromAbove", 8);
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel.Depth != 8; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, "la profondità 8 non arriva");
                yield return null;
            }

            yield return null;
            yield return null;
        }

        // il cavaliere resta in vita mentre il test aspetta: il re picchia forte
        private IEnumerator Wait(Func<bool> done, float timeout, string what)
        {
            for (float t = 0f; !done(); t += Time.deltaTime)
            {
                Assert.Less(t, timeout, what);
                if (!PlayerHealth.IsDead)
                {
                    PlayerHealth.SetCurrent(PlayerHealth.Max);
                }

                yield return null;
            }
        }

        private IEnumerator EnterTheRoom(BossController boss)
        {
            Assert.IsFalse(boss.IsFighting);
            Assert.IsFalse(Manager.CurrentLevel.BossGate.IsClosed, "prima dello scontro la porta è aperta");
            Vector3 home = boss.GetComponent<EnemyAI>().HomePosition;
            Assert.IsTrue(NavMesh.SamplePosition(home + Vector3.back * 5f, out var hit, 2f, NavMesh.AllAreas));
            Player.GetComponent<NavMeshAgent>().Warp(hit.position);
            yield return Wait(() => boss.IsFighting, 3f, "lo scontro non comincia");
        }

        [UnityTest, Description("Con la porta che si chiude entra la musica dello scontro al posto di quella della caverna, senza versi nel buio; alla vittoria la caverna torna")]
        public IEnumerator BossMusic_EntersAndFadesOut()
        {
            yield return GoToDepth8();
            var ambience = UnityEngine.Object.FindFirstObjectByType<AmbiencePlayer>();
            var caves = ambience.Current;
            var boss = Manager.CurrentLevel.Boss;
            yield return EnterTheRoom(boss);

            Assert.AreNotSame(caves, ambience.Current, "la musica dello scontro");
            Assert.IsNotNull(ambience.Current.Music, "con un brano");
            Assert.AreSame(ambience.Current.Music, ambience.Music.clip);
            Assert.AreEqual(0, ambience.Current.StingerCount, "niente versi durante lo scontro");

            boss.Health.TakeDamage(Damage(boss.Health.Current));
            Assert.AreSame(caves, ambience.Current, "vinto, torna la caverna");
            Assert.AreSame(caves.Music, ambience.Music.clip);
        }

        [UnityTest, Description("Entrando nella sala la porta si chiude e la vita del re resta in alto; le fasi: evoca lo sciame, poi in furia corre e telegrafa l'anello; morto, la porta si apre, la vittoria va nel salvataggio e lascia un oggetto raro e la pergamena del ritorno")]
        public IEnumerator Boss_FightsInThreePhases()
        {
            yield return GoToDepth8();
            var boss = Manager.CurrentLevel.Boss;
            Assert.IsNotNull(boss, "alla profondità 8 c'è il re");
            Assert.IsNotNull(Manager.CurrentLevel.BossGate);
            yield return EnterTheRoom(boss);

            var gate = Manager.CurrentLevel.BossGate;
            Assert.IsTrue(gate.IsClosed);
            Assert.AreSame(boss.Health, Bar.Pinned);
            Assert.AreEqual("The Bone King", Bar.NameText);
            yield return null;
            var path = new NavMeshPath();
            Vector3 entrance = Manager.CurrentLevel.GetEntrance("FromAbove").position;
            NavMesh.CalculatePath(Player.position, entrance, NavMesh.AllAreas, path);
            Assert.AreNotEqual(NavMeshPathStatus.PathComplete, path.status, "chiusa la porta, non si esce");

            boss.Health.TakeDamage(Damage(boss.Health.Max * 0.5f));
            yield return Wait(() => boss.Summons.Count > 0, 3f, "in seconda fase non evoca");
            Assert.AreEqual(BossPhase.Summon, boss.Phase);
            var summoned = boss.Summons[0];
            Assert.AreEqual("enemy.swarm", summoned.Archetype.NameKey);
            Assert.LessOrEqual(Vector3.Distance(summoned.transform.position, boss.transform.position), 6f, "nasce nella sala");

            float speed = boss.GetComponent<NavMeshAgent>().speed;
            boss.Health.TakeDamage(Damage(boss.Health.Max * 0.25f));
            yield return Wait(() => boss.IsCastingRing, 3f, "in furia non lancia l'anello");
            Assert.AreEqual(BossPhase.Fury, boss.Phase);
            Assert.Greater(boss.GetComponent<NavMeshAgent>().speed, speed, "in furia è più veloce");
            Assert.IsTrue(boss.GetComponent<TelegraphCircle>().IsShown, "il cerchio a terra annuncia l'anello");

            boss.Health.TakeDamage(Damage(boss.Health.Current));
            yield return null;
            Assert.IsFalse(gate.IsClosed, "morto il re, la porta si apre");
            Assert.IsTrue(Root.RunStats.BossDefeated);
            Assert.IsNull(Bar.Pinned);
            Assert.IsTrue(summoned == null || summoned.GetComponent<Health>().IsDead, "gli evocati cadono con lui");

            yield return new WaitForSeconds(1.5f);
            var drops = UnityEngine.Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None)
                .Where(g => g.Item != null && Vector3.Distance(g.transform.position, boss.transform.position) < 4f).ToList();
            Assert.IsTrue(drops.Any(g => g.Item.Rarity >= Rarity.Rare), "almeno un oggetto raro");
            Assert.IsTrue(drops.Any(g => g.Item.Definition is ReturnScrollDefinition), "la pergamena del ritorno");
        }

        [UnityTest, Description("Morto il cavaliere durante lo scontro, tornato in vita all'ingresso: la porta si riapre, gli evocati spariscono e il re ricomincia a vita piena")]
        public IEnumerator PlayerDeath_ResetsTheFight()
        {
            yield return GoToDepth8();
            var boss = Manager.CurrentLevel.Boss;
            yield return EnterTheRoom(boss);
            boss.Health.TakeDamage(Damage(boss.Health.Max * 0.5f));
            yield return Wait(() => boss.Summons.Count > 0, 3f, "in seconda fase non evoca");

            PlayerHealth.TakeDamage(Damage(100000f));
            var screen = UnityEngine.Object.FindFirstObjectByType<DeathScreen>();
            yield return Wait(() => screen.IsShown, 6f, "la schermata di morte non compare");
            GameObject.Find("RestartButton").GetComponent<Button>().onClick.Invoke();
            yield return Wait(() => !PlayerHealth.IsDead && !Manager.IsTransitioning, 5f, "il cavaliere non torna in vita");

            Assert.IsFalse(boss.IsFighting);
            Assert.IsFalse(Manager.CurrentLevel.BossGate.IsClosed);
            Assert.AreEqual(0, boss.Summons.Count);
            Assert.AreEqual(boss.Health.Max, boss.Health.Current, "a vita piena");
            Assert.AreEqual(BossPhase.Melee, boss.Phase);
            Assert.IsNull(Bar.Pinned);
            Assert.IsFalse(Root.RunStats.BossDefeated);
        }

        [UnityTest, Description("Sconfitto il re, la profondità 8 rigenerata non lo ha più, la porta resta aperta e il portale della vittoria c'è")]
        public IEnumerator DefeatedBoss_DoesNotComeBack()
        {
            yield return LoadGeneratedCore();
            Root.RunStats.MarkBossDefeated();
            Manager.LoadLevel("Level_Caves", "FromAbove", 8);
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel.Depth != 8; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, "la profondità 8 non arriva");
                yield return null;
            }

            yield return null;
            Assert.IsTrue(Manager.CurrentLevel.Boss == null, "il re non rinasce");
            Assert.IsFalse(Manager.CurrentLevel.BossGate.IsClosed);
            Assert.IsFalse(UnityEngine.Object.FindObjectsByType<BossController>(FindObjectsSortMode.None).Any());
            Assert.IsNotNull(UnityEngine.Object.FindFirstObjectByType<Town.TownPortals>().VictoryPortal, "al suo posto, il portale della vittoria");
        }
    }
}
