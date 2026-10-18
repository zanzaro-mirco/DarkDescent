using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Player;
using DarkDescent.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class SpecialEnemyCombatTests : SandboxFixture
    {
        private static ChampionSettings Settings => AssetDatabase.LoadAssetAtPath<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset");

        private static DamageInfo Damage(float amount, GameObject source = null)
        {
            return new DamageInfo(amount, DamageType.Physical, source);
        }

        // lo scheletro della sandbox, fatto campione con questi modificatori e legato come fa il composition root
        private SpecialEnemy MakeChampion(params string[] modifiers)
        {
            var skeleton = Object.FindFirstObjectByType<EnemyAI>();
            var special = skeleton.gameObject.AddComponent<SpecialEnemy>();
            special.Promote(EnemyRank.Champion, modifiers);
            special.Bind(Settings, Player.GetComponent<Health>(), Player.GetComponent<MovementSlow>());
            return special;
        }

        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
        }

        [UnityTest, Description("Un campione ha il triplo della vita, a vita piena, colpisce più forte, è più grande, ha l'anello blu sotto i piedi e dà quattro volte l'esperienza; morto, l'anello si spegne")]
        public IEnumerator Champion_IsStrongerAndShowsIt()
        {
            yield return LoadSandbox();
            var skeleton = Object.FindFirstObjectByType<EnemyAI>();
            var health = skeleton.GetComponent<Health>();
            float life = health.Max;
            Vector3 scale = skeleton.transform.localScale;
            var progress = Player.GetComponent<PlayerProgress>();
            int depth = Object.FindFirstObjectByType<Levels.LevelManager>().CurrentLevel.Depth;

            var special = MakeChampion();
            Assert.AreEqual(life * 3f, health.Max, 0.01f);
            Assert.AreEqual(health.Max, health.Current, 0.01f, "a vita piena");
            Assert.AreEqual(1.5f, skeleton.GetComponent<MeleeAttack>().DamageMultiplier, 0.001f);
            Assert.AreEqual(scale.x * 1.15f, skeleton.transform.localScale.x, 0.001f);
            Assert.IsNotNull(special.Ring);
            Assert.IsTrue(special.Ring.activeInHierarchy);

            special.Bind(Settings, Player.GetComponent<Health>(), Player.GetComponent<MovementSlow>());
            Assert.AreEqual(life * 3f, health.Max, 0.01f, "legato due volte, i numeri non si moltiplicano due volte");

            int expected = progress.Settings.ExperienceForKill(skeleton.Archetype.Experience * 4, depth, 1);
            health.TakeDamage(Damage(10000f));
            yield return null;
            // quattro volte uno scheletro bastano a salire al livello 2: il resto è l'esperienza del livello nuovo
            int toNext = progress.Settings.ExperienceToNext(1);
            Assert.AreEqual(expected >= toNext ? 2 : 1, progress.Progress.Level);
            Assert.AreEqual(expected >= toNext ? expected - toNext : expected, progress.Progress.Experience);
            Assert.IsFalse(special.Ring.activeSelf, "l'anello si spegne con lui");
        }

        [UnityTest, Description("Rapido: si muove e colpisce il 30% più in fretta")]
        public IEnumerator Fast_MovesAndStrikesQuicker()
        {
            yield return LoadSandbox();
            var skeleton = Object.FindFirstObjectByType<EnemyAI>();
            float speed = skeleton.GetComponent<NavMeshAgent>().speed;
            var attack = skeleton.GetComponent<MeleeAttack>();
            float delay = attack.SwingHitDelay;

            MakeChampion("fast");
            Assert.AreEqual(speed * 1.3f, skeleton.GetComponent<NavMeshAgent>().speed, 0.001f);
            Assert.AreEqual(delay / 1.3f, attack.SwingHitDelay, 0.001f);
        }

        [UnityTest, Description("Gelido: un suo colpo rallenta il cavaliere del 40% per 2 secondi, un secondo colpo rinnova senza sommare, poi la velocità torna; i colpi di altri no")]
        public IEnumerator Frozen_SlowsTheKnight()
        {
            yield return LoadSandbox();
            var special = MakeChampion("frozen");
            // fermo: i colpi li decide il test, non lo scheletro che si avvicina
            special.GetComponent<EnemyAI>().enabled = false;
            special.GetComponent<MeleeAttack>().enabled = false;
            var playerHealth = Player.GetComponent<Health>();
            var slow = Player.GetComponent<MovementSlow>();
            float speed = PlayerAgent.speed;

            playerHealth.TakeDamage(Damage(1f));
            Assert.IsFalse(slow.IsSlowed, "un colpo che non è suo");

            playerHealth.TakeDamage(Damage(1f, special.gameObject));
            Assert.IsTrue(slow.IsSlowed);
            Assert.AreEqual(speed * 0.6f, PlayerAgent.speed, 0.001f);
            playerHealth.TakeDamage(Damage(1f, special.gameObject));
            Assert.AreEqual(speed * 0.6f, PlayerAgent.speed, 0.001f, "non si somma");

            Time.timeScale = 4f;
            yield return new WaitForSeconds(2.2f);
            Assert.IsFalse(slow.IsSlowed);
            Assert.AreEqual(speed, PlayerAgent.speed, 0.001f);
        }

        [UnityTest, Description("Rigenerante: dopo 3 secondi senza colpi riprende il 2% della vita al secondo")]
        public IEnumerator Regenerating_HealsAfterAPause()
        {
            yield return LoadSandbox();
            var special = MakeChampion("regenerating");
            var health = special.GetComponent<Health>();
            health.TakeDamage(Damage(health.Max * 0.5f));
            float wounded = health.Current;

            Time.timeScale = 4f;
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(wounded, health.Current, 0.01f, "ferma subito dopo il colpo");
            yield return new WaitForSeconds(2f);
            Assert.Greater(health.Current, wounded);
            Assert.LessOrEqual(health.Current, wounded + health.Max * 0.02f * 3f);
        }

        [UnityTest, Description("Inamovibile: i colpi non lo fanno barcollare e lo scudo del cavaliere non ne ferma i colpi")]
        public IEnumerator Unstoppable_IgnoresStaggerAndBlock()
        {
            yield return LoadSandbox();
            var special = MakeChampion("unstoppable");
            Assert.IsFalse(special.GetComponent<HitRecovery>().enabled);
            Assert.IsTrue(special.GetComponent<MeleeAttack>().IsUnblockable);
        }

        [UnityTest, Description("Furioso: sotto metà della vita il danno sale di metà, una volta sola")]
        public IEnumerator Enraged_HitsHarderWhenWounded()
        {
            yield return LoadSandbox();
            var special = MakeChampion("enraged");
            var health = special.GetComponent<Health>();
            var attack = special.GetComponent<MeleeAttack>();

            health.TakeDamage(Damage(health.Max * 0.3f));
            Assert.IsFalse(special.IsEnraged);
            Assert.AreEqual(1.5f, attack.DamageMultiplier, 0.001f);

            health.TakeDamage(Damage(health.Max * 0.3f));
            Assert.IsTrue(special.IsEnraged);
            Assert.AreEqual(1.5f * 1.5f, attack.DamageMultiplier, 0.001f);
            health.TakeDamage(Damage(1f));
            Assert.AreEqual(1.5f * 1.5f, attack.DamageMultiplier, 0.001f, "non sale di nuovo");
        }
    }
}
